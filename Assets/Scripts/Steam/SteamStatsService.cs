using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

namespace RavenDevOps.Fishing.Steam
{
    public sealed class SteamStatsService : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private bool _verboseLogs = true;
        [SerializeField, Min(0.1f)] private float _storeStatsMinimumIntervalSeconds = 15f;
        [SerializeField, Min(0.1f)] private float _storeStatsFailureInitialBackoffSeconds = 2f;
        [SerializeField, Min(0.1f)] private float _storeStatsFailureMaxBackoffSeconds = 60f;
        [SerializeField, Min(1f)] private float _storeStatsFailureBackoffMultiplier = 2f;
        [SerializeField, Min(0.1f)] private float _requestStatsRetryIntervalSeconds = 5f;

        [Header("Steam Stats")]
        [SerializeField] private string _statTotalCatches = "STAT_TOTAL_CATCHES";
        [SerializeField] private string _statTotalPurchases = "STAT_TOTAL_PURCHASES";
        [SerializeField] private string _statTotalTrips = "STAT_TOTAL_TRIPS";
        [SerializeField] private string _statCatchValueCopecs = "STAT_CATCH_VALUE_COPECS";

        [Header("Steam Achievements")]
        [SerializeField] private string _achievementFirstCatch = "ACH_FIRST_CATCH";
        [SerializeField] private string _achievementFirstPurchase = "ACH_FIRST_PURCHASE";
        [SerializeField] private string _achievementTripMilestone = "ACH_TRIP_5";
        [SerializeField] private int _tripMilestoneTarget = 5;

        private bool _steamReady;
        private bool _statsRequestInFlight;
        private float _nextStatsRequestAt;
        private SteamStoreStatsSyncPolicy _storeSyncPolicy;
        private SteamStoreStatsGateReason _lastReportedThrottleReason = SteamStoreStatsGateReason.None;
        private float _lastReportedThrottleUntil = -1f;

#if STEAMWORKS_NET
        private Callback<UserStatsReceived_t> _userStatsReceived;
        private Callback<UserStatsStored_t> _userStatsStored;
#endif

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Register(this);
            _storeSyncPolicy = new SteamStoreStatsSyncPolicy(
                _storeStatsMinimumIntervalSeconds,
                _storeStatsFailureInitialBackoffSeconds,
                _storeStatsFailureMaxBackoffSeconds,
                _storeStatsFailureBackoffMultiplier);
            TouchConfigInNonSteamBuilds();
            InitializeSteamHooks();
        }

        private void OnEnable()
        {
            if (_saveManager != null)
            {
                _saveManager.CatchRecorded += OnCatchRecorded;
                _saveManager.TripCompleted += OnTripCompleted;
                _saveManager.PurchaseRecorded += OnPurchaseRecorded;
                _saveManager.SaveDataChanged += OnSaveDataChanged;
            }

            MarkSyncPending("enable");
        }

        private void OnDisable()
        {
            if (_saveManager != null)
            {
                _saveManager.CatchRecorded -= OnCatchRecorded;
                _saveManager.TripCompleted -= OnTripCompleted;
                _saveManager.PurchaseRecorded -= OnPurchaseRecorded;
                _saveManager.SaveDataChanged -= OnSaveDataChanged;
            }
        }

        private void Update()
        {
            if (!_steamReady)
            {
                InitializeSteamHooks();
                return;
            }

            SyncStatsFromSave();
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
#if STEAMWORKS_NET
            _userStatsReceived = null;
            _userStatsStored = null;
#endif
        }

        private void OnCatchRecorded(CatchLogEntry entry)
        {
            MarkSyncPending("catch-recorded");
        }

        private void OnTripCompleted(int totalTrips)
        {
            MarkSyncPending("trip-completed");
        }

        private void OnPurchaseRecorded(string itemId, int priceCopecs, int totalPurchases)
        {
            MarkSyncPending("purchase-recorded");
        }

        private void OnSaveDataChanged(SaveDataV1 data)
        {
            MarkSyncPending("save-data-changed");
        }

        private void InitializeSteamHooks()
        {
            if (_steamReady)
            {
                return;
            }

            if (!SteamBootstrap.IsSteamInitialized)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (_statsRequestInFlight || now + 0.0001f < _nextStatsRequestAt)
            {
                return;
            }

#if STEAMWORKS_NET
            _userStatsReceived ??= Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
            _userStatsStored ??= Callback<UserStatsStored_t>.Create(OnUserStatsStored);

            if (!SteamUserStats.RequestCurrentStats())
            {
                _nextStatsRequestAt = now + Mathf.Max(0.1f, _requestStatsRetryIntervalSeconds);
                if (_verboseLogs)
                {
                    Debug.LogWarning($"SteamStatsService: RequestCurrentStats returned false; retry in {_nextStatsRequestAt - now:0.00}s.");
                }

                return;
            }

            _statsRequestInFlight = true;
            if (_verboseLogs)
            {
                Debug.Log("SteamStatsService: requested current Steam stats.");
            }
#else
            if (_verboseLogs)
            {
                Debug.Log("SteamStatsService: STEAMWORKS_NET not enabled, running without Steam stats.");
            }
#endif
        }

        private void SyncStatsFromSave()
        {
            if (_storeSyncPolicy == null || !_storeSyncPolicy.HasPendingSync)
            {
                return;
            }

            if (_saveManager == null)
            {
                return;
            }

            var save = _saveManager.Current;
            if (save == null || save.stats == null)
            {
                return;
            }

            var now = Time.unscaledTime;
            var gateReason = _storeSyncPolicy.GetGateReason(now);
            if (gateReason != SteamStoreStatsGateReason.None)
            {
                ReportThrottledWriteIfNeeded(gateReason, now);
                return;
            }

            ClearThrottleReport();

#if STEAMWORKS_NET
            if (!_steamReady)
            {
                return;
            }

            SetStat(_statTotalCatches, Mathf.Max(0, save.stats.totalFishCaught));
            SetStat(_statTotalPurchases, Mathf.Max(0, save.stats.totalPurchases));
            SetStat(_statTotalTrips, Mathf.Max(0, save.stats.totalTrips));
            SetStat(_statCatchValueCopecs, Mathf.Max(0, save.stats.totalCatchValueCopecs));

            if (save.stats.totalFishCaught > 0)
            {
                UnlockAchievement(_achievementFirstCatch);
            }

            if (save.stats.totalPurchases > 0)
            {
                UnlockAchievement(_achievementFirstPurchase);
            }

            if (save.stats.totalTrips >= Mathf.Max(1, _tripMilestoneTarget))
            {
                UnlockAchievement(_achievementTripMilestone);
            }

            var storeResult = SteamUserStats.StoreStats();
            _storeSyncPolicy.RecordStoreAttempt(now, storeResult);
            LogStoreStatsDiagnostics(storeResult ? "StoreStats attempt queued" : "StoreStats attempt failed", now, warning: !storeResult);
#else
            _storeSyncPolicy.RecordStoreAttempt(now, success: true);
#endif
        }

#if STEAMWORKS_NET
        private void OnUserStatsReceived(UserStatsReceived_t callback)
        {
            var appId = SteamUtils.GetAppID().m_AppId;
            if (callback.m_nGameID != appId)
            {
                return;
            }

            _statsRequestInFlight = false;
            _steamReady = callback.m_eResult == EResult.k_EResultOK;
            if (!_steamReady)
            {
                _nextStatsRequestAt = Time.unscaledTime + Mathf.Max(0.1f, _requestStatsRetryIntervalSeconds);
                if (_verboseLogs)
                {
                    Debug.LogWarning($"SteamStatsService: stats request failed ({callback.m_eResult}); retry in {_nextStatsRequestAt - Time.unscaledTime:0.00}s.");
                }

                return;
            }

            _nextStatsRequestAt = 0f;
            MarkSyncPending("steam-stats-ready");
            if (_verboseLogs)
            {
                Debug.Log("SteamStatsService: Steam stats ready.");
            }
        }

        private void OnUserStatsStored(UserStatsStored_t callback)
        {
            var appId = SteamUtils.GetAppID().m_AppId;
            if (callback.m_nGameID != appId)
            {
                return;
            }

            if (callback.m_eResult != EResult.k_EResultOK)
            {
                if (_storeSyncPolicy != null)
                {
                    _storeSyncPolicy.RecordStoreCallbackFailure(Time.unscaledTime);
                }

                if (_verboseLogs)
                {
                    Debug.LogWarning($"SteamStatsService: stats store callback result was {callback.m_eResult}; queued backoff retry.");
                    LogStoreStatsDiagnostics("StoreStats callback failure", Time.unscaledTime, warning: false);
                }
            }
        }

        private void SetStat(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            SteamUserStats.SetStat(key, Mathf.Max(0, value));
        }

        private void UnlockAchievement(string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
            {
                return;
            }

            SteamUserStats.SetAchievement(achievementId);
        }
#endif

        private void MarkSyncPending(string reason)
        {
            if (_storeSyncPolicy == null)
            {
                return;
            }

            var wasPending = _storeSyncPolicy.HasPendingSync;
            _storeSyncPolicy.MarkPending();
            if (_verboseLogs && !wasPending)
            {
                Debug.Log($"SteamStatsService: marked sync pending ({reason}).");
            }
        }

        private void ReportThrottledWriteIfNeeded(SteamStoreStatsGateReason gateReason, float now)
        {
            if (_storeSyncPolicy == null
                || gateReason == SteamStoreStatsGateReason.None
                || gateReason == SteamStoreStatsGateReason.NoPendingSync)
            {
                return;
            }

            var nextAttemptAt = _storeSyncPolicy.NextStoreAttemptAt;
            if (gateReason == _lastReportedThrottleReason
                && Mathf.Abs(_lastReportedThrottleUntil - nextAttemptAt) <= 0.0001f)
            {
                return;
            }

            _lastReportedThrottleReason = gateReason;
            _lastReportedThrottleUntil = nextAttemptAt;
            _storeSyncPolicy.RecordThrottledWrite();

            if (_verboseLogs)
            {
                var waitSeconds = Mathf.Max(0f, nextAttemptAt - now);
                Debug.Log($"SteamStatsService: throttled StoreStats write ({gateReason}); retry in {waitSeconds:0.00}s.");
                LogStoreStatsDiagnostics("StoreStats throttled", now, warning: false);
            }
        }

        private void ClearThrottleReport()
        {
            _lastReportedThrottleReason = SteamStoreStatsGateReason.None;
            _lastReportedThrottleUntil = -1f;
        }

        private void LogStoreStatsDiagnostics(string context, float now, bool warning)
        {
            if (!_verboseLogs || _storeSyncPolicy == null)
            {
                return;
            }

            var nextAttemptInSeconds = Mathf.Max(0f, _storeSyncPolicy.NextStoreAttemptAt - now);
            var message = $"SteamStatsService: {context}; pending={_storeSyncPolicy.HasPendingSync} attempts={_storeSyncPolicy.StoreAttemptCount} success={_storeSyncPolicy.StoreSuccessCount} failures={_storeSyncPolicy.StoreFailureCount} throttled={_storeSyncPolicy.StoreThrottledCount} nextAttemptIn={nextAttemptInSeconds:0.00}s backoff={_storeSyncPolicy.ActiveBackoffSeconds:0.00}s.";
            if (warning)
            {
                Debug.LogWarning(message);
                return;
            }

            Debug.Log(message);
        }

        private void TouchConfigInNonSteamBuilds()
        {
#if !STEAMWORKS_NET
            _ = _statTotalCatches;
            _ = _statTotalPurchases;
            _ = _statTotalTrips;
            _ = _statCatchValueCopecs;
            _ = _achievementFirstCatch;
            _ = _achievementFirstPurchase;
            _ = _achievementTripMilestone;
            _ = _tripMilestoneTarget;
            _ = _storeStatsMinimumIntervalSeconds;
            _ = _storeStatsFailureInitialBackoffSeconds;
            _ = _storeStatsFailureMaxBackoffSeconds;
            _ = _storeStatsFailureBackoffMultiplier;
            _ = _requestStatsRetryIntervalSeconds;
#endif
        }
    }
}
