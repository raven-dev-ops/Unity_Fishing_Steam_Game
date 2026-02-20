using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Audio
{
    public sealed class SceneMusicController : MonoBehaviour
    {
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private GameFlowManager _gameFlowManager;

        [SerializeField] private AudioClip _menuMusicLoop;
        [SerializeField] private AudioClip _harborMusicLoop;
        [SerializeField] private AudioClip _fishingMusicLoop;

        private void Awake()
        {
            _audioManager ??= FindObjectOfType<AudioManager>();
            _gameFlowManager ??= FindObjectOfType<GameFlowManager>();
        }

        private void OnEnable()
        {
            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged += OnStateChanged;
                OnStateChanged(GameFlowState.None, _gameFlowManager.CurrentState);
            }
        }

        private void OnDisable()
        {
            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(GameFlowState previous, GameFlowState next)
        {
            if (_audioManager == null)
            {
                return;
            }

            switch (next)
            {
                case GameFlowState.MainMenu:
                case GameFlowState.Cinematic:
                    _audioManager.PlayMusic(_menuMusicLoop, true);
                    break;
                case GameFlowState.Harbor:
                    _audioManager.PlayMusic(_harborMusicLoop, true);
                    break;
                case GameFlowState.Fishing:
                    _audioManager.PlayMusic(_fishingMusicLoop, true);
                    break;
            }
        }
    }
}
