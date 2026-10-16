using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class L2ProtocolAuditValidation
{
    public static void Run()
    {
        var report = new StringBuilder();
        ValidateSelection(report, 0);
        ValidateSelection(report, 2);
        foreach (var test in new[] {
            (race: 0, classId: 9, body: CharacterRaceAnimation.MFighter),
            (race: 0, classId: 92, body: CharacterRaceAnimation.MFighter),
            (race: 0, classId: 12, body: CharacterRaceAnimation.MMagic),
            (race: 0, classId: 94, body: CharacterRaceAnimation.MMagic),
            (race: 1, classId: 24, body: CharacterRaceAnimation.MElf),
            (race: 2, classId: 37, body: CharacterRaceAnimation.MDarkElf),
            (race: 3, classId: 46, body: CharacterRaceAnimation.MOrc),
            (race: 3, classId: 51, body: CharacterRaceAnimation.MShaman),
            (race: 4, classId: 57, body: CharacterRaceAnimation.MDwarf) })
            ValidateSelection(report, 1, test.race, test.classId, test.body);
        ValidateSelected(report);
        ValidateMoveType(report);
        ValidateLoginFieldLimits(report);
        AuditPose(report, "MMagic");
        AuditPose(report, "MFighter");
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/ProtocolAuditValidation.txt", report.ToString());
        Debug.Log(report.ToString());
    }

    public static void ValidateAndBuild()
    {
        Run();
        L2CleanWindowsBuild.BuildIncremental();
    }

    private static void ValidateSelection(StringBuilder report, int count, int race = 0, int classId = 10,
        CharacterRaceAnimation expectedBody = CharacterRaceAnimation.MMagic)
    {
        MapClassId.Init();
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.Unicode, true);
        writer.Write(count);
        for (int slot = 0; slot < count; slot++)
        {
            WriteString(writer, "Audit" + slot);
            writer.Write(700 + slot);
            WriteString(writer, "audit");
            writer.Write(123); writer.Write(11); writer.Write(0);
            writer.Write(slot % 2); writer.Write(race); writer.Write(classId); writer.Write(1);
            writer.Write(-84037); writer.Write(242782); writer.Write(-3730);
            writer.Write(100d + slot); writer.Write(200d + slot);
            writer.Write(12); writer.Write((1L << 33) + slot); writer.Write(80);
            writer.Write(0); writer.Write(0); writer.Write(0);
            for (int zero = 0; zero < 7; zero++) writer.Write(0);
            for (int paperdoll = 0; paperdoll < 34; paperdoll++) writer.Write(0);
            writer.Write(1); writer.Write(2); writer.Write(0);
            writer.Write(1000d + slot); writer.Write(2000d + slot);
            writer.Write(0); writer.Write(classId); writer.Write(slot == count - 1 ? 1 : 0);
            writer.Write((byte)7); writer.Write(4321);
        }
        writer.Flush();
        var reader = new PacketReader(stream.ToArray(), false);
        var dto = new CharSelectionInfoDto();
        dto.ReadFrom(reader);
        if (dto.Characters.Count != count || reader.Remaining != 0)
            throw new InvalidOperationException("CharSelectionInfo size or consumption mismatch.");
        for (int slot = 0; slot < count; slot++)
        {
            var character = dto.Characters[slot];
            if (character.Hp != 100d + slot || character.MaxHp != 1000d + slot ||
                character.Mp != 200d + slot || character.MaxMp != 2000d + slot ||
                character.ClassId != classId || character.EnchantId != 7 || character.Autgmentation != 4321)
                throw new InvalidOperationException("CharSelectionInfo field ordering mismatch.");
        }
        if (count > 0 && dto.SelectedSlotId != count - 1)
            throw new InvalidOperationException("CharSelectionInfo selected slot mismatch.");
        report.AppendLine($"CharSelectionInfo: PASS count={count} consumed={stream.Length} remaining={reader.Remaining}");
        if (count > 0 && dto.Characters[0].CharacterRaceAnimation != expectedBody)
            throw new InvalidOperationException($"Incorrect selection body for wire race={race} class={classId}.");
        if (count > 0) report.AppendLine($"ClassBody: PASS wireRace={race} class={classId} body={expectedBody}");
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        writer.Write(Encoding.Unicode.GetBytes(value));
        writer.Write((ushort)0);
    }

    private static void ValidateSelected(StringBuilder report)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.Unicode, true);
        WriteString(writer, "Audit"); writer.Write(700); WriteString(writer, "");
        writer.Write(123); writer.Write(0); writer.Write(0);
        writer.Write(1); writer.Write(0); writer.Write(10); writer.Write(1);
        writer.Write(-84037); writer.Write(242782); writer.Write(-3730);
        writer.Write(1234d); writer.Write(567d); writer.Write(765432);
        writer.Write(1L << 33); writer.Write(80); writer.Write(0); writer.Write(0);
        for (int stat = 0; stat < 6; stat++) writer.Write(20 + stat);
        for (int padding = 0; padding < 32; padding++) writer.Write(0);
        writer.Write(12345); writer.Write(0); writer.Write(10);
        for (int padding = 0; padding < 4; padding++) writer.Write(0);
        writer.Flush();
        var reader = new PacketReader(stream.ToArray(), false);
        var dto = new CharSelectedDto();
        dto.ReadFrom(reader);
        var info = dto.PlayeInfo;
        if (reader.Remaining != 0 || info.Stats.Sp != 765432 || info.Status.Cp != 0 ||
            info.Stats.Exp != (1L << 33) || info.Appearance.Sex != 1 || info.Identity.ResetTick != 12345)
            throw new InvalidOperationException($"CharSelected field mismatch: remaining={reader.Remaining} SP={info.Stats.Sp} CP={info.Status.Cp} EXP={info.Stats.Exp} sex={info.Appearance.Sex} tick={info.Identity.ResetTick}");
        report.AppendLine($"CharSelected: PASS consumed={stream.Length} remaining={reader.Remaining} SP preserved; UI independent");
    }

    private static void AuditPose(StringBuilder report, string race)
    {
        var prototype = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Resources/Data/Animations/Human/{race}/Pawn_{race}.prefab");
        if (prototype == null) throw new InvalidOperationException("Missing pose probe prefab.");
        var pawn = UnityEngine.Object.Instantiate(prototype);
        pawn.name = "PoseAudit";
        pawn.transform.position = Vector3.zero;
        pawn.SetActive(true);
        try
        {
            var animator = pawn.GetComponentInChildren<Animator>(true);
            if (animator == null) throw new InvalidOperationException("Pose probe has no animator.");
            // This probe evaluates transforms, without the game's audio/VFX services.
            animator.fireEvents = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);
            string family = race == "MMagic" ? "Magic" : "Fighter";
            string meshName = race == "MMagic" ? "MMagic_m000_u" : "MFighter_m000_u";
            var torsoAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Resources/Data/Animations/{family}/{meshName}.prefab");
            var torso = UnityEngine.Object.Instantiate(torsoAsset, animator.transform.parent, false);
            var torsoRenderer = torso.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var sourceRenderer = animator.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var originalBones = torsoRenderer.bones;
            var sourceBones = sourceRenderer.bones;
            report.AppendLine($"Skin: race={race} rendererScale={torsoRenderer.transform.lossyScale} root={torsoRenderer.rootBone?.name} animatedRoot={sourceRenderer.rootBone?.name} bounds={torsoRenderer.bounds.size}");
            int differences = Enumerable.Range(0, Math.Min(originalBones.Length, sourceBones.Length))
                .Count(i => originalBones[i].name != sourceBones[i].name);
            report.AppendLine($"Skin: race={race} sourceBones={sourceBones.Length} targetBones={originalBones.Length} indexNameDifferences={differences}");
            pawn.GetComponentInChildren<SkinnedMeshSync>(true).SyncMesh();
            foreach (string state in new[] { "wait_hand", "wait_bow", "walk_bow", "wait_1HS", "walk_1HS", "wait_2HS", "walk_2HS", "death" })
            {
                bool exists = animator.HasState(0, Animator.StringToHash(state));
                if (!exists) { report.AppendLine($"Pose: race={race} state={state} MISSING"); continue; }
                animator.Play(state, 0, 0f);
                for (int frame = 0; frame < 30; frame++) animator.Update(1f / 30f);
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                var transforms = animator.GetComponentsInChildren<Transform>(true);
                var head = transforms.FirstOrDefault(t => t.name.Equals("Bip01_Head", StringComparison.OrdinalIgnoreCase) &&
                    AnimationUtility.CalculateTransformPath(t, animator.transform).Contains(".ao/"));
                float headY = head != null ? head.position.y - pawn.transform.position.y : float.NaN;
                report.AppendLine($"Pose: race={race} state={state} actual={animator.GetCurrentAnimatorStateInfo(0).shortNameHash} clip={string.Join(",", clips.Select(c => c.clip.name))} headY={headY:F4} deathFlag={animator.GetBool("death")}");
                var baked = new Mesh();
                try
                {
                    torsoRenderer.BakeMesh(baked);
                    // BakeMesh already includes renderer scale in its default output.
                    var points = baked.vertices.Select(v => torsoRenderer.transform.position + torsoRenderer.transform.rotation * v).ToArray();
                    var bounds = new Bounds(points[0], Vector3.zero);
                    foreach (var point in points) bounds.Encapsulate(point);
                    report.AppendLine($"Skin: race={race} state={state} worldSize={bounds.size} center={bounds.center}");
                }
                finally { UnityEngine.Object.DestroyImmediate(baked); }
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(pawn); }
    }

    private static void ValidateMoveType(StringBuilder report)
    {
        foreach (int gait in new[] { 0, 1 })
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(700); writer.Write(gait); writer.Write(0);
            writer.Flush();
            var reader = new PacketReader(stream.ToArray(), false);
            var dto = new ChangeMoveTypeDto(); dto.ReadFrom(reader);
            if (dto.ObjectId != 700 || dto.Running != (gait == 1) || reader.Remaining != 0)
                throw new InvalidOperationException("ChangeMoveType mismatch.");
        }
        try
        {
            new ChangeMoveTypeDto().ReadFrom(new PacketReader(new byte[8], false));
            throw new InvalidOperationException("Truncated ChangeMoveType accepted.");
        }
        catch (FormatException) { }
        report.AppendLine("ChangeMoveType: PASS walk/run; truncated input rejected");
    }

    private static void ValidateLoginFieldLimits(StringBuilder report)
    {
        var method = typeof(RequestAuthLogin).GetMethod("TryCreate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        bool Valid(string account, string password) => (bool)method.Invoke(null, new object[] { account, password });
        if (!Valid(new string('a', 14), new string('p', 16)) ||
            Valid(new string('a', 15), "p") || Valid("a", new string('p', 17)) ||
            !Valid(new string('é', 7), "p") || Valid(new string('é', 8), "p"))
            throw new InvalidOperationException("Login RSA field boundary mismatch.");
        report.AppendLine("LoginFields: PASS 14-byte account / 16-byte password; UTF-8 byte limits; no network authentication");
    }
}
