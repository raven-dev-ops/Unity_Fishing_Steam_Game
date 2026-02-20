using RavenDevOps.Fishing.Core;
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

        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private GameFlowManager _gameFlowManager;

        public int CurrentDistanceTier { get; set; } = 1;
        public float CurrentDepth { get; set; }

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
