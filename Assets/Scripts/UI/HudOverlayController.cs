using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Fishing;
using RavenDevOps.Fishing.Save;
using TMPro;
using UnityEngine;

namespace RavenDevOps.Fishing.UI
{
    public sealed class HudOverlayController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _copecsText;
        [SerializeField] private TMP_Text _dayText;
        [SerializeField] private TMP_Text _distanceTierText;
        [SerializeField] private TMP_Text _depthText;
        [SerializeField] private TMP_Text _tensionStateText;
        [SerializeField] private TMP_Text _fishingStatusText;
        [SerializeField] private TMP_Text _fishingFailureText;

        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private GameFlowManager _gameFlowManager;

        public int CurrentDistanceTier { get; set; } = 1;
        public float CurrentDepth { get; set; }
        public float CurrentTensionNormalized { get; private set; }
        public FishingTensionState CurrentTensionState { get; private set; }
        public string CurrentFishingStatus { get; private set; } = string.Empty;
        public string CurrentFishingFailure { get; private set; } = string.Empty;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged += OnSaveDataChanged;
            }

            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged += OnGameFlowStateChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged -= OnSaveDataChanged;
            }

            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged -= OnGameFlowStateChanged;
            }
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

        public void ClearFishingFeedback()
        {
            CurrentTensionNormalized = 0f;
            CurrentTensionState = FishingTensionState.None;
            CurrentFishingStatus = string.Empty;
            CurrentFishingFailure = string.Empty;
            Refresh();
        }

        public void Refresh()
        {
            if (_saveManager == null)
            {
                return;
            }

            var save = _saveManager.Current;
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
                    ? $"Line Tension: {CurrentTensionState} ({CurrentTensionNormalized:0.00})"
                    : string.Empty;
            }

            if (_fishingStatusText != null)
            {
                _fishingStatusText.text = inFishing ? CurrentFishingStatus : string.Empty;
            }

            if (_fishingFailureText != null)
            {
                _fishingFailureText.text = inFishing ? CurrentFishingFailure : string.Empty;
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
    }
}
