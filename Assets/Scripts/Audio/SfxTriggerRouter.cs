using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using UnityEngine;

namespace RavenDevOps.Fishing.Audio
{
    public sealed class SfxTriggerRouter : MonoBehaviour
    {
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private bool _usePhaseTwoAudioOverrides = true;

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
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
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
                    return ResolvePhaseTwoAudio(eventType, _uiNavigate);
                case SfxEvent.UiSelect:
                    return ResolvePhaseTwoAudio(eventType, _uiSelect);
                case SfxEvent.UiCancel:
                    return ResolvePhaseTwoAudio(eventType, _uiCancel);
                case SfxEvent.Cast:
                    return ResolvePhaseTwoAudio(eventType, _cast);
                case SfxEvent.Hooked:
                    return ResolvePhaseTwoAudio(eventType, _hooked);
                case SfxEvent.Catch:
                    return ResolvePhaseTwoAudio(eventType, _catch);
                case SfxEvent.Sell:
                    return ResolvePhaseTwoAudio(eventType, _sell);
                case SfxEvent.Purchase:
                    return ResolvePhaseTwoAudio(eventType, _purchase);
                case SfxEvent.Depart:
                    return ResolvePhaseTwoAudio(eventType, _depart);
                case SfxEvent.Return:
                    return ResolvePhaseTwoAudio(eventType, _return);
                default:
                    return null;
            }
        }

        private AudioClip ResolvePhaseTwoAudio(SfxEvent eventType, AudioClip fallback)
        {
            if (!_usePhaseTwoAudioOverrides || _catalogService == null)
            {
                return fallback;
            }

            var key = ResolvePhaseTwoKey(eventType);
            if (!string.IsNullOrWhiteSpace(key) && _catalogService.TryGetPhaseTwoAudioClip(key, out var overrideClip))
            {
                return overrideClip;
            }

            return fallback;
        }

        private static string ResolvePhaseTwoKey(SfxEvent eventType)
        {
            switch (eventType)
            {
                case SfxEvent.UiNavigate:
                    return "sfx_ui_navigate";
                case SfxEvent.UiSelect:
                    return "sfx_ui_select";
                case SfxEvent.UiCancel:
                    return "sfx_ui_cancel";
                case SfxEvent.Cast:
                    return "sfx_cast";
                case SfxEvent.Hooked:
                    return "sfx_hooked";
                case SfxEvent.Catch:
                    return "sfx_catch";
                case SfxEvent.Sell:
                    return "sfx_sell";
                case SfxEvent.Purchase:
                    return "sfx_purchase";
                case SfxEvent.Depart:
                    return "sfx_depart";
                case SfxEvent.Return:
                    return "sfx_return";
                default:
                    return string.Empty;
            }
        }
    }
}
