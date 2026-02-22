using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Harbor
{
    public sealed class HarborPauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _pauseRoot;
        [SerializeField] private GameObject _harborHudRoot;
        [SerializeField] private GameObject _resumeButton;
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private InputActionMapController _inputMapController;

        private InputAction _cancelAction;

        public void Configure(
            GameObject pauseRoot,
            GameObject harborHudRoot,
            GameObject resumeButton)
        {
            _pauseRoot = pauseRoot;
            _harborHudRoot = harborHudRoot;
            _resumeButton = resumeButton;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged -= HandleStateChanged;
                _gameFlowManager.StateChanged += HandleStateChanged;
            }

            ApplyPauseState();
        }

        private void OnDisable()
        {
            if (_gameFlowManager != null)
            {
                _gameFlowManager.StateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            RefreshActionsIfNeeded();
            if (_gameFlowManager == null || _gameFlowManager.CurrentState != GameFlowState.Pause)
            {
                return;
            }

            if (_cancelAction != null && _cancelAction.WasPressedThisFrame())
            {
                OnResumePressed();
            }
        }

        public void OnResumePressed()
        {
            _gameFlowManager?.ResumeFromPause();
            ApplyPauseState();
        }

        public void OnMainMenuPressed()
        {
            ExitPauseIfNeeded();
            _gameFlowManager?.SetState(GameFlowState.MainMenu);
        }

        public void OnExitGamePressed()
        {
            ExitPauseIfNeeded();
            Application.Quit();
        }

        private void HandleStateChanged(GameFlowState previous, GameFlowState next)
        {
            ApplyPauseState();
        }

        private void ExitPauseIfNeeded()
        {
            if (_gameFlowManager == null || !_gameFlowManager.IsPaused)
            {
                return;
            }

            _gameFlowManager.ResumeFromPause();
        }

        private void ApplyPauseState()
        {
            var paused = _gameFlowManager != null && _gameFlowManager.CurrentState == GameFlowState.Pause;
            if (_pauseRoot != null)
            {
                _pauseRoot.SetActive(paused);
            }

            if (_harborHudRoot != null)
            {
                _harborHudRoot.SetActive(!paused);
            }

            if (paused)
            {
                SetSelected(_resumeButton);
            }
        }

        private void RefreshActionsIfNeeded()
        {
            if (_cancelAction != null)
            {
                return;
            }

            _cancelAction = _inputMapController != null
                ? _inputMapController.FindAction("UI/Cancel")
                : null;
        }

        private static void SetSelected(GameObject target)
        {
            if (target == null || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
