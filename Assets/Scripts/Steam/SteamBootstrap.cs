using RavenDevOps.Fishing.Core;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

namespace RavenDevOps.Fishing.Steam
{
    public sealed class SteamBootstrap : MonoBehaviour
    {
        private static SteamBootstrap _instance;
        private static bool _steamInitialized;
        private static string _lastFallbackReason = string.Empty;

        [SerializeField] private uint _steamAppId = 480;
        [SerializeField] private bool _dontDestroyOnLoad = true;
        [SerializeField] private bool _verboseLogs = true;

        public static bool IsSteamInitialized => _steamInitialized;
        public static string LastFallbackReason => _lastFallbackReason;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            RuntimeServiceRegistry.Register(this);
            TryInitializeSteam();
        }

        private void Update()
        {
#if STEAMWORKS_NET
            if (_steamInitialized)
            {
                SteamAPI.RunCallbacks();
            }
#endif
        }

        private void OnApplicationQuit()
        {
            ShutdownSteam();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ShutdownSteam();
                _instance = null;
            }

            RuntimeServiceRegistry.Unregister(this);
        }

        private void TryInitializeSteam()
        {
#if STEAMWORKS_NET
            if (_steamInitialized)
            {
                if (_verboseLogs)
                {
                    Debug.Log("SteamBootstrap: Steam API already initialized, skipping duplicate init.");
                }

                return;
            }

            if (!Packsize.Test())
            {
                SetFallback("Packsize mismatch. Steamworks binaries are not compatible with this build.");
                return;
            }

            if (!DllCheck.Test())
            {
                SetFallback("Steamworks DLL check failed. Ensure Steamworks redistributables are present.");
                return;
            }

            try
            {
                _steamInitialized = SteamAPI.Init();
            }
            catch (System.Exception ex)
            {
                SetFallback($"SteamAPI.Init threw exception: {ex.Message}");
                return;
            }

            if (!_steamInitialized)
            {
                SetFallback("SteamAPI.Init returned false (not launched via Steam or Steam client unavailable).");
                return;
            }

            _lastFallbackReason = string.Empty;
            var appId = SteamUtils.GetAppID().m_AppId;
            Debug.Log($"SteamBootstrap: Steam initialized successfully. RequestedAppId={_steamAppId}, RuntimeAppId={appId}.");
#else
            SetFallback("STEAMWORKS_NET symbol not defined. Running non-Steam fallback mode.");
#endif
        }

        private static void ShutdownSteam()
        {
#if STEAMWORKS_NET
            if (!_steamInitialized)
            {
                return;
            }

            SteamAPI.Shutdown();
            _steamInitialized = false;
#endif
        }

        private void SetFallback(string reason)
        {
            _steamInitialized = false;
            _lastFallbackReason = reason ?? "Unknown fallback reason.";

            if (_verboseLogs)
            {
                Debug.LogWarning($"SteamBootstrap: fallback mode active. {_lastFallbackReason}");
            }
        }
    }
}
