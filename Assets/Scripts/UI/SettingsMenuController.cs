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
        [SerializeField] private Toggle _fullscreenToggle;

        [SerializeField] private TMP_Text _displayModeText;
        [SerializeField] private TMP_Text _resolutionText;
        [SerializeField] private TMP_Text _inputSensitivityText;
        [SerializeField] private TMP_Text _fishingActionBindingText;
        [SerializeField] private TMP_Text _harborInteractBindingText;
        [SerializeField] private TMP_Text _menuCancelBindingText;
        [SerializeField] private TMP_Text _returnHarborBindingText;

        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private InputRebindingService _inputRebindingService;

        private Resolution[] _resolutions = Array.Empty<Resolution>();
        private int _resolutionIndex;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputRebindingService, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            SyncFromSettings();
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

        public void OnFullscreenChanged(bool fullscreen)
        {
            _settingsService?.SetFullscreen(fullscreen);
            RefreshDisplayModeText(fullscreen);
            RefreshResolutionText();
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

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.isOn = _settingsService.Fullscreen;
            }

            RefreshDisplayModeText(_settingsService.Fullscreen);
            RefreshResolutionText();
            RefreshInputSensitivityText(_settingsService.InputSensitivity);
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
    }
}
