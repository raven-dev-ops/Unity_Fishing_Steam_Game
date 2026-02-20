using System.Collections.Generic;
using UnityEngine;

namespace RavenDevOps.Fishing.Audio
{
    public sealed class SfxTriggerRouter : MonoBehaviour
    {
        [SerializeField] private AudioManager _audioManager;

        [SerializeField] private AudioClip _uiNavigate;
        [SerializeField] private AudioClip _uiSelect;
        [SerializeField] private AudioClip _uiCancel;
        [SerializeField] private AudioClip _cast;
        [SerializeField] private AudioClip _hooked;
        [SerializeField] private AudioClip _catch;
        [SerializeField] private AudioClip _sell;
        [SerializeField] private AudioClip _purchase;
        [SerializeField] private AudioClip _depart;
        [SerializeField] private AudioClip _return;

        private readonly Dictionary<SfxEvent, int> _lastPlayedFrame = new Dictionary<SfxEvent, int>();

        private void Awake()
        {
            _audioManager ??= FindObjectOfType<AudioManager>();
        }

        public void Play(SfxEvent eventType)
        {
            if (_audioManager == null)
            {
                return;
            }

            if (_lastPlayedFrame.TryGetValue(eventType, out var lastFrame) && lastFrame == Time.frameCount)
            {
                return;
            }

            var clip = ResolveClip(eventType);
            if (clip == null)
            {
                return;
            }

            _lastPlayedFrame[eventType] = Time.frameCount;
            _audioManager.PlaySfx(clip);
        }

        private AudioClip ResolveClip(SfxEvent eventType)
        {
            switch (eventType)
            {
                case SfxEvent.UiNavigate:
                    return _uiNavigate;
                case SfxEvent.UiSelect:
                    return _uiSelect;
                case SfxEvent.UiCancel:
                    return _uiCancel;
                case SfxEvent.Cast:
                    return _cast;
                case SfxEvent.Hooked:
                    return _hooked;
                case SfxEvent.Catch:
                    return _catch;
                case SfxEvent.Sell:
                    return _sell;
                case SfxEvent.Purchase:
                    return _purchase;
                case SfxEvent.Depart:
                    return _depart;
                case SfxEvent.Return:
                    return _return;
                default:
                    return null;
            }
        }
    }
}
