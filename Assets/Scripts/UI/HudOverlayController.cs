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
            _saveManager ??= FindObjectOfType<SaveManager>();
            _gameFlowManager ??= FindObjectOfType<GameFlowManager>();
        }

        private void Update()
        {
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
    }
}
