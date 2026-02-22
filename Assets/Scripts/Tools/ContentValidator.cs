using System.Collections.Generic;
using System.Text.RegularExpressions;
using RavenDevOps.Fishing.Data;
using UnityEngine;

namespace RavenDevOps.Fishing.Tools
{
    public static class ContentValidator
    {
        private static readonly Regex IdPattern = new Regex("^[a-z0-9_]+$");

        public static List<string> Validate(GameConfigSO config)
        {
            var messages = new List<string>();
            if (config == null)
            {
                messages.Add("ERROR: Missing GameConfigSO reference.");
                return messages;
            }

            var ids = new HashSet<string>();

            ValidateCatalogPresence(config, messages);
            ValidateFish(config, ids, messages);
            ValidateShips(config, ids, messages);
            ValidateHooks(config, ids, messages);

            return messages;
        }

        public static int CountErrors(List<string> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return 0;
            }

            var errorCount = 0;
            foreach (var message in messages)
            {
                if (message != null && message.StartsWith("ERROR"))
                {
                    errorCount++;
                }
            }

            return errorCount;
        }

        private static void ValidateCatalogPresence(GameConfigSO config, List<string> messages)
        {
            if (config.fishDefinitions == null || config.fishDefinitions.Length == 0)
            {
                messages.Add("WARN: No fish definitions configured.");
            }

            if (config.shipDefinitions == null || config.shipDefinitions.Length == 0)
            {
                messages.Add("WARN: No ship definitions configured.");
            }

            if (config.hookDefinitions == null || config.hookDefinitions.Length == 0)
            {
                messages.Add("WARN: No hook definitions configured.");
            }
        }

        private static void ValidateFish(GameConfigSO config, HashSet<string> ids, List<string> messages)
        {
            if (config.fishDefinitions == null)
            {
                return;
            }

            foreach (var fish in config.fishDefinitions)
            {
                if (fish == null)
                {
                    messages.Add("ERROR: Fish entry is null.");
                    continue;
                }

                ValidateId(fish.id, "Fish", ids, messages);

                if (fish.icon == null)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' missing icon/sprite.");
                }

                if (fish.minDistanceTier > fish.maxDistanceTier)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' invalid distance tier range.");
                }

                if (fish.minDepth > fish.maxDepth)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' invalid depth range.");
                }

                if (fish.minDistanceTier < 0)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' has negative minDistanceTier.");
                }

                if (fish.maxDistanceTier < 0)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' has negative maxDistanceTier.");
                }

                if (fish.minDepth < 0f)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' has negative minDepth.");
                }

                if (fish.maxDepth < 0f)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' has negative maxDepth.");
                }

                if (fish.baseValue <= 0)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' has non-positive baseValue.");
                }

                if (fish.rarityWeight <= 0)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' has non-positive rarityWeight.");
                }

                if (fish.minBiteDelaySeconds < 0f || fish.maxBiteDelaySeconds < 0f || fish.minBiteDelaySeconds > fish.maxBiteDelaySeconds)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' invalid bite delay range.");
                }

                if (fish.fightStamina <= 0f)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' has non-positive fightStamina.");
                }

                if (fish.pullIntensity <= 0f)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' has non-positive pullIntensity.");
                }

                if (fish.escapeSeconds <= 0f)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' has non-positive escapeSeconds.");
                }

                if (fish.minCatchWeightKg <= 0f || fish.maxCatchWeightKg <= 0f || fish.minCatchWeightKg > fish.maxCatchWeightKg)
                {
                    messages.Add($"ERROR: Fish '{fish.id}' invalid catch weight range.");
                }
            }
        }

        private static void ValidateShips(GameConfigSO config, HashSet<string> ids, List<string> messages)
        {
            if (config.shipDefinitions == null)
            {
                return;
            }

            foreach (var ship in config.shipDefinitions)
            {
                if (ship == null)
                {
                    messages.Add("ERROR: Ship entry is null.");
                    continue;
                }

                ValidateId(ship.id, "Ship", ids, messages);

                if (ship.icon == null)
                {
                    messages.Add($"ERROR: Ship '{ship.id}' missing icon/sprite.");
                }

                if (ship.moveSpeed <= 0f)
                {
                    messages.Add($"ERROR: Ship '{ship.id}' has non-positive moveSpeed.");
                }

                if (ship.price < 0)
                {
                    messages.Add($"ERROR: Ship '{ship.id}' has negative price.");
                }

                if (ship.maxDistanceTier < 0)
                {
                    messages.Add($"ERROR: Ship '{ship.id}' has negative maxDistanceTier.");
                }

                if (ship.cargoCapacity <= 0)
                {
                    messages.Add($"ERROR: Ship '{ship.id}' has non-positive cargoCapacity.");
                }
            }
        }

        private static void ValidateHooks(GameConfigSO config, HashSet<string> ids, List<string> messages)
        {
            if (config.hookDefinitions == null)
            {
                return;
            }

            foreach (var hook in config.hookDefinitions)
            {
                if (hook == null)
                {
                    messages.Add("ERROR: Hook entry is null.");
                    continue;
                }

                ValidateId(hook.id, "Hook", ids, messages);

                if (hook.icon == null)
                {
                    messages.Add($"ERROR: Hook '{hook.id}' missing icon/sprite.");
                }

                if (hook.maxDepth <= 0f)
                {
                    messages.Add($"ERROR: Hook '{hook.id}' has non-positive maxDepth.");
                }

                if (hook.price < 0)
                {
                    messages.Add($"ERROR: Hook '{hook.id}' has negative price.");
                }
            }
        }

        private static void ValidateId(string id, string label, HashSet<string> ids, List<string> messages)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                messages.Add($"ERROR: {label} has empty id.");
                return;
            }

            if (!IdPattern.IsMatch(id))
            {
                messages.Add($"ERROR: {label} id '{id}' must match pattern ^[a-z0-9_]+$.");
            }

            if (!ids.Add(id))
            {
                messages.Add($"ERROR: Duplicate id '{id}'.");
            }
        }
    }
}
