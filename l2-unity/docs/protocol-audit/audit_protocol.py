"""Inventory the actual server writers, request routes and Unity bindings.

Static presence is not proof of layout correctness or runtime behavior.
"""
import csv
import json
import re
import hashlib
from collections import Counter
from pathlib import Path

SERVER = Path(r"D:\Documentos\Servers\L2J4Team_Unity")
CLIENT = Path(__file__).resolve().parents[2]
NET = CLIENT / "Assets/Scripts/Networking"
OUT = Path(__file__).resolve().parent


def source(path):
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    # Preserve strings (URLs and braces included) while removing comments.
    return re.sub(r'@?"(?:""|\\.|[^"\\])*"|\x27(?:\\.|[^\x27\\])*\x27|/\*.*?\*/|//[^\n]*',
                  lambda match: "" if match[0].startswith(("//", "/*")) else match[0], text, flags=re.S)


def body(text, start):
    # Ignore braces inside literals when locating a method's end.
    masked = re.sub(r'@?"(?:""|\\.|[^"\\])*"|\x27(?:\\.|[^\x27\\])*\x27',
                    lambda match: " " * len(match[0]), text)
    depth = 1
    index = start + 1
    while index < len(text) and depth:
        if masked[index] == "{": depth += 1
        elif masked[index] == "}": depth -= 1
        index += 1
    return text[start + 1:index - 1]


def enums():
    values = {}
    for path in (NET / "ClientLibrary/Packet/Opcode").rglob("*.cs"):
        text = source(path)
        name = re.search(r"enum\s+(\w+)", text)
        if not name: continue
        for field, number in re.findall(r"\b(\w+)\s*=\s*(0[xX][0-9a-fA-F]+|\d+)", text):
            values[name[1] + "." + field] = int(number, 0)
    return values


def bindings(values):
    found = {}
    dto_paths = {p.stem: p for p in (NET / "ClientLibrary/Packet/Dto/Incoming").rglob("*.cs")}
    for path in (NET / "ClientLibrary/Packet/Incoming").rglob("*.cs"):
        text = source(path)
        attr = re.search(r"\[Incoming(Game|Login|Ex)Packet\((\w+\.\w+)\)\]", text)
        model = re.search(r"Incoming(?:Wire)?Packet<([\w]+)>", text)
        if not attr or not model: continue
        if attr[2] not in values: continue
        channel = {"Game": "game", "Login": "login", "Ex": "game-ex"}[attr[1]]
        apply_match = re.search(r"\bvoid\s+Apply\s*\([^)]*\)\s*\{", text)
        apply_body = body(text, apply_match.end() - 1).strip() if apply_match else ""
        dto_path = dto_paths.get(model[1])
        dto_text = source(dto_path) if dto_path else ""
        if "IncomingWirePacket<" not in text:
            read_match = re.search(r"\bRead\s*\([^)]*\)\s*\{", text)
            dto_text = body(text, read_match.end() - 1) if read_match else ""
        record = {"binding": path.stem, "dto": model[1],
                  "client_source": str(path.relative_to(CLIENT)),
                  "dto_source": str(dto_path.relative_to(CLIENT)) if dto_path else "",
                  "reads": " ".join(re.findall(r"\breader\.Read([A-Za-z0-9_]+)\s*\(", dto_text)),
                  "apply": "empty" if not apply_body else "present-review-required"}
        found.setdefault((channel, values[attr[2]]), []).append(record)
    return found


def writers(found):
    rows = []
    roots = [("game", SERVER / "java/com/l2j4team/gameserver/network/serverpackets"),
             ("login", SERVER / "java/com/l2j4team/loginserver/serverpackets")]
    for default_channel, root in roots:
        for path in sorted(root.glob("*.java")):
            text = source(path)
            method = re.search(r"\bvoid\s+writeImpl\s*\([^)]*\)\s*\{", text)
            if not method: continue
            wire = body(text, method.end() - 1)
            first = re.search(r"\bwriteC\(\s*(0[xX][\da-fA-F]+|\d+)\s*\)", wire)
            channel = default_channel
            number = int(first[1], 0) if first else None
            if default_channel == "game" and number == 0xFE:
                ext = re.search(r"\bwriteH\(\s*(0[xX][\da-fA-F]+|\d+)\s*\)", wire[first.end():])
                channel = "game-ex"
                number = int(ext[1], 0) if ext else None
            candidates = found.get((channel, number), []) if number is not None else []
            status = "unresolved-server-opcode" if number is None else "missing-client-binding"
            if path.stem == "AdminForgePacket":
                status = "custom-dynamic-admin-packet"
            if candidates:
                status = "duplicate-client-binding" if len(candidates) > 1 else ("client-apply-empty" if candidates[0]["apply"] == "empty" else "present-layout-and-runtime-unverified")
            row = {"channel": channel, "opcode": "" if number is None else f"0x{number:04X}" if channel == "game-ex" else f"0x{number:02X}",
                   "server_packet": path.stem, "status": status,
                   "server_source": str(path.relative_to(SERVER)),
                   "server_writes": " ".join(re.findall(r"\bwrite([CDHFQS])\s*\(", wire)),
                   "client_binding": " | ".join(c["binding"] for c in candidates),
                   "client_dto": " | ".join(c["dto"] for c in candidates),
                   "client_reads": " | ".join(c["reads"] for c in candidates),
                   "client_apply": " | ".join(c["apply"] for c in candidates),
                   "client_source": " | ".join(c["client_source"] for c in candidates),
                   "dto_source": " | ".join(c["dto_source"] for c in candidates)}
            row["server_sha256"] = hashlib.sha256(path.read_bytes()).hexdigest()
            row["on_parsed_review"] = " | ".join(
                "present" if re.search(r"\bOnParsed\s*\(", source(CLIENT / c["client_source"])) else "absent"
                for c in candidates)
            rows.append(row)
            # This fork defines three concrete writers inside AbstractNpcInfo.
            # They share 0x16 but differ in payload and entity semantics.
            if path.stem == "AbstractNpcInfo":
                methods = list(re.finditer(r"\bvoid\s+writeImpl\s*\([^)]*\)\s*\{", text))
                row["server_packet"] = "AbstractNpcInfo.NpcInfo"
                for variant in methods[1:]:
                    variant_wire = body(text, variant.end() - 1)
                    concrete = re.findall(r"\bclass\s+(\w+)", text[:variant.start()])[-1]
                    additional = dict(row)
                    additional["server_packet"] = "AbstractNpcInfo." + concrete
                    additional["server_writes"] = " ".join(re.findall(r"\bwrite([CDHFQS])\s*\(", variant_wire))
                    rows.append(additional)
    return rows


def requests(values):
    outgoing = {}
    outgoing_details = {}
    dto_paths = {p.stem: p for p in (NET / "ClientLibrary/Packet/Dto/Outgoing").rglob("*.cs")}
    for path in (NET / "ClientLibrary/Packet/Outgoing/Game").rglob("*.cs"):
        text = source(path)
        match = re.search(r"\bOpcode\s*=>\s*\(byte\)\s*(\w+\.\w+)", text)
        if match and match[1] in values:
            outgoing.setdefault(values[match[1]], []).append(path.stem)
            model = re.search(r"OutgoingWirePacket<(\w+)>", text)
            dto_path = dto_paths.get(model[1]) if model else None
            wire = source(dto_path) if dto_path else text
            outgoing_details[path.stem] = (str(path.relative_to(CLIENT)),
                " ".join(re.findall(r"\bwriter\.Write(\w+)\s*\(", wire)))
    handler = SERVER / "java/com/l2j4team/gameserver/network/L2GamePacketHandler.java"
    parents = {}
    state = ""
    rows = []
    for line in source(handler).splitlines():
        case = re.search(r"^(\s*)case\s+(\w+)\s*:", line)
        if case:
            indent = len(case[1].expandtabs(4))
            if case[2].startswith("0x") or case[2].isdigit():
                parents = {k: v for k, v in parents.items() if k < indent}
                parents[indent] = int(case[2], 0)
            else:
                state = case[2]
                parents = {}
        packet = re.search(r"msg\s*=\s*new\s+([\w.]+)\s*\(", line)
        if not packet or not parents: continue
        chain = list(parents.values())
        opcode = chain[0]
        extended = chain[1] if len(chain) > 1 else None
        server_path = SERVER / "java/com/l2j4team/gameserver/network/clientpackets" / (packet[1] + ".java")
        server_text = source(server_path) if server_path.exists() else ""
        read_match = re.search(r"\bvoid\s+readImpl\s*\([^)]*\)\s*\{", server_text)
        read_body = body(server_text, read_match.end() - 1) if read_match else ""
        client_names = outgoing.get(opcode, []) if extended is None else []
        rows.append({"state": state, "opcode": f"0x{opcode:02X}",
                     "extended_opcode": "" if extended is None else f"0x{extended:04X}",
                     "server_packet": packet[1],
                     "client_outgoing": " | ".join(outgoing.get(opcode, [])) if extended is None else "requires-extended-review",
                     "status": "present-layout-unverified" if extended is None and opcode in outgoing else "missing-or-extended-review",
                     "server_source": str(server_path.relative_to(SERVER)) if server_path.exists() else "",
                     "server_reads": " ".join(re.findall(r"\bread([CDHFQS])\s*\(", read_body)),
                     "client_source": " | ".join(outgoing_details[n][0] for n in client_names),
                     "client_writes": " | ".join(outgoing_details[n][1] for n in client_names)})
    return rows


def save_csv(path, rows):
    with path.open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def application_paths(incoming):
    """Trace world handoff and subtype hooks without claiming runtime success."""
    root = CLIENT / "Assets/Scripts/Game/WorldApply"
    world_path = root / "WorldPacketApply.cs"
    world = source(world_path)
    rows = []
    for packet in incoming:
        for binding_path in packet["client_source"].split(" | "):
            if not binding_path: continue
            binding = source(CLIENT / binding_path)
            for target in re.findall(r"(?:Apply|Queue)World\(\s*apply\s*=>\s*apply\.(\w+)\(", binding):
                method = re.search(r"\bvoid\s+" + target + r"\s*\([^)]*\)\s*\{", world)
                implementation = body(world, method.end() - 1) if method else ""
                hooks = re.findall(r"\bapply\.(On\w+)\s*\(", implementation)
                row = {"server_packet": packet["server_packet"], "opcode": packet["opcode"],
                       "binding_source": binding_path, "world_method": target,
                       "world_source": str(world_path.relative_to(CLIENT)),
                       "entity_hooks": " | ".join(hooks),
                       "status": "explicit-log-only-ignore" if target == "SocialAction" else "dispatch-present-runtime-unverified"}
                for subtype in ("Player", "Npc", "Monster", "User"):
                    path = root / (subtype + "WorldApply.cs")
                    text = source(path)
                    statuses = []
                    for hook in hooks:
                        match = re.search(r"\bvoid\s+" + hook + r"\s*\([^)]*\)\s*\{", text)
                        statuses.append(hook + ":" + (
                            "override-present-review-required" if match and body(text, match.end() - 1).strip()
                            else "empty-override" if match else "inherited-empty-default"))
                    row[subtype.lower() + "_hooks"] = " | ".join(statuses) or "direct-world-method-review"
                rows.append(row)
    return rows


def login_requests(values):
    handler = SERVER / "java/com/l2j4team/loginserver/L2LoginPacketHandler.java"
    text = source(handler)
    outgoing = {}
    dto_paths = {p.stem: p for p in (NET / "ClientLibrary/Packet/Dto/Outgoing").rglob("*.cs")}
    for path in (NET / "ClientLibrary/Packet/Outgoing/Login").glob("*.cs"):
        client_text = source(path)
        match = re.search(r"Opcode\s*=>\s*\(byte\)\s*(\w+\.\w+)", client_text)
        if match and match[1] in values:
            outgoing[values[match[1]]] = path
    rows = []
    state = ""
    opcode = None
    for line in text.splitlines():
        match = re.search(r"case\s+(\w+)\s*:", line)
        if match: state = match[1]
        match = re.search(r"if\s*\(opcode\s*==\s*(\d+)\)", line)
        if match: opcode = int(match[1])
        match = re.search(r"packet\s*=\s*new\s+(\w+)\(", line)
        if not match or opcode is None: continue
        server_path = SERVER / "java/com/l2j4team/loginserver/network/clientpackets" / (match[1] + ".java")
        server_text = source(server_path)
        read = re.search(r"\breadImpl\s*\([^)]*\)\s*\{", server_text)
        wire = body(server_text, read.end() - 1) if read else ""
        path = outgoing.get(opcode)
        client_text = source(path) if path else ""
        model = re.search(r"OutgoingWirePacket<(\w+)>", client_text)
        dto_path = dto_paths.get(model[1]) if model else None
        dto_text = source(dto_path) if dto_path else ""
        rows.append({"state": state, "opcode": f"0x{opcode:02X}", "server_packet": match[1],
            "server_source": str(server_path.relative_to(SERVER)),
            "server_reads": " ".join(re.findall(r"\bread([BCDHFQS])\s*\(", wire)),
            "client_source": str(path.relative_to(CLIENT)) if path else "",
            "client_writes": " ".join(re.findall(r"\bwriter\.Write(\w+)\s*\(", dto_text)),
            "status": "present-layout-unverified" if path else "missing-client-writer"})
    return rows


def main():
    values = enums()
    found = bindings(values)
    incoming = writers(found)
    outgoing = requests(values)
    save_csv(OUT / "server-to-client.csv", incoming)
    save_csv(OUT / "client-to-server.csv", outgoing)
    world_paths = application_paths(incoming)
    save_csv(OUT / "world-application.csv", world_paths)
    login = login_requests(values)
    save_csv(OUT / "login-client-to-server.csv", login)
    counts = Counter(row["status"] for row in incoming)
    summary = {"server_writers": len(incoming), "server_request_routes": len(outgoing),
               "distinct_server_opcode_keys": len({(r["channel"], r["opcode"]) for r in incoming if r["opcode"]}),
               "client_binding_keys": len(found), "writer_statuses": dict(counts),
               "duplicate_bindings": sum(len(items) > 1 for items in found.values()),
               "request_statuses": dict(Counter(r["status"] for r in outgoing)),
               "world_handoff_rows": len(world_paths),
               "login_request_routes": len(login),
               "limits": ["Static inventory; present does not mean correct.", "Counts are writer classes and state-dependent routes, not unique opcodes.", "Write/read traces include loops and conditionals without expansion.", "Extended client requests need separate validation.", "Empty Apply must be reviewed with OnParsed and other lifecycle side effects.", "Login request routes are recorded separately from Game."]}
    (OUT / "inventory.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary))


if __name__ == "__main__": main()
