using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RavenDevOps.Fishing.Tools
{
    public static class ModRuntimeCatalogLoader
    {
        public const string ManifestFileName = "manifest.json";

        private static readonly Regex ContentIdPattern = new Regex("^[a-z0-9_]+$");
        private static readonly Regex SemverPattern = new Regex("^(?<major>\\d+)\\.(?<minor>\\d+)\\.(?<patch>\\d+)(?:[-+].*)?$");

        private sealed class CandidatePack
        {
            public string directoryPath;
            public ModManifestV1 manifest;
            public List<ModCatalogDataV1> catalogs = new List<ModCatalogDataV1>();
        }

        public static ModRuntimeCatalogLoadResult Load(string modsRootPath, bool modsEnabled, string currentGameVersion)
        {
            var result = new ModRuntimeCatalogLoadResult
            {
                modsEnabled = modsEnabled,
                modsRootPath = modsRootPath ?? string.Empty
            };

            if (!modsEnabled)
            {
                result.messages.Add("INFO: Mod runtime loading disabled.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(modsRootPath) || !Directory.Exists(modsRootPath))
            {
                result.messages.Add($"INFO: Mods directory not found at '{modsRootPath}'.");
                return result;
            }

            var knownModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var candidates = DiscoverCandidatePacks(modsRootPath, knownModIds, currentGameVersion, result);
            candidates.Sort((a, b) => string.Compare(a.manifest.modId, b.manifest.modId, StringComparison.Ordinal));
            ApplyCandidatePacks(candidates, result);
            return result;
        }

        private static List<CandidatePack> DiscoverCandidatePacks(
            string modsRootPath,
            HashSet<string> knownModIds,
            string currentGameVersion,
            ModRuntimeCatalogLoadResult result)
        {
            var candidates = new List<CandidatePack>();
            var modDirectories = Directory.GetDirectories(modsRootPath);
            Array.Sort(modDirectories, StringComparer.Ordinal);

            for (var i = 0; i < modDirectories.Length; i++)
            {
                var modDirectory = modDirectories[i];
                var manifestPath = Path.Combine(modDirectory, ManifestFileName);
                if (!File.Exists(manifestPath))
                {
                    Reject(result, modDirectory, $"Missing {ManifestFileName}.");
                    continue;
                }

                if (!TryLoadManifest(manifestPath, knownModIds, currentGameVersion, out var manifest, out var reason))
                {
                    Reject(result, modDirectory, reason);
                    continue;
                }

                if (!TryLoadCatalogs(modDirectory, manifest, out var catalogs, out reason))
                {
                    Reject(result, modDirectory, reason);
                    continue;
                }

                candidates.Add(new CandidatePack
                {
                    directoryPath = modDirectory,
                    manifest = manifest,
                    catalogs = catalogs
                });
            }

            return candidates;
        }

        private static bool TryLoadManifest(
            string manifestPath,
            HashSet<string> knownModIds,
            string currentGameVersion,
            out ModManifestV1 manifest,
            out string reason)
        {
            manifest = null;
            reason = string.Empty;

            string manifestJson;
            try
            {
                manifestJson = File.ReadAllText(manifestPath);
            }
            catch (Exception exception)
            {
                reason = $"Manifest read failed ({exception.Message}).";
                return false;
            }

            if (!ModManifestValidator.TryParseJson(manifestJson, out manifest, out var parseError))
            {
                reason = parseError;
                return false;
            }

            var validationMessages = ModManifestValidator.Validate(manifest, knownModIds);
            if (ModManifestValidator.CountErrors(validationMessages) > 0)
            {
                reason = string.Join(" | ", validationMessages.FindAll(message => message.StartsWith("ERROR")));
                return false;
            }

            if (!string.IsNullOrWhiteSpace(manifest.modId))
            {
                knownModIds.Add(manifest.modId.Trim());
            }

            if (!IsGameVersionCompatible(manifest, currentGameVersion, out reason))
            {
                return false;
            }

            return true;
        }

        private static bool TryLoadCatalogs(
            string modDirectory,
            ModManifestV1 manifest,
            out List<ModCatalogDataV1> catalogs,
            out string reason)
        {
            catalogs = new List<ModCatalogDataV1>();
            reason = string.Empty;

            if (manifest == null || manifest.dataCatalogs == null || manifest.dataCatalogs.Count == 0)
            {
                reason = "Manifest does not define dataCatalogs entries.";
                return false;
            }

            for (var i = 0; i < manifest.dataCatalogs.Count; i++)
            {
                var relativePath = manifest.dataCatalogs[i];
                var fullPath = Path.GetFullPath(Path.Combine(modDirectory, relativePath ?? string.Empty));
                if (!File.Exists(fullPath))
                {
                    reason = $"Catalog file not found '{relativePath}'.";
                    return false;
                }

                string json;
                try
                {
                    json = File.ReadAllText(fullPath);
                }
                catch (Exception exception)
                {
                    reason = $"Catalog read failed '{relativePath}' ({exception.Message}).";
                    return false;
                }

                ModCatalogDataV1 catalog;
                try
                {
                    catalog = JsonUtility.FromJson<ModCatalogDataV1>(json);
                }
                catch (Exception exception)
                {
                    reason = $"Catalog parse failed '{relativePath}' ({exception.Message}).";
                    return false;
                }

                if (catalog == null)
                {
                    reason = $"Catalog parse produced null for '{relativePath}'.";
                    return false;
                }

                var catalogErrors = ValidateCatalogEntries(catalog);
                if (catalogErrors.Count > 0)
                {
                    reason = $"Catalog validation failed '{relativePath}': {string.Join(" | ", catalogErrors)}";
                    return false;
                }

                catalogs.Add(catalog);
            }

            return true;
        }

        private static List<string> ValidateCatalogEntries(ModCatalogDataV1 catalog)
        {
            var errors = new List<string>();
            if (catalog == null)
            {
                errors.Add("catalog is null");
                return errors;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            ValidateFishEntries(catalog.fishDefinitions, ids, errors);
            ValidateShipEntries(catalog.shipDefinitions, ids, errors);
            ValidateHookEntries(catalog.hookDefinitions, ids, errors);
            return errors;
        }

        private static void ValidateFishEntries(List<ModFishDefinitionData> fishEntries, HashSet<string> ids, List<string> errors)
        {
            if (fishEntries == null)
            {
                return;
            }

            for (var i = 0; i < fishEntries.Count; i++)
            {
                var fish = fishEntries[i];
                if (fish == null)
                {
                    errors.Add($"fishDefinitions[{i}] is null");
                    continue;
                }

                ValidateContentId(fish.id, "fish", ids, errors);
                if (fish.minDistanceTier < 0 || fish.maxDistanceTier < fish.minDistanceTier)
                {
                    errors.Add($"fish '{fish.id}' has invalid distance tier range");
                }

                if (fish.minDepth < 0f || fish.maxDepth < fish.minDepth)
                {
                    errors.Add($"fish '{fish.id}' has invalid depth range");
                }

                if (fish.rarityWeight <= 0 || fish.baseValue <= 0)
                {
                    errors.Add($"fish '{fish.id}' requires positive rarityWeight and baseValue");
                }

                if (fish.minBiteDelaySeconds < 0f || fish.maxBiteDelaySeconds < fish.minBiteDelaySeconds)
                {
                    errors.Add($"fish '{fish.id}' has invalid bite delay range");
                }

                if (fish.fightStamina <= 0f || fish.pullIntensity <= 0f || fish.escapeSeconds <= 0f)
                {
                    errors.Add($"fish '{fish.id}' has non-positive fight values");
                }

                if (fish.minCatchWeightKg <= 0f || fish.maxCatchWeightKg < fish.minCatchWeightKg)
                {
                    errors.Add($"fish '{fish.id}' has invalid catch weight range");
                }
            }
        }

        private static void ValidateShipEntries(List<ModShipDefinitionData> shipEntries, HashSet<string> ids, List<string> errors)
        {
            if (shipEntries == null)
            {
                return;
            }

            for (var i = 0; i < shipEntries.Count; i++)
            {
                var ship = shipEntries[i];
                if (ship == null)
                {
                    errors.Add($"shipDefinitions[{i}] is null");
                    continue;
                }

                ValidateContentId(ship.id, "ship", ids, errors);
                if (ship.price < 0 || ship.maxDistanceTier < 0 || ship.moveSpeed <= 0f)
                {
                    errors.Add($"ship '{ship.id}' has invalid numeric values");
                }
            }
        }

        private static void ValidateHookEntries(List<ModHookDefinitionData> hookEntries, HashSet<string> ids, List<string> errors)
        {
            if (hookEntries == null)
            {
                return;
            }

            for (var i = 0; i < hookEntries.Count; i++)
            {
                var hook = hookEntries[i];
                if (hook == null)
                {
                    errors.Add($"hookDefinitions[{i}] is null");
                    continue;
                }

                ValidateContentId(hook.id, "hook", ids, errors);
                if (hook.price < 0 || hook.maxDepth <= 0f)
                {
                    errors.Add($"hook '{hook.id}' has invalid numeric values");
                }
            }
        }

        private static void ValidateContentId(string id, string label, HashSet<string> ids, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add($"{label} entry has empty id");
                return;
            }

            if (!ContentIdPattern.IsMatch(id))
            {
                errors.Add($"{label} id '{id}' must match ^[a-z0-9_]+$");
            }

            if (!ids.Add(id))
            {
                errors.Add($"duplicate catalog id '{id}'");
            }
        }

        private static void ApplyCandidatePacks(List<CandidatePack> candidates, ModRuntimeCatalogLoadResult result)
        {
            var fishSourceMap = new Dictionary<string, string>(StringComparer.Ordinal);
            var shipSourceMap = new Dictionary<string, string>(StringComparer.Ordinal);
            var hookSourceMap = new Dictionary<string, string>(StringComparer.Ordinal);

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var assetAllowList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (candidate.manifest.assetOverrides != null)
                {
                    for (var j = 0; j < candidate.manifest.assetOverrides.Count; j++)
                    {
                        var value = candidate.manifest.assetOverrides[j];
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            continue;
                        }

                        assetAllowList.Add(NormalizeRelativePath(value));
                    }
                }

                for (var c = 0; c < candidate.catalogs.Count; c++)
                {
                    var catalog = candidate.catalogs[c];
                    ApplyFishEntries(candidate, catalog.fishDefinitions, assetAllowList, fishSourceMap, result);
                    ApplyShipEntries(candidate, catalog.shipDefinitions, assetAllowList, shipSourceMap, result);
                    ApplyHookEntries(candidate, catalog.hookDefinitions, assetAllowList, hookSourceMap, result);
                }

                result.acceptedMods.Add(new ModAcceptedPackInfo
                {
                    modId = candidate.manifest.modId,
                    modVersion = candidate.manifest.modVersion,
                    directoryPath = candidate.directoryPath
                });
            }
        }

        private static void ApplyFishEntries(
            CandidatePack candidate,
            List<ModFishDefinitionData> fishEntries,
            HashSet<string> assetAllowList,
            Dictionary<string, string> sourceMap,
            ModRuntimeCatalogLoadResult result)
        {
            if (fishEntries == null)
            {
                return;
            }

            for (var i = 0; i < fishEntries.Count; i++)
            {
                var fish = fishEntries[i];
                if (fish == null || string.IsNullOrWhiteSpace(fish.id))
                {
                    continue;
                }

                var clone = new ModFishDefinitionData
                {
                    id = fish.id,
                    minDistanceTier = fish.minDistanceTier,
                    maxDistanceTier = fish.maxDistanceTier,
                    minDepth = fish.minDepth,
                    maxDepth = fish.maxDepth,
                    rarityWeight = fish.rarityWeight,
                    baseValue = fish.baseValue,
                    minBiteDelaySeconds = fish.minBiteDelaySeconds,
                    maxBiteDelaySeconds = fish.maxBiteDelaySeconds,
                    fightStamina = fish.fightStamina,
                    pullIntensity = fish.pullIntensity,
                    escapeSeconds = fish.escapeSeconds,
                    minCatchWeightKg = fish.minCatchWeightKg,
                    maxCatchWeightKg = fish.maxCatchWeightKg,
                    iconPath = fish.iconPath,
                    sourceModId = candidate.manifest.modId,
                    sourceDirectory = candidate.directoryPath
                };

                clone.resolvedIconPath = ResolveOptionalAssetPath(candidate.directoryPath, fish.iconPath, assetAllowList, result);
                if (result.fishById.ContainsKey(clone.id))
                {
                    var source = sourceMap.TryGetValue(clone.id, out var previousSource) ? previousSource : "unknown";
                    result.messages.Add($"WARN: Mod '{candidate.manifest.modId}' overrides fish id '{clone.id}' (previous source '{source}').");
                    result.fishById[clone.id] = clone;
                    sourceMap[clone.id] = candidate.manifest.modId;
                    continue;
                }

                result.fishById.Add(clone.id, clone);
                sourceMap[clone.id] = candidate.manifest.modId;
            }
        }

        private static void ApplyShipEntries(
            CandidatePack candidate,
            List<ModShipDefinitionData> shipEntries,
            HashSet<string> assetAllowList,
            Dictionary<string, string> sourceMap,
            ModRuntimeCatalogLoadResult result)
        {
            if (shipEntries == null)
            {
                return;
            }

            for (var i = 0; i < shipEntries.Count; i++)
            {
                var ship = shipEntries[i];
                if (ship == null || string.IsNullOrWhiteSpace(ship.id))
                {
                    continue;
                }

                var clone = new ModShipDefinitionData
                {
                    id = ship.id,
                    price = ship.price,
                    maxDistanceTier = ship.maxDistanceTier,
                    moveSpeed = ship.moveSpeed,
                    iconPath = ship.iconPath,
                    sourceModId = candidate.manifest.modId,
                    sourceDirectory = candidate.directoryPath
                };

                clone.resolvedIconPath = ResolveOptionalAssetPath(candidate.directoryPath, ship.iconPath, assetAllowList, result);
                if (result.shipById.ContainsKey(clone.id))
                {
                    var source = sourceMap.TryGetValue(clone.id, out var previousSource) ? previousSource : "unknown";
                    result.messages.Add($"WARN: Mod '{candidate.manifest.modId}' overrides ship id '{clone.id}' (previous source '{source}').");
                    result.shipById[clone.id] = clone;
                    sourceMap[clone.id] = candidate.manifest.modId;
                    continue;
                }

                result.shipById.Add(clone.id, clone);
                sourceMap[clone.id] = candidate.manifest.modId;
            }
        }

        private static void ApplyHookEntries(
            CandidatePack candidate,
            List<ModHookDefinitionData> hookEntries,
            HashSet<string> assetAllowList,
            Dictionary<string, string> sourceMap,
            ModRuntimeCatalogLoadResult result)
        {
            if (hookEntries == null)
            {
                return;
            }

            for (var i = 0; i < hookEntries.Count; i++)
            {
                var hook = hookEntries[i];
                if (hook == null || string.IsNullOrWhiteSpace(hook.id))
                {
                    continue;
                }

                var clone = new ModHookDefinitionData
                {
                    id = hook.id,
                    price = hook.price,
                    maxDepth = hook.maxDepth,
                    iconPath = hook.iconPath,
                    sourceModId = candidate.manifest.modId,
                    sourceDirectory = candidate.directoryPath
                };

                clone.resolvedIconPath = ResolveOptionalAssetPath(candidate.directoryPath, hook.iconPath, assetAllowList, result);
                if (result.hookById.ContainsKey(clone.id))
                {
                    var source = sourceMap.TryGetValue(clone.id, out var previousSource) ? previousSource : "unknown";
                    result.messages.Add($"WARN: Mod '{candidate.manifest.modId}' overrides hook id '{clone.id}' (previous source '{source}').");
                    result.hookById[clone.id] = clone;
                    sourceMap[clone.id] = candidate.manifest.modId;
                    continue;
                }

                result.hookById.Add(clone.id, clone);
                sourceMap[clone.id] = candidate.manifest.modId;
            }
        }

        private static string ResolveOptionalAssetPath(
            string modDirectory,
            string relativePath,
            HashSet<string> assetAllowList,
            ModRuntimeCatalogLoadResult result)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            var normalized = NormalizeRelativePath(relativePath);
            if (!assetAllowList.Contains(normalized))
            {
                result.messages.Add($"WARN: Asset path '{relativePath}' is not declared in manifest assetOverrides.");
                return string.Empty;
            }

            if (Path.IsPathRooted(normalized) || normalized.StartsWith("/") || normalized.Contains("../") || normalized.Contains("/.."))
            {
                result.messages.Add($"WARN: Ignoring unsafe asset path '{relativePath}'.");
                return string.Empty;
            }

            var fullPath = Path.GetFullPath(Path.Combine(modDirectory, normalized));
            if (!File.Exists(fullPath))
            {
                result.messages.Add($"WARN: Asset file not found '{relativePath}'.");
                return string.Empty;
            }

            return fullPath;
        }

        private static string NormalizeRelativePath(string value)
        {
            return (value ?? string.Empty).Replace('\\', '/').Trim();
        }

        private static bool IsGameVersionCompatible(ModManifestV1 manifest, string currentGameVersion, out string reason)
        {
            reason = string.Empty;
            if (manifest == null || string.IsNullOrWhiteSpace(currentGameVersion))
            {
                return true;
            }

            if (!TryParseSemver(currentGameVersion, out var gameMajor, out var gameMinor, out var gamePatch))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(manifest.minGameVersion) &&
                TryParseSemver(manifest.minGameVersion, out var minMajor, out var minMinor, out var minPatch))
            {
                if (CompareSemver(gameMajor, gameMinor, gamePatch, minMajor, minMinor, minPatch) < 0)
                {
                    reason = $"Current game version '{currentGameVersion}' is below minGameVersion '{manifest.minGameVersion}'.";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(manifest.maxGameVersion) &&
                TryParseSemver(manifest.maxGameVersion, out var maxMajor, out var maxMinor, out var maxPatch))
            {
                if (CompareSemver(gameMajor, gameMinor, gamePatch, maxMajor, maxMinor, maxPatch) > 0)
                {
                    reason = $"Current game version '{currentGameVersion}' is above maxGameVersion '{manifest.maxGameVersion}'.";
                    return false;
                }
            }

            return true;
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

        private static void Reject(ModRuntimeCatalogLoadResult result, string directoryPath, string reason)
        {
            result.rejectedMods.Add(new ModRejectedPackInfo
            {
                directoryPath = directoryPath,
                reason = reason
            });

            result.messages.Add($"WARN: Rejected mod pack '{directoryPath}' ({reason}).");
        }
    }
}
