using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Fishing;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Tools
{
    public sealed class DebugPanelController : MonoBehaviour
    {
        [SerializeField] private bool _visible;
        [SerializeField] private float _waveA = 0.3f;
        [SerializeField] private float _waveB = 0.6f;
        [SerializeField] private float _spawnRate = 6f;

        [SerializeField] private WaveAnimator _waveAnimator;
        [SerializeField] private FishSpawner _fishSpawner;
        [SerializeField] private SaveManager _saveManager;

        public void Configure(WaveAnimator waveAnimator, FishSpawner fishSpawner, SaveManager saveManager)
        {
            _waveAnimator = waveAnimator;
            _fishSpawner = fishSpawner;
            _saveManager = saveManager;

            if (_fishSpawner != null)
            {
                _spawnRate = Mathf.Max(0f, _fishSpawner.SpawnRatePerMinute);
            }
        }

        private void Awake()
        {
            EnsureDependencies();
        }

        private void Update()
        {
            EnsureDependencies();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                _visible = !_visible;
            }
#else
            _visible = false;
#endif
        }

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_visible)
            {
                return;
            }

            EnsureDependencies();

            GUILayout.BeginArea(new Rect(16f, 16f, 320f, 340f), "DEV Debug Panel", GUI.skin.window);

            GUILayout.Label($"Wave A: {_waveA:0.00}");
            _waveA = GUILayout.HorizontalSlider(_waveA, 0f, 2f);

            GUILayout.Label($"Wave B: {_waveB:0.00}");
            _waveB = GUILayout.HorizontalSlider(_waveB, 0f, 2f);

            GUILayout.Label($"Spawn Rate: {_spawnRate:0.0}");
            _spawnRate = GUILayout.HorizontalSlider(_spawnRate, 0f, 30f);

            if (GUILayout.Button("Apply Wave/Spawn Tuning"))
            {
                _waveAnimator?.SetWaveSpeeds(_waveA, _waveB);
                _fishSpawner?.SetSpawnRate(_spawnRate);
            }

            if (GUILayout.Button("Add 100 Copecs"))
            {
                _saveManager?.AddCopecs(100);
            }

            if (GUILayout.Button("Unlock Starter Ship/Hook"))
            {
                _saveManager?.EnsureStarterOwnership();
            }

            if (GUILayout.Button("Spawn Fish Test (roll only)"))
            {
                _fishSpawner?.RollFish(1, 2f);
            }

            if (GUILayout.Button("Clear Inventory"))
            {
                _saveManager?.ClearFishInventory();
            }

            GUILayout.EndArea();
#endif
        }

        private void EnsureDependencies()
        {
            if (_waveAnimator == null)
            {
                RuntimeServiceRegistry.Resolve(ref _waveAnimator, this, warnIfMissing: false);
            }

            if (_fishSpawner == null)
            {
                RuntimeServiceRegistry.Resolve(ref _fishSpawner, this, warnIfMissing: false);
            }

            if (_saveManager == null)
            {
                RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            }
        }
    }
}
