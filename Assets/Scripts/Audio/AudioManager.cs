using UnityEngine;
using UnityEngine.Audio;

namespace RavenDevOps.Fishing.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _voSource;

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

        public void SetMixerVolume(string parameterName, float linearValue)
        {
            if (_mixer == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            var clamped = Mathf.Clamp(linearValue, 0.0001f, 1f);
            _mixer.SetFloat(parameterName, Mathf.Log10(clamped) * 20f);
        }
    }
}
