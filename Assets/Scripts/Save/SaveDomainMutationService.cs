using System;
using System.Collections.Generic;
using UnityEngine;

namespace RavenDevOps.Fishing.Save
{
    public sealed class SaveDomainMutationService
    {
        public void NormalizeCurrentDataEnvelope(
            SaveDataV1 saveData,
            Func<string> currentLocalDateProvider,
            int maxCatchLogEntries,
            int maxFishSaleHistoryEntries)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.ownedShips ??= new List<string>();
            saveData.ownedHooks ??= new List<string>();
            saveData.fishInventory ??= new List<FishInventoryEntry>();
            saveData.catchLog ??= new List<CatchLogEntry>();
            saveData.fishSaleHistory ??= new List<FishSaleHistoryEntry>();
            saveData.tutorialFlags ??= new TutorialFlags();
            saveData.dailyFishBonus ??= new DailyFishBonusState();
            saveData.fishingMarketQuest ??= new FishingMarketQuestState();
            saveData.stats ??= new SaveStats();
            saveData.progression ??= new ProgressionData();
            saveData.progression.unlockedContentIds ??= new List<string>();
            saveData.objectiveProgress ??= new ObjectiveProgressData();
            saveData.objectiveProgress.entries ??= new List<ObjectiveProgressEntry>();

            EnsureStarterOwnership(saveData);

            if (string.IsNullOrWhiteSpace(saveData.equippedShipId))
            {
                saveData.equippedShipId = saveData.ownedShips[0];
            }

            if (string.IsNullOrWhiteSpace(saveData.equippedHookId))
            {
                saveData.equippedHookId = saveData.ownedHooks[0];
            }

            if (string.IsNullOrWhiteSpace(saveData.careerStartLocalDate))
            {
                saveData.careerStartLocalDate = currentLocalDateProvider != null
                    ? currentLocalDateProvider()
                    : string.Empty;
            }

            if (string.IsNullOrWhiteSpace(saveData.lastLoginLocalDate))
            {
                saveData.lastLoginLocalDate = saveData.careerStartLocalDate;
            }

            TrimCatchLog(saveData.catchLog, maxCatchLogEntries);
            TrimFishSaleHistory(saveData.fishSaleHistory, maxFishSaleHistoryEntries);
        }

        public bool SetTutorialSeen(SaveDataV1 saveData, bool tutorialSeen)
        {
            if (saveData == null)
            {
                return false;
            }

            saveData.tutorialFlags ??= new TutorialFlags();
            var changed = saveData.tutorialFlags.tutorialSeen != tutorialSeen;
            if (tutorialSeen && saveData.tutorialFlags.introTutorialReplayRequested)
            {
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            saveData.tutorialFlags.tutorialSeen = tutorialSeen;
            if (tutorialSeen)
            {
                saveData.tutorialFlags.introTutorialReplayRequested = false;
            }

            return true;
        }

        public bool ShouldRunIntroTutorial(SaveDataV1 saveData)
        {
            if (saveData == null)
            {
                return true;
            }

            saveData.tutorialFlags ??= new TutorialFlags();
            return !saveData.tutorialFlags.tutorialSeen || saveData.tutorialFlags.introTutorialReplayRequested;
        }

        public bool RequestIntroTutorialReplay(SaveDataV1 saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            saveData.tutorialFlags ??= new TutorialFlags();
            var changed = saveData.tutorialFlags.tutorialSeen || !saveData.tutorialFlags.introTutorialReplayRequested;
            saveData.tutorialFlags.tutorialSeen = false;
            saveData.tutorialFlags.introTutorialReplayRequested = true;
            return changed;
        }

        public bool MarkIntroTutorialStarted(SaveDataV1 saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            saveData.tutorialFlags ??= new TutorialFlags();
            if (!saveData.tutorialFlags.introTutorialReplayRequested)
            {
                return false;
            }

            saveData.tutorialFlags.introTutorialReplayRequested = false;
            return true;
        }

        public bool ShouldRunFishingLoopTutorial(SaveDataV1 saveData)
        {
            if (saveData == null)
            {
                return true;
            }

            saveData.tutorialFlags ??= new TutorialFlags();
            return !saveData.tutorialFlags.fishingLoopTutorialCompleted || saveData.tutorialFlags.fishingLoopTutorialReplayRequested;
        }

        public bool RequestFishingLoopTutorialReplay(SaveDataV1 saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            saveData.tutorialFlags ??= new TutorialFlags();
            var changed = saveData.tutorialFlags.fishingLoopTutorialCompleted
                || saveData.tutorialFlags.fishingLoopTutorialSkipped
                || !saveData.tutorialFlags.fishingLoopTutorialReplayRequested;
            saveData.tutorialFlags.fishingLoopTutorialCompleted = false;
            saveData.tutorialFlags.fishingLoopTutorialSkipped = false;
            saveData.tutorialFlags.fishingLoopTutorialReplayRequested = true;
            return changed;
        }

        public bool MarkFishingLoopTutorialStarted(SaveDataV1 saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            saveData.tutorialFlags ??= new TutorialFlags();
            if (!saveData.tutorialFlags.fishingLoopTutorialReplayRequested)
            {
                return false;
            }

            saveData.tutorialFlags.fishingLoopTutorialReplayRequested = false;
            return true;
        }

        public bool CompleteFishingLoopTutorial(SaveDataV1 saveData, bool skipped)
        {
            if (saveData == null)
            {
                return false;
            }

            saveData.tutorialFlags ??= new TutorialFlags();
            var changed = !saveData.tutorialFlags.fishingLoopTutorialCompleted
                || saveData.tutorialFlags.fishingLoopTutorialSkipped != skipped
                || saveData.tutorialFlags.fishingLoopTutorialReplayRequested;
            saveData.tutorialFlags.fishingLoopTutorialCompleted = true;
            saveData.tutorialFlags.fishingLoopTutorialSkipped = skipped;
            saveData.tutorialFlags.fishingLoopTutorialReplayRequested = false;
            return changed;
        }

        public bool ResetProfileStats(SaveDataV1 saveData, List<int> levelXpThresholds)
        {
            if (saveData == null)
            {
                return false;
            }

            saveData.stats ??= new SaveStats();
            saveData.progression ??= new ProgressionData();
            saveData.objectiveProgress ??= new ObjectiveProgressData();
            saveData.objectiveProgress.entries ??= new List<ObjectiveProgressEntry>();
            saveData.progression.unlockedContentIds ??= new List<string>();

            saveData.copecs = 0;
            saveData.stats.totalFishCaught = 0;
            saveData.stats.farthestDistanceTier = 0;
            saveData.stats.totalTrips = 0;
            saveData.stats.totalPurchases = 0;
            saveData.stats.totalCatchValueCopecs = 0;
            saveData.progression.totalXp = 0;
            saveData.progression.level = 1;
            saveData.progression.xpIntoLevel = 0;
            saveData.progression.xpToNextLevel = levelXpThresholds != null && levelXpThresholds.Count > 1
                ? Mathf.Max(0, levelXpThresholds[1])
                : 0;
            saveData.progression.unlockedContentIds.Clear();
            saveData.progression.lastUnlockId = string.Empty;

            for (var i = 0; i < saveData.objectiveProgress.entries.Count; i++)
            {
                var entry = saveData.objectiveProgress.entries[i];
                if (entry == null)
                {
                    continue;
                }

                entry.currentCount = 0;
                entry.completed = false;
            }

            saveData.objectiveProgress.completedObjectives = 0;
            return true;
        }

        public bool ClearFishInventory(SaveDataV1 saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            saveData.fishInventory ??= new List<FishInventoryEntry>();
            if (saveData.fishInventory.Count == 0)
            {
                return false;
            }

            saveData.fishInventory.Clear();
            return true;
        }

        public bool EnsureStarterOwnership(SaveDataV1 saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            saveData.ownedShips ??= new List<string>();
            saveData.ownedHooks ??= new List<string>();

            var changed = false;
            if (!saveData.ownedShips.Contains("ship_lv1"))
            {
                saveData.ownedShips.Add("ship_lv1");
                changed = true;
            }

            if (!saveData.ownedHooks.Contains("hook_lv1"))
            {
                saveData.ownedHooks.Add("hook_lv1");
                changed = true;
            }

            return changed;
        }

        public CatchLogEntry AppendCatchLog(
            SaveDataV1 saveData,
            int maxCatchLogEntries,
            string fishId,
            int distanceTier,
            bool landed,
            float depthMeters,
            float weightKg,
            int valueCopecs,
            string failReason,
            string sessionId,
            DateTime timestampUtc)
        {
            if (saveData == null)
            {
                return null;
            }

            saveData.catchLog ??= new List<CatchLogEntry>();
            var entry = new CatchLogEntry
            {
                fishId = fishId ?? string.Empty,
                distanceTier = Mathf.Max(1, distanceTier),
                depthMeters = Mathf.Max(0f, depthMeters),
                weightKg = Mathf.Max(0f, weightKg),
                valueCopecs = Mathf.Max(0, valueCopecs),
                timestampUtc = timestampUtc.ToString("O"),
                sessionId = sessionId ?? string.Empty,
                landed = landed,
                failReason = failReason ?? string.Empty
            };

            saveData.catchLog.Add(entry);
            TrimCatchLog(saveData.catchLog, maxCatchLogEntries);
            return entry;
        }

        public static void TrimCatchLog(List<CatchLogEntry> log, int maxCatchLogEntries)
        {
            if (log == null)
            {
                return;
            }

            if (log.Count <= Mathf.Max(1, maxCatchLogEntries))
            {
                return;
            }

            var removeCount = log.Count - Mathf.Max(1, maxCatchLogEntries);
            log.RemoveRange(0, removeCount);
        }

        public static void TrimFishSaleHistory(List<FishSaleHistoryEntry> history, int maxFishSaleHistoryEntries)
        {
            if (history == null)
            {
                return;
            }

            if (history.Count <= Mathf.Max(1, maxFishSaleHistoryEntries))
            {
                return;
            }

            var removeCount = history.Count - Mathf.Max(1, maxFishSaleHistoryEntries);
            history.RemoveRange(0, removeCount);
        }

        public static int CountFishInventory(List<FishInventoryEntry> fishInventory)
        {
            if (fishInventory == null || fishInventory.Count == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < fishInventory.Count; i++)
            {
                var entry = fishInventory[i];
                if (entry == null)
                {
                    continue;
                }

                count += Mathf.Max(0, entry.count);
            }

            return count;
        }
    }
}
