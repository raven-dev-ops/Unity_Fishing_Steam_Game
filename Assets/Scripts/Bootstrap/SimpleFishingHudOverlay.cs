using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Fishing;
using TMPro;
using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    public sealed class SimpleFishingHudOverlay : MonoBehaviour, IFishingHudOverlay
    {
        [SerializeField] private TMP_Text _telemetryText;
        [SerializeField] private TMP_Text _tensionText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _failureText;
        [SerializeField] private TMP_Text _conditionsText;
        [SerializeField] private TMP_Text _objectiveText;
        [SerializeField] private ObjectivesService _objectivesService;
        [SerializeField] private string _objectiveFallbackText = "Objective: Follow current task goals.";

        private int _distanceTier = 1;
        private float _depth;

        public void Configure(
            TMP_Text telemetryText,
            TMP_Text tensionText,
            TMP_Text statusText,
            TMP_Text failureText,
            TMP_Text conditionsText,
            TMP_Text objectiveText)
        {
            _telemetryText = telemetryText;
            _tensionText = tensionText;
            _statusText = statusText;
            _failureText = failureText;
            _conditionsText = conditionsText;
            _objectiveText = objectiveText;
            RefreshObjectiveLabel();
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _objectivesService, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            if (_objectivesService != null)
            {
                _objectivesService.ObjectiveLabelChanged += OnObjectiveLabelChanged;
            }

            RefreshObjectiveLabel();
        }

        private void OnDisable()
        {
            if (_objectivesService != null)
            {
                _objectivesService.ObjectiveLabelChanged -= OnObjectiveLabelChanged;
            }
        }

        public void SetFishingTelemetry(int distanceTier, float depth)
        {
            _distanceTier = Mathf.Max(1, distanceTier);
            _depth = Mathf.Max(0f, depth);
            if (_telemetryText != null)
            {
                _telemetryText.text = $"Distance Tier: {_distanceTier} | Depth: {_depth:0.0}";
            }
        }

        public void SetFishingTension(float normalizedTension, FishingTensionState tensionState)
        {
            if (_tensionText != null)
            {
                _tensionText.text = $"Tension: {tensionState} ({Mathf.Clamp01(normalizedTension):0.00})";
            }
        }

        public void SetFishingStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = string.IsNullOrWhiteSpace(status)
                    ? "Status: Ready."
                    : $"Status: {status}";
            }
        }

        public void SetFishingFailure(string failure)
        {
            if (_failureText != null)
            {
                _failureText.text = string.IsNullOrWhiteSpace(failure)
                    ? string.Empty
                    : $"Failure: {failure}";
            }
        }

        public void SetFishingConditions(string conditionLabel)
        {
            if (_conditionsText != null)
            {
                _conditionsText.text = string.IsNullOrWhiteSpace(conditionLabel)
                    ? string.Empty
                    : $"Conditions: {conditionLabel}";
            }
        }

        private void OnObjectiveLabelChanged(string label)
        {
            ApplyObjectiveLabel(label);
        }

        private void RefreshObjectiveLabel()
        {
            if (_objectiveText == null)
            {
                return;
            }

            if (_objectivesService == null)
            {
                ApplyObjectiveLabel(_objectiveFallbackText);
                return;
            }

            ApplyObjectiveLabel(_objectivesService.BuildActiveObjectiveLabel());
        }

        private void ApplyObjectiveLabel(string label)
        {
            if (_objectiveText == null)
            {
                return;
            }

            _objectiveText.text = string.IsNullOrWhiteSpace(label)
                ? _objectiveFallbackText
                : label;
        }
    }
}
