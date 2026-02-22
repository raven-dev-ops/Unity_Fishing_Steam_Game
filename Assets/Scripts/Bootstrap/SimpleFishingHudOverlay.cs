using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Fishing;
using UnityEngine;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Core
{
    public sealed class SimpleFishingHudOverlay : MonoBehaviour, IFishingHudOverlay
    {
        [SerializeField] private Text _telemetryText;
        [SerializeField] private Text _tensionText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _failureText;
        [SerializeField] private Text _conditionsText;

        private int _distanceTier = 1;
        private float _depth;

        public void Configure(Text telemetryText, Text tensionText, Text statusText, Text failureText, Text conditionsText)
        {
            _telemetryText = telemetryText;
            _tensionText = tensionText;
            _statusText = statusText;
            _failureText = failureText;
            _conditionsText = conditionsText;
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
    }
}
