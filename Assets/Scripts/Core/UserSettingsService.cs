using System;
using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    public sealed class UserSettingsService : MonoBehaviour
    {
        private const string KeyMasterVolume = "settings.masterVolume";
        private const string KeyMusicVolume = "settings.musicVolume";
        private const string KeySfxVolume = "settings.sfxVolume";
        private const string KeyVoVolume = "settings.voVolume";
        private const string KeyInputSensitivity = "settings.inputSensitivity";
        private const string KeyFullscreen = "settings.fullscreen";
        private const string KeyResolutionWidth = "settings.resolutionWidth";
        private const string KeyResolutionHeight = "settings.resolutionHeight";
        private const string KeyResolutionRefresh = "settings.resolutionRefresh";

        private static UserSettingsService _instance;

        [SerializeField] private float _masterVolume = 1f;
        [SerializeField] private float _musicVolume = 1f;
        [SerializeField] private float _sfxVolume = 1f;
        [SerializeField] private float _voVolume = 1f;
        [SerializeField] private float _inputSensitivity = 1f;
        [SerializeField] private bool _fullscreen = true;
        [SerializeField] private int _resolutionWidth;
        [SerializeField] private int _resolutionHeight;
        [SerializeField] private int _resolutionRefresh;

        public static UserSettingsService Instance => _instance;

        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;
        public float VoVolume => _voVolume;
        public float InputSensitivity => _inputSensitivity;
        public bool Fullscreen => _fullscreen;
        public int ResolutionWidth => _resolutionWidth;
        public int ResolutionHeight => _resolutionHeight;
        public int ResolutionRefreshRate => _resolutionRefresh;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            RuntimeServiceRegistry.Register(this);
            LoadFromPrefs();
            ApplyDisplaySettings();
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            SaveToPrefs();
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            SaveToPrefs();
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            SaveToPrefs();
        }

        public void SetVoVolume(float value)
        {
            _voVolume = Mathf.Clamp01(value);
            SaveToPrefs();
        }

        public void SetInputSensitivity(float value)
        {
            _inputSensitivity = Mathf.Clamp(value, 0.5f, 2f);
            SaveToPrefs();
        }

        public void SetFullscreen(bool fullscreen)
        {
            _fullscreen = fullscreen;
            SaveToPrefs();
            ApplyDisplaySettings();
        }

        public void SetResolution(int width, int height, int refreshRate)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            _resolutionWidth = width;
            _resolutionHeight = height;
            _resolutionRefresh = Mathf.Max(0, refreshRate);
            SaveToPrefs();
            ApplyDisplaySettings();
        }

        public Resolution[] GetSupportedResolutions()
        {
            var resolutions = Screen.resolutions;
            if (resolutions == null || resolutions.Length == 0)
            {
                return new[]
                {
                    new Resolution
                    {
                        width = Screen.width,
                        height = Screen.height,
                        refreshRate = Screen.currentResolution.refreshRate
                    }
                };
            }

            return resolutions;
        }

        public void ApplyDisplaySettings()
        {
            if (_resolutionWidth <= 0 || _resolutionHeight <= 0)
            {
                var current = Screen.currentResolution;
                _resolutionWidth = current.width;
                _resolutionHeight = current.height;
                _resolutionRefresh = current.refreshRate;
            }

            var mode = _fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(_resolutionWidth, _resolutionHeight, mode, _resolutionRefresh);
        }

        private void LoadFromPrefs()
        {
            _masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMasterVolume, 1f));
            _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusicVolume, 1f));
            _sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfxVolume, 1f));
            _voVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVoVolume, 1f));
            _inputSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(KeyInputSensitivity, 1f), 0.5f, 2f);
            _fullscreen = PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;

            var current = Screen.currentResolution;
            _resolutionWidth = PlayerPrefs.GetInt(KeyResolutionWidth, current.width);
            _resolutionHeight = PlayerPrefs.GetInt(KeyResolutionHeight, current.height);
            _resolutionRefresh = PlayerPrefs.GetInt(KeyResolutionRefresh, current.refreshRate);
        }

        private void SaveToPrefs()
        {
            PlayerPrefs.SetFloat(KeyMasterVolume, _masterVolume);
            PlayerPrefs.SetFloat(KeyMusicVolume, _musicVolume);
            PlayerPrefs.SetFloat(KeySfxVolume, _sfxVolume);
            PlayerPrefs.SetFloat(KeyVoVolume, _voVolume);
            PlayerPrefs.SetFloat(KeyInputSensitivity, _inputSensitivity);
            PlayerPrefs.SetInt(KeyFullscreen, _fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(KeyResolutionWidth, _resolutionWidth);
            PlayerPrefs.SetInt(KeyResolutionHeight, _resolutionHeight);
            PlayerPrefs.SetInt(KeyResolutionRefresh, _resolutionRefresh);
            PlayerPrefs.Save();
        }
    }
}
