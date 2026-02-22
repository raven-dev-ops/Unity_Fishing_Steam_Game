using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Fishing;
using RavenDevOps.Fishing.Save;
using TMPro;
using UnityEngine;

namespace RavenDevOps.Fishing.UI
{
    public sealed class HudOverlayController : MonoBehaviour, IFishingHudOverlay
    {
        [SerializeField] private TMP_Text _copecsText;
        [SerializeField] private TMP_Text _dayText;
        [SerializeField] private TMP_Text _distanceTierText;
        [SerializeField] private TMP_Text _depthText;
        [SerializeField] private TMP_Text _tensionStateText;
        [SerializeField] private TMP_Text _conditionsText;
        [SerializeField] private TMP_Text _objectiveStatusText;
        [SerializeField] private TMP_Text _fishingStatusText;
        [SerializeField] private TMP_Text _fishingFailureText;

        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private ObjectivesService _objectivesService;

        private ISaveDataView _saveDataView;

        public int CurrentDistanceTier { get; set; } = 1;
        public float CurrentDepth { get; set; }
        public float CurrentTensionNormalized { get; private set; }
        public FishingTensionState CurrentTensionState { get; private set; }
        public string CurrentConditionsLabel { get; private set; } = string.Empty;
        public string CurrentObjectiveStatus { get; private set; } = string.Empty;
        public string CurrentFishingStatus { get; private set; } = string.Empty;
        public string CurrentFishingFailure { get; private set; } = string.Empty;

        private void Awake()
        {
            if (_saveDataView == null)
            {
                RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
                _saveDataView = _saveManager;
            }

            if (_gameFlowManager == null)
            {
                RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
            }

            if (_settingsService == null)
            {
                RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            }

            if (_objectivesService == null)
            {
                RuntimeServiceRegistry.Resolve(ref _objectivesService, this, warnIfMissing: false);
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_saveDataView != null)
            {
                _saveDataView.SaveDataChanged += OnSaveDataChanged;
            }

            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged += OnGameFlowStateChanged;
            }

            if (_settingsService != null)
            {
                _settingsService.SettingsChanged += OnSettingsChanged;
            }

            if (_objectivesService != null)
            {
                _objectivesService.ObjectiveLabelChanged += OnObjectiveLabelChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_saveDataView != null)
            {
                _saveDataView.SaveDataChanged -= OnSaveDataChanged;
            }

            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged -= OnGameFlowStateChanged;
            }

            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= OnSettingsChanged;
            }

            if (_objectivesService != null)
            {
                _objectivesService.ObjectiveLabelChanged -= OnObjectiveLabelChanged;
            }
        }

        public void Configure(
            TMP_Text copecsText,
            TMP_Text dayText,
            TMP_Text distanceTierText,
            TMP_Text depthText,
            TMP_Text tensionStateText,
            TMP_Text conditionsText,
            TMP_Text objectiveStatusText,
            TMP_Text fishingStatusText,
            TMP_Text fishingFailureText,
            ObjectivesService objectivesService = null)
        {
            _copecsText = copecsText;
            _dayText = dayText;
            _distanceTierText = distanceTierText;
            _depthText = depthText;
            _tensionStateText = tensionStateText;
            _conditionsText = conditionsText;
            _objectiveStatusText = objectiveStatusText;
            _fishingStatusText = fishingStatusText;
            _fishingFailureText = fishingFailureText;
            if (objectivesService != null)
            {
                _objectivesService = objectivesService;
            }

            if (isActiveAndEnabled)
            {
                UnsubscribeEvents();
                SubscribeEvents();
            }

            Refresh();
        }

        public void ConfigureDependencies(ISaveDataView saveDataView, GameFlowManager gameFlowManager = null, UserSettingsService settingsService = null)
        {
            if (isActiveAndEnabled)
            {
                UnsubscribeEvents();
            }

            _saveDataView = saveDataView;
            _saveManager = saveDataView as SaveManager;
            if (gameFlowManager != null)
            {
                _gameFlowManager = gameFlowManager;
            }

            if (settingsService != null)
            {
                _settingsService = settingsService;
            }

            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }

            Refresh();
        }

        public void SetFishingTelemetry(int distanceTier, float depth)
        {
            var clampedTier = Mathf.Max(1, distanceTier);
            if (CurrentDistanceTier == clampedTier && Mathf.Approximately(CurrentDepth, depth))
            {
                return;
            }

            CurrentDistanceTier = clampedTier;
            CurrentDepth = Mathf.Max(0f, depth);
            Refresh();
        }

        public void SetFishingTension(float normalizedTension, FishingTensionState tensionState)
        {
            CurrentTensionNormalized = Mathf.Clamp01(normalizedTension);
            CurrentTensionState = tensionState;
            Refresh();
        }

        public void SetFishingStatus(string status)
        {
            CurrentFishingStatus = status ?? string.Empty;
            Refresh();
        }

        public void SetFishingFailure(string failure)
        {
            CurrentFishingFailure = failure ?? string.Empty;
            Refresh();
        }

        public void SetFishingConditions(string conditionLabel)
        {
            CurrentConditionsLabel = conditionLabel ?? string.Empty;
            Refresh();
        }

        public void SetObjectiveStatus(string objectiveStatus)
        {
            CurrentObjectiveStatus = objectiveStatus ?? string.Empty;
            Refresh();
        }

        public void ClearFishingFeedback()
        {
            CurrentTensionNormalized = 0f;
            CurrentTensionState = FishingTensionState.None;
            CurrentConditionsLabel = string.Empty;
            CurrentFishingStatus = string.Empty;
            CurrentFishingFailure = string.Empty;
            Refresh();
        }

        public void Refresh()
        {
            if (_saveDataView == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentObjectiveStatus) && _objectivesService != null)
            {
                CurrentObjectiveStatus = _objectivesService.BuildActiveObjectiveLabel();
            }

            var save = _saveDataView.Current;
            if (_copecsText != null)
            {
                _copecsText.text = $"Copecs: {save.copecs}";
            }

            if (_dayText != null)
            {
                _dayText.text = $"Day {DayCounterService.ComputeDayNumber(save.careerStartLocalDate)}";
            }

            var inFishing = _gameFlowManager != null && _gameFlowManager.CurrentState == GameFlowState.Fishing;
            if (_distanceTierText != null)
            {
                _distanceTierText.text = inFishing ? $"Distance Tier: {CurrentDistanceTier}" : string.Empty;
            }

            if (_depthText != null)
            {
                _depthText.text = inFishing ? $"Depth: {CurrentDepth:0.0}" : string.Empty;
            }

            if (_tensionStateText != null)
            {
                _tensionStateText.text = inFishing && CurrentTensionState != FishingTensionState.None
                    ? BuildTensionText(CurrentTensionState, CurrentTensionNormalized)
                    : string.Empty;
            }

            if (_conditionsText != null)
            {
                _conditionsText.text = inFishing && !string.IsNullOrWhiteSpace(CurrentConditionsLabel)
                    ? $"Conditions: {CurrentConditionsLabel}"
                    : string.Empty;
            }

            if (_objectiveStatusText != null)
            {
                var showObjective = _gameFlowManager != null
                    && (_gameFlowManager.CurrentState == GameFlowState.Fishing || _gameFlowManager.CurrentState == GameFlowState.Harbor);
                _objectiveStatusText.text = showObjective ? CurrentObjectiveStatus : string.Empty;
            }

            if (_fishingStatusText != null)
            {
                _fishingStatusText.text = inFishing
                    ? BuildFishingStatusText(inFishing)
                    : string.Empty;
            }

            if (_fishingFailureText != null)
            {
                _fishingFailureText.text = inFishing
                    ? FormatFailureText(CurrentFishingFailure)
                    : string.Empty;
            }
        }

        private void OnSaveDataChanged(SaveDataV1 data)
        {
            Refresh();
        }

        private void OnGameFlowStateChanged(GameFlowState previous, GameFlowState next)
        {
            Refresh();
        }

        private void OnSettingsChanged()
        {
            Refresh();
        }

        private void OnObjectiveLabelChanged(string label)
        {
            SetObjectiveStatus(label);
        }

        private string BuildTensionText(FishingTensionState tensionState, float tensionNormalized)
        {
            var label = $"Line Tension: {tensionState} ({tensionNormalized:0.00})";
            if (!IsHighContrastFishingCuesEnabled())
            {
                return label;
            }

            switch (tensionState)
            {
                case FishingTensionState.Safe:
                    return $"Line Tension: [=] {tensionState} ({tensionNormalized:0.00})";
                case FishingTensionState.Warning:
                    return $"Line Tension: [!!] {tensionState} ({tensionNormalized:0.00})";
                case FishingTensionState.Critical:
                    return $"Line Tension: [!!!] {tensionState} ({tensionNormalized:0.00})";
                default:
                    return label;
            }
        }

        private string FormatFailureText(string failureText)
        {
            var value = failureText ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value) || !IsHighContrastFishingCuesEnabled())
            {
                return value;
            }

            return $"[ALERT] {value}";
        }

        private bool IsHighContrastFishingCuesEnabled()
        {
            return _settingsService != null && _settingsService.HighContrastFishingCues;
        }

        private string BuildFishingStatusText(bool inFishing)
        {
            if (!inFishing)
            {
                return string.Empty;
            }

            var status = CurrentFishingStatus ?? string.Empty;
            if (_conditionsText == null && !string.IsNullOrWhiteSpace(CurrentConditionsLabel))
            {
                status = $"Conditions: {CurrentConditionsLabel}\n{status}".Trim();
            }

            if (_objectiveStatusText == null && !string.IsNullOrWhiteSpace(CurrentObjectiveStatus))
            {
                status = $"{CurrentObjectiveStatus}\n{status}".Trim();
            }

            return status;
        }
    }
}
