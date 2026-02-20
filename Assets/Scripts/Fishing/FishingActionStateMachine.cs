using System;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingActionStateMachine : MonoBehaviour
    {
        [SerializeField] private FishingActionState _state = FishingActionState.Cast;
        [SerializeField] private InputActionMapController _inputMapController;

        private InputAction _actionInput;

        public FishingActionState State => _state;
        public event Action<FishingActionState, FishingActionState> StateChanged;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
        }

        private void Update()
        {
            RefreshActionsIfNeeded();
            if (_actionInput != null && _actionInput.WasPressedThisFrame())
            {
                AdvanceByAction();
            }
        }

        public void AdvanceByAction()
        {
            switch (_state)
            {
                case FishingActionState.Cast:
                    SetState(FishingActionState.InWater);
                    break;
                case FishingActionState.InWater:
                    SetState(FishingActionState.Reel);
                    break;
                case FishingActionState.Hooked:
                    SetState(FishingActionState.Reel);
                    break;
                case FishingActionState.Reel:
                    SetState(FishingActionState.Resolve);
                    break;
                case FishingActionState.Resolve:
                    SetState(FishingActionState.Cast);
                    break;
            }
        }

        public void SetHooked()
        {
            if (_state == FishingActionState.InWater)
            {
                SetState(FishingActionState.Hooked);
            }
        }

        public void ResetToCast()
        {
            SetState(FishingActionState.Cast);
        }

        private void SetState(FishingActionState next)
        {
            if (_state == next)
            {
                return;
            }

            var previous = _state;
            _state = next;
            StateChanged?.Invoke(previous, next);
        }

        private void RefreshActionsIfNeeded()
        {
            if (_actionInput != null)
            {
                return;
            }

            _actionInput = _inputMapController != null
                ? _inputMapController.FindAction("Fishing/Action")
                : null;
        }
    }
}
