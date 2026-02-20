using RavenDevOps.Fishing.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.UI
{
    public sealed class ModDiagnosticsPanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _summaryText;
        [SerializeField] private TMP_Text _safeModeStatusText;
        [SerializeField] private TMP_Text _acceptedModsText;
        [SerializeField] private TMP_Text _rejectedModsText;
        [SerializeField] private TMP_Text _messagesText;
        [SerializeField] private Toggle _safeModeNextLaunchToggle;
        [SerializeField] private bool _includeInfoMessages;
        [SerializeField] private int _maxMessageLines = 16;

        [SerializeField] private ModRuntimeCatalogService _modCatalogService;
        [SerializeField] private UserSettingsService _settingsService;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _modCatalogService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            if (_modCatalogService != null)
            {
                _modCatalogService.CatalogReloaded -= HandleCatalogReloaded;
                _modCatalogService.CatalogReloaded += HandleCatalogReloaded;
            }

            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= HandleSettingsChanged;
                _settingsService.SettingsChanged += HandleSettingsChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_modCatalogService != null)
            {
                _modCatalogService.CatalogReloaded -= HandleCatalogReloaded;
            }

            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= HandleSettingsChanged;
            }
        }

        public void OnSafeModeNextLaunchChanged(bool enabled)
        {
            _settingsService?.SetModSafeModeEnabled(enabled);
            Refresh();
        }

        public void OnReloadModsPressed()
        {
            _modCatalogService?.Reload();
            Refresh();
        }

        public void Refresh()
        {
            var safeModePreference = _settingsService != null
                ? _settingsService.ModSafeModeEnabled
                : PlayerPrefs.GetInt(UserSettingsService.ModsSafeModePlayerPrefsKey, 0) == 1;

            if (_safeModeNextLaunchToggle != null)
            {
                _safeModeNextLaunchToggle.SetIsOnWithoutNotify(safeModePreference);
            }

            if (_modCatalogService == null)
            {
                SetText(_summaryText, "Mods: unavailable");
                SetText(_safeModeStatusText, ModDiagnosticsTextFormatter.BuildSafeModeStatus(safeModePreference, false, string.Empty));
                SetText(_acceptedModsText, "Accepted Mods: none");
                SetText(_rejectedModsText, "Rejected Mods: none");
                SetText(_messagesText, "Mod Loader Messages: unavailable");
                return;
            }

            var result = _modCatalogService.LastLoadResult;
            SetText(_summaryText, ModDiagnosticsTextFormatter.BuildSummary(result, _modCatalogService.SafeModeActive, _modCatalogService.SafeModeReason));
            SetText(_safeModeStatusText, ModDiagnosticsTextFormatter.BuildSafeModeStatus(safeModePreference, _modCatalogService.SafeModeActive, _modCatalogService.SafeModeReason));
            SetText(_acceptedModsText, ModDiagnosticsTextFormatter.BuildAcceptedModsText(result));
            SetText(_rejectedModsText, ModDiagnosticsTextFormatter.BuildRejectedModsText(result));
            SetText(_messagesText, ModDiagnosticsTextFormatter.BuildMessagesText(result, _includeInfoMessages, _maxMessageLines));
        }

        private void HandleCatalogReloaded()
        {
            Refresh();
        }

        private void HandleSettingsChanged()
        {
            Refresh();
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            text.text = value ?? string.Empty;
        }
    }
}
