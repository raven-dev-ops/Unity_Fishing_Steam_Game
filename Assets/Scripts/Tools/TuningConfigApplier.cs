using RavenDevOps.Fishing.Core;
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

            RuntimeServiceRegistry.Resolve(ref _waveAnimator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _shipMovement, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _hookMovement, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _fishSpawner, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _sellSummaryCalculator, this, warnIfMissing: false);

            _waveAnimator?.SetWaveSpeeds(_config.waveSpeedA, _config.waveSpeedB);
            _shipMovement?.SetSpeedMultiplier(_config.shipSpeedMultiplier);
            _hookMovement?.SetSpeedMultiplier(_config.hookSpeedMultiplier);
            _fishSpawner?.SetSpawnRate(_config.spawnRatePerMinute);
            _sellSummaryCalculator?.SetDistanceTierStep(_config.distanceTierSellStep);
        }
    }
}
