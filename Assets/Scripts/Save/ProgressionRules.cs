using System.Collections.Generic;
using UnityEngine;

namespace RavenDevOps.Fishing.Save
{
    public static class ProgressionRules
    {
        private static readonly int[] DefaultLevelXpThresholds = { 0, 100, 250, 450, 700, 1000 };

        public static IReadOnlyList<int> Defaults => DefaultLevelXpThresholds;

        public static int CalculateCatchXp(int distanceTier, float weightKg, int valueCopecs)
        {
            var baseXp = 10;
            var distanceBonus = Mathf.Max(0, distanceTier - 1) * 5;
            var weightBonus = Mathf.RoundToInt(Mathf.Max(0f, weightKg) * 3f);
            var valueBonus = Mathf.RoundToInt(Mathf.Max(0, valueCopecs) / 25f);
            return Mathf.Clamp(baseXp + distanceBonus + weightBonus + valueBonus, 5, 200);
        }

        public static int ResolveLevel(int totalXp, IReadOnlyList<int> levelThresholds)
        {
            var thresholds = levelThresholds ?? Defaults;
            if (thresholds.Count == 0)
            {
                return 1;
            }

            var clampedXp = Mathf.Max(0, totalXp);
            var level = 1;

            for (var i = 0; i < thresholds.Count; i++)
            {
                if (clampedXp >= thresholds[i])
                {
                    level = i + 1;
                }
                else
                {
                    break;
                }
            }

            return Mathf.Max(1, level);
        }

        public static void ResolveXpProgress(
            int totalXp,
            IReadOnlyList<int> levelThresholds,
            out int level,
            out int xpIntoLevel,
            out int xpToNextLevel)
        {
            var thresholds = levelThresholds ?? Defaults;
            if (thresholds.Count == 0)
            {
                level = 1;
                xpIntoLevel = Mathf.Max(0, totalXp);
                xpToNextLevel = 0;
                return;
            }

            level = ResolveLevel(totalXp, thresholds);
            var currentLevelIndex = Mathf.Clamp(level - 1, 0, thresholds.Count - 1);
            var currentThreshold = thresholds[currentLevelIndex];
            var nextThreshold = currentLevelIndex + 1 < thresholds.Count
                ? thresholds[currentLevelIndex + 1]
                : currentThreshold;

            var clampedTotalXp = Mathf.Max(0, totalXp);
            xpIntoLevel = Mathf.Max(0, clampedTotalXp - currentThreshold);
            xpToNextLevel = Mathf.Max(0, nextThreshold - currentThreshold);
        }
    }
}
