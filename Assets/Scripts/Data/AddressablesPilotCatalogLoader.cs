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
        [SerializeField] private string _resourcesFallbackPath = "Pilot/FishDefinitions";

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

        public void LoadFishDefinitionsAsync(Action<List<FishDefinitionSO>> onCompleted)
        {
            StartCoroutine(LoadFishDefinitionsRoutine(onCompleted));
        }

        private IEnumerator LoadFishDefinitionsRoutine(Action<List<FishDefinitionSO>> onCompleted)
        {
            var results = new List<FishDefinitionSO>();

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
                    Debug.LogWarning($"AddressablesPilotCatalogLoader: failed to start Addressables load ({ex.Message}).");
                    onCompleted?.Invoke(results);
                    yield break;
                }

                yield return handle;
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogWarning("AddressablesPilotCatalogLoader: Addressables load did not succeed.");
                }

                Addressables.Release(handle);
                onCompleted?.Invoke(results);
                yield break;
            }
#endif

            var fallback = Resources.LoadAll<FishDefinitionSO>(_resourcesFallbackPath);
            for (var i = 0; i < fallback.Length; i++)
            {
                if (fallback[i] != null)
                {
                    results.Add(fallback[i]);
                }
            }

            onCompleted?.Invoke(results);
        }
    }
}
