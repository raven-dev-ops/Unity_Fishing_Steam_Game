using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Input
{
    public sealed class KeyboardFlowInputDriver : MonoBehaviour
    {
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private GameFlowOrchestrator _orchestrator;

        public void Initialize(GameFlowManager gameFlowManager, GameFlowOrchestrator orchestrator)
        {
            _gameFlowManager = gameFlowManager;
            _orchestrator = orchestrator;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);

            _gameFlowManager ??= GetComponent<GameFlowManager>();
            _orchestrator ??= GetComponent<GameFlowOrchestrator>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _gameFlowManager == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _gameFlowManager.TogglePause();
            }

            if (_gameFlowManager.CurrentState == GameFlowState.Pause && keyboard.hKey.wasPressedThisFrame)
            {
                _orchestrator?.RequestReturnToHarborFromPause();
            }
        }
    }
}
