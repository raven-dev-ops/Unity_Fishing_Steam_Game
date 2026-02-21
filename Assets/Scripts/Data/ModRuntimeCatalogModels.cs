using System;
using System.Collections.Generic;

namespace RavenDevOps.Fishing.Tools
{
    [Serializable]
    public sealed class ModCatalogDataV1
    {
        public List<ModFishDefinitionData> fishDefinitions = new List<ModFishDefinitionData>();
        public List<ModShipDefinitionData> shipDefinitions = new List<ModShipDefinitionData>();
        public List<ModHookDefinitionData> hookDefinitions = new List<ModHookDefinitionData>();
    }

    [Serializable]
    public sealed class ModFishDefinitionData
    {
        public string id = string.Empty;
        public int minDistanceTier;
        public int maxDistanceTier = 1;
        public float minDepth;
        public float maxDepth = 10f;
        public int rarityWeight = 1;
        public int baseValue = 10;
        public float minBiteDelaySeconds = 0.8f;
        public float maxBiteDelaySeconds = 2.5f;
        public float fightStamina = 5f;
        public float pullIntensity = 1f;
        public float escapeSeconds = 8f;
        public float minCatchWeightKg = 0.5f;
        public float maxCatchWeightKg = 2f;
        public string iconPath = string.Empty;

        [NonSerialized] public string sourceModId;
        [NonSerialized] public string sourceDirectory;
        [NonSerialized] public string resolvedIconPath;
    }

    [Serializable]
    public sealed class ModShipDefinitionData
    {
        public string id = string.Empty;
        public int price = 100;
        public int maxDistanceTier = 1;
        public float moveSpeed = 6f;
        public string iconPath = string.Empty;

        [NonSerialized] public string sourceModId;
        [NonSerialized] public string sourceDirectory;
        [NonSerialized] public string resolvedIconPath;
    }

    [Serializable]
    public sealed class ModHookDefinitionData
    {
        public string id = string.Empty;
        public int price = 100;
        public float maxDepth = 8f;
        public string iconPath = string.Empty;

        [NonSerialized] public string sourceModId;
        [NonSerialized] public string sourceDirectory;
        [NonSerialized] public string resolvedIconPath;
    }

    public sealed class ModAcceptedPackInfo
    {
        public string modId;
        public string modVersion;
        public string directoryPath;
    }

    public sealed class ModRejectedPackInfo
    {
        public string directoryPath;
        public string reason;
    }

    public sealed class ModRuntimeCatalogLoadResult
    {
        public bool modsEnabled;
        public string modsRootPath = string.Empty;
        public readonly List<ModAcceptedPackInfo> acceptedMods = new List<ModAcceptedPackInfo>();
        public readonly List<ModRejectedPackInfo> rejectedMods = new List<ModRejectedPackInfo>();
        public readonly List<string> messages = new List<string>();
        public readonly Dictionary<string, ModFishDefinitionData> fishById = new Dictionary<string, ModFishDefinitionData>();
        public readonly Dictionary<string, ModShipDefinitionData> shipById = new Dictionary<string, ModShipDefinitionData>();
        public readonly Dictionary<string, ModHookDefinitionData> hookById = new Dictionary<string, ModHookDefinitionData>();
    }
}
