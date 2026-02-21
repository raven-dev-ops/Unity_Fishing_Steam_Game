using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace RavenDevOps.Fishing.Data
{
    public sealed class AddressablesPilotCatalogLoader : MonoBehaviour
    {
        [SerializeField] private bool _useAddressablesWhenAvailable = true;
        [SerializeField] private string _fishDefinitionsLabel = "pilot/fish-definitions";
        [SerializeField] private string _fishResourcesFallbackPath = "Pilot/FishDefinitions";

        [SerializeField] private bool _enablePhaseTwoAudio = true;
        [SerializeField] private bool _enablePhaseTwoEnvironment = true;
        [SerializeField] private string _phaseTwoAudioLabel = "pilot/audio-packs";
        [SerializeField] private string _phaseTwoEnvironmentLabel = "pilot/environment-bundles";
        [SerializeField] private string _phaseTwoAudioFallbackPath = "Pilot/Audio";
        [SerializeField] private string _phaseTwoEnvironmentFallbackPath = "Pilot/Environment/Materials";

        private readonly Dictionary<string, AudioClip> _phaseTwoAudioByKey = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Material> _phaseTwoEnvironmentByKey = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Action<List<AudioClip>>> _pendingPhaseTwoAudioCallbacks = new List<Action<List<AudioClip>>>();
        private readonly List<Action<List<Material>>> _pendingPhaseTwoEnvironmentCallbacks = new List<Action<List<Material>>>();
        private bool _phaseTwoAudioLoadRequested;
        private bool _phaseTwoAudioLoadCompleted;
        private bool _phaseTwoEnvironmentLoadRequested;
        private bool _phaseTwoEnvironmentLoadCompleted;
        private bool _phaseTwoAudioLoadUsedFallback;
        private bool _phaseTwoEnvironmentLoadUsedFallback;
        private string _phaseTwoAudioLoadError = string.Empty;
        private string _phaseTwoEnvironmentLoadError = string.Empty;

        public bool IsAddressablesRuntimeAvailable
        {
            get
            {
#if ENABLE_ADDRESSABLES
                return _useAddressablesWhenAvailable;
#else
                return false;
#endif
            }
        }

        public bool PhaseTwoAudioLoadCompleted => _phaseTwoAudioLoadCompleted;
        public bool PhaseTwoEnvironmentLoadCompleted => _phaseTwoEnvironmentLoadCompleted;
        public bool PhaseTwoAudioLoadUsedFallback => _phaseTwoAudioLoadUsedFallback;
        public bool PhaseTwoEnvironmentLoadUsedFallback => _phaseTwoEnvironmentLoadUsedFallback;
        public string PhaseTwoAudioLoadError => _phaseTwoAudioLoadError;
        public string PhaseTwoEnvironmentLoadError => _phaseTwoEnvironmentLoadError;
        public int PhaseTwoAudioClipCount => _phaseTwoAudioByKey.Count;
        public int PhaseTwoEnvironmentMaterialCount => _phaseTwoEnvironmentByKey.Count;

        public void LoadFishDefinitionsAsync(Action<List<FishDefinitionSO>> onCompleted)
        {
            StartCoroutine(LoadFishDefinitionsRoutine(onCompleted));
        }

        public void LoadPhaseTwoAudioClipsAsync(Action<List<AudioClip>> onCompleted)
        {
            if (_phaseTwoAudioLoadCompleted)
            {
                onCompleted?.Invoke(BuildPhaseTwoAudioSnapshot());
                return;
            }

            if (onCompleted != null)
            {
                _pendingPhaseTwoAudioCallbacks.Add(onCompleted);
            }

            if (_phaseTwoAudioLoadRequested)
            {
                return;
            }

            _phaseTwoAudioLoadRequested = true;
            StartCoroutine(LoadPhaseTwoAudioRoutine());
        }

        public void LoadPhaseTwoEnvironmentMaterialsAsync(Action<List<Material>> onCompleted)
        {
            if (_phaseTwoEnvironmentLoadCompleted)
            {
                onCompleted?.Invoke(BuildPhaseTwoEnvironmentSnapshot());
                return;
            }

            if (onCompleted != null)
            {
                _pendingPhaseTwoEnvironmentCallbacks.Add(onCompleted);
            }

            if (_phaseTwoEnvironmentLoadRequested)
            {
                return;
            }

            _phaseTwoEnvironmentLoadRequested = true;
            StartCoroutine(LoadPhaseTwoEnvironmentRoutine());
        }

        public bool TryGetPhaseTwoAudioClip(string key, out AudioClip clip)
        {
            return _phaseTwoAudioByKey.TryGetValue(NormalizeLookupKey(key), out clip) && clip != null;
        }

        public bool TryGetPhaseTwoEnvironmentMaterial(string key, out Material material)
        {
            return _phaseTwoEnvironmentByKey.TryGetValue(NormalizeLookupKey(key), out material) && material != null;
        }

        private IEnumerator LoadFishDefinitionsRoutine(Action<List<FishDefinitionSO>> onCompleted)
        {
            var results = new List<FishDefinitionSO>();
            var usedFallback = true;

#if ENABLE_ADDRESSABLES
            if (_useAddressablesWhenAvailable)
            {
                AsyncOperationHandle<IList<FishDefinitionSO>> handle;
                try
                {
                    handle = Addressables.LoadAssetsAsync<FishDefinitionSO>(_fishDefinitionsLabel, fish =>
                    {
                        if (fish != null)
                        {
                            results.Add(fish);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"AddressablesPilotCatalogLoader: failed to start fish Addressables load ({ex.Message}).");
                    handle = default;
                }

                if (handle.IsValid())
                {
                    yield return handle;
                    if (handle.Status == AsyncOperationStatus.Succeeded && results.Count > 0)
                    {
                        usedFallback = false;
                    }
                    else
                    {
                        Debug.LogWarning("AddressablesPilotCatalogLoader: fish Addressables load did not succeed or returned no assets.");
                    }

                    Addressables.Release(handle);
                }
            }
#endif

            if (usedFallback)
            {
                var fallback = Resources.LoadAll<FishDefinitionSO>(_fishResourcesFallbackPath);
                for (var i = 0; i < fallback.Length; i++)
                {
                    if (fallback[i] != null)
                    {
                        results.Add(fallback[i]);
                    }
                }
            }

            Debug.Log(
                $"AddressablesPilotCatalogLoader: fish definitions loaded count={results.Count}, source={(usedFallback ? "resources_fallback" : "addressables")}.");
            onCompleted?.Invoke(results);
            yield break;
        }

        private IEnumerator LoadPhaseTwoAudioRoutine()
        {
            var results = new List<AudioClip>();
            var usedFallback = true;
            var loadError = string.Empty;

#if ENABLE_ADDRESSABLES
            if (_useAddressablesWhenAvailable && _enablePhaseTwoAudio)
            {
                AsyncOperationHandle<IList<AudioClip>> handle;
                try
                {
                    handle = Addressables.LoadAssetsAsync<AudioClip>(_phaseTwoAudioLabel, clip =>
                    {
                        if (clip != null)
                        {
                            results.Add(clip);
                        }
                    });
                }
                catch (Exception ex)
                {
                    loadError = $"start_failed:{ex.Message}";
                    handle = default;
                }

                if (handle.IsValid())
                {
                    yield return handle;
                    if (handle.Status == AsyncOperationStatus.Succeeded && results.Count > 0)
                    {
                        usedFallback = false;
                    }
                    else if (handle.Status != AsyncOperationStatus.Succeeded)
                    {
                        loadError = "addressables_load_failed";
                    }
                    else if (results.Count == 0)
                    {
                        loadError = "addressables_empty";
                    }

                    Addressables.Release(handle);
                }
            }
#endif

            if (usedFallback)
            {
                if (!_enablePhaseTwoAudio)
                {
                    loadError = "phase_two_audio_disabled";
                }
                else
                {
                    var fallback = Resources.LoadAll<AudioClip>(_phaseTwoAudioFallbackPath);
                    for (var i = 0; i < fallback.Length; i++)
                    {
                        if (fallback[i] != null)
                        {
                            results.Add(fallback[i]);
                        }
                    }
                }
            }

            CachePhaseTwoAudio(results);
            _phaseTwoAudioLoadCompleted = true;
            _phaseTwoAudioLoadUsedFallback = usedFallback;
            _phaseTwoAudioLoadError = loadError ?? string.Empty;

            Debug.Log(
                $"AddressablesPilotCatalogLoader: phase-two audio loaded count={results.Count}, source={(usedFallback ? "fallback" : "addressables")}, error='{_phaseTwoAudioLoadError}'.");
            DispatchPhaseTwoAudioCallbacks();
            yield break;
        }

        private IEnumerator LoadPhaseTwoEnvironmentRoutine()
        {
            var results = new List<Material>();
            var usedFallback = true;
            var loadError = string.Empty;

#if ENABLE_ADDRESSABLES
            if (_useAddressablesWhenAvailable && _enablePhaseTwoEnvironment)
            {
                AsyncOperationHandle<IList<Material>> handle;
                try
                {
                    handle = Addressables.LoadAssetsAsync<Material>(_phaseTwoEnvironmentLabel, material =>
                    {
                        if (material != null)
                        {
                            results.Add(material);
                        }
                    });
                }
                catch (Exception ex)
                {
                    loadError = $"start_failed:{ex.Message}";
                    handle = default;
                }

                if (handle.IsValid())
                {
                    yield return handle;
                    if (handle.Status == AsyncOperationStatus.Succeeded && results.Count > 0)
                    {
                        usedFallback = false;
                    }
                    else if (handle.Status != AsyncOperationStatus.Succeeded)
                    {
                        loadError = "addressables_load_failed";
                    }
                    else if (results.Count == 0)
                    {
                        loadError = "addressables_empty";
                    }

                    Addressables.Release(handle);
                }
            }
#endif

            if (usedFallback)
            {
                if (!_enablePhaseTwoEnvironment)
                {
                    loadError = "phase_two_environment_disabled";
                }
                else
                {
                    var fallback = Resources.LoadAll<Material>(_phaseTwoEnvironmentFallbackPath);
                    for (var i = 0; i < fallback.Length; i++)
                    {
                        if (fallback[i] != null)
                        {
                            results.Add(fallback[i]);
                        }
                    }
                }
            }

            CachePhaseTwoEnvironment(results);
            _phaseTwoEnvironmentLoadCompleted = true;
            _phaseTwoEnvironmentLoadUsedFallback = usedFallback;
            _phaseTwoEnvironmentLoadError = loadError ?? string.Empty;

            Debug.Log(
                $"AddressablesPilotCatalogLoader: phase-two environment loaded count={results.Count}, source={(usedFallback ? "fallback" : "addressables")}, error='{_phaseTwoEnvironmentLoadError}'.");
            DispatchPhaseTwoEnvironmentCallbacks();
            yield break;
        }

        private void CachePhaseTwoAudio(List<AudioClip> audioClips)
        {
            _phaseTwoAudioByKey.Clear();
            if (audioClips == null)
            {
                return;
            }

            for (var i = 0; i < audioClips.Count; i++)
            {
                var clip = audioClips[i];
                if (clip == null || string.IsNullOrWhiteSpace(clip.name))
                {
                    continue;
                }

                _phaseTwoAudioByKey[NormalizeLookupKey(clip.name)] = clip;
            }
        }

        private void CachePhaseTwoEnvironment(List<Material> materials)
        {
            _phaseTwoEnvironmentByKey.Clear();
            if (materials == null)
            {
                return;
            }

            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material == null || string.IsNullOrWhiteSpace(material.name))
                {
                    continue;
                }

                _phaseTwoEnvironmentByKey[NormalizeLookupKey(material.name)] = material;
            }
        }

        private void DispatchPhaseTwoAudioCallbacks()
        {
            if (_pendingPhaseTwoAudioCallbacks.Count == 0)
            {
                return;
            }

            var snapshot = BuildPhaseTwoAudioSnapshot();
            for (var i = 0; i < _pendingPhaseTwoAudioCallbacks.Count; i++)
            {
                try
                {
                    _pendingPhaseTwoAudioCallbacks[i]?.Invoke(snapshot);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"AddressablesPilotCatalogLoader: phase-two audio callback failed ({ex.Message}).");
                }
            }

            _pendingPhaseTwoAudioCallbacks.Clear();
        }

        private void DispatchPhaseTwoEnvironmentCallbacks()
        {
            if (_pendingPhaseTwoEnvironmentCallbacks.Count == 0)
            {
                return;
            }

            var snapshot = BuildPhaseTwoEnvironmentSnapshot();
            for (var i = 0; i < _pendingPhaseTwoEnvironmentCallbacks.Count; i++)
            {
                try
                {
                    _pendingPhaseTwoEnvironmentCallbacks[i]?.Invoke(snapshot);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"AddressablesPilotCatalogLoader: phase-two environment callback failed ({ex.Message}).");
                }
            }

            _pendingPhaseTwoEnvironmentCallbacks.Clear();
        }

        private List<AudioClip> BuildPhaseTwoAudioSnapshot()
        {
            var snapshot = new List<AudioClip>();
            foreach (var pair in _phaseTwoAudioByKey)
            {
                if (pair.Value != null)
                {
                    snapshot.Add(pair.Value);
                }
            }

            return snapshot;
        }

        private List<Material> BuildPhaseTwoEnvironmentSnapshot()
        {
            var snapshot = new List<Material>();
            foreach (var pair in _phaseTwoEnvironmentByKey)
            {
                if (pair.Value != null)
                {
                    snapshot.Add(pair.Value);
                }
            }

            return snapshot;
        }

        private static string NormalizeLookupKey(string key)
        {
            return string.IsNullOrWhiteSpace(key)
                ? string.Empty
                : key.Trim().ToLowerInvariant();
        }
    }
}
