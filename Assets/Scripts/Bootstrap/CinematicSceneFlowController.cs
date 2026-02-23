using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Core
{
    public sealed class CinematicSceneFlowController : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float _autoAdvanceSeconds = 5.5f;
        [SerializeField] private Text _statusText;
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private InputContextRouter _inputContextRouter;
        [SerializeField] private SaveManager _saveManager;

        private InputAction _submitAction;
        private InputAction _cancelAction;
        private float _startedAtTime;
        private bool _advanced;

        public void Configure(Text statusText)
        {
            _statusText = statusText;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputContextRouter, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            _advanced = false;
            _startedAtTime = Time.unscaledTime;
            _inputContextRouter?.SetContext(InputContext.UI);
            _inputMapController?.ApplyContext(InputContext.UI);
            SetStatus("Cinematic: Enter/Esc to skip.");
        }

        private void Update()
        {
            if (_advanced)
            {
                return;
            }

            RefreshActionsIfNeeded();
            if ((_submitAction != null && _submitAction.WasPressedThisFrame())
                || (_cancelAction != null && _cancelAction.WasPressedThisFrame()))
            {
                AdvanceFromCinematic();
                return;
            }

            if (Time.unscaledTime - _startedAtTime >= _autoAdvanceSeconds)
            {
                AdvanceFromCinematic();
            }
        }

        private void RefreshActionsIfNeeded()
        {
            if (_submitAction == null)
            {
                _submitAction = _inputMapController != null
                    ? _inputMapController.FindAction("UI/Submit")
                    : null;
            }

            if (_cancelAction == null)
            {
                _cancelAction = _inputMapController != null
                    ? _inputMapController.FindAction("UI/Cancel")
                    : null;
            }
        }

        private void AdvanceFromCinematic()
        {
            if (_advanced)
            {
                return;
            }

            _advanced = true;
            if (ShouldRunFirstTimeFishingLoopTutorial())
            {
                SetStatus("Opening fishing tutorial...");
                if (_orchestrator != null)
                {
                    _orchestrator.RequestOpenFishingTutorialFromCinematicFirstTime();
                }
                else
                {
                    _gameFlowManager?.SetState(GameFlowState.Fishing);
                }

                return;
            }

            SetStatus("Opening main menu...");
            _gameFlowManager?.SetState(GameFlowState.MainMenu);
        }

        private bool ShouldRunFirstTimeFishingLoopTutorial()
        {
            if (_saveManager == null || _saveManager.Current == null)
            {
                return false;
            }

            var flags = _saveManager.Current.tutorialFlags ?? new TutorialFlags();
            var isFirstTimeUser = !flags.tutorialSeen && !flags.introTutorialReplayRequested;
            return isFirstTimeUser && _saveManager.ShouldRunFishingLoopTutorial();
        }

        private void SetStatus(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text ?? string.Empty;
            }
        }
    }
}
