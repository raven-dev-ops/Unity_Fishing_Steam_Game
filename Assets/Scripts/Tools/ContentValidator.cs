using System.Collections.Generic;
using RavenDevOps.Fishing.Data;
using UnityEngine;

namespace RavenDevOps.Fishing.Tools
{
    public static class ContentValidator
    {
        public static List<string> Validate(GameConfigSO config)
        {
            var messages = new List<string>();
            if (config == null)
            {
                messages.Add("ERROR: Missing GameConfigSO reference.");
                return messages;
            }

            var ids = new HashSet<string>();

            ValidateFish(config, ids, messages);
            ValidateShips(config, ids, messages);
            ValidateHooks(config, ids, messages);

            return messages;
        }

        private static void ValidateFish(GameConfigSO config, HashSet<string> ids, List<string> messages)
        {
            if (config.fishDefinitions == null) return;
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

                if (fish.baseValue <= 0)
                {
                    messages.Add($"WARN: Fish '{fish.id}' baseValue <= 0.");
                }
            }
        }

        private static void ValidateShips(GameConfigSO config, HashSet<string> ids, List<string> messages)
        {
            if (config.shipDefinitions == null) return;
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
            }
        }

        private static void ValidateHooks(GameConfigSO config, HashSet<string> ids, List<string> messages)
        {
            if (config.hookDefinitions == null) return;
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
            }
        }

        private static void ValidateId(string id, string label, HashSet<string> ids, List<string> messages)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                messages.Add($"ERROR: {label} has empty id.");
                return;
            }

            if (!ids.Add(id))
            {
                messages.Add($"ERROR: Duplicate id '{id}'.");
            }
        }
    }
}
