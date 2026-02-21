using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Tools;
using UnityEngine;

namespace RavenDevOps.Fishing.Data
{
    public sealed class CatalogService : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _gameConfig;
        [SerializeField] private AddressablesPilotCatalogLoader _addressablesPilotLoader;
        [SerializeField] private ModRuntimeCatalogService _modCatalogService;
        private static MethodInfo _imageConversionLoadImageMethod;
        private static bool _imageConversionLookupCompleted;

        private readonly Dictionary<string, FishDefinitionSO> _fishById = new Dictionary<string, FishDefinitionSO>();
        private readonly Dictionary<string, ShipDefinitionSO> _shipById = new Dictionary<string, ShipDefinitionSO>();
        private readonly Dictionary<string, HookDefinitionSO> _hookById = new Dictionary<string, HookDefinitionSO>();
        private readonly Dictionary<string, FishDefinitionSO> _phaseOneFishById = new Dictionary<string, FishDefinitionSO>();
        private readonly Dictionary<string, AudioClip> _phaseTwoAudioById = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Material> _phaseTwoEnvironmentById = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private readonly List<UnityEngine.Object> _generatedRuntimeObjects = new List<UnityEngine.Object>();
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
            RuntimeServiceRegistry.Resolve(ref _modCatalogService, this, warnIfMissing: false);
            SubscribeToModCatalog();
            RequestPhaseOneFishLoad();
            RequestPhaseTwoAudioLoad();
            RequestPhaseTwoEnvironmentLoad();
            Rebuild();
        }

        private void OnDestroy()
        {
            UnsubscribeFromModCatalog();
            CleanupGeneratedRuntimeObjects();
            _phaseTwoAudioById.Clear();
            _phaseTwoEnvironmentById.Clear();
            RuntimeServiceRegistry.Unregister(this);
        }

        public void SetModCatalogService(ModRuntimeCatalogService modCatalogService)
        {
            if (_modCatalogService == modCatalogService)
            {
                return;
            }

            UnsubscribeFromModCatalog();
            _modCatalogService = modCatalogService;
            SubscribeToModCatalog();
            Rebuild();
        }

        public void Rebuild()
        {
            CleanupGeneratedRuntimeObjects();
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
            ApplyModCatalogOverrides();
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

        private void ApplyModCatalogOverrides()
        {
            if (_modCatalogService == null || !_modCatalogService.ModsEnabled)
            {
                return;
            }

            var loadResult = _modCatalogService.LastLoadResult;
            if (loadResult == null)
            {
                return;
            }

            foreach (var pair in loadResult.fishById)
            {
                var existing = _fishById.TryGetValue(pair.Key, out var existingFish) ? existingFish : null;
                var runtimeFish = BuildRuntimeFishDefinition(pair.Value, existing);
                if (runtimeFish == null)
                {
                    continue;
                }

                _fishById[pair.Key] = runtimeFish;
            }

            foreach (var pair in loadResult.shipById)
            {
                var existing = _shipById.TryGetValue(pair.Key, out var existingShip) ? existingShip : null;
                var runtimeShip = BuildRuntimeShipDefinition(pair.Value, existing);
                if (runtimeShip == null)
                {
                    continue;
                }

                _shipById[pair.Key] = runtimeShip;
            }

            foreach (var pair in loadResult.hookById)
            {
                var existing = _hookById.TryGetValue(pair.Key, out var existingHook) ? existingHook : null;
                var runtimeHook = BuildRuntimeHookDefinition(pair.Value, existing);
                if (runtimeHook == null)
                {
                    continue;
                }

                _hookById[pair.Key] = runtimeHook;
            }

            if (loadResult.acceptedMods.Count > 0)
            {
                Debug.Log(
                    $"CatalogService: applied mod overrides from {loadResult.acceptedMods.Count} pack(s): fish={loadResult.fishById.Count}, ships={loadResult.shipById.Count}, hooks={loadResult.hookById.Count}.");
            }
        }

        private FishDefinitionSO BuildRuntimeFishDefinition(ModFishDefinitionData source, FishDefinitionSO fallback)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.id))
            {
                return null;
            }

            var fish = CreateRuntimeAsset<FishDefinitionSO>();
            fish.id = source.id;
            fish.minDistanceTier = source.minDistanceTier;
            fish.maxDistanceTier = source.maxDistanceTier;
            fish.minDepth = source.minDepth;
            fish.maxDepth = source.maxDepth;
            fish.rarityWeight = source.rarityWeight;
            fish.baseValue = source.baseValue;
            fish.minBiteDelaySeconds = source.minBiteDelaySeconds;
            fish.maxBiteDelaySeconds = source.maxBiteDelaySeconds;
            fish.fightStamina = source.fightStamina;
            fish.pullIntensity = source.pullIntensity;
            fish.escapeSeconds = source.escapeSeconds;
            fish.minCatchWeightKg = source.minCatchWeightKg;
            fish.maxCatchWeightKg = source.maxCatchWeightKg;
            fish.icon = ResolveIcon(source.resolvedIconPath, fallback != null ? fallback.icon : null);
            return fish;
        }

        private ShipDefinitionSO BuildRuntimeShipDefinition(ModShipDefinitionData source, ShipDefinitionSO fallback)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.id))
            {
                return null;
            }

            var ship = CreateRuntimeAsset<ShipDefinitionSO>();
            ship.id = source.id;
            ship.price = source.price;
            ship.maxDistanceTier = source.maxDistanceTier;
            ship.moveSpeed = source.moveSpeed;
            ship.icon = ResolveIcon(source.resolvedIconPath, fallback != null ? fallback.icon : null);
            return ship;
        }

        private HookDefinitionSO BuildRuntimeHookDefinition(ModHookDefinitionData source, HookDefinitionSO fallback)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.id))
            {
                return null;
            }

            var hook = CreateRuntimeAsset<HookDefinitionSO>();
            hook.id = source.id;
            hook.price = source.price;
            hook.maxDepth = source.maxDepth;
            hook.icon = ResolveIcon(source.resolvedIconPath, fallback != null ? fallback.icon : null);
            return hook;
        }

        private T CreateRuntimeAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _generatedRuntimeObjects.Add(asset);
            return asset;
        }

        private Sprite ResolveIcon(string resolvedPath, Sprite fallback)
        {
            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                return fallback;
            }

            if (!File.Exists(resolvedPath))
            {
                return fallback;
            }

            try
            {
                var bytes = File.ReadAllBytes(resolvedPath);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!TryDecodeTexture(texture, bytes))
                {
                    DestroyRuntimeObject(texture);
                    return fallback;
                }

                var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                _generatedRuntimeObjects.Add(texture);
                _generatedRuntimeObjects.Add(sprite);
                return sprite;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CatalogService: failed to load icon at '{resolvedPath}' ({ex.Message}).");
                return fallback;
            }
        }

        private static bool TryDecodeTexture(Texture2D texture, byte[] bytes)
        {
            if (texture == null || bytes == null || bytes.Length == 0)
            {
                return false;
            }

            if (!_imageConversionLookupCompleted)
            {
                _imageConversionLookupCompleted = true;
                var imageConversionType = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule", throwOnError: false);
                if (imageConversionType != null)
                {
                    _imageConversionLoadImageMethod = imageConversionType.GetMethod(
                        "LoadImage",
                        BindingFlags.Public | BindingFlags.Static,
                        binder: null,
                        types: new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) },
                        modifiers: null);
                }
            }

            if (_imageConversionLoadImageMethod == null)
            {
                return false;
            }

            try
            {
                var decodeResult = _imageConversionLoadImageMethod.Invoke(null, new object[] { texture, bytes, false });
                return decodeResult is bool loaded && loaded;
            }
            catch
            {
                return false;
            }
        }

        private void SubscribeToModCatalog()
        {
            if (_modCatalogService == null)
            {
                return;
            }

            _modCatalogService.CatalogReloaded -= HandleModCatalogReloaded;
            _modCatalogService.CatalogReloaded += HandleModCatalogReloaded;
        }

        private void UnsubscribeFromModCatalog()
        {
            if (_modCatalogService == null)
            {
                return;
            }

            _modCatalogService.CatalogReloaded -= HandleModCatalogReloaded;
        }

        private void HandleModCatalogReloaded()
        {
            Rebuild();
        }

        private void CleanupGeneratedRuntimeObjects()
        {
            for (var i = 0; i < _generatedRuntimeObjects.Count; i++)
            {
                DestroyRuntimeObject(_generatedRuntimeObjects[i]);
            }

            _generatedRuntimeObjects.Clear();
        }

        private static void DestroyRuntimeObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
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
