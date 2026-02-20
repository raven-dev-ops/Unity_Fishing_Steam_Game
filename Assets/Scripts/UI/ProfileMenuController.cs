using RavenDevOps.Fishing.Save;
using TMPro;
using UnityEngine;

namespace RavenDevOps.Fishing.UI
{
    public sealed class ProfileMenuController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _dayText;
        [SerializeField] private TMP_Text _copecsText;
        [SerializeField] private TMP_Text _totalFishText;
        [SerializeField] private TMP_Text _farthestDistanceText;

        [SerializeField] private SaveManager _saveManager;

        private void Awake()
        {
            _saveManager ??= FindObjectOfType<SaveManager>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            var save = _saveManager != null ? _saveManager.Current : null;
            if (save == null)
            {
                SetFallbackText();
                return;
            }

            var dayNumber = DayCounterService.ComputeDayNumber(save.careerStartLocalDate);

            if (_dayText != null) _dayText.text = $"Day {dayNumber}";
            if (_copecsText != null) _copecsText.text = $"Copecs: {save.copecs}";
            if (_totalFishText != null) _totalFishText.text = $"Total Fish Caught: {save.stats.totalFishCaught}";
            if (_farthestDistanceText != null) _farthestDistanceText.text = $"Farthest Distance Tier: {save.stats.farthestDistanceTier}";
        }

        public void ResetProfile()
        {
            if (_saveManager == null)
            {
                return;
            }

            _saveManager.Current.copecs = 0;
            _saveManager.Current.stats.totalFishCaught = 0;
            _saveManager.Current.stats.farthestDistanceTier = 0;
            _saveManager.Current.stats.totalTrips = 0;
            _saveManager.Save();
            Refresh();
        }

        private void SetFallbackText()
        {
            if (_dayText != null) _dayText.text = "Day -";
            if (_copecsText != null) _copecsText.text = "Copecs: -";
            if (_totalFishText != null) _totalFishText.text = "Total Fish Caught: -";
            if (_farthestDistanceText != null) _farthestDistanceText.text = "Farthest Distance Tier: -";
        }
    }
}
