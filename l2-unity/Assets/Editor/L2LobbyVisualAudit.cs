using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class L2LobbyVisualAudit
{
    public static void RepairAndBuild()
    {
        Repair();
        L2CleanWindowsBuild.BuildIncremental();
    }

    public static void Repair()
    {
        const string brushesPath = "Assets/Resources/Data/Maps/l2_lobby/Brushes.prefab";
        var brushes = PrefabUtility.LoadPrefabContents(brushesPath);
        try
        {
            var constructionBrush = brushes.transform.Find("Brush4");
            if (constructionBrush != null)
            {
                var faces = constructionBrush.GetComponentsInChildren<MeshRenderer>();
                if (faces.Length != 6) throw new System.InvalidOperationException("Unexpected Brush4 geometry.");
                foreach (var face in faces)
                    foreach (var material in face.sharedMaterials)
                        if (material != null) throw new System.InvalidOperationException("Brush4 has a surface material; refusing to remove it.");
                Object.DestroyImmediate(constructionBrush.gameObject);
                PrefabUtility.SaveAsPrefabAsset(brushes, brushesPath);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(brushes); }

        var nativeWater = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Data/StaticMeshes/Ertheia_V_S/Ertheia_water_sh.mat");
        var lobbyWater = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Data/Effects/lobby_water/lobby_ocean.mat");
        if (nativeWater == null || lobbyWater == null || !nativeWater.shader.isSupported)
            throw new System.InvalidOperationException("Native water material unavailable.");
        lobbyWater.shader = nativeWater.shader;
        lobbyWater.CopyPropertiesFromMaterial(nativeWater);
        EditorUtility.SetDirty(lobbyWater);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.OpenScene("Assets/Resources/Scenes/l2_lobby.unity");
        var ocean = GameObject.Find("Water (1)");
        if (ocean == null) throw new System.InvalidOperationException("Lobby ocean missing.");
        var mesh = ocean.GetComponent<MeshFilter>().sharedMesh;
        // World-space water UVs keep wave size independent of the ocean's extent.
        ocean.transform.localScale = new Vector3(2048f / mesh.bounds.size.x, 1f, 2048f / mesh.bounds.size.z);
        ocean.GetComponent<Renderer>().sharedMaterial = lobbyWater;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Capture("Logs/LobbyAfter.png");
        Debug.Log("[Lobby repair] Removed untextured construction cube; restored native ocean material and horizon coverage.");
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Resources/Scenes/l2_lobby.unity");
        var report = new StringBuilder();
        foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            foreach (var material in renderer.sharedMaterials)
            {
                var shader = material != null ? material.shader : null;
                if (shader == null || !shader.isSupported || renderer.name.ToLowerInvariant().Contains("water"))
                {
                    string hierarchy = renderer.name;
                    for (var parent = renderer.transform.parent; parent != null; parent = parent.parent)
                        hierarchy = parent.name + "/" + hierarchy;
                    report.AppendLine($"Object={hierarchy}; Source={PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(renderer)}; Position={renderer.transform.position}; Bounds={renderer.bounds}; Material={AssetDatabase.GetAssetPath(material)}; Shader={shader?.name}; Supported={shader != null && shader.isSupported}");
                }
            }
        }
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            report.AppendLine($"Camera={camera.name}; Enabled={camera.enabled}; Position={camera.transform.position}; Rotation={camera.transform.eulerAngles}; Far={camera.farClipPlane}");
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/LobbyVisualAudit.txt", report.ToString());
        Debug.Log(report.ToString());
        Capture("Logs/LobbyBefore.png");
    }

    private static void Capture(string path)
    {
        Camera camera = null;
        foreach (var candidate in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            if (candidate.name == "Login") camera = candidate;
        if (camera == null) throw new System.InvalidOperationException("Login camera missing.");
        var target = new RenderTexture(1024, 768, 24);
        var previous = RenderTexture.active;
        target.Create();
        try
        {
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            var texture = new Texture2D(1024, 768, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1024, 768), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        finally
        {
            RenderTexture.active = previous;
            target.Release();
            Object.DestroyImmediate(target);
        }
    }
}
