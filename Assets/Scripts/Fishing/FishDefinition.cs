using System;

namespace RavenDevOps.Fishing.Fishing
{
    [Serializable]
    public sealed class FishDefinition
    {
        public string id;
        public int minDistanceTier;
        public int maxDistanceTier = 1;
        public float minDepth;
        public float maxDepth = 10f;
        public int rarityWeight = 1;
        public int baseValue = 10;
    }
}
