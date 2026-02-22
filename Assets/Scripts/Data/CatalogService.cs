using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Data
{
    public sealed class CatalogService : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _gameConfig;
        [SerializeField] private AddressablesPilotCatalogLoader _addressablesPilotLoader;
        [SerializeField] private string _defaultConfigResourcePath = "Config/SO_GameConfig";

        private readonly Dictionary<string, FishDefinitionSO> _fishById = new Dictionary<string, FishDefinitionSO>();
        private readonly Dictionary<string, ShipDefinitionSO> _shipById = new Dictionary<string, ShipDefinitionSO>();
        private readonly Dictionary<string, HookDefinitionSO> _hookById = new Dictionary<string, HookDefinitionSO>();
        private readonly Dictionary<string, FishDefinitionSO> _phaseOneFishById = new Dictionary<string, FishDefinitionSO>();
        private readonly Dictionary<string, AudioClip> _phaseTwoAudioById = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Material> _phaseTwoEnvironmentById = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private bool _phaseOneFishLoadRequested;
        private bool _phaseOneFishLoadCompleted;
        private bool _phaseTwoAudioLoadRequested;
        private bool _phaseTwoEnvironmentLoadRequested;
        private bool _phaseTwoAudioLoadCompleted;
        private bool _phaseTwoEnvironmentLoadCompleted;

        public IReadOnlyDictionary<string, FishDefinitionSO> FishById => _fishById;
        public IReadOnlyDictionary<string, ShipDefinitionSO> ShipById => _shipById;
        public IReadOnlyDictionary<string, HookDefinitionSO> HookById => _hookById;
        public bool PhaseTwoAudioLoadCompleted => _phaseTwoAudioLoadCompleted;
        public bool PhaseTwoEnvironmentLoadCompleted => _phaseTwoEnvironmentLoadCompleted;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _addressablesPilotLoader, this, warnIfMissing: false);
            _addressablesPilotLoader ??= GetComponent<AddressablesPilotCatalogLoader>();
            EnsureGameConfigReference();
            RequestPhaseOneFishLoad();
            RequestPhaseTwoAudioLoad();
            RequestPhaseTwoEnvironmentLoad();
            Rebuild();
        }

        private void OnDestroy()
        {
            _phaseTwoAudioById.Clear();
            _phaseTwoEnvironmentById.Clear();
            RuntimeServiceRegistry.Unregister(this);
        }

        public void Rebuild()
        {
            _fishById.Clear();
            _shipById.Clear();
            _hookById.Clear();

            if (_gameConfig == null)
            {
                Debug.LogWarning("CatalogService: Missing GameConfigSO reference.");
                return;
            }

            BuildFishCatalog();
            BuildShipCatalog();
            BuildHookCatalog();
            ApplyPhaseOneFishCatalog();
        }

        public bool TryGetFish(string id, out FishDefinitionSO fish)
        {
            return _fishById.TryGetValue(id, out fish);
        }

        public bool TryGetShip(string id, out ShipDefinitionSO ship)
        {
            return _shipById.TryGetValue(id, out ship);
        }

        public bool TryGetHook(string id, out HookDefinitionSO hook)
        {
            return _hookById.TryGetValue(id, out hook);
        }

        public bool TryGetPhaseTwoAudioClip(string key, out AudioClip clip)
        {
            return _phaseTwoAudioById.TryGetValue(NormalizeLookupKey(key), out clip) && clip != null;
        }

        public bool TryGetPhaseTwoEnvironmentMaterial(string key, out Material material)
        {
            return _phaseTwoEnvironmentById.TryGetValue(NormalizeLookupKey(key), out material) && material != null;
        }

        private void EnsureGameConfigReference()
        {
            if (_gameConfig != null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_defaultConfigResourcePath))
            {
                return;
            }

            _gameConfig = Resources.Load<GameConfigSO>(_defaultConfigResourcePath);
            if (_gameConfig != null)
            {
                Debug.Log($"CatalogService: loaded default GameConfigSO from Resources/{_defaultConfigResourcePath}.");
            }
        }

        private void BuildFishCatalog()
        {
            if (_gameConfig.fishDefinitions == null)
            {
                return;
            }

            foreach (var fish in _gameConfig.fishDefinitions)
            {
                if (fish == null || string.IsNullOrWhiteSpace(fish.id))
                {
                    Debug.LogError("CatalogService: Fish definition missing or has empty id.");
                    continue;
                }

                if (_fishById.ContainsKey(fish.id))
                {
                    Debug.LogError($"CatalogService: Duplicate fish id '{fish.id}'.");
                    continue;
                }

                _fishById.Add(fish.id, fish);
            }
        }

        private void RequestPhaseOneFishLoad()
        {
            if (_addressablesPilotLoader == null || _phaseOneFishLoadRequested)
            {
                return;
            }

            _phaseOneFishLoadRequested = true;
            _addressablesPilotLoader.LoadFishDefinitionsAsync(HandlePhaseOneFishLoaded);
        }

        private void RequestPhaseTwoAudioLoad()
        {
            if (_addressablesPilotLoader == null || _phaseTwoAudioLoadRequested)
            {
                return;
            }

            _phaseTwoAudioLoadRequested = true;
            _addressablesPilotLoader.LoadPhaseTwoAudioClipsAsync(HandlePhaseTwoAudioLoaded);
        }

        private void RequestPhaseTwoEnvironmentLoad()
        {
            if (_addressablesPilotLoader == null || _phaseTwoEnvironmentLoadRequested)
            {
                return;
            }

            _phaseTwoEnvironmentLoadRequested = true;
            _addressablesPilotLoader.LoadPhaseTwoEnvironmentMaterialsAsync(HandlePhaseTwoEnvironmentLoaded);
        }

        private void HandlePhaseOneFishLoaded(List<FishDefinitionSO> fishDefinitions)
        {
            _phaseOneFishById.Clear();
            if (fishDefinitions != null)
            {
                for (var i = 0; i < fishDefinitions.Count; i++)
                {
                    var fish = fishDefinitions[i];
                    if (fish == null || string.IsNullOrWhiteSpace(fish.id))
                    {
                        continue;
                    }

                    _phaseOneFishById[fish.id] = fish;
                }
            }

            _phaseOneFishLoadCompleted = true;
            Debug.Log(
                $"CatalogService: phase-one fish load completed with {_phaseOneFishById.Count} fish definition(s). AddressablesRuntime={(_addressablesPilotLoader != null && _addressablesPilotLoader.IsAddressablesRuntimeAvailable)}.");
            Rebuild();
        }

        private void HandlePhaseTwoAudioLoaded(List<AudioClip> audioClips)
        {
            _phaseTwoAudioById.Clear();
            if (audioClips != null)
            {
                for (var i = 0; i < audioClips.Count; i++)
                {
                    var clip = audioClips[i];
                    if (clip == null || string.IsNullOrWhiteSpace(clip.name))
                    {
                        continue;
                    }

                    _phaseTwoAudioById[NormalizeLookupKey(clip.name)] = clip;
                }
            }

            var source = _addressablesPilotLoader != null && _addressablesPilotLoader.PhaseTwoAudioLoadUsedFallback
                ? "fallback"
                : "addressables";
            var error = _addressablesPilotLoader != null ? _addressablesPilotLoader.PhaseTwoAudioLoadError : string.Empty;
            _phaseTwoAudioLoadCompleted = true;
            Debug.Log($"CatalogService: phase-two audio load completed count={_phaseTwoAudioById.Count}, source={source}, error='{error}'.");
        }

        private void HandlePhaseTwoEnvironmentLoaded(List<Material> materials)
        {
            _phaseTwoEnvironmentById.Clear();
            if (materials != null)
            {
                for (var i = 0; i < materials.Count; i++)
                {
                    var material = materials[i];
                    if (material == null || string.IsNullOrWhiteSpace(material.name))
                    {
                        continue;
                    }

                    _phaseTwoEnvironmentById[NormalizeLookupKey(material.name)] = material;
                }
            }

            var source = _addressablesPilotLoader != null && _addressablesPilotLoader.PhaseTwoEnvironmentLoadUsedFallback
                ? "fallback"
                : "addressables";
            var error = _addressablesPilotLoader != null ? _addressablesPilotLoader.PhaseTwoEnvironmentLoadError : string.Empty;
            _phaseTwoEnvironmentLoadCompleted = true;
            Debug.Log($"CatalogService: phase-two environment load completed count={_phaseTwoEnvironmentById.Count}, source={source}, error='{error}'.");
        }

        private void ApplyPhaseOneFishCatalog()
        {
            if (!_phaseOneFishLoadCompleted || _phaseOneFishById.Count == 0)
            {
                return;
            }

            foreach (var pair in _phaseOneFishById)
            {
                _fishById[pair.Key] = pair.Value;
            }
        }

        private void BuildShipCatalog()
        {
            if (_gameConfig.shipDefinitions == null)
            {
                return;
            }

            foreach (var ship in _gameConfig.shipDefinitions)
            {
                if (ship == null || string.IsNullOrWhiteSpace(ship.id))
                {
                    Debug.LogError("CatalogService: Ship definition missing or has empty id.");
                    continue;
                }

                if (_shipById.ContainsKey(ship.id))
                {
                    Debug.LogError($"CatalogService: Duplicate ship id '{ship.id}'.");
                    continue;
                }

                _shipById.Add(ship.id, ship);
            }
        }

        private void BuildHookCatalog()
        {
            if (_gameConfig.hookDefinitions == null)
            {
                return;
            }

            foreach (var hook in _gameConfig.hookDefinitions)
            {
                if (hook == null || string.IsNullOrWhiteSpace(hook.id))
                {
                    Debug.LogError("CatalogService: Hook definition missing or has empty id.");
                    continue;
                }

                if (_hookById.ContainsKey(hook.id))
                {
                    Debug.LogError($"CatalogService: Duplicate hook id '{hook.id}'.");
                    continue;
                }

                _hookById.Add(hook.id, hook);
            }
        }

        private static string NormalizeLookupKey(string key)
        {
            return string.IsNullOrWhiteSpace(key)
                ? string.Empty
                : key.Trim().ToLowerInvariant();
        }
    }
}
