using System;
using System.Collections.Generic;

namespace RavenDevOps.Fishing.Save
{
    [Serializable]
    public sealed class SaveDataV1
    {
        public int saveVersion = 1;
        public int copecs = 0;

        public string equippedShipId = "ship_lv1";
        public string equippedHookId = "hook_lv1";

        public List<string> ownedShips = new List<string> { "ship_lv1" };
        public List<string> ownedHooks = new List<string> { "hook_lv1" };

        public List<FishInventoryEntry> fishInventory = new List<FishInventoryEntry>();
        public List<CatchLogEntry> catchLog = new List<CatchLogEntry>();

        public TutorialFlags tutorialFlags = new TutorialFlags();

        public string careerStartLocalDate = string.Empty;
        public string lastLoginLocalDate = string.Empty;

        public SaveStats stats = new SaveStats();
        public ProgressionData progression = new ProgressionData();
        public ObjectiveProgressData objectiveProgress = new ObjectiveProgressData();
    }

    [Serializable]
    public sealed class FishInventoryEntry
    {
        public string fishId;
        public int distanceTier;
        public int count;
    }

    [Serializable]
    public sealed class CatchLogEntry
    {
        public string fishId;
        public int distanceTier;
        public float weightKg;
        public int valueCopecs;
        public string timestampUtc;
        public string sessionId;
        public bool landed;
        public string failReason;
    }

    [Serializable]
    public sealed class TutorialFlags
    {
        public bool tutorialSeen;
        public bool fishingLoopTutorialCompleted;
        public bool fishingLoopTutorialSkipped;
        public bool fishingLoopTutorialReplayRequested;
    }

    [Serializable]
    public sealed class SaveStats
    {
        public int totalFishCaught;
        public int farthestDistanceTier;
        public int totalTrips;
        public int totalPurchases;
        public int totalCatchValueCopecs;
    }

    [Serializable]
    public sealed class ProgressionData
    {
        public int level = 1;
        public int totalXp;
        public int xpIntoLevel;
        public int xpToNextLevel = 100;
        public List<string> unlockedContentIds = new List<string>();
        public string lastUnlockId = string.Empty;
    }

    [Serializable]
    public sealed class ObjectiveProgressData
    {
        public List<ObjectiveProgressEntry> entries = new List<ObjectiveProgressEntry>();
        public int completedObjectives;
    }

    [Serializable]
    public sealed class ObjectiveProgressEntry
    {
        public string id;
        public string description;
        public int currentCount;
        public int targetCount;
        public int rewardCopecs;
        public bool completed;
    }
}
