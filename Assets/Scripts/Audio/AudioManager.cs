using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace RavenDevOps.Fishing.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        public const string MusicVolumeParam = "MusicVolume";
        public const string SfxVolumeParam = "SFXVolume";
        public const string VoVolumeParam = "VOVolume";

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _voSource;

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
            EnsureDefaultSources();
        }

        private void EnsureDefaultSources()
        {
            if (_musicSource == null)
            {
                _musicSource = CreateSource("MusicSource", true);
            }

            if (_sfxSource == null)
            {
                _sfxSource = CreateSource("SfxSource", false);
            }

            if (_voSource == null)
            {
                _voSource = CreateSource("VoSource", false);
            }
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var sourceGo = new GameObject(name);
            sourceGo.transform.SetParent(transform, false);
            var source = sourceGo.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            return source;
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

        public void SetMusicVolume(float linearValue)
        {
            SetMixerVolume(MusicVolumeParam, linearValue);
        }

        public void SetSfxVolume(float linearValue)
        {
            SetMixerVolume(SfxVolumeParam, linearValue);
        }

        public void SetVoVolume(float linearValue)
        {
            SetMixerVolume(VoVolumeParam, linearValue);
        }

        public void SetMixerVolume(string parameterName, float linearValue)
        {
            if (_mixer == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            var clamped = Mathf.Clamp(linearValue, 0.0001f, 1f);
            _mixer.SetFloat(parameterName, Mathf.Log10(clamped) * 20f);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            RuntimeServiceRegistry.Unregister(this);
        }
    }
}
