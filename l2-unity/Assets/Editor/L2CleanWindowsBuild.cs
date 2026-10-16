using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class L2CleanWindowsBuild
{
    [MenuItem("L2/Build/Clean Windows Player")]
    public static void Build()
    {
        BuildPlayer(BuildOptions.CleanBuildCache);
    }

    public static void BuildIncremental()
    {
        BuildPlayer(BuildOptions.None);
    }

    private static void BuildPlayer(BuildOptions buildOptions)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Saia do Play Mode antes de gerar o executável.");

        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
            .Select(scene => scene.path).Distinct().ToArray();
        if (scenes.Length == 0 || !scenes[0].EndsWith("/Menu.unity", StringComparison.Ordinal))
            throw new InvalidOperationException("A primeira cena habilitada deve ser Menu.unity.");

        string output = Path.GetFullPath(Path.Combine("Builds", "WindowsClean",
            DateTime.Now.ToString("yyyyMMdd-HHmmss"), "L2-Unity.exe"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            target = BuildTarget.StandaloneWindows64,
            locationPathName = output,
            options = buildOptions
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        string summary = $"Resultado: {report.summary.result}\nErros: {report.summary.totalErrors}\nAvisos: {report.summary.totalWarnings}\nSaída: {output}\n";
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "BuildSummary.txt"), summary);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Build limpo falhou. " + summary);

        Debug.Log("[L2 Build] Build limpo gerado. Execute o novo arquivo para verificar a inicialização. " + output);
        if (!Application.isBatchMode && !Environment.GetCommandLineArgs().Contains("-executeMethod"))
            EditorUtility.RevealInFinder(output);
    }
}
