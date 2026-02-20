using System;
using RavenDevOps.Fishing.Audio;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.UI
{
    public sealed class SettingsMenuController : MonoBehaviour
    {
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _voSlider;
        [SerializeField] private Slider _inputSensitivitySlider;
        [SerializeField] private Slider _uiScaleSlider;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private Toggle _subtitlesToggle;
        [SerializeField] private Toggle _highContrastFishingCuesToggle;
        [SerializeField] private Toggle _steamRichPresenceToggle;
        [SerializeField] private Toggle _modSafeModeToggle;

        [SerializeField] private TMP_Text _displayModeText;
        [SerializeField] private TMP_Text _resolutionText;
        [SerializeField] private TMP_Text _inputSensitivityText;
        [SerializeField] private TMP_Text _uiScaleText;
        [SerializeField] private TMP_Text _modSafeModeStatusText;
        [SerializeField] private TMP_Text _fishingActionBindingText;
        [SerializeField] private TMP_Text _harborInteractBindingText;
        [SerializeField] private TMP_Text _menuCancelBindingText;
        [SerializeField] private TMP_Text _returnHarborBindingText;

        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private InputRebindingService _inputRebindingService;
        [SerializeField] private ModRuntimeCatalogService _modCatalogService;

        private Resolution[] _resolutions = Array.Empty<Resolution>();
        private int _resolutionIndex;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputRebindingService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _modCatalogService, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= HandleSettingsChanged;
                _settingsService.SettingsChanged += HandleSettingsChanged;
            }

            if (_modCatalogService != null)
            {
                _modCatalogService.CatalogReloaded -= HandleCatalogReloaded;
                _modCatalogService.CatalogReloaded += HandleCatalogReloaded;
            }

            SyncFromSettings();
        }

        private void OnDisable()
        {
            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= HandleSettingsChanged;
            }

            if (_modCatalogService != null)
            {
                _modCatalogService.CatalogReloaded -= HandleCatalogReloaded;
            }
        }

        public void OnMasterVolumeChanged(float value)
        {
            _audioManager?.SetMasterVolume(value);
            _settingsService?.SetMasterVolume(value);
        }

        public void OnMusicVolumeChanged(float value)
        {
            _audioManager?.SetMusicVolume(value);
            _settingsService?.SetMusicVolume(value);
        }

        public void OnSfxVolumeChanged(float value)
        {
            _audioManager?.SetSfxVolume(value);
            _settingsService?.SetSfxVolume(value);
        }

        public void OnVoVolumeChanged(float value)
        {
            _audioManager?.SetVoVolume(value);
            _settingsService?.SetVoVolume(value);
        }

        public void OnInputSensitivityChanged(float value)
        {
            _settingsService?.SetInputSensitivity(value);
            RefreshInputSensitivityText(value);
        }

        public void OnUiScaleChanged(float value)
        {
            _settingsService?.SetUiScale(value);
            RefreshUiScaleText(value);
        }

        public void OnFullscreenChanged(bool fullscreen)
        {
            _settingsService?.SetFullscreen(fullscreen);
            RefreshDisplayModeText(fullscreen);
            RefreshResolutionText();
        }

        public void OnSubtitlesChanged(bool enabled)
        {
            _settingsService?.SetSubtitlesEnabled(enabled);
        }

        public void OnHighContrastFishingCuesChanged(bool enabled)
        {
            _settingsService?.SetHighContrastFishingCues(enabled);
        }

        public void OnSteamRichPresenceChanged(bool enabled)
        {
            _settingsService?.SetSteamRichPresenceEnabled(enabled);
        }

        public void OnModSafeModeChanged(bool enabled)
        {
            _settingsService?.SetModSafeModeEnabled(enabled);
            RefreshModSafeModeStatus(enabled);
        }

        public void OnNextResolutionPressed()
        {
            if (_resolutions == null || _resolutions.Length == 0)
            {
                return;
            }

            _resolutionIndex = (_resolutionIndex + 1) % _resolutions.Length;
            ApplyCurrentResolutionSelection();
        }

        public void OnPreviousResolutionPressed()
        {
            if (_resolutions == null || _resolutions.Length == 0)
            {
                return;
            }

            _resolutionIndex = (_resolutionIndex - 1 + _resolutions.Length) % _resolutions.Length;
            ApplyCurrentResolutionSelection();
        }

        private void SyncFromSettings()
        {
            if (_settingsService == null)
            {
                RefreshDisplayModeText(Screen.fullScreenMode != FullScreenMode.Windowed);
                RefreshResolutionText();
                RefreshUiScaleText(1f);
                var safeModePreferenceEnabled = PlayerPrefs.GetInt(UserSettingsService.ModsSafeModePlayerPrefsKey, 0) == 1;
                RefreshModSafeModeStatus(safeModePreferenceEnabled);
                RefreshBindingTexts();
                return;
            }

            _resolutions = _settingsService.GetSupportedResolutions();
            _resolutionIndex = ResolveCurrentResolutionIndex();

            SetSliderValue(_masterSlider, _settingsService.MasterVolume);
            SetSliderValue(_musicSlider, _settingsService.MusicVolume);
            SetSliderValue(_sfxSlider, _settingsService.SfxVolume);
            SetSliderValue(_voSlider, _settingsService.VoVolume);
            SetSliderValue(_inputSensitivitySlider, _settingsService.InputSensitivity);
            SetSliderValue(_uiScaleSlider, _settingsService.UiScale);

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.isOn = _settingsService.Fullscreen;
            }

            if (_subtitlesToggle != null)
            {
                _subtitlesToggle.isOn = _settingsService.SubtitlesEnabled;
            }

            if (_highContrastFishingCuesToggle != null)
            {
                _highContrastFishingCuesToggle.isOn = _settingsService.HighContrastFishingCues;
            }

            if (_steamRichPresenceToggle != null)
            {
                _steamRichPresenceToggle.isOn = _settingsService.SteamRichPresenceEnabled;
            }

            if (_modSafeModeToggle != null)
            {
                _modSafeModeToggle.SetIsOnWithoutNotify(_settingsService.ModSafeModeEnabled);
            }

            RefreshDisplayModeText(_settingsService.Fullscreen);
            RefreshResolutionText();
            RefreshInputSensitivityText(_settingsService.InputSensitivity);
            RefreshUiScaleText(_settingsService.UiScale);
            RefreshModSafeModeStatus(_settingsService.ModSafeModeEnabled);
            RefreshBindingTexts();
        }

        private int ResolveCurrentResolutionIndex()
        {
            if (_resolutions == null || _resolutions.Length == 0)
            {
                return 0;
            }

            var targetWidth = _settingsService != null ? _settingsService.ResolutionWidth : Screen.width;
            var targetHeight = _settingsService != null ? _settingsService.ResolutionHeight : Screen.height;

            for (var i = 0; i < _resolutions.Length; i++)
            {
                if (_resolutions[i].width == targetWidth && _resolutions[i].height == targetHeight)
                {
                    return i;
                }
            }

            return _resolutions.Length - 1;
        }

        private void ApplyCurrentResolutionSelection()
        {
            if (_settingsService == null || _resolutions == null || _resolutions.Length == 0)
            {
                return;
            }

            var resolution = _resolutions[_resolutionIndex];
            _settingsService.SetResolution(resolution.width, resolution.height, resolution.refreshRate);
            RefreshResolutionText();
        }

        private void RefreshDisplayModeText(bool fullscreen)
        {
            if (_displayModeText != null)
            {
                _displayModeText.text = fullscreen ? "Display: Fullscreen Window" : "Display: Windowed";
            }
        }

        private void RefreshResolutionText()
        {
            if (_resolutionText == null)
            {
                return;
            }

            if (_resolutions == null || _resolutions.Length == 0)
            {
                _resolutionText.text = $"Resolution: {Screen.width}x{Screen.height}";
                return;
            }

            var resolution = _resolutions[Mathf.Clamp(_resolutionIndex, 0, _resolutions.Length - 1)];
            _resolutionText.text = $"Resolution: {resolution.width}x{resolution.height} @ {resolution.refreshRate}Hz";
        }

        private void RefreshInputSensitivityText(float value)
        {
            if (_inputSensitivityText != null)
            {
                _inputSensitivityText.text = $"Input Sensitivity: {value:0.00}x";
            }
        }

        private void RefreshUiScaleText(float value)
        {
            if (_uiScaleText != null)
            {
                _uiScaleText.text = $"UI Scale: {value:0.00}x";
            }
        }

        public void OnRebindFishingActionPressed()
        {
            StartRebind("Fishing/Action", _fishingActionBindingText, "Action");
        }

        public void OnRebindHarborInteractPressed()
        {
            StartRebind("Harbor/Interact", _harborInteractBindingText, "Interact");
        }

        public void OnRebindMenuCancelPressed()
        {
            StartRebind("UI/Cancel", _menuCancelBindingText, "Cancel");
        }

        public void OnRebindReturnHarborPressed()
        {
            StartRebind("UI/ReturnHarbor", _returnHarborBindingText, "Return Harbor");
        }

        public void OnResetRebindsPressed()
        {
            _inputRebindingService?.ResetAllOverrides();
            RefreshBindingTexts();
        }

        private void StartRebind(string actionPath, TMP_Text bindingText, string label)
        {
            if (_inputRebindingService == null)
            {
                return;
            }

            if (bindingText != null)
            {
                bindingText.text = $"{label}: Listening...";
            }

            _inputRebindingService.StartRebindForAction(actionPath, _ => RefreshBindingTexts());
        }

        private void RefreshBindingTexts()
        {
            if (_inputRebindingService == null)
            {
                return;
            }

            SetBindingText(_fishingActionBindingText, "Action", _inputRebindingService.GetDisplayBindingForAction("Fishing/Action"));
            SetBindingText(_harborInteractBindingText, "Interact", _inputRebindingService.GetDisplayBindingForAction("Harbor/Interact"));
            SetBindingText(_menuCancelBindingText, "Cancel", _inputRebindingService.GetDisplayBindingForAction("UI/Cancel"));
            SetBindingText(_returnHarborBindingText, "Return Harbor", _inputRebindingService.GetDisplayBindingForAction("UI/ReturnHarbor"));
        }

        private static void SetBindingText(TMP_Text text, string label, string binding)
        {
            if (text == null)
            {
                return;
            }

            text.text = string.IsNullOrWhiteSpace(binding)
                ? $"{label}: Unbound"
                : $"{label}: {binding}";
        }

        private static void SetSliderValue(Slider slider, float value)
        {
            if (slider == null)
            {
                return;
            }

            slider.SetValueWithoutNotify(value);
        }

        private void RefreshModSafeModeStatus(bool safeModePreferenceEnabled)
        {
            if (_modSafeModeStatusText == null)
            {
                return;
            }

            var safeModeActive = _modCatalogService != null && _modCatalogService.SafeModeActive;
            var safeModeReason = _modCatalogService != null ? _modCatalogService.SafeModeReason : string.Empty;
            _modSafeModeStatusText.text = ModDiagnosticsTextFormatter.BuildSafeModeStatus(
                safeModePreferenceEnabled,
                safeModeActive,
                safeModeReason);
        }

        private void HandleSettingsChanged()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            SyncFromSettings();
        }

        private void HandleCatalogReloaded()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            var safeModePreferenceEnabled = _settingsService != null
                ? _settingsService.ModSafeModeEnabled
                : PlayerPrefs.GetInt(UserSettingsService.ModsSafeModePlayerPrefsKey, 0) == 1;
            RefreshModSafeModeStatus(safeModePreferenceEnabled);
        }
    }
}
