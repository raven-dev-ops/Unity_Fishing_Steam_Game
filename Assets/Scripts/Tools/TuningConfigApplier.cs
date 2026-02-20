using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Fishing;
using UnityEngine;

namespace RavenDevOps.Fishing.Tools
{
    public sealed class TuningConfigApplier : MonoBehaviour
    {
        [SerializeField] private TuningConfigSO _config;
        [SerializeField] private WaveAnimator _waveAnimator;
        [SerializeField] private ShipMovementController _shipMovement;
        [SerializeField] private HookMovementController _hookMovement;
        [SerializeField] private FishSpawner _fishSpawner;
        [SerializeField] private SellSummaryCalculator _sellSummaryCalculator;

        private void Awake()
        {
            if (_config == null)
            {
                return;
            }

            _waveAnimator ??= FindObjectOfType<WaveAnimator>();
            _shipMovement ??= FindObjectOfType<ShipMovementController>();
            _hookMovement ??= FindObjectOfType<HookMovementController>();
            _fishSpawner ??= FindObjectOfType<FishSpawner>();
            _sellSummaryCalculator ??= FindObjectOfType<SellSummaryCalculator>();

            _waveAnimator?.SetWaveSpeeds(_config.waveSpeedA, _config.waveSpeedB);
            _shipMovement?.SetSpeedMultiplier(_config.shipSpeedMultiplier);
            _hookMovement?.SetSpeedMultiplier(_config.hookSpeedMultiplier);
            _fishSpawner?.SetSpawnRate(_config.spawnRatePerMinute);
            _sellSummaryCalculator?.SetDistanceTierStep(_config.distanceTierSellStep);
        }
    }
}
