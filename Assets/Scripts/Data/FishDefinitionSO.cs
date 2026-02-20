using UnityEngine;

namespace RavenDevOps.Fishing.Data
{
    [CreateAssetMenu(menuName = "Raven/Fish Definition", fileName = "SO_FishDefinition")]
    public sealed class FishDefinitionSO : ScriptableObject
    {
        public string id;
        public Sprite icon;
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
    }
}
