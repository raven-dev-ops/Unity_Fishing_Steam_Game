using System.Collections.Generic;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Economy
{
    public sealed class SellSummaryCalculator : MonoBehaviour
    {
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private float _distanceTierStep = 0.25f;

        private void Awake()
        {
            _catalogService ??= FindObjectOfType<CatalogService>();
        }

        public void SetDistanceTierStep(float distanceTierStep)
        {
            _distanceTierStep = Mathf.Max(0f, distanceTierStep);
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

                var multiplier = 1f + Mathf.Max(0, stack.distanceTier) * _distanceTierStep;
                var stackValue = Mathf.RoundToInt(baseValue * multiplier) * Mathf.Max(0, stack.count);
                summary.totalEarned += stackValue;
                summary.itemCount += Mathf.Max(0, stack.count);
            }

            return summary;
        }
    }
}
