using RavenDevOps.Fishing.Audio;
using RavenDevOps.Fishing.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.UI
{
    public sealed class SettingsMenuController : MonoBehaviour
    {
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _voSlider;
        [SerializeField] private TMP_Text _displayModeText;

        [SerializeField] private AudioManager _audioManager;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            if (_displayModeText != null)
            {
                _displayModeText.text = "Display: Fullscreen (Windowed/Exclusive toggle deferred)";
            }
        }

        public void OnMusicVolumeChanged(float value)
        {
            _audioManager?.SetMusicVolume(value);
        }

        public void OnSfxVolumeChanged(float value)
        {
            _audioManager?.SetSfxVolume(value);
        }

        public void OnVoVolumeChanged(float value)
        {
            _audioManager?.SetVoVolume(value);
        }
    }
}
