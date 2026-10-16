using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class L2ImportRecovery
{
    static L2ImportRecovery()
    {
        if (!AssetDatabase.IsAssetImportWorkerProcess())
            EditorApplication.delayCall += LimitWorkers;
    }

    static void LimitWorkers()
    {
        EditorUserSettings.desiredImportWorkerCount = 2;
        AssetDatabase.DesiredWorkerCount = 2;
        AssetDatabase.ForceToDesiredWorkerCount();
    }

    [MenuItem("L2/Tools/Recover Failed Imports")]
    public static void Recover()
    {
        LimitWorkers();
        var paths = new HashSet<string>(StringComparer.Ordinal);
        string logs = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        if (Directory.Exists(logs))
        {
            foreach (string log in Directory.GetFiles(logs, "AssetImportWorker*-prev.log"))
            {
                string text = File.ReadAllText(log);
                if (!text.Contains("A crash has been intercepted")) continue;
                Match match = Regex.Match(text, "Importing 'GUID: [^\\r\\n]*?Path: ([^']+)' with importer", RegexOptions.RightToLeft, TimeSpan.FromSeconds(2));
                if (match.Success) paths.Add(match.Groups[1].Value);
            }
        }
        paths.Add("Assets/Resources/Data/Animations/Magic/MMagic/Models/MMagic_m004_m00_ah.fbx");
        paths.Add("Assets/Resources/Data/Animations/Elf/FElf/Models/FElf_m001_g.fbx");
        int imported = 0;
        foreach (string path in paths)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..") || !File.Exists(path)) continue;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            imported++;
        }
        foreach (string path in new[] {
            "Assets/Resources/Data/Animations/Magic/MMagic_m004_m00_ah.prefab",
            "Assets/Resources/Data/Animations/Elf/FElf_m001_g.prefab",
            "Assets/Resources/Data/Animations/Human/MMagic/Hair/MMagic_h_4_0_ah.prefab",
            "Assets/Resources/Data/Animations/Human/MMagic/Hair/MMagic_h_4_1_ah.prefab",
            "Assets/Resources/Data/Animations/Human/MMagic/Hair/MMagic_h_4_2_ah.prefab",
            "Assets/Resources/Data/Animations/Human/MMagic/Hair/MMagic_h_4_3_ah.prefab" })
            if (File.Exists(path))
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[L2 Import Recovery] Reimportados " + imported + " arquivos de origem. Confira o Console; quedas nativas recorrentes precisam de investigação adicional.");
        Debug.LogWarning("[L2 Import Recovery] ImpactMeshFX.blend precisa de Blender. MMagic_m000_b_ao_full.fbx está ausente e precisa ser recuperado com seu .meta original.");
    }
}
