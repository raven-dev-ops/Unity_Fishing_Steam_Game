using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RavenDevOps.Fishing.EditorTools
{
    public static class AssetImportComplianceRunner
    {
        [MenuItem("Raven/Validate Asset Import Compliance")]
        public static void ValidateFromMenu()
        {
            RunAudit(failOnWarnings: false);
        }

        public static void ValidateAssetImportsBatchMode()
        {
            var failOnWarnings = GetBoolArgument("assetImportAuditFailOnWarnings", false);
            var success = RunAudit(failOnWarnings);
            if (!success)
            {
                throw new BuildFailedException("Asset import audit failed.");
            }
        }

        private static bool RunAudit(bool failOnWarnings)
        {
            try
            {
                var result = AssetImportComplianceAuditor.Run();
                for (var i = 0; i < result.Warnings.Count; i++)
                {
                    Debug.LogWarning(result.Warnings[i]);
                }

                Debug.Log(
                    $"Asset Import Audit: checked {result.TextureAssetsChecked} texture(s) and {result.AudioAssetsChecked} audio clip(s) with {result.WarningCount} warning(s). Report: '{result.ReportPath}'.");

                if (failOnWarnings && result.WarningCount > 0)
                {
                    Debug.LogError("Asset Import Audit: warnings present and failOnWarnings is enabled.");
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Asset Import Audit: unexpected failure ({exception.Message}).");
                return false;
            }
        }

        private static bool GetBoolArgument(string argumentName, bool fallback)
        {
            var prefix = "-" + argumentName + "=";
            var arguments = Environment.GetCommandLineArgs();
            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = argument.Substring(prefix.Length).Trim();
                if (bool.TryParse(value, out var boolValue))
                {
                    return boolValue;
                }

                if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "y", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "n", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return fallback;
            }

            return fallback;
        }
    }
}
