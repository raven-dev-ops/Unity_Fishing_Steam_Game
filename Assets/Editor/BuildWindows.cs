#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWindows
{
    [MenuItem("Raven/Build/Build Windows x64")]
    public static void BuildWindowsX64()
    {
        var outputDir = Path.Combine("Builds", "Windows");
        Directory.CreateDirectory(outputDir);

        var options = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/00_Boot.unity",
                "Assets/Scenes/01_Cinematic.unity",
                "Assets/Scenes/02_MainMenu.unity",
                "Assets/Scenes/03_Harbor.unity",
                "Assets/Scenes/04_Fishing.unity"
            },
            locationPathName = Path.Combine(outputDir, "UnityFishingSteamGame.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Windows build succeeded: {summary.totalSize} bytes at {options.locationPathName}");
        }
        else
        {
            Debug.LogError($"Windows build failed: {summary.result}");
        }
    }
}
#endif
