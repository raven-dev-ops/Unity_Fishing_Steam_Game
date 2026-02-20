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
        private bool _syncPending;

#if STEAMWORKS_NET
        private Callback<UserStatsReceived_t> _userStatsReceived;
        private Callback<UserStatsStored_t> _userStatsStored;
#endif

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Register(this);
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

            _syncPending = true;
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

            if (_syncPending)
            {
                SyncStatsFromSave();
            }
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
            _syncPending = true;
        }

        private void OnTripCompleted(int totalTrips)
        {
            _syncPending = true;
        }

        private void OnPurchaseRecorded(string itemId, int priceCopecs, int totalPurchases)
        {
            _syncPending = true;
        }

        private void OnSaveDataChanged(SaveDataV1 data)
        {
            _syncPending = true;
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

#if STEAMWORKS_NET
            _userStatsReceived ??= Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
            _userStatsStored ??= Callback<UserStatsStored_t>.Create(OnUserStatsStored);

            if (!SteamUserStats.RequestCurrentStats())
            {
                if (_verboseLogs)
                {
                    Debug.LogWarning("SteamStatsService: RequestCurrentStats returned false; retrying.");
                }

                return;
            }

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
            if (_saveManager == null)
            {
                return;
            }

            var save = _saveManager.Current;
            if (save == null || save.stats == null)
            {
                return;
            }

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

            if (!SteamUserStats.StoreStats() && _verboseLogs)
            {
                Debug.LogWarning("SteamStatsService: StoreStats returned false.");
            }
#endif

            _syncPending = false;
        }

#if STEAMWORKS_NET
        private void OnUserStatsReceived(UserStatsReceived_t callback)
        {
            var appId = SteamUtils.GetAppID().m_AppId;
            if (callback.m_nGameID != appId)
            {
                return;
            }

            _steamReady = callback.m_eResult == EResult.k_EResultOK;
            if (!_steamReady)
            {
                if (_verboseLogs)
                {
                    Debug.LogWarning($"SteamStatsService: stats request failed ({callback.m_eResult}).");
                }

                return;
            }

            _syncPending = true;
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

            if (callback.m_eResult != EResult.k_EResultOK && _verboseLogs)
            {
                Debug.LogWarning($"SteamStatsService: stats store result was {callback.m_eResult}.");
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
    }
}
