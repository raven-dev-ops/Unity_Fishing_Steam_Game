#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RavenDevOps.Fishing.EditorTools
{
    public static class BuildCommandLine
    {
        private const string DefaultOutputDirectory = "Builds/Windows";
        private const string DefaultExecutableName = "UnityFishingSteamGame.exe";
        private const string MetadataFileName = "build_metadata.json";

        private sealed class BuildMetadata
        {
            public string productName;
            public string companyName;
            public string bundleVersion;
            public string unityVersion;
            public string buildNumber;
            public string commitSha;
            public string branch;
            public string buildTimestampUtc;
            public string outputExecutable;
        }

        [MenuItem("Raven/Build/Build Windows x64")]
        public static void BuildWindowsFromMenu()
        {
            var result = BuildWindowsInternal(LogErrorAndReturnFailure);
            if (!result.Succeeded)
            {
                Debug.LogError(result.ErrorMessage);
            }
        }

        public static void BuildWindowsBatchMode()
        {
            var result = BuildWindowsInternal(LogErrorAndReturnFailure);
            if (!result.Succeeded)
            {
                Debug.LogError(result.ErrorMessage);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        private static BuildResultInfo BuildWindowsInternal(Func<string, BuildResultInfo> fail)
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene != null && scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                return fail("BuildCommandLine: no enabled scenes found in EditorBuildSettings.");
            }

            var outputDirectory = GetArgumentValue("buildOutput", DefaultOutputDirectory);
            var executableName = GetArgumentValue("buildExeName", DefaultExecutableName);
            Directory.CreateDirectory(outputDirectory);

            var locationPathName = Path.Combine(outputDirectory, executableName);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                return fail($"BuildCommandLine: build failed ({summary.result}).");
            }

            var metadataPath = Path.Combine(outputDirectory, MetadataFileName);
            WriteMetadata(metadataPath, locationPathName);
            Debug.Log($"BuildCommandLine: build succeeded at '{locationPathName}'.");
            Debug.Log($"BuildCommandLine: metadata written to '{metadataPath}'.");
            return BuildResultInfo.Success();
        }

        private static void WriteMetadata(string metadataPath, string executablePath)
        {
            var metadata = new BuildMetadata
            {
                productName = PlayerSettings.productName,
                companyName = PlayerSettings.companyName,
                bundleVersion = ResolveBundleVersion(),
                unityVersion = Application.unityVersion,
                buildNumber = ResolveBuildNumber(),
                commitSha = ResolveCommitSha(),
                branch = ResolveBranchName(),
                buildTimestampUtc = DateTime.UtcNow.ToString("O"),
                outputExecutable = executablePath
            };

            var json = JsonUtility.ToJson(metadata, true);
            File.WriteAllText(metadataPath, json);
        }

        private static string ResolveBundleVersion()
        {
            var explicitValue = GetArgumentValue("buildVersion", string.Empty);
            if (!string.IsNullOrWhiteSpace(explicitValue))
            {
                return explicitValue;
            }

            return PlayerSettings.bundleVersion;
        }

        private static string ResolveBuildNumber()
        {
            return GetArgumentValue("buildNumber", Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER") ?? "local");
        }

        private static string ResolveCommitSha()
        {
            return GetArgumentValue("buildCommit", Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local");
        }

        private static string ResolveBranchName()
        {
            return GetArgumentValue("buildBranch", Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ?? "local");
        }

        private static string GetArgumentValue(string argumentName, string fallback)
        {
            var argumentPrefix = "-" + argumentName + "=";
            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (!argument.StartsWith(argumentPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return argument.Substring(argumentPrefix.Length).Trim();
            }

            return fallback;
        }

        private static BuildResultInfo LogErrorAndReturnFailure(string message)
        {
            Debug.LogError(message);
            return BuildResultInfo.Failure(message);
        }

        private readonly struct BuildResultInfo
        {
            private BuildResultInfo(bool succeeded, string errorMessage)
            {
                Succeeded = succeeded;
                ErrorMessage = errorMessage;
            }

            public bool Succeeded { get; }
            public string ErrorMessage { get; }

            public static BuildResultInfo Success()
            {
                return new BuildResultInfo(true, string.Empty);
            }

            public static BuildResultInfo Failure(string errorMessage)
            {
                return new BuildResultInfo(false, errorMessage ?? "Build failed.");
            }
        }
    }
}
#endif
