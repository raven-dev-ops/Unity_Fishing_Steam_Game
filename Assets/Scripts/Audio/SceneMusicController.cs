using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using UnityEngine;

namespace RavenDevOps.Fishing.Audio
{
    public sealed class SceneMusicController : MonoBehaviour
    {
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private CatalogService _catalogService;

        [SerializeField] private AudioClip _menuMusicLoop;
        [SerializeField] private AudioClip _harborMusicLoop;
        [SerializeField] private AudioClip _fishingMusicLoop;
        [SerializeField] private bool _usePhaseTwoAudioOverrides = true;
        [SerializeField] private string _menuMusicPhaseTwoKey = "menu_music_loop";
        [SerializeField] private string _harborMusicPhaseTwoKey = "harbor_music_loop";
        [SerializeField] private string _fishingMusicPhaseTwoKey = "fishing_music_loop";

        private bool _phaseTwoRefreshPending;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            _phaseTwoRefreshPending = _usePhaseTwoAudioOverrides;
            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged += OnStateChanged;
                OnStateChanged(GameFlowState.None, _gameFlowManager.CurrentState);
            }
        }

        private void OnDisable()
        {
            _phaseTwoRefreshPending = false;
            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged -= OnStateChanged;
            }
        }

        private void Update()
        {
            if (!_phaseTwoRefreshPending || _catalogService == null || !_catalogService.PhaseTwoAudioLoadCompleted || _gameFlowManager == null)
            {
                return;
            }

            _phaseTwoRefreshPending = false;
            OnStateChanged(_gameFlowManager.CurrentState, _gameFlowManager.CurrentState);
        }

        private void OnStateChanged(GameFlowState previous, GameFlowState next)
        {
            if (_audioManager == null)
            {
                return;
            }

            AudioClip clip;
            switch (next)
            {
                case GameFlowState.MainMenu:
                case GameFlowState.Cinematic:
                    clip = ResolvePhaseTwoAudio(_menuMusicPhaseTwoKey, _menuMusicLoop);
                    _audioManager.PlayMusic(clip, true);
                    break;
                case GameFlowState.Harbor:
                    clip = ResolvePhaseTwoAudio(_harborMusicPhaseTwoKey, _harborMusicLoop);
                    _audioManager.PlayMusic(clip, true);
                    break;
                case GameFlowState.Fishing:
                    clip = ResolvePhaseTwoAudio(_fishingMusicPhaseTwoKey, _fishingMusicLoop);
                    _audioManager.PlayMusic(clip, true);
                    break;
            }
        }

        private AudioClip ResolvePhaseTwoAudio(string key, AudioClip fallback)
        {
            if (_usePhaseTwoAudioOverrides &&
                _catalogService != null &&
                !string.IsNullOrWhiteSpace(key) &&
                _catalogService.TryGetPhaseTwoAudioClip(key, out var overrideClip))
            {
                return overrideClip;
            }

            return fallback;
        }
    }
}
