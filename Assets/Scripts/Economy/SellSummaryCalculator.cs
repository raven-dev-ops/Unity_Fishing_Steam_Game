using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Economy
{
    public sealed class SellSummaryCalculator : MonoBehaviour
    {
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private MetaLoopRuntimeService _metaLoopService;
        [SerializeField] private float _distanceTierStep = 0.25f;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _metaLoopService, this, warnIfMissing: false);
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public void SetDistanceTierStep(float distanceTierStep)
        {
            _distanceTierStep = Mathf.Max(0f, distanceTierStep);
        }

        public float CalculateDistanceMultiplier(int distanceTier)
        {
            var normalizedTier = Mathf.Max(1, distanceTier);
            var tierOffset = normalizedTier - 1;
            return 1f + tierOffset * _distanceTierStep;
        }

        public SellSummary Calculate(List<FishInventoryEntry> inventory)
        {
            var summary = new SellSummary();
            if (inventory == null)
            {
                return summary;
            }

            foreach (var stack in inventory)
            {
                var baseValue = 10;
                if (_catalogService != null && _catalogService.TryGetFish(stack.fishId, out var fishDef))
                {
                    baseValue = Mathf.Max(1, fishDef.baseValue);
                }

                var multiplier = CalculateDistanceMultiplier(stack.distanceTier);
                var demandMultiplier = _metaLoopService != null
                    ? _metaLoopService.GetMarketDemandMultiplier(stack.fishId)
                    : 1f;

                var synergyMultiplier = 1f;
                if (_metaLoopService != null && _saveManager != null && _saveManager.Current != null)
                {
                    synergyMultiplier = _metaLoopService.GetGearSynergyMultiplier(
                        _saveManager.Current.equippedShipId,
                        _saveManager.Current.equippedHookId,
                        out _);
                }

                var stackValue = Mathf.RoundToInt(baseValue * multiplier * demandMultiplier * synergyMultiplier) * Mathf.Max(0, stack.count);
                summary.totalEarned += stackValue;
                summary.itemCount += Mathf.Max(0, stack.count);
            }

            return summary;
        }
    }
}
