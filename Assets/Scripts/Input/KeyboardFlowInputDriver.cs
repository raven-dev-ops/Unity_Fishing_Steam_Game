using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Input
{
    public sealed class KeyboardFlowInputDriver : MonoBehaviour
    {
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private InputActionMapController _inputMapController;

        private InputAction _harborPauseAction;
        private InputAction _fishingPauseAction;
        private InputAction _uiCancelAction;

        public void Initialize(GameFlowManager gameFlowManager, GameFlowOrchestrator orchestrator)
        {
            _gameFlowManager = gameFlowManager;
            _orchestrator = orchestrator;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);

            _gameFlowManager ??= GetComponent<GameFlowManager>();
            _orchestrator ??= GetComponent<GameFlowOrchestrator>();
            _inputMapController ??= GetComponent<InputActionMapController>();
        }

        private void Update()
        {
            if (_gameFlowManager == null)
            {
                return;
            }

            RefreshActionCacheIfNeeded();

            switch (_gameFlowManager.CurrentState)
            {
                case GameFlowState.Harbor:
                    if (WasPressedThisFrame(_harborPauseAction))
                    {
                        _gameFlowManager.TogglePause();
                    }
                    break;
                case GameFlowState.Fishing:
                    if (WasPressedThisFrame(_fishingPauseAction))
                    {
                        _gameFlowManager.TogglePause();
                    }
                    break;
                case GameFlowState.Pause:
                    if (WasPressedThisFrame(_uiCancelAction))
                    {
                        _gameFlowManager.TogglePause();
                    }
                    break;
            }
        }

        private void RefreshActionCacheIfNeeded()
        {
            if (_harborPauseAction != null && _fishingPauseAction != null && _uiCancelAction != null)
            {
                return;
            }

            if (_inputMapController == null)
            {
                return;
            }

            _harborPauseAction = _inputMapController.FindAction("Harbor/Pause");
            _fishingPauseAction = _inputMapController.FindAction("Fishing/Pause");
            _uiCancelAction = _inputMapController.FindAction("UI/Cancel");
        }

        private static bool WasPressedThisFrame(InputAction action)
        {
            return action != null && action.WasPressedThisFrame();
        }
    }
}
