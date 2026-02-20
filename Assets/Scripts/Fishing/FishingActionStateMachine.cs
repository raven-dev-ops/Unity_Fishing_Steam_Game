using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingActionStateMachine : MonoBehaviour
    {
        [SerializeField] private FishingActionState _state = FishingActionState.Cast;

        public FishingActionState State => _state;

        public event Action<FishingActionState, FishingActionState> StateChanged;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
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
    }
}
