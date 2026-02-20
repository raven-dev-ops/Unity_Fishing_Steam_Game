using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingPauseBridge : MonoBehaviour
    {
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private InputActionMapController _inputMapController;

        private InputAction _returnHarborAction;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
        }

        private void Update()
        {
            RefreshActionIfNeeded();
            if (_gameFlowManager == null || _gameFlowManager.CurrentState != GameFlowState.Pause)
            {
                return;
            }

            if (_returnHarborAction != null && _returnHarborAction.WasPressedThisFrame())
            {
                _orchestrator?.RequestReturnToHarborFromPause();
            }
        }

        private void RefreshActionIfNeeded()
        {
            if (_returnHarborAction != null)
            {
                return;
            }

            _returnHarborAction = _inputMapController != null
                ? _inputMapController.FindAction("UI/ReturnHarbor")
                : null;
        }
    }
}
