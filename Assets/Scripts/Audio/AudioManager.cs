using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace RavenDevOps.Fishing.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        public const string MasterVolumeParam = "MasterVolume";
        public const string MusicVolumeParam = "MusicVolume";
        public const string SfxVolumeParam = "SFXVolume";
        public const string VoVolumeParam = "VOVolume";

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioMixerGroup _musicMixerGroup;
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;
        [SerializeField] private AudioMixerGroup _voMixerGroup;

        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _voSource;

        [SerializeField] private bool _enableVoDucking = true;
        [SerializeField] private float _duckedMusicMultiplier = 0.45f;
        [SerializeField] private float _duckSmoothing = 8f;

        [SerializeField] private UserSettingsService _settingsService;

        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private float _voVolume = 1f;
        private float _duckMultiplier = 1f;

        public static AudioManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);

            EnsureDefaultSources();
            LoadFromSettings();
            ApplyAllVolumes();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            RuntimeServiceRegistry.Unregister(this);
        }

        private void Update()
        {
            UpdateDucking();
        }

        private void EnsureDefaultSources()
        {
            if (_musicSource == null)
            {
                _musicSource = CreateSource("MusicSource", true, _musicMixerGroup);
            }

            if (_sfxSource == null)
            {
                _sfxSource = CreateSource("SfxSource", false, _sfxMixerGroup);
            }

            if (_voSource == null)
            {
                _voSource = CreateSource("VoSource", false, _voMixerGroup);
            }
        }

        private AudioSource CreateSource(string name, bool loop, AudioMixerGroup mixerGroup)
        {
            var sourceGo = new GameObject(name);
            sourceGo.transform.SetParent(transform, false);
            var source = sourceGo.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.outputAudioMixerGroup = mixerGroup;
            return source;
        }

        private void LoadFromSettings()
        {
            if (_settingsService == null)
            {
                return;
            }

            _masterVolume = _settingsService.MasterVolume;
            _musicVolume = _settingsService.MusicVolume;
            _sfxVolume = _settingsService.SfxVolume;
            _voVolume = _settingsService.VoVolume;
        }

        private void UpdateDucking()
        {
            var target = 1f;
            if (_enableVoDucking && _voSource != null && _voSource.isPlaying)
            {
                target = Mathf.Clamp01(_duckedMusicMultiplier);
            }

            _duckMultiplier = Mathf.Lerp(_duckMultiplier, target, Mathf.Clamp01(_duckSmoothing * Time.unscaledDeltaTime));
            ApplyAllVolumes();
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (_musicSource == null || clip == null)
            {
                return;
            }

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.Play();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (_sfxSource == null || clip == null)
            {
                return;
            }

            _sfxSource.PlayOneShot(clip);
        }

        public void PlayVoice(AudioClip clip)
        {
            if (_voSource == null || clip == null)
            {
                return;
            }

            _voSource.clip = clip;
            _voSource.loop = false;
            _voSource.Play();
        }

        public void SetMasterVolume(float linearValue)
        {
            _masterVolume = Mathf.Clamp(linearValue, 0.0001f, 1f);
            _settingsService?.SetMasterVolume(_masterVolume);
            ApplyAllVolumes();
        }

        public void SetMusicVolume(float linearValue)
        {
            _musicVolume = Mathf.Clamp(linearValue, 0.0001f, 1f);
            _settingsService?.SetMusicVolume(_musicVolume);
            ApplyAllVolumes();
        }

        public void SetSfxVolume(float linearValue)
        {
            _sfxVolume = Mathf.Clamp(linearValue, 0.0001f, 1f);
            _settingsService?.SetSfxVolume(_sfxVolume);
            ApplyAllVolumes();
        }

        public void SetVoVolume(float linearValue)
        {
            _voVolume = Mathf.Clamp(linearValue, 0.0001f, 1f);
            _settingsService?.SetVoVolume(_voVolume);
            ApplyAllVolumes();
        }

        public void SetMixerVolume(string parameterName, float linearValue)
        {
            var clamped = Mathf.Clamp(linearValue, 0.0001f, 1f);
            switch (parameterName)
            {
                case MasterVolumeParam:
                    SetMasterVolume(clamped);
                    break;
                case MusicVolumeParam:
                    SetMusicVolume(clamped);
                    break;
                case SfxVolumeParam:
                    SetSfxVolume(clamped);
                    break;
                case VoVolumeParam:
                    SetVoVolume(clamped);
                    break;
                default:
                    if (_mixer != null && !string.IsNullOrWhiteSpace(parameterName))
                    {
                        _mixer.SetFloat(parameterName, Mathf.Log10(clamped) * 20f);
                    }

                    break;
            }
        }

        private void ApplyAllVolumes()
        {
            var duckedMusic = Mathf.Clamp(_musicVolume * _duckMultiplier, 0.0001f, 1f);
            ApplyMixerOrSourceVolume(MasterVolumeParam, _masterVolume, null);
            ApplyMixerOrSourceVolume(MusicVolumeParam, duckedMusic, _musicSource);
            ApplyMixerOrSourceVolume(SfxVolumeParam, _sfxVolume, _sfxSource);
            ApplyMixerOrSourceVolume(VoVolumeParam, _voVolume, _voSource);
        }

        private void ApplyMixerOrSourceVolume(string mixerParam, float linear, AudioSource source)
        {
            var clamped = Mathf.Clamp(linear, 0.0001f, 1f);

            if (_mixer != null)
            {
                _mixer.SetFloat(mixerParam, Mathf.Log10(clamped) * 20f);
                return;
            }

            if (source != null)
            {
                source.volume = clamped;
            }
        }
    }
}
