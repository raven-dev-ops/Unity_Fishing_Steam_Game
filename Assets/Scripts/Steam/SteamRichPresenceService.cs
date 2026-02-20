using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

namespace RavenDevOps.Fishing.Steam
{
    public sealed class SteamRichPresenceService : MonoBehaviour
    {
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private bool _enabledByDefault = true;
        [SerializeField] private float _updateCooldownSeconds = 6f;
        [SerializeField] private bool _verboseLogs;

        private bool _dirty = true;
        private bool _clearedDueToDisabled;
        private float _lastUpdateTime = -999f;
        private string _lastStatus = string.Empty;
        private string _lastDetails = string.Empty;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Register(this);
        }

        private void OnEnable()
        {
            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged += OnStateChanged;
            }

            if (_saveManager != null)
            {
                _saveManager.LevelChanged += OnLevelChanged;
            }

            if (_settingsService != null)
            {
                _settingsService.SettingsChanged += OnSettingsChanged;
            }
        }

        private void OnDisable()
        {
            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged -= OnStateChanged;
            }

            if (_saveManager != null)
            {
                _saveManager.LevelChanged -= OnLevelChanged;
            }

            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= OnSettingsChanged;
            }
        }

        private void Update()
        {
            if (!_dirty)
            {
                return;
            }

            if (!IsFeatureEnabled())
            {
                ClearPresenceIfNeeded();
                _dirty = false;
                return;
            }

            if (!SteamBootstrap.IsSteamInitialized)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now - _lastUpdateTime < Mathf.Max(1f, _updateCooldownSeconds))
            {
                return;
            }

            ApplyPresence(now);
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        private void OnStateChanged(GameFlowState previous, GameFlowState next)
        {
            _dirty = true;
        }

        private void OnLevelChanged(int previousLevel, int newLevel)
        {
            _dirty = true;
        }

        private void OnSettingsChanged()
        {
            _dirty = true;
        }

        private bool IsFeatureEnabled()
        {
            if (!_enabledByDefault)
            {
                return false;
            }

            return _settingsService == null || _settingsService.SteamRichPresenceEnabled;
        }

        private void ApplyPresence(float now)
        {
#if STEAMWORKS_NET
            var status = BuildStatusString();
            var details = BuildDetailsString();
            if (string.Equals(status, _lastStatus) && string.Equals(details, _lastDetails))
            {
                _dirty = false;
                return;
            }

            SteamFriends.SetRichPresence("status", status);
            SteamFriends.SetRichPresence("details", details);

            _lastStatus = status;
            _lastDetails = details;
            _lastUpdateTime = now;
            _dirty = false;
            _clearedDueToDisabled = false;

            if (_verboseLogs)
            {
                Debug.Log($"SteamRichPresenceService: status='{status}', details='{details}'.");
            }
#endif
        }

        private void ClearPresenceIfNeeded()
        {
#if STEAMWORKS_NET
            if (_clearedDueToDisabled || !SteamBootstrap.IsSteamInitialized)
            {
                return;
            }

            SteamFriends.SetRichPresence("status", null);
            SteamFriends.SetRichPresence("details", null);
            _clearedDueToDisabled = true;
            _lastStatus = string.Empty;
            _lastDetails = string.Empty;
            if (_verboseLogs)
            {
                Debug.Log("SteamRichPresenceService: rich presence disabled and cleared.");
            }
#endif
        }

        private string BuildStatusString()
        {
            if (_gameFlowManager == null)
            {
                return "Loading";
            }

            switch (_gameFlowManager.CurrentState)
            {
                case GameFlowState.MainMenu:
                    return "Browsing menus";
                case GameFlowState.Harbor:
                    return "At harbor";
                case GameFlowState.Fishing:
                    return "Fishing at sea";
                case GameFlowState.Pause:
                    return "Paused";
                case GameFlowState.Cinematic:
                    return "Watching intro";
                default:
                    return "Loading";
            }
        }

        private string BuildDetailsString()
        {
            if (_saveManager == null || _saveManager.Current == null)
            {
                return "Starting new session";
            }

            return $"Level {_saveManager.CurrentLevel} | {_saveManager.Current.copecs} copecs";
        }
    }
}
