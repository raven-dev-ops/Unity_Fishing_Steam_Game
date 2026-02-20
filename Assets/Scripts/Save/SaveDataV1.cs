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
    }

    [Serializable]
    public sealed class SaveStats
    {
        public int totalFishCaught;
        public int farthestDistanceTier;
        public int totalTrips;
    }
}
