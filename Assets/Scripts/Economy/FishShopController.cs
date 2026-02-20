using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Economy
{
    public sealed class FishShopController : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private SellSummaryCalculator _sellSummaryCalculator;

        private void Awake()
        {
            _saveManager ??= FindObjectOfType<SaveManager>();
            _sellSummaryCalculator ??= FindObjectOfType<SellSummaryCalculator>();
        }

        public int SellAll()
        {
            if (_saveManager == null)
            {
                return 0;
            }

            var save = _saveManager.Current;
            var summary = _sellSummaryCalculator != null
                ? _sellSummaryCalculator.Calculate(save.fishInventory)
                : new SellSummary();

            save.copecs += summary.totalEarned;
            save.fishInventory.Clear();
            _saveManager.Save();
            return summary.totalEarned;
        }
    }
}
