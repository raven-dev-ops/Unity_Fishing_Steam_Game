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

        private void Awake()
        {
            _waveAnimator ??= FindObjectOfType<WaveAnimator>();
            _fishSpawner ??= FindObjectOfType<FishSpawner>();
            _saveManager ??= FindObjectOfType<SaveManager>();
        }

        private void Update()
        {
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
                if (_saveManager != null)
                {
                    if (!_saveManager.Current.ownedShips.Contains("ship_lv1")) _saveManager.Current.ownedShips.Add("ship_lv1");
                    if (!_saveManager.Current.ownedHooks.Contains("hook_lv1")) _saveManager.Current.ownedHooks.Add("hook_lv1");
                    _saveManager.Save();
                }
            }

            if (GUILayout.Button("Spawn Fish Test (roll only)"))
            {
                _fishSpawner?.RollFish(1, 2f);
            }

            if (GUILayout.Button("Clear Inventory"))
            {
                if (_saveManager != null)
                {
                    _saveManager.Current.fishInventory.Clear();
                    _saveManager.Save();
                }
            }

            GUILayout.EndArea();
#endif
        }
    }
}
