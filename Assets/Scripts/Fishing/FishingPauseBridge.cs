using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingPauseBridge : MonoBehaviour
    {
        [SerializeField] private GameFlowOrchestrator _orchestrator;

        private void Awake()
        {
            _orchestrator ??= FindObjectOfType<GameFlowOrchestrator>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
            {
                _orchestrator?.RequestReturnToHarbor();
            }
        }
    }
}
