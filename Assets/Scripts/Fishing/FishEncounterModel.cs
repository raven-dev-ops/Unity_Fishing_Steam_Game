using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishEncounterModel
    {
        private const float SafeThreshold = 0.45f;
        private const float WarningThreshold = 0.75f;

        private FishDefinition _fish;
        private float _staminaRemaining;
        private float _escapeTimeRemaining;
        private float _tension;
        private float _elapsedFightSeconds;

        public bool IsActive { get; private set; }
        public float TensionNormalized => _tension;
        public float StaminaRemaining => _staminaRemaining;
        public float EscapeTimeRemaining => _escapeTimeRemaining;

        public void Begin(FishDefinition fish, float initialTension = 0.2f)
        {
            _fish = fish;
            _staminaRemaining = fish != null ? Mathf.Max(0.2f, fish.fightStamina) : 0f;
            _escapeTimeRemaining = fish != null ? Mathf.Max(0.5f, fish.escapeSeconds) : 0f;
            _tension = Mathf.Clamp01(initialTension);
            _elapsedFightSeconds = 0f;
            IsActive = fish != null;
        }

        public void End()
        {
            IsActive = false;
            _fish = null;
            _staminaRemaining = 0f;
            _escapeTimeRemaining = 0f;
            _tension = 0f;
            _elapsedFightSeconds = 0f;
        }

        public FishingTensionState Step(float deltaTime, bool isReeling, out bool landed, out FishingFailReason failReason)
        {
            landed = false;
            failReason = FishingFailReason.None;

            if (!IsActive || _fish == null)
            {
                return FishingTensionState.None;
            }

            var dt = Mathf.Max(0f, deltaTime);
            _elapsedFightSeconds += dt;

            var pullIntensity = Mathf.Max(0.1f, _fish.pullIntensity);
            var wobble = Mathf.Sin((_elapsedFightSeconds + pullIntensity) * 4.2f) * 0.02f;

            if (isReeling)
            {
                var rise = (0.36f + pullIntensity * 0.55f) * dt;
                _tension += rise + wobble;

                var efficientZone = 1f - Mathf.Clamp01(_tension);
                var staminaDrain = (0.55f + efficientZone * 1.35f) * dt;
                _staminaRemaining -= staminaDrain;
            }
            else
            {
                _tension += (0.15f + pullIntensity * 0.2f) * dt + wobble;
                _tension -= 0.55f * dt;
            }

            _tension = Mathf.Clamp01(_tension);
            _escapeTimeRemaining -= dt * (isReeling ? 0.35f : 1f);

            if (_tension >= 0.999f)
            {
                failReason = FishingFailReason.LineSnap;
                End();
                return FishingTensionState.Critical;
            }

            if (_escapeTimeRemaining <= 0f)
            {
                failReason = FishingFailReason.FishEscaped;
                End();
                return ResolveTensionState(_tension);
            }

            if (_staminaRemaining <= 0f)
            {
                landed = true;
                End();
                return ResolveTensionState(_tension);
            }

            return ResolveTensionState(_tension);
        }

        public static FishingTensionState ResolveTensionState(float tension)
        {
            var normalized = Mathf.Clamp01(tension);
            if (normalized < SafeThreshold)
            {
                return FishingTensionState.Safe;
            }

            if (normalized < WarningThreshold)
            {
                return FishingTensionState.Warning;
            }

            return FishingTensionState.Critical;
        }
    }
}
