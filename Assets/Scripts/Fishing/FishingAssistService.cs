using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingAssistSettings
    {
        public bool EnableNoBitePity = true;
        public int NoBitePityThresholdCasts = 3;
        public float PityBiteDelayScale = 0.55f;
        public bool EnableAdaptiveHookWindow = true;
        public int AdaptiveFailureThreshold = 3;
        public float AdaptiveHookWindowBonusSeconds = 0.35f;
        public int AssistCooldownCatches = 2;
    }

    public readonly struct FishingAssistSnapshot
    {
        public FishingAssistSnapshot(
            int noBiteStreak,
            int failureStreak,
            int cooldownCatchesRemaining,
            int pityActivationCount,
            int adaptiveActivationCount)
        {
            NoBiteStreak = noBiteStreak;
            FailureStreak = failureStreak;
            CooldownCatchesRemaining = cooldownCatchesRemaining;
            PityActivationCount = pityActivationCount;
            AdaptiveActivationCount = adaptiveActivationCount;
        }

        public int NoBiteStreak { get; }
        public int FailureStreak { get; }
        public int CooldownCatchesRemaining { get; }
        public int PityActivationCount { get; }
        public int AdaptiveActivationCount { get; }
    }

    public sealed class FishingAssistService
    {
        private FishingAssistSettings _settings = new FishingAssistSettings();
        private int _noBiteStreak;
        private int _failureStreak;
        private int _cooldownCatchesRemaining;
        private int _pityActivationCount;
        private int _adaptiveActivationCount;
        private bool _assistActivatedForCurrentAttempt;

        public void Configure(FishingAssistSettings settings)
        {
            _settings = settings ?? new FishingAssistSettings();
        }

        public bool TryActivateNoBitePity()
        {
            if (_settings == null || !_settings.EnableNoBitePity)
            {
                return false;
            }

            if (_cooldownCatchesRemaining > 0)
            {
                return false;
            }

            var threshold = Mathf.Max(1, _settings.NoBitePityThresholdCasts);
            if ((_noBiteStreak + 1) < threshold)
            {
                return false;
            }

            _pityActivationCount++;
            _assistActivatedForCurrentAttempt = true;
            return true;
        }

        public float ApplyPityDelayScale(float baseDelaySeconds, bool pityActivated)
        {
            var clampedBase = Mathf.Max(0f, baseDelaySeconds);
            if (!pityActivated)
            {
                return clampedBase;
            }

            var scale = Mathf.Clamp(_settings != null ? _settings.PityBiteDelayScale : 0.55f, 0.25f, 1f);
            return clampedBase * scale;
        }

        public float ResolveHookWindow(float baseWindowSeconds, out bool adaptiveActivated)
        {
            adaptiveActivated = false;
            var clampedBase = Mathf.Max(0.2f, baseWindowSeconds);

            if (_settings == null || !_settings.EnableAdaptiveHookWindow)
            {
                return clampedBase;
            }

            if (_cooldownCatchesRemaining > 0)
            {
                return clampedBase;
            }

            var threshold = Mathf.Max(1, _settings.AdaptiveFailureThreshold);
            if (_failureStreak < threshold)
            {
                return clampedBase;
            }

            var bonus = Mathf.Clamp(_settings.AdaptiveHookWindowBonusSeconds, 0f, 0.75f);
            if (bonus <= 0f)
            {
                return clampedBase;
            }

            adaptiveActivated = true;
            _adaptiveActivationCount++;
            _assistActivatedForCurrentAttempt = true;
            return clampedBase + bonus;
        }

        public void RecordCastResult(bool biteOccurred)
        {
            if (biteOccurred)
            {
                _noBiteStreak = 0;
                return;
            }

            _noBiteStreak++;
        }

        public void RecordCatchOutcome(bool success)
        {
            if (success)
            {
                _failureStreak = 0;
                if (_cooldownCatchesRemaining > 0)
                {
                    _cooldownCatchesRemaining--;
                }

                _assistActivatedForCurrentAttempt = false;
                return;
            }

            _failureStreak++;
            if (_assistActivatedForCurrentAttempt)
            {
                _cooldownCatchesRemaining = Mathf.Max(_cooldownCatchesRemaining, Mathf.Max(0, _settings.AssistCooldownCatches));
            }

            _assistActivatedForCurrentAttempt = false;
        }

        public FishingAssistSnapshot Snapshot()
        {
            return new FishingAssistSnapshot(
                _noBiteStreak,
                _failureStreak,
                _cooldownCatchesRemaining,
                _pityActivationCount,
                _adaptiveActivationCount);
        }
    }
}
