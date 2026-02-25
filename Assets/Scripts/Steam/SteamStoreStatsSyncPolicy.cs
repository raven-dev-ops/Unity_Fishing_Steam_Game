using UnityEngine;

namespace RavenDevOps.Fishing.Steam
{
    public enum SteamStoreStatsGateReason
    {
        None = 0,
        NoPendingSync = 1,
        MinimumInterval = 2,
        Backoff = 3
    }

    public sealed class SteamStoreStatsSyncPolicy
    {
        private readonly float _minimumIntervalSeconds;
        private readonly float _failureInitialBackoffSeconds;
        private readonly float _failureMaxBackoffSeconds;
        private readonly float _failureBackoffMultiplier;

        private float _activeBackoffSeconds;

        public SteamStoreStatsSyncPolicy(
            float minimumIntervalSeconds,
            float failureInitialBackoffSeconds,
            float failureMaxBackoffSeconds,
            float failureBackoffMultiplier)
        {
            _minimumIntervalSeconds = Mathf.Max(0.1f, minimumIntervalSeconds);
            _failureInitialBackoffSeconds = Mathf.Max(0.1f, failureInitialBackoffSeconds);
            _failureMaxBackoffSeconds = Mathf.Max(_failureInitialBackoffSeconds, failureMaxBackoffSeconds);
            _failureBackoffMultiplier = Mathf.Max(1f, failureBackoffMultiplier);
        }

        public bool HasPendingSync { get; private set; }
        public int StoreAttemptCount { get; private set; }
        public int StoreSuccessCount { get; private set; }
        public int StoreFailureCount { get; private set; }
        public int StoreThrottledCount { get; private set; }
        public float NextStoreAttemptAt { get; private set; }
        public float ActiveBackoffSeconds => _activeBackoffSeconds;

        public void MarkPending()
        {
            HasPendingSync = true;
        }

        public SteamStoreStatsGateReason GetGateReason(float now)
        {
            if (!HasPendingSync)
            {
                return SteamStoreStatsGateReason.NoPendingSync;
            }

            if (now + 0.0001f >= NextStoreAttemptAt)
            {
                return SteamStoreStatsGateReason.None;
            }

            return _activeBackoffSeconds > 0.0001f
                ? SteamStoreStatsGateReason.Backoff
                : SteamStoreStatsGateReason.MinimumInterval;
        }

        public void RecordThrottledWrite()
        {
            StoreThrottledCount += 1;
        }

        public void RecordStoreAttempt(float now, bool success)
        {
            StoreAttemptCount += 1;
            if (success)
            {
                StoreSuccessCount += 1;
                HasPendingSync = false;
                _activeBackoffSeconds = 0f;
                NextStoreAttemptAt = now + _minimumIntervalSeconds;
                return;
            }

            ApplyFailureBackoff(now);
        }

        public void RecordStoreCallbackFailure(float now)
        {
            ApplyFailureBackoff(now);
        }

        private void ApplyFailureBackoff(float now)
        {
            StoreFailureCount += 1;
            HasPendingSync = true;
            if (_activeBackoffSeconds <= 0.0001f)
            {
                _activeBackoffSeconds = _failureInitialBackoffSeconds;
            }
            else
            {
                _activeBackoffSeconds = Mathf.Min(
                    _failureMaxBackoffSeconds,
                    _activeBackoffSeconds * _failureBackoffMultiplier);
            }

            NextStoreAttemptAt = now + _activeBackoffSeconds;
        }
    }
}
