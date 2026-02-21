using UnityEngine;

namespace RavenDevOps.Fishing.Save
{
    public sealed class SaveWriteThrottle
    {
        private readonly float _minimumIntervalSeconds;
        private float _lastPersistRealtimeSeconds = float.NegativeInfinity;
        private bool _pending;

        public SaveWriteThrottle(float minimumIntervalSeconds)
        {
            _minimumIntervalSeconds = Mathf.Max(0f, minimumIntervalSeconds);
        }

        public bool HasPendingRequest => _pending;

        public bool Request(float realtimeSinceStartup, bool forceImmediate)
        {
            if (forceImmediate || CanPersist(realtimeSinceStartup))
            {
                return true;
            }

            _pending = true;
            return false;
        }

        public bool TryFlush(float realtimeSinceStartup)
        {
            if (!_pending)
            {
                return false;
            }

            if (!CanPersist(realtimeSinceStartup))
            {
                return false;
            }

            return true;
        }

        public void MarkPersisted(float realtimeSinceStartup)
        {
            _lastPersistRealtimeSeconds = realtimeSinceStartup;
            _pending = false;
        }

        public void MarkPending()
        {
            _pending = true;
        }

        private bool CanPersist(float realtimeSinceStartup)
        {
            if (_minimumIntervalSeconds <= 0f)
            {
                return true;
            }

            var elapsed = realtimeSinceStartup - _lastPersistRealtimeSeconds;
            return elapsed >= _minimumIntervalSeconds;
        }
    }
}
