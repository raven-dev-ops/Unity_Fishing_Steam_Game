using UnityEngine;

namespace RavenDevOps.Fishing.Tools
{
    [CreateAssetMenu(menuName = "Raven/Tuning Config", fileName = "SO_TuningConfig")]
    public sealed class TuningConfigSO : ScriptableObject
    {
        public float waveSpeedA = 0.3f;
        public float waveSpeedB = 0.6f;
        public float shipSpeedMultiplier = 1f;
        public float hookSpeedMultiplier = 1f;
        public float spawnRatePerMinute = 6f;
        public float distanceTierSellStep = 0.25f;
    }
}
