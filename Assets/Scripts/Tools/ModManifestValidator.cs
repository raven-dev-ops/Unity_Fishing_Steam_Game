using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RavenDevOps.Fishing.Tools
{
    public static class ModManifestValidator
    {
        private static readonly Regex ModIdPattern = new Regex("^[a-z0-9_.-]+$");
        private static readonly Regex SemverPattern = new Regex("^(?<major>\\d+)\\.(?<minor>\\d+)\\.(?<patch>\\d+)(?:[-+].*)?$");
        private static readonly HashSet<string> SupportedSchemaVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "1.0"
        };

        private static readonly HashSet<string> AllowedDataCatalogExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".json"
        };

        private static readonly HashSet<string> AllowedAssetExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".wav",
            ".ogg",
            ".mp3"
        };

        public static bool TryParseJson(string json, out ModManifestV1 manifest, out string errorMessage)
        {
            manifest = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                errorMessage = "Manifest JSON is empty.";
                return false;
            }

            try
            {
                manifest = JsonUtility.FromJson<ModManifestV1>(json);
                if (manifest == null)
                {
                    errorMessage = "Manifest JSON deserialized to null.";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = $"Manifest JSON parse failed: {exception.Message}";
                return false;
            }
        }

        public static List<string> Validate(ModManifestV1 manifest, ISet<string> knownModIds = null)
        {
            var messages = new List<string>();
            if (manifest == null)
            {
                messages.Add("ERROR: Manifest is null.");
                return messages;
            }

            ValidateSchemaVersion(manifest, messages);
            ValidateModIdentity(manifest, knownModIds, messages);
            ValidateVersionRange(manifest, messages);
            ValidatePathList(manifest.dataCatalogs, "dataCatalogs", AllowedDataCatalogExtensions, messages);
            ValidatePathList(manifest.assetOverrides, "assetOverrides", AllowedAssetExtensions, messages);
            return messages;
        }

        public static int CountErrors(List<string> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < messages.Count; i++)
            {
                if (messages[i] != null && messages[i].StartsWith("ERROR"))
                {
                    count++;
                }
            }

            return count;
        }

        private static void ValidateSchemaVersion(ModManifestV1 manifest, List<string> messages)
        {
            if (string.IsNullOrWhiteSpace(manifest.schemaVersion))
            {
                messages.Add("ERROR: schemaVersion is required.");
                return;
            }

            if (!SupportedSchemaVersions.Contains(manifest.schemaVersion.Trim()))
            {
                messages.Add($"ERROR: Unsupported schemaVersion '{manifest.schemaVersion}'. Supported: 1.0.");
            }
        }

        private static void ValidateModIdentity(ModManifestV1 manifest, ISet<string> knownModIds, List<string> messages)
        {
            if (string.IsNullOrWhiteSpace(manifest.modId))
            {
                messages.Add("ERROR: modId is required.");
            }
            else
            {
                var trimmed = manifest.modId.Trim();
                if (!ModIdPattern.IsMatch(trimmed))
                {
                    messages.Add($"ERROR: modId '{manifest.modId}' must match pattern ^[a-z0-9_.-]+$.");
                }

                if (knownModIds != null && knownModIds.Contains(trimmed))
                {
                    messages.Add($"ERROR: Duplicate modId '{trimmed}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(manifest.displayName))
            {
                messages.Add("ERROR: displayName is required.");
            }

            if (string.IsNullOrWhiteSpace(manifest.modVersion))
            {
                messages.Add("ERROR: modVersion is required.");
            }
            else if (!TryParseSemver(manifest.modVersion, out _, out _, out _))
            {
                messages.Add($"ERROR: modVersion '{manifest.modVersion}' must be semver (MAJOR.MINOR.PATCH).");
            }
        }

        private static void ValidateVersionRange(ModManifestV1 manifest, List<string> messages)
        {
            var hasMin = !string.IsNullOrWhiteSpace(manifest.minGameVersion);
            var hasMax = !string.IsNullOrWhiteSpace(manifest.maxGameVersion);

            var minValid = true;
            var maxValid = true;
            var minMajor = 0;
            var minMinor = 0;
            var minPatch = 0;
            var maxMajor = 0;
            var maxMinor = 0;
            var maxPatch = 0;

            if (hasMin)
            {
                minValid = TryParseSemver(manifest.minGameVersion, out minMajor, out minMinor, out minPatch);
                if (!minValid)
                {
                    messages.Add($"ERROR: minGameVersion '{manifest.minGameVersion}' is not valid semver.");
                }
            }

            if (hasMax)
            {
                maxValid = TryParseSemver(manifest.maxGameVersion, out maxMajor, out maxMinor, out maxPatch);
                if (!maxValid)
                {
                    messages.Add($"ERROR: maxGameVersion '{manifest.maxGameVersion}' is not valid semver.");
                }
            }

            if (hasMin && hasMax && minValid && maxValid)
            {
                if (CompareSemver(minMajor, minMinor, minPatch, maxMajor, maxMinor, maxPatch) > 0)
                {
                    messages.Add("ERROR: minGameVersion cannot be greater than maxGameVersion.");
                }
            }
        }

        private static void ValidatePathList(
            List<string> paths,
            string fieldName,
            HashSet<string> allowedExtensions,
            List<string> messages)
        {
            if (paths == null || paths.Count == 0)
            {
                messages.Add($"WARN: {fieldName} list is empty.");
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    messages.Add($"ERROR: {fieldName}[{i}] is empty.");
                    continue;
                }

                var normalized = path.Replace('\\', '/').Trim();
                if (!seen.Add(normalized))
                {
                    messages.Add($"ERROR: Duplicate {fieldName} entry '{normalized}'.");
                    continue;
                }

                if (Path.IsPathRooted(normalized) || normalized.StartsWith("/") || normalized.Contains("../") || normalized.Contains("/.."))
                {
                    messages.Add($"ERROR: {fieldName} entry '{normalized}' must be a safe relative path.");
                }

                var extension = Path.GetExtension(normalized);
                if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
                {
                    messages.Add($"ERROR: {fieldName} entry '{normalized}' has disallowed extension '{extension}'.");
                }
            }
        }

        private static bool TryParseSemver(string value, out int major, out int minor, out int patch)
        {
            major = 0;
            minor = 0;
            patch = 0;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var match = SemverPattern.Match(value.Trim());
            if (!match.Success)
            {
                return false;
            }

            return int.TryParse(match.Groups["major"].Value, out major)
                && int.TryParse(match.Groups["minor"].Value, out minor)
                && int.TryParse(match.Groups["patch"].Value, out patch);
        }

        private static int CompareSemver(int aMajor, int aMinor, int aPatch, int bMajor, int bMinor, int bPatch)
        {
            var major = aMajor.CompareTo(bMajor);
            if (major != 0)
            {
                return major;
            }

            var minor = aMinor.CompareTo(bMinor);
            if (minor != 0)
            {
                return minor;
            }

            return aPatch.CompareTo(bPatch);
        }
    }
}
