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
        private static readonly string[] RequiredPhaseTwoAudioKeys =
        {
            "menu_music_loop",
            "harbor_music_loop",
            "fishing_music_loop",
            "sfx_ui_navigate",
            "sfx_ui_select",
            "sfx_ui_cancel",
            "sfx_cast",
            "sfx_hooked",
            "sfx_catch",
            "sfx_sell",
            "sfx_purchase",
            "sfx_depart",
            "sfx_return"
        };

        private const string RequiredPhaseTwoSkyboxKey = "fishing_skybox";
        private const int GeneratedAudioSampleRate = 22050;

        [SerializeField] private bool _useAddressablesWhenAvailable = true;
        [SerializeField] private string _fishDefinitionsLabel = "pilot/fish-definitions";
        [SerializeField] private string _fishResourcesFallbackPath = "Pilot/FishDefinitions";

        [SerializeField] private bool _enablePhaseTwoAudio = true;
        [SerializeField] private bool _enablePhaseTwoEnvironment = true;
        [SerializeField] private string _phaseTwoAudioLabel = "pilot/audio-packs";
        [SerializeField] private string _phaseTwoEnvironmentLabel = "pilot/environment-bundles";
        [SerializeField] private string _phaseTwoAudioFallbackPath = "Pilot/Audio";
        [SerializeField] private string _phaseTwoEnvironmentFallbackPath = "Pilot/Environment/Materials";
        [SerializeField] private bool _verboseLogs;

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
        private readonly List<AudioClip> _generatedPhaseTwoAudioClips = new List<AudioClip>();
        private readonly List<Material> _generatedPhaseTwoEnvironmentMaterials = new List<Material>();

        public bool IsAddressablesRuntimeAvailable
        {
            get
            {
                return IsAddressablesCompiledIn && _useAddressablesWhenAvailable;
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

        private void OnDestroy()
        {
            DisposeGeneratedFallbackAssets();
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

            LogInfo(
                $"AddressablesPilotCatalogLoader: fish definitions loaded count={results.Count}, source={(usedFallback ? "resources_fallback" : "addressables")}, label='{_fishDefinitionsLabel}', addressablesCompiledIn={IsAddressablesCompiledIn}.");
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

                    var generatedFallback = EnsurePhaseTwoAudioFallbackCoverage(results);
                    if (generatedFallback && string.IsNullOrWhiteSpace(loadError))
                    {
                        loadError = "fallback_generated";
                    }
                }
            }

            CachePhaseTwoAudio(results);
            _phaseTwoAudioLoadCompleted = true;
            _phaseTwoAudioLoadUsedFallback = usedFallback;
            _phaseTwoAudioLoadError = loadError ?? string.Empty;

            LogInfo(
                $"AddressablesPilotCatalogLoader: phase-two audio loaded count={results.Count}, source={(usedFallback ? "fallback" : "addressables")}, label='{_phaseTwoAudioLabel}', error='{_phaseTwoAudioLoadError}', addressablesCompiledIn={IsAddressablesCompiledIn}.");
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

                    var generatedFallback = EnsurePhaseTwoEnvironmentFallbackCoverage(results);
                    if (generatedFallback && string.IsNullOrWhiteSpace(loadError))
                    {
                        loadError = "fallback_generated";
                    }
                }
            }

            CachePhaseTwoEnvironment(results);
            _phaseTwoEnvironmentLoadCompleted = true;
            _phaseTwoEnvironmentLoadUsedFallback = usedFallback;
            _phaseTwoEnvironmentLoadError = loadError ?? string.Empty;

            LogInfo(
                $"AddressablesPilotCatalogLoader: phase-two environment loaded count={results.Count}, source={(usedFallback ? "fallback" : "addressables")}, label='{_phaseTwoEnvironmentLabel}', error='{_phaseTwoEnvironmentLoadError}', addressablesCompiledIn={IsAddressablesCompiledIn}.");
            DispatchPhaseTwoEnvironmentCallbacks();
            yield break;
        }

        private static bool IsAddressablesCompiledIn
        {
            get
            {
#if ENABLE_ADDRESSABLES
                return true;
#else
                return false;
#endif
            }
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

        private bool EnsurePhaseTwoAudioFallbackCoverage(List<AudioClip> clips)
        {
            if (clips == null)
            {
                return false;
            }

            var generatedAny = false;
            var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                if (clip != null && !string.IsNullOrWhiteSpace(clip.name))
                {
                    present.Add(NormalizeLookupKey(clip.name));
                }
            }

            for (var i = 0; i < RequiredPhaseTwoAudioKeys.Length; i++)
            {
                var key = RequiredPhaseTwoAudioKeys[i];
                if (present.Contains(NormalizeLookupKey(key)))
                {
                    continue;
                }

                var generatedClip = CreateGeneratedPhaseTwoAudioClip(key, i);
                if (generatedClip == null)
                {
                    continue;
                }

                clips.Add(generatedClip);
                present.Add(NormalizeLookupKey(key));
                _generatedPhaseTwoAudioClips.Add(generatedClip);
                generatedAny = true;
            }

            return generatedAny;
        }

        private bool EnsurePhaseTwoEnvironmentFallbackCoverage(List<Material> materials)
        {
            if (materials == null)
            {
                return false;
            }

            var requiredKey = NormalizeLookupKey(RequiredPhaseTwoSkyboxKey);
            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material == null || string.IsNullOrWhiteSpace(material.name))
                {
                    continue;
                }

                if (NormalizeLookupKey(material.name) == requiredKey)
                {
                    return false;
                }
            }

            var generatedMaterial = CreateGeneratedPhaseTwoSkyboxMaterial(RequiredPhaseTwoSkyboxKey);
            if (generatedMaterial == null)
            {
                return false;
            }

            materials.Add(generatedMaterial);
            _generatedPhaseTwoEnvironmentMaterials.Add(generatedMaterial);
            return true;
        }

        private static AudioClip CreateGeneratedPhaseTwoAudioClip(string key, int index)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            var normalized = NormalizeLookupKey(key);
            var durationSeconds = normalized.Contains("music_loop", StringComparison.OrdinalIgnoreCase) ? 1.8f : 0.18f;
            var sampleCount = Mathf.Max(256, Mathf.RoundToInt(durationSeconds * GeneratedAudioSampleRate));
            var frequency = ResolveGeneratedToneFrequency(index);
            var amplitude = normalized.Contains("music_loop", StringComparison.OrdinalIgnoreCase) ? 0.04f : 0.06f;
            var samples = new float[sampleCount];
            for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                var phase = sampleIndex / (float)GeneratedAudioSampleRate;
                samples[sampleIndex] = Mathf.Sin(2f * Mathf.PI * frequency * phase) * amplitude;
            }

            var clip = AudioClip.Create(key, sampleCount, 1, GeneratedAudioSampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float ResolveGeneratedToneFrequency(int index)
        {
            var tones = new[]
            {
                261.63f,
                293.66f,
                329.63f,
                349.23f,
                392.0f,
                440.0f,
                493.88f
            };

            return tones[Mathf.Abs(index) % tones.Length];
        }

        private static Material CreateGeneratedPhaseTwoSkyboxMaterial(string materialName)
        {
            Shader shader = null;
            var shaderCandidates = new[]
            {
                "Skybox/Procedural",
                "Skybox/Panoramic",
                "Universal Render Pipeline/Unlit",
                "Unlit/Color",
                "Sprites/Default"
            };

            for (var i = 0; i < shaderCandidates.Length; i++)
            {
                shader = Shader.Find(shaderCandidates[i]);
                if (shader != null)
                {
                    break;
                }
            }

            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = materialName
            };

            if (material.HasProperty("_SkyTint"))
            {
                material.SetColor("_SkyTint", new Color(0.27f, 0.58f, 0.92f, 1f));
            }

            if (material.HasProperty("_GroundColor"))
            {
                material.SetColor("_GroundColor", new Color(0.18f, 0.22f, 0.25f, 1f));
            }

            if (material.HasProperty("_Tint"))
            {
                material.SetColor("_Tint", new Color(0.42f, 0.69f, 0.95f, 1f));
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", new Color(0.42f, 0.69f, 0.95f, 1f));
            }

            if (material.HasProperty("_Exposure"))
            {
                material.SetFloat("_Exposure", 1.1f);
            }

            return material;
        }

        private void DisposeGeneratedFallbackAssets()
        {
            for (var i = 0; i < _generatedPhaseTwoAudioClips.Count; i++)
            {
                var clip = _generatedPhaseTwoAudioClips[i];
                if (clip != null)
                {
                    Destroy(clip);
                }
            }

            _generatedPhaseTwoAudioClips.Clear();

            for (var i = 0; i < _generatedPhaseTwoEnvironmentMaterials.Count; i++)
            {
                var material = _generatedPhaseTwoEnvironmentMaterials[i];
                if (material != null)
                {
                    Destroy(material);
                }
            }

            _generatedPhaseTwoEnvironmentMaterials.Clear();
        }

        private static string NormalizeLookupKey(string key)
        {
            return string.IsNullOrWhiteSpace(key)
                ? string.Empty
                : key.Trim().ToLowerInvariant();
        }

        private void LogInfo(string message)
        {
            if (_verboseLogs || Application.isBatchMode)
            {
                Debug.Log(message);
            }
        }
    }
}
