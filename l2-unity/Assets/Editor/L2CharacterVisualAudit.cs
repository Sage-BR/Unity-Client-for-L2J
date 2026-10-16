using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class L2CharacterVisualAudit
{
    public static void ValidateAndBuild()
    {
        Run();
        L2CleanWindowsBuild.BuildIncremental();
    }

    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Directory.CreateDirectory("Logs/CharacterVisual");
        var report = new StringBuilder();
        var anchorRoot = new GameObject("AnchorRegression");
        var colliderChild = new GameObject("Collider");
        colliderChild.transform.SetParent(anchorRoot.transform, false);
        colliderChild.transform.localPosition = Vector3.up * 0.2f;
        colliderChild.transform.localScale = Vector3.one * 2f;
        var controller = colliderChild.AddComponent<CharacterController>();
        controller.height = 0.8f; controller.center = Vector3.up * 0.4f;
        var anchor = L2NameplateAnchor.GetHeadWorldPos(anchorRoot.transform, controller, null, 0.4f,
            L2NameplateAnchor.DefaultHeadHeightOffsetMeters);
        if (Mathf.Abs(anchor.y - 1.88f) > 0.001f)
            throw new InvalidOperationException("Nameplate ignored collider transform or clearance.");
        colliderChild.transform.localPosition = Vector3.zero;
        colliderChild.transform.localScale = Vector3.one;
        anchor = L2NameplateAnchor.GetHeadWorldPos(anchorRoot.transform, controller, null, 22f,
            L2NameplateAnchor.DefaultHeadHeightOffsetMeters);
        if (Mathf.Abs(anchor.y - (44f / 52.5f + 0.08f)) > 0.001f)
            throw new InvalidOperationException("Nameplate preferred undersized collider over NPC collision height.");
        UnityEngine.Object.DestroyImmediate(anchorRoot);
        report.AppendLine("Nameplate: PASS child collider translation/scale and positive clearance.");
        typeof(ModelTable).GetMethod("CacheHair", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(ModelTable.Instance, null);
        int hairVariants = 0;
        foreach (string path in Directory.GetFiles("Assets/Resources/Data/Animations", "*_h_*_*.prefab", SearchOption.AllDirectories))
        {
            var parts = Path.GetFileNameWithoutExtension(path).Split('_');
            if (parts.Length != 5 || !Enum.TryParse(parts[0], out CharacterRaceAnimation race)) continue;
            if (!byte.TryParse(parts[2], out byte style) || !byte.TryParse(parts[3], out byte color)) continue;
            var expected = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
            if (ModelTable.Instance.GetHair(race, style, color, parts[4] == "bh") != expected)
                throw new InvalidOperationException("Hair cache mismatch: " + path);
            hairVariants++;
        }
        report.AppendLine($"Hair cache: PASS {hairVariants} existing variants resolve to their exact prefab.");
        foreach (int style in new[] { 0, 1, 2 })
        for (int color = 0; color < 4; color++)
        {
            string textureName = $"MFighter_m00{style}_t0{color}_m00_ah";
            var material = Resources.Load<Material>("Data/SysTextures/MFighter/Materials/" + textureName);
            var texture = Resources.Load<Texture2D>("Data/SysTextures/MFighter/" + textureName + "_ori");
            if (texture == null || material.GetTexture("_BaseMap") != texture || material.GetTexture("_MainTex") != texture ||
                !material.IsKeywordEnabled("_ALPHATEST_ON") ||
                Mathf.Abs(material.GetFloat("_Cutoff") - 5f / 255f) > 0.00001f ||
                material.GetFloat("_SrcBlend") != 5 || material.GetFloat("_DstBlend") != 10)
                throw new InvalidOperationException("MFighter hair atlas/alpha mismatch: " + textureName);
        }
        report.AppendLine("MFighter hair: PASS 12 frontal atlas references, original AlphaRef=5 and alpha blend.");
        var pawn = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Data/Animations/Human/MFighter/Pawn_MFighter"));
        pawn.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        pawn.SetActive(true);
        var animator = pawn.GetComponentInChildren<Animator>(true);
        animator.fireEvents = false;
        animator.Rebind(); animator.Play("wait_hand"); animator.Update(0.1f);
        foreach (string part in new[] { "Faces/MFighter_f_0", "Hair/MFighter_h_0_0_ah", "Hair/MFighter_h_0_0_bh" })
            UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Data/Animations/Human/MFighter/" + part), animator.transform.parent, false);
        var torso = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Data/Animations/Fighter/MFighter_m001_u"), animator.transform.parent, false);
        torso.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial = Resources.Load<Material>("Data/SysTextures/MFighter/Materials/MFighter_m001_t02_u");
        var root = animator.GetComponentInChildren<SkinnedMeshRenderer>(true);
        foreach (var renderer in pawn.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            int differences = renderer.bones.Where((b, i) => i >= root.bones.Length || b.name != root.bones[i].name).Count();
            report.AppendLine($"{renderer.name}: mesh={renderer.sharedMesh?.name}; bones={renderer.bones.Length}; differences={differences}; submeshes={renderer.sharedMesh?.subMeshCount}; materials={string.Join(",", renderer.sharedMaterials.Select(m => m != null ? m.name : "NULL"))}");
        }
        pawn.GetComponentInChildren<SkinnedMeshSync>(true).SyncMesh();
        animator.Update(0f);
        var camera = new GameObject("AuditCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0.78f, 1.8f);
        camera.transform.LookAt(new Vector3(0, 0.68f, 0));
        camera.orthographic = true; camera.orthographicSize = 0.34f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.gray;
        camera.nearClipPlane = 0.01f;
        Shader.SetGlobalVector("_L2ActorAmbientHalf", new Vector4(0.5f, 0.5f, 0.5f, 1));
        Shader.SetGlobalVector("_L2HsvActorLight", Vector4.zero);
        Capture(camera, "Logs/CharacterVisual/CorrectedActor.png");
        foreach (var renderer in pawn.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
            {
                if (source == null) return source;
                var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                material.SetFloat("_Cull", 0);
                if (source.IsKeywordEnabled("_ALPHATEST_ON"))
                {
                    material.EnableKeyword("_ALPHATEST_ON"); material.SetFloat("_AlphaClip", 1);
                    material.SetFloat("_Cutoff", source.GetFloat("_Cutoff"));
                }
                if (source.GetFloat("_SrcBlend") == 5)
                {
                    material.SetFloat("_Surface", 1); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetFloat("_SrcBlend", 5); material.SetFloat("_DstBlend", 10);
                    material.renderQueue = source.renderQueue;
                }
                return material;
            }).ToArray();
        }
        Capture(camera, "Logs/CharacterVisual/Corrected.png");
        File.WriteAllText("Logs/CharacterVisual/Audit.txt", report.ToString());
        Debug.Log(report.ToString());
    }

    private static void Capture(Camera camera, string path)
    {
        var target = new RenderTexture(640, 640, 24);
        var previous = RenderTexture.active;
        target.Create();
        try
        {
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            var texture = new Texture2D(640, 640, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
    }
}
