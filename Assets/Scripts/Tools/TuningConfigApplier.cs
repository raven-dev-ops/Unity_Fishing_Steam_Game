using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Fishing;
using UnityEngine;

namespace RavenDevOps.Fishing.Tools
{
    public sealed class TuningConfigApplier : MonoBehaviour
    {
        [SerializeField] private TuningConfigSO _config;
        [SerializeField] private string _configResourcePath = "Config/SO_TuningConfig";
        [SerializeField] private bool _createRuntimeDefaultWhenMissing = true;
        [SerializeField] private WaveAnimator _waveAnimator;
        [SerializeField] private ShipMovementController _shipMovement;
        [SerializeField] private HookMovementController _hookMovement;
        [SerializeField] private FishSpawner _fishSpawner;
        [SerializeField] private SellSummaryCalculator _sellSummaryCalculator;

        private TuningConfigSO _runtimeFallbackConfig;

        public void Configure(
            TuningConfigSO config,
            WaveAnimator waveAnimator,
            ShipMovementController shipMovement,
            HookMovementController hookMovement,
            FishSpawner fishSpawner,
            SellSummaryCalculator sellSummaryCalculator)
        {
            if (config != null)
            {
                _config = config;
            }

            _waveAnimator = waveAnimator;
            _shipMovement = shipMovement;
            _hookMovement = hookMovement;
            _fishSpawner = fishSpawner;
            _sellSummaryCalculator = sellSummaryCalculator;
        }

        private void Awake()
        {
            EnsureDependencies();
            LoadConfigIfNeeded();
            ApplyNow();
        }

        public bool ApplyNow()
        {
            EnsureDependencies();
            LoadConfigIfNeeded();
            var activeConfig = ResolveActiveConfig();
            if (activeConfig == null)
            {
                return false;
            }

            _waveAnimator?.SetWaveSpeeds(activeConfig.waveSpeedA, activeConfig.waveSpeedB);
            _shipMovement?.SetSpeedMultiplier(activeConfig.shipSpeedMultiplier);
            _hookMovement?.SetSpeedMultiplier(activeConfig.hookSpeedMultiplier);
            _fishSpawner?.SetSpawnRate(activeConfig.spawnRatePerMinute);
            _sellSummaryCalculator?.SetDistanceTierStep(activeConfig.distanceTierSellStep);
            return true;
        }

        private void EnsureDependencies()
        {
            RuntimeServiceRegistry.Resolve(ref _waveAnimator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _shipMovement, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _hookMovement, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _fishSpawner, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _sellSummaryCalculator, this, warnIfMissing: false);
        }

        private void LoadConfigIfNeeded()
        {
            if (_config != null || string.IsNullOrWhiteSpace(_configResourcePath))
            {
                return;
            }

            _config = Resources.Load<TuningConfigSO>(_configResourcePath);
        }

        private TuningConfigSO ResolveActiveConfig()
        {
            if (_config != null)
            {
                return _config;
            }

            if (!_createRuntimeDefaultWhenMissing)
            {
                return null;
            }

            if (_runtimeFallbackConfig == null)
            {
                _runtimeFallbackConfig = ScriptableObject.CreateInstance<TuningConfigSO>();
            }

            return _runtimeFallbackConfig;
        }
    }
}
