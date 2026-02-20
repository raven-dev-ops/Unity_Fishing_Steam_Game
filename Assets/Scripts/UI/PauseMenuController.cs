using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.UI
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _pauseRoot;
        [SerializeField] private GameObject _settingsPanel;

        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private GameFlowOrchestrator _orchestrator;

        private void Awake()
        {
            _gameFlowManager ??= FindObjectOfType<GameFlowManager>();
            _orchestrator ??= FindObjectOfType<GameFlowOrchestrator>();
        }

        private void OnEnable()
        {
            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged += OnStateChanged;
            }

            UpdatePauseRoot();
            SetSettingsVisible(false);
        }

        private void OnDisable()
        {
            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged -= OnStateChanged;
            }
        }

        public void OnResumePressed()
        {
            _gameFlowManager?.ResumeFromPause();
        }

        public void OnTownHarborPressed()
        {
            _gameFlowManager?.ReturnToHarborFromFishingPause();
            _orchestrator?.RequestReturnToHarbor();
        }

        public void OnSettingsPressed()
        {
            SetSettingsVisible(true);
        }

        public void OnBackFromSettingsPressed()
        {
            SetSettingsVisible(false);
        }

        public void OnExitGamePressed()
        {
            _orchestrator?.RequestExitGame();
        }

        private void OnStateChanged(GameFlowState previous, GameFlowState next)
        {
            UpdatePauseRoot();
            if (next != GameFlowState.Pause)
            {
                SetSettingsVisible(false);
            }
        }

        private void UpdatePauseRoot()
        {
            if (_pauseRoot == null || _gameFlowManager == null)
            {
                return;
            }

            _pauseRoot.SetActive(_gameFlowManager.CurrentState == GameFlowState.Pause);
        }

        private void SetSettingsVisible(bool visible)
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(visible);
            }
        }
    }
}
