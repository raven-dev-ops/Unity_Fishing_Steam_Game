using System;
using System.Text;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.UI
{
    public sealed class ProfileMenuController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _dayText;
        [SerializeField] private TMP_Text _copecsText;
        [SerializeField] private TMP_Text _totalFishText;
        [SerializeField] private TMP_Text _farthestDistanceText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _xpProgressText;
        [SerializeField] private TMP_Text _nextUnlockText;
        [SerializeField] private TMP_Text _objectiveText;
        [SerializeField] private TMP_Text _catchLogText;
        [SerializeField] private TMP_Text _modSafeModeStatusText;
        [SerializeField] private Toggle _modSafeModeToggle;
        [SerializeField] private int _maxCatchLogEntries = 8;

        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private ObjectivesService _objectivesService;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private ModRuntimeCatalogService _modCatalogService;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _objectivesService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _modCatalogService, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged += OnSaveDataChanged;
            }

            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= OnSettingsChanged;
                _settingsService.SettingsChanged += OnSettingsChanged;
            }

            if (_modCatalogService != null)
            {
                _modCatalogService.CatalogReloaded -= OnCatalogReloaded;
                _modCatalogService.CatalogReloaded += OnCatalogReloaded;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged -= OnSaveDataChanged;
            }

            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= OnSettingsChanged;
            }

            if (_modCatalogService != null)
            {
                _modCatalogService.CatalogReloaded -= OnCatalogReloaded;
            }
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
            var level = _saveManager.CurrentLevel;
            var xpIntoLevel = _saveManager.CurrentXpIntoLevel;
            var xpToNextLevel = _saveManager.CurrentXpToNextLevel;
            if (_levelText != null) _levelText.text = $"Level: {level}";
            if (_xpProgressText != null)
            {
                _xpProgressText.text = xpToNextLevel > 0
                    ? $"XP: {xpIntoLevel}/{xpToNextLevel} ({_saveManager.CurrentTotalXp} total)"
                    : $"XP: {_saveManager.CurrentTotalXp} (max level configured)";
            }

            if (_nextUnlockText != null)
            {
                _nextUnlockText.text = $"Next Unlock: {_saveManager.GetNextUnlockDescription()}";
            }

            if (_objectiveText != null && _objectivesService != null)
            {
                _objectiveText.text = _objectivesService.BuildActiveObjectiveLabel();
            }

            RefreshCatchLogText(save);
            RefreshModSafeModeStatus();
        }

        public void ResetProfile()
        {
            _saveManager?.ResetProfileStats();
        }

        public void ResetObjectivesForQa()
        {
            _objectivesService?.ResetObjectiveProgressForQA();
        }

        public void OnModSafeModeChanged(bool enabled)
        {
            _settingsService?.SetModSafeModeEnabled(enabled);
            RefreshModSafeModeStatus();
        }

        private void SetFallbackText()
        {
            if (_dayText != null) _dayText.text = "Day -";
            if (_copecsText != null) _copecsText.text = "Copecs: -";
            if (_totalFishText != null) _totalFishText.text = "Total Fish Caught: -";
            if (_farthestDistanceText != null) _farthestDistanceText.text = "Farthest Distance Tier: -";
            if (_levelText != null) _levelText.text = "Level: -";
            if (_xpProgressText != null) _xpProgressText.text = "XP: -";
            if (_nextUnlockText != null) _nextUnlockText.text = "Next Unlock: -";
            if (_objectiveText != null) _objectiveText.text = "Objective: -";
            if (_catchLogText != null) _catchLogText.text = "Catch Log: -";
            RefreshModSafeModeStatus();
        }

        private void OnSaveDataChanged(SaveDataV1 data)
        {
            Refresh();
        }

        private void RefreshCatchLogText(SaveDataV1 save)
        {
            if (_catchLogText == null)
            {
                return;
            }

            if (save == null || _saveManager == null)
            {
                _catchLogText.text = "Catch Log: No entries";
                return;
            }

            var recentEntries = _saveManager.GetRecentCatchLogSnapshot(Mathf.Max(1, _maxCatchLogEntries));
            if (recentEntries == null || recentEntries.Count == 0)
            {
                _catchLogText.text = "Catch Log: No entries";
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Catch Log");
            for (var i = recentEntries.Count - 1; i >= 0; i--)
            {
                var entry = recentEntries[i];
                if (entry == null)
                {
                    continue;
                }

                var timeLabel = FormatTime(entry.timestampUtc);
                var fishId = string.IsNullOrWhiteSpace(entry.fishId) ? "unknown" : entry.fishId;
                if (entry.landed)
                {
                    builder.AppendLine($"[{timeLabel}] {fishId} {entry.weightKg:0.0}kg +{entry.valueCopecs}c");
                }
                else
                {
                    var reason = string.IsNullOrWhiteSpace(entry.failReason) ? "failed" : entry.failReason;
                    builder.AppendLine($"[{timeLabel}] {fishId} - {reason}");
                }

            }

            _catchLogText.text = builder.ToString().TrimEnd();
        }

        private void OnSettingsChanged()
        {
            RefreshModSafeModeStatus();
        }

        private void OnCatalogReloaded()
        {
            RefreshModSafeModeStatus();
        }

        private void RefreshModSafeModeStatus()
        {
            var safeModePreferenceEnabled = _settingsService != null
                ? _settingsService.ModSafeModeEnabled
                : PlayerPrefs.GetInt(UserSettingsService.ModsSafeModePlayerPrefsKey, 0) == 1;

            if (_modSafeModeToggle != null)
            {
                _modSafeModeToggle.SetIsOnWithoutNotify(safeModePreferenceEnabled);
            }

            if (_modSafeModeStatusText != null)
            {
                var safeModeActive = _modCatalogService != null && _modCatalogService.SafeModeActive;
                var safeModeReason = _modCatalogService != null ? _modCatalogService.SafeModeReason : string.Empty;
                _modSafeModeStatusText.text = ModDiagnosticsTextFormatter.BuildSafeModeStatus(
                    safeModePreferenceEnabled,
                    safeModeActive,
                    safeModeReason);
            }
        }

        private static string FormatTime(string timestampUtc)
        {
            if (DateTime.TryParse(timestampUtc, out var parsed))
            {
                return parsed.ToLocalTime().ToString("HH:mm");
            }

            return "--:--";
        }
    }
}
