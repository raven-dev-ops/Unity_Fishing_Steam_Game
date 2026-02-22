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
            if (WasActionPressedThisFrame())
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
                    SetState(FishingActionState.Cast);
                    break;
                case FishingActionState.Hooked:
                    SetState(FishingActionState.Reel);
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

        public void SetResolve()
        {
            SetState(FishingActionState.Resolve);
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

        private bool WasActionPressedThisFrame()
        {
            if (_actionInput != null && _actionInput.WasPressedThisFrame())
            {
                return true;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                return true;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                return true;
            }
#endif

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        }
    }
}
