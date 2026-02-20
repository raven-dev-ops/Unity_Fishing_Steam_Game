using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishSpawner : MonoBehaviour
    {
        [SerializeField] private List<FishDefinition> _fishDefinitions = new List<FishDefinition>();
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private float _spawnRatePerMinute = 6f;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public float SpawnRatePerMinute => _spawnRatePerMinute;

        public void SetSpawnRate(float spawnRatePerMinute)
        {
            _spawnRatePerMinute = Mathf.Max(0f, spawnRatePerMinute);
        }

        public FishDefinition RollFish(int distanceTier, float depth)
        {
            var candidates = new List<FishDefinition>();
            var totalWeight = 0;

            foreach (var fish in BuildRuntimeDefinitions())
            {
                if (fish == null)
                {
                    continue;
                }

                var inDistanceRange = distanceTier >= fish.minDistanceTier && distanceTier <= fish.maxDistanceTier;
                var inDepthRange = depth >= fish.minDepth && depth <= fish.maxDepth;
                if (!inDistanceRange || !inDepthRange)
                {
                    continue;
                }

                var weight = Mathf.Max(1, fish.rarityWeight);
                totalWeight += weight;
                candidates.Add(fish);
            }

            if (candidates.Count == 0 || totalWeight <= 0)
            {
                return null;
            }

            var roll = Random.Range(0, totalWeight);
            var cursor = 0;
            foreach (var fish in candidates)
            {
                cursor += Mathf.Max(1, fish.rarityWeight);
                if (roll < cursor)
                {
                    return fish;
                }
            }

            return candidates[candidates.Count - 1];
        }

        private IEnumerable<FishDefinition> BuildRuntimeDefinitions()
        {
            if (_catalogService != null && _catalogService.FishById.Count > 0)
            {
                foreach (var pair in _catalogService.FishById)
                {
                    var so = pair.Value;
                    yield return new FishDefinition
                    {
                        id = so.id,
                        minDistanceTier = so.minDistanceTier,
                        maxDistanceTier = so.maxDistanceTier,
                        minDepth = so.minDepth,
                        maxDepth = so.maxDepth,
                        rarityWeight = so.rarityWeight,
                        baseValue = so.baseValue
                    };
                }
            }
            else
            {
                foreach (var fish in _fishDefinitions)
                {
                    yield return fish;
                }
            }
        }
    }
}
