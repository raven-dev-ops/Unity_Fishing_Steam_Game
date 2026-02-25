using System;
using System.Collections.Generic;
using System.Text;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Core
{
    public sealed class BootstrapAssetValidationIssue
    {
        public BootstrapAssetValidationIssue(string ownerSubsystem, string assetKey, string resourcePath, string expectedType, string details)
        {
            OwnerSubsystem = ownerSubsystem ?? string.Empty;
            AssetKey = assetKey ?? string.Empty;
            ResourcePath = resourcePath ?? string.Empty;
            ExpectedType = expectedType ?? string.Empty;
            Details = details ?? string.Empty;
        }

        public string OwnerSubsystem { get; }
        public string AssetKey { get; }
        public string ResourcePath { get; }
        public string ExpectedType { get; }
        public string Details { get; }
    }

    public sealed class BootstrapAssetValidationReport
    {
        private readonly List<BootstrapAssetValidationIssue> _issues = new List<BootstrapAssetValidationIssue>();

        public IReadOnlyList<BootstrapAssetValidationIssue> Issues => _issues;
        public bool IsValid => _issues.Count == 0;

        public void AddMissing(string ownerSubsystem, string assetKey, string resourcePath, string expectedType, string details)
        {
            _issues.Add(new BootstrapAssetValidationIssue(ownerSubsystem, assetKey, resourcePath, expectedType, details));
        }

        public string BuildFailureMessage()
        {
            if (IsValid)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.Append($"Bootstrap asset validation failed with {_issues.Count} missing requirement(s):");
            for (var i = 0; i < _issues.Count; i++)
            {
                var issue = _issues[i];
                if (issue == null)
                {
                    continue;
                }

                builder.Append(
                    $" [owner={issue.OwnerSubsystem}, asset={issue.AssetKey}, path=Resources/{issue.ResourcePath}, expected={issue.ExpectedType}, details={issue.Details}]");
            }

            return builder.ToString();
        }
    }

    public sealed class BootstrapAssetValidationDependencies
    {
        public Func<string, InputActionAsset> LoadInputActionAsset { get; set; }
        public Func<string, TextAsset> LoadTextAsset { get; set; }
        public Func<string, GameConfigSO> LoadGameConfig { get; set; }
        public Func<string, TuningConfigSO> LoadTuningConfig { get; set; }
        public Func<string, TutorialSpriteLibrary> LoadTutorialSpriteLibrary { get; set; }

        public static BootstrapAssetValidationDependencies CreateDefault()
        {
            return new BootstrapAssetValidationDependencies
            {
                LoadInputActionAsset = path => Resources.Load<InputActionAsset>(path),
                LoadTextAsset = path => Resources.Load<TextAsset>(path),
                LoadGameConfig = path => Resources.Load<GameConfigSO>(path),
                LoadTuningConfig = path => Resources.Load<TuningConfigSO>(path),
                LoadTutorialSpriteLibrary = path => Resources.Load<TutorialSpriteLibrary>(path)
            };
        }
    }

    public static class BootstrapAssetContractValidator
    {
        public const string InputActionsAssetKey = "input_actions";
        public const string GameConfigAssetKey = "game_config";
        public const string TuningConfigAssetKey = "tuning_config";
        public const string TutorialSpriteLibraryAssetKey = "tutorial_sprite_library";

        public static BootstrapAssetValidationReport ValidateRequiredAssets(BootstrapAssetValidationDependencies dependencies = null)
        {
            var resolvedDependencies = dependencies ?? BootstrapAssetValidationDependencies.CreateDefault();
            var report = new BootstrapAssetValidationReport();

            ValidateInputActions(report, resolvedDependencies);
            ValidateRequiredResource(
                report,
                ownerSubsystem: "CatalogService",
                assetKey: GameConfigAssetKey,
                resourcePath: CatalogService.DefaultConfigResourcePath,
                expectedType: nameof(GameConfigSO),
                loader: resolvedDependencies.LoadGameConfig);
            ValidateRequiredResource(
                report,
                ownerSubsystem: "SceneRuntimeCompositionBootstrap",
                assetKey: TuningConfigAssetKey,
                resourcePath: SceneRuntimeCompositionBootstrap.TuningConfigResourcePath,
                expectedType: nameof(TuningConfigSO),
                loader: resolvedDependencies.LoadTuningConfig);
            ValidateRequiredResource(
                report,
                ownerSubsystem: "SceneRuntimeCompositionBootstrap",
                assetKey: TutorialSpriteLibraryAssetKey,
                resourcePath: SceneRuntimeCompositionBootstrap.TutorialSpriteLibraryResourcePath,
                expectedType: nameof(TutorialSpriteLibrary),
                loader: resolvedDependencies.LoadTutorialSpriteLibrary);

            return report;
        }

        public static void LogValidationReport(BootstrapAssetValidationReport report)
        {
            if (report == null || report.IsValid)
            {
                return;
            }

            for (var i = 0; i < report.Issues.Count; i++)
            {
                var issue = report.Issues[i];
                if (issue == null)
                {
                    continue;
                }

                Debug.LogError(BuildStructuredError(issue));
            }

            Debug.LogError($"BOOTSTRAP_ASSET_VALIDATION|status=failed|missing_count={report.Issues.Count}|summary={SanitizeForLog(report.BuildFailureMessage())}");
        }

        private static void ValidateInputActions(BootstrapAssetValidationReport report, BootstrapAssetValidationDependencies dependencies)
        {
            if (report == null)
            {
                return;
            }

            var inputAsset = dependencies != null && dependencies.LoadInputActionAsset != null
                ? dependencies.LoadInputActionAsset(RuntimeServicesBootstrap.InputActionsResourcePath)
                : null;
            if (inputAsset != null)
            {
                return;
            }

            var inputJson = dependencies != null && dependencies.LoadTextAsset != null
                ? dependencies.LoadTextAsset(RuntimeServicesBootstrap.InputActionsResourcePath)
                : null;
            if (inputJson != null)
            {
                return;
            }

            report.AddMissing(
                ownerSubsystem: "RuntimeServicesBootstrap+InputActionMapController",
                assetKey: InputActionsAssetKey,
                resourcePath: RuntimeServicesBootstrap.InputActionsResourcePath,
                expectedType: "InputActionAsset or TextAsset",
                details: "Missing input action asset/json resource required for map activation.");
        }

        private static void ValidateRequiredResource<T>(
            BootstrapAssetValidationReport report,
            string ownerSubsystem,
            string assetKey,
            string resourcePath,
            string expectedType,
            Func<string, T> loader) where T : UnityEngine.Object
        {
            if (report == null)
            {
                return;
            }

            var asset = loader != null
                ? loader(resourcePath)
                : null;
            if (asset != null)
            {
                return;
            }

            report.AddMissing(
                ownerSubsystem: ownerSubsystem,
                assetKey: assetKey,
                resourcePath: resourcePath,
                expectedType: expectedType,
                details: $"Required resource not found for type {expectedType}.");
        }

        private static string BuildStructuredError(BootstrapAssetValidationIssue issue)
        {
            return
                $"BOOTSTRAP_ASSET_VALIDATION|status=missing|owner={SanitizeForLog(issue.OwnerSubsystem)}|asset={SanitizeForLog(issue.AssetKey)}|path=Resources/{SanitizeForLog(issue.ResourcePath)}|expected={SanitizeForLog(issue.ExpectedType)}|details={SanitizeForLog(issue.Details)}";
        }

        private static string SanitizeForLog(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "none";
            }

            return value.Trim().Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
