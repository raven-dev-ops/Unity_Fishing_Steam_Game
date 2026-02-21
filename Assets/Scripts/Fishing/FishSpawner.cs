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
        [SerializeField] private FishingConditionController _conditionController;
        [SerializeField] private float _spawnRatePerMinute = 6f;

        private readonly List<FishDefinition> _runtimeDefinitions = new List<FishDefinition>(64);
        private readonly List<FishDefinition> _candidateBuffer = new List<FishDefinition>(64);
        private readonly List<int> _candidateWeightBuffer = new List<int>(64);
        private bool _cacheDirty = true;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _conditionController, this, warnIfMissing: false);
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

        public void SetConditionController(FishingConditionController conditionController)
        {
            _conditionController = conditionController;
        }

        public string GetActiveConditionSummary()
        {
            return _conditionController != null
                ? _conditionController.GetConditionLabel()
                : $"{FishingTimeOfDay.Day} | {FishingWeatherState.Clear}";
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
            return ApplyConditionModifiers(ResolveByWeightedRoll(roll));
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
            return ApplyConditionModifiers(ResolveByWeightedRoll(normalizedRoll));
        }

        public FishDefinition RollFishByDistanceOnly(int distanceTier)
        {
            EnsureRuntimeDefinitions();
            var totalWeight = BuildDistanceOnlyCandidates(distanceTier);
            if (_candidateBuffer.Count == 0 || totalWeight <= 0)
            {
                return null;
            }

            var roll = Random.Range(0, totalWeight);
            return ApplyConditionModifiers(ResolveByWeightedRoll(roll));
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
            _candidateWeightBuffer.Clear();
            var totalWeight = 0;
            var modifier = _conditionController != null ? _conditionController.GetCombinedModifier() : FishConditionModifier.Identity;

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

                var weight = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(0.1f, fish.rarityWeight) * Mathf.Max(0.1f, modifier.rarityWeightMultiplier)));
                totalWeight += weight;
                _candidateBuffer.Add(fish);
                _candidateWeightBuffer.Add(weight);
            }

            return totalWeight;
        }

        private int BuildDistanceOnlyCandidates(int distanceTier)
        {
            _candidateBuffer.Clear();
            _candidateWeightBuffer.Clear();
            var totalWeight = 0;
            var modifier = _conditionController != null ? _conditionController.GetCombinedModifier() : FishConditionModifier.Identity;

            for (var i = 0; i < _runtimeDefinitions.Count; i++)
            {
                var fish = _runtimeDefinitions[i];
                if (fish == null)
                {
                    continue;
                }

                var inDistanceRange = distanceTier >= fish.minDistanceTier && distanceTier <= fish.maxDistanceTier;
                if (!inDistanceRange)
                {
                    continue;
                }

                var weight = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(0.1f, fish.rarityWeight) * Mathf.Max(0.1f, modifier.rarityWeightMultiplier)));
                totalWeight += weight;
                _candidateBuffer.Add(fish);
                _candidateWeightBuffer.Add(weight);
            }

            return totalWeight;
        }

        private FishDefinition ResolveByWeightedRoll(int roll)
        {
            var cursor = 0;
            for (var i = 0; i < _candidateBuffer.Count; i++)
            {
                var fish = _candidateBuffer[i];
                var weight = i < _candidateWeightBuffer.Count ? _candidateWeightBuffer[i] : Mathf.Max(1, fish.rarityWeight);
                cursor += Mathf.Max(1, weight);
                if (roll < cursor)
                {
                    return fish;
                }
            }

            return _candidateBuffer[_candidateBuffer.Count - 1];
        }

        private FishDefinition ApplyConditionModifiers(FishDefinition source)
        {
            if (source == null || _conditionController == null)
            {
                return source;
            }

            var modifier = _conditionController.GetCombinedModifier();
            if (IsIdentityModifier(modifier))
            {
                return source;
            }

            var biteMin = Mathf.Max(0f, source.minBiteDelaySeconds * modifier.biteDelayMultiplier);
            var biteMax = Mathf.Max(biteMin, source.maxBiteDelaySeconds * modifier.biteDelayMultiplier);
            var minWeight = Mathf.Max(0.1f, source.minCatchWeightKg);
            var maxWeight = Mathf.Max(minWeight, source.maxCatchWeightKg);

            return new FishDefinition
            {
                id = source.id,
                minDistanceTier = source.minDistanceTier,
                maxDistanceTier = source.maxDistanceTier,
                minDepth = source.minDepth,
                maxDepth = source.maxDepth,
                rarityWeight = source.rarityWeight,
                baseValue = source.baseValue,
                minBiteDelaySeconds = biteMin,
                maxBiteDelaySeconds = biteMax,
                fightStamina = Mathf.Max(0.1f, source.fightStamina * modifier.fightStaminaMultiplier),
                pullIntensity = Mathf.Max(0.1f, source.pullIntensity * modifier.pullIntensityMultiplier),
                escapeSeconds = Mathf.Max(0.5f, source.escapeSeconds * modifier.escapeSecondsMultiplier),
                minCatchWeightKg = minWeight,
                maxCatchWeightKg = maxWeight
            };
        }

        private static bool IsIdentityModifier(FishConditionModifier modifier)
        {
            return Mathf.Approximately(modifier.rarityWeightMultiplier, 1f)
                && Mathf.Approximately(modifier.biteDelayMultiplier, 1f)
                && Mathf.Approximately(modifier.fightStaminaMultiplier, 1f)
                && Mathf.Approximately(modifier.pullIntensityMultiplier, 1f)
                && Mathf.Approximately(modifier.escapeSecondsMultiplier, 1f);
        }
    }
}
