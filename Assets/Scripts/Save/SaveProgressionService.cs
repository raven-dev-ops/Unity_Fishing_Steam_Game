using System;
using System.Collections.Generic;
using UnityEngine;

namespace RavenDevOps.Fishing.Save
{
    public sealed class SaveProgressionService
    {
        public void NormalizeConfig(List<int> levelXpThresholds, List<ProgressionUnlockDefinition> progressionUnlocks)
        {
            if (levelXpThresholds == null)
            {
                throw new ArgumentNullException(nameof(levelXpThresholds));
            }

            if (progressionUnlocks == null)
            {
                throw new ArgumentNullException(nameof(progressionUnlocks));
            }

            if (levelXpThresholds.Count == 0)
            {
                levelXpThresholds.AddRange(ProgressionRules.Defaults);
            }

            for (var i = 0; i < levelXpThresholds.Count; i++)
            {
                levelXpThresholds[i] = Mathf.Max(0, levelXpThresholds[i]);
            }

            levelXpThresholds.Sort();
            for (var i = levelXpThresholds.Count - 1; i > 0; i--)
            {
                if (levelXpThresholds[i] == levelXpThresholds[i - 1])
                {
                    levelXpThresholds.RemoveAt(i);
                }
            }

            if (levelXpThresholds.Count == 0 || levelXpThresholds[0] != 0)
            {
                levelXpThresholds.Insert(0, 0);
            }

            if (progressionUnlocks.Count == 0)
            {
                SeedDefaultProgressionUnlocks(progressionUnlocks);
            }

            progressionUnlocks.RemoveAll(IsInvalidUnlockDefinition);
            progressionUnlocks.Sort((a, b) => a.level.CompareTo(b.level));
        }

        public void NormalizeProgressionData(
            SaveDataV1 saveData,
            List<int> levelXpThresholds,
            List<ProgressionUnlockDefinition> progressionUnlocks)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.progression ??= new ProgressionData();
            saveData.progression.totalXp = Mathf.Max(0, saveData.progression.totalXp);
            ProgressionRules.ResolveXpProgress(
                saveData.progression.totalXp,
                levelXpThresholds,
                out var resolvedLevel,
                out var xpIntoLevel,
                out var xpToNextLevel);

            saveData.progression.level = resolvedLevel;
            saveData.progression.xpIntoLevel = xpIntoLevel;
            saveData.progression.xpToNextLevel = xpToNextLevel;
            ApplyProgressionUnlocks(saveData, progressionUnlocks, minLevelInclusive: 1, maxLevelInclusive: resolvedLevel);
        }

        public bool ApplyProgressionXp(
            SaveDataV1 saveData,
            int xpAmount,
            List<int> levelXpThresholds,
            List<ProgressionUnlockDefinition> progressionUnlocks,
            out int previousLevel,
            out int newLevel)
        {
            previousLevel = saveData != null && saveData.progression != null
                ? Mathf.Max(1, saveData.progression.level)
                : 1;
            newLevel = previousLevel;
            if (saveData == null || xpAmount <= 0)
            {
                return false;
            }

            saveData.progression ??= new ProgressionData();
            saveData.progression.totalXp = Mathf.Max(0, saveData.progression.totalXp + xpAmount);
            ProgressionRules.ResolveXpProgress(
                saveData.progression.totalXp,
                levelXpThresholds,
                out var resolvedLevel,
                out var xpIntoLevel,
                out var xpToNextLevel);

            saveData.progression.level = resolvedLevel;
            saveData.progression.xpIntoLevel = xpIntoLevel;
            saveData.progression.xpToNextLevel = xpToNextLevel;
            newLevel = resolvedLevel;

            if (resolvedLevel <= previousLevel)
            {
                return false;
            }

            ApplyProgressionUnlocks(saveData, progressionUnlocks, previousLevel + 1, resolvedLevel);
            return true;
        }

        public string GetNextUnlockDescription(SaveDataV1 saveData, List<ProgressionUnlockDefinition> progressionUnlocks)
        {
            if (saveData == null)
            {
                return "All configured unlocks claimed";
            }

            saveData.progression ??= new ProgressionData();
            saveData.progression.unlockedContentIds ??= new List<string>();
            for (var i = 0; i < progressionUnlocks.Count; i++)
            {
                var unlock = progressionUnlocks[i];
                if (unlock == null || string.IsNullOrWhiteSpace(unlock.unlockId))
                {
                    continue;
                }

                if (saveData.progression.unlockedContentIds.Contains(unlock.unlockId))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(unlock.displayName)
                    ? unlock.unlockId
                    : unlock.displayName;
                return $"Level {Mathf.Max(1, unlock.level)}: {label}";
            }

            return "All configured unlocks claimed";
        }

        public bool IsContentUnlocked(SaveDataV1 saveData, List<ProgressionUnlockDefinition> progressionUnlocks, string contentId)
        {
            if (saveData == null || string.IsNullOrWhiteSpace(contentId))
            {
                return false;
            }

            saveData.progression ??= new ProgressionData();
            saveData.progression.unlockedContentIds ??= new List<string>();
            var isTrackedUnlock = false;
            for (var i = 0; i < progressionUnlocks.Count; i++)
            {
                var unlock = progressionUnlocks[i];
                if (unlock == null || string.IsNullOrWhiteSpace(unlock.unlockId))
                {
                    continue;
                }

                if (!string.Equals(unlock.unlockId, contentId, StringComparison.Ordinal))
                {
                    continue;
                }

                isTrackedUnlock = true;
                if (saveData.progression.unlockedContentIds.Contains(unlock.unlockId))
                {
                    return true;
                }
            }

            return !isTrackedUnlock;
        }

        public int GetUnlockLevel(List<ProgressionUnlockDefinition> progressionUnlocks, string contentId)
        {
            if (string.IsNullOrWhiteSpace(contentId))
            {
                return 1;
            }

            for (var i = 0; i < progressionUnlocks.Count; i++)
            {
                var unlock = progressionUnlocks[i];
                if (unlock == null || string.IsNullOrWhiteSpace(unlock.unlockId))
                {
                    continue;
                }

                if (string.Equals(unlock.unlockId, contentId, StringComparison.Ordinal))
                {
                    return Mathf.Max(1, unlock.level);
                }
            }

            return 1;
        }

        private static void ApplyProgressionUnlocks(
            SaveDataV1 saveData,
            List<ProgressionUnlockDefinition> progressionUnlocks,
            int minLevelInclusive,
            int maxLevelInclusive)
        {
            saveData.ownedShips ??= new List<string>();
            saveData.ownedHooks ??= new List<string>();
            saveData.progression ??= new ProgressionData();
            saveData.progression.unlockedContentIds ??= new List<string>();

            for (var i = 0; i < progressionUnlocks.Count; i++)
            {
                var unlock = progressionUnlocks[i];
                if (unlock == null || unlock.level < minLevelInclusive || unlock.level > maxLevelInclusive)
                {
                    continue;
                }

                if (!saveData.progression.unlockedContentIds.Contains(unlock.unlockId))
                {
                    saveData.progression.unlockedContentIds.Add(unlock.unlockId);
                    saveData.progression.lastUnlockId = unlock.unlockId;
                }

                ApplyUnlockOwnership(saveData, unlock);
            }
        }

        private static void ApplyUnlockOwnership(SaveDataV1 saveData, ProgressionUnlockDefinition unlock)
        {
            if (saveData == null || unlock == null || string.IsNullOrWhiteSpace(unlock.unlockId))
            {
                return;
            }

            // Progression unlocks gate availability in shops.
            // Ownership of ships/hooks is granted through shop purchase/upgrade flow.
        }

        private static bool IsInvalidUnlockDefinition(ProgressionUnlockDefinition unlock)
        {
            return unlock == null || unlock.level < 2 || string.IsNullOrWhiteSpace(unlock.unlockId);
        }

        private static void SeedDefaultProgressionUnlocks(List<ProgressionUnlockDefinition> progressionUnlocks)
        {
            progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 2,
                unlockType = ProgressionUnlockType.Hook,
                unlockId = "hook_lv2",
                displayName = "Hook Lv2"
            });
            progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 3,
                unlockType = ProgressionUnlockType.Ship,
                unlockId = "ship_lv2",
                displayName = "Ship Lv2"
            });
            progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 4,
                unlockType = ProgressionUnlockType.Hook,
                unlockId = "hook_lv3",
                displayName = "Hook Lv3"
            });
            progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 5,
                unlockType = ProgressionUnlockType.Ship,
                unlockId = "ship_lv3",
                displayName = "Ship Lv3"
            });
            progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 6,
                unlockType = ProgressionUnlockType.Hook,
                unlockId = "hook_lv4",
                displayName = "Hook Lv4"
            });
            progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 7,
                unlockType = ProgressionUnlockType.Hook,
                unlockId = "hook_lv5",
                displayName = "Hook Lv5"
            });
            progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 8,
                unlockType = ProgressionUnlockType.Ship,
                unlockId = "ship_lv4",
                displayName = "Ship Lv4"
            });
            progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 9,
                unlockType = ProgressionUnlockType.Ship,
                unlockId = "ship_lv5",
                displayName = "Ship Lv5"
            });
        }
    }
}
