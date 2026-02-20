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

        private readonly List<FishDefinition> _runtimeDefinitions = new List<FishDefinition>(64);
        private readonly List<FishDefinition> _candidateBuffer = new List<FishDefinition>(64);
        private bool _cacheDirty = true;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            RebuildRuntimeDefinitions();
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

        public void SetCatalogService(CatalogService catalogService)
        {
            _catalogService = catalogService;
            MarkCacheDirty();
        }

        public void SetFallbackDefinitions(List<FishDefinition> definitions)
        {
            _fishDefinitions = definitions ?? new List<FishDefinition>();
            MarkCacheDirty();
        }

        public void MarkCacheDirty()
        {
            _cacheDirty = true;
        }

        public FishDefinition RollFish(int distanceTier, float depth)
        {
            EnsureRuntimeDefinitions();
            var totalWeight = BuildCandidates(distanceTier, depth);
            if (_candidateBuffer.Count == 0 || totalWeight <= 0)
            {
                return null;
            }

            var roll = Random.Range(0, totalWeight);
            return ResolveByWeightedRoll(roll);
        }

        public FishDefinition RollFishDeterministic(int distanceTier, float depth, int weightedRoll)
        {
            EnsureRuntimeDefinitions();
            var totalWeight = BuildCandidates(distanceTier, depth);
            if (_candidateBuffer.Count == 0 || totalWeight <= 0)
            {
                return null;
            }

            var normalizedRoll = Mathf.Abs(weightedRoll) % totalWeight;
            return ResolveByWeightedRoll(normalizedRoll);
        }

        private void EnsureRuntimeDefinitions()
        {
            if (!_cacheDirty && _runtimeDefinitions.Count > 0)
            {
                if (_catalogService == null)
                {
                    return;
                }

                if (_catalogService.FishById.Count == _runtimeDefinitions.Count)
                {
                    return;
                }
            }

            RebuildRuntimeDefinitions();
        }

        private void RebuildRuntimeDefinitions()
        {
            _runtimeDefinitions.Clear();

            if (_catalogService != null && _catalogService.FishById.Count > 0)
            {
                foreach (var pair in _catalogService.FishById)
                {
                    var so = pair.Value;
                    if (so == null)
                    {
                        continue;
                    }

                    _runtimeDefinitions.Add(new FishDefinition
                    {
                        id = so.id,
                        minDistanceTier = so.minDistanceTier,
                        maxDistanceTier = so.maxDistanceTier,
                        minDepth = so.minDepth,
                        maxDepth = so.maxDepth,
                        rarityWeight = so.rarityWeight,
                        baseValue = so.baseValue,
                        minBiteDelaySeconds = so.minBiteDelaySeconds,
                        maxBiteDelaySeconds = so.maxBiteDelaySeconds,
                        fightStamina = so.fightStamina,
                        pullIntensity = so.pullIntensity,
                        escapeSeconds = so.escapeSeconds,
                        minCatchWeightKg = so.minCatchWeightKg,
                        maxCatchWeightKg = so.maxCatchWeightKg
                    });
                }
            }
            else
            {
                if (_fishDefinitions != null)
                {
                    _runtimeDefinitions.AddRange(_fishDefinitions);
                }
            }

            _cacheDirty = false;
        }

        private int BuildCandidates(int distanceTier, float depth)
        {
            _candidateBuffer.Clear();
            var totalWeight = 0;

            for (var i = 0; i < _runtimeDefinitions.Count; i++)
            {
                var fish = _runtimeDefinitions[i];
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
                _candidateBuffer.Add(fish);
            }

            return totalWeight;
        }

        private FishDefinition ResolveByWeightedRoll(int roll)
        {
            var cursor = 0;
            for (var i = 0; i < _candidateBuffer.Count; i++)
            {
                var fish = _candidateBuffer[i];
                cursor += Mathf.Max(1, fish.rarityWeight);
                if (roll < cursor)
                {
                    return fish;
                }
            }

            return _candidateBuffer[_candidateBuffer.Count - 1];
        }
    }
}
