using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RavenDevOps.Fishing.EditorTools
{
    public static class AssetImportComplianceRunner
    {
        [Serializable]
        private sealed class WarningAllowlistConfig
        {
            public List<WarningAllowlistEntry> entries = new List<WarningAllowlistEntry>();
        }

        [Serializable]
        private sealed class WarningAllowlistEntry
        {
            public string pattern = string.Empty;
            public string owner = string.Empty;
            public string reason = string.Empty;
            public string expiresOn = string.Empty;
        }

        [MenuItem("Raven/Validate Asset Import Compliance")]
        public static void ValidateFromMenu()
        {
            RunAudit(failOnWarnings: false, allowlistPath: string.Empty);
        }

        public static void ValidateAssetImportsBatchMode()
        {
            var failOnWarnings = GetBoolArgument("assetImportAuditFailOnWarnings", false);
            var allowlistPath = GetStringArgument("assetImportAuditAllowlistPath", "ci/asset-import-warning-allowlist.json");
            var success = RunAudit(failOnWarnings, allowlistPath);
            if (!success)
            {
                throw new BuildFailedException("Asset import audit failed.");
            }
        }

        private static bool RunAudit(bool failOnWarnings, string allowlistPath)
        {
            try
            {
                var result = AssetImportComplianceAuditor.Run();
                var originalWarningCount = result.WarningCount;
                var allowlistErrors = new List<string>();
                var suppressedCount = 0;
                var filteredWarnings = ApplyAllowlist(result.Warnings, allowlistPath, allowlistErrors, out suppressedCount);
                result.Warnings.Clear();
                result.Warnings.AddRange(filteredWarnings);

                if (allowlistErrors.Count > 0)
                {
                    for (var i = 0; i < allowlistErrors.Count; i++)
                    {
                        Debug.LogError(allowlistErrors[i]);
                    }

                    Debug.LogError("Asset Import Audit: allowlist configuration errors detected.");
                    return false;
                }

                for (var i = 0; i < result.Warnings.Count; i++)
                {
                    Debug.LogWarning(result.Warnings[i]);
                }

                Debug.Log(
                    $"Asset Import Audit: checked {result.TextureAssetsChecked} texture(s) and {result.AudioAssetsChecked} audio clip(s) with {result.WarningCount} active warning(s), {suppressedCount} allowlisted warning(s), {originalWarningCount} total warning(s). Report: '{result.ReportPath}'.");

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

        private static List<string> ApplyAllowlist(
            List<string> warnings,
            string allowlistPath,
            List<string> allowlistErrors,
            out int suppressedCount)
        {
            suppressedCount = 0;
            if (warnings == null || warnings.Count == 0)
            {
                return new List<string>();
            }

            var allowlistEntries = LoadAllowlistEntries(allowlistPath, allowlistErrors);
            var filteredWarnings = new List<string>(warnings.Count);
            for (var i = 0; i < warnings.Count; i++)
            {
                var warning = warnings[i];
                if (string.IsNullOrWhiteSpace(warning))
                {
                    continue;
                }

                var matchedAllowlist = false;
                for (var j = 0; j < allowlistEntries.Count; j++)
                {
                    var entry = allowlistEntries[j];
                    if (warning.IndexOf(entry.pattern, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    matchedAllowlist = true;
                    suppressedCount++;
                    break;
                }

                if (!matchedAllowlist)
                {
                    filteredWarnings.Add(warning);
                }
            }

            return filteredWarnings;
        }

        private static List<WarningAllowlistEntry> LoadAllowlistEntries(string allowlistPath, List<string> allowlistErrors)
        {
            var entries = new List<WarningAllowlistEntry>();
            if (string.IsNullOrWhiteSpace(allowlistPath))
            {
                return entries;
            }

            if (!File.Exists(allowlistPath))
            {
                allowlistErrors.Add($"Asset Import Audit: allowlist file not found at '{allowlistPath}'.");
                return entries;
            }

            WarningAllowlistConfig config;
            try
            {
                var json = File.ReadAllText(allowlistPath);
                config = JsonUtility.FromJson<WarningAllowlistConfig>(json);
            }
            catch (Exception ex)
            {
                allowlistErrors.Add($"Asset Import Audit: failed to read allowlist '{allowlistPath}' ({ex.Message}).");
                return entries;
            }

            if (config == null || config.entries == null || config.entries.Count == 0)
            {
                return entries;
            }

            for (var i = 0; i < config.entries.Count; i++)
            {
                var entry = config.entries[i];
                if (!TryValidateAllowlistEntry(entry, i, allowlistErrors))
                {
                    continue;
                }

                entries.Add(entry);
            }

            return entries;
        }

        private static bool TryValidateAllowlistEntry(WarningAllowlistEntry entry, int index, List<string> allowlistErrors)
        {
            if (entry == null)
            {
                allowlistErrors.Add($"Asset Import Audit: allowlist entry #{index} is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.pattern))
            {
                allowlistErrors.Add($"Asset Import Audit: allowlist entry #{index} is missing required field 'pattern'.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.owner))
            {
                allowlistErrors.Add($"Asset Import Audit: allowlist entry '{entry.pattern}' is missing required field 'owner'.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.reason))
            {
                allowlistErrors.Add($"Asset Import Audit: allowlist entry '{entry.pattern}' is missing required field 'reason'.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.expiresOn))
            {
                allowlistErrors.Add($"Asset Import Audit: allowlist entry '{entry.pattern}' is missing required field 'expiresOn' (YYYY-MM-DD).");
                return false;
            }

            if (!DateTime.TryParseExact(entry.expiresOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresOn))
            {
                allowlistErrors.Add($"Asset Import Audit: allowlist entry '{entry.pattern}' has invalid 'expiresOn' value '{entry.expiresOn}' (expected YYYY-MM-DD).");
                return false;
            }

            if (expiresOn.Date < DateTime.UtcNow.Date)
            {
                allowlistErrors.Add($"Asset Import Audit: allowlist entry '{entry.pattern}' expired on {entry.expiresOn}.");
                return false;
            }

            return true;
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

        private static string GetStringArgument(string argumentName, string fallback)
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
                if (string.IsNullOrWhiteSpace(value))
                {
                    return fallback;
                }

                return value;
            }

            return fallback;
        }
    }
}
