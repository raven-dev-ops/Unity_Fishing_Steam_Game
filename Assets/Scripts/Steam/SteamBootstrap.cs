using RavenDevOps.Fishing.Core;
using System;
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
        [SerializeField] private bool _enforceSteamRelaunch = false;
        [SerializeField] private bool _allowRelaunchInDevelopmentBuild = false;
        [SerializeField] private bool _verboseLogs;

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

            if (!TryHandleRelaunchGuard())
            {
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

        private bool TryHandleRelaunchGuard()
        {
#if STEAMWORKS_NET
            if (!ShouldAttemptSteamRelaunch(out var skipReason))
            {
                if (_verboseLogs)
                {
                    Debug.Log($"SteamBootstrap: skipping Steam relaunch guard ({skipReason}).");
                }

                return true;
            }

            try
            {
                var shouldRelaunch = SteamAPI.RestartAppIfNecessary(new AppId_t(_steamAppId));
                if (shouldRelaunch)
                {
                    SetFallback($"RestartAppIfNecessary requested relaunch via Steam for appId={_steamAppId}. Quitting current process.");
                    Application.Quit();
                    return false;
                }

                if (_verboseLogs)
                {
                    Debug.Log($"SteamBootstrap: relaunch guard passed for appId={_steamAppId}.");
                }
            }
            catch (System.Exception ex)
            {
                SetFallback($"RestartAppIfNecessary threw exception: {ex.Message}");
                return false;
            }
#endif

            return true;
        }

        private bool ShouldAttemptSteamRelaunch(out string skipReason)
        {
            if (!_enforceSteamRelaunch)
            {
                skipReason = "feature disabled in inspector";
                return false;
            }

            if (_steamAppId == 0)
            {
                skipReason = "steam app id is not configured";
                return false;
            }

            if (Application.isEditor)
            {
                skipReason = "running in Unity editor";
                return false;
            }

            if (Debug.isDebugBuild && !_allowRelaunchInDevelopmentBuild)
            {
                skipReason = "debug/development build without override";
                return false;
            }

            skipReason = string.Empty;
            return true;
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
                if (IsExpectedNonSteamFallback(_lastFallbackReason))
                {
                    if (Application.isBatchMode)
                    {
                        Debug.Log($"SteamBootstrap: fallback mode active. {_lastFallbackReason}");
                    }

                    return;
                }

                Debug.LogWarning($"SteamBootstrap: fallback mode active. {_lastFallbackReason}");
                return;
            }

            if (!IsExpectedNonSteamFallback(_lastFallbackReason))
            {
                Debug.LogWarning($"SteamBootstrap: fallback mode active. {_lastFallbackReason}");
            }
        }

        private static bool IsExpectedNonSteamFallback(string reason)
        {
            return !string.IsNullOrWhiteSpace(reason)
                && reason.IndexOf("STEAMWORKS_NET symbol not defined", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
