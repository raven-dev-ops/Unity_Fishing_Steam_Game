using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingHookCastDropController : MonoBehaviour
    {
        [SerializeField] private FishingActionStateMachine _stateMachine;
        [SerializeField] private HookMovementController _hookController;
        [SerializeField] private Transform _ship;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private float _dockOffsetY = 0.65f;
        [SerializeField] private float _dockSnapLerp = 16f;
        [SerializeField] private float _autoDropSpeed = 4.2f;
        [SerializeField] private float _manualOverrideThreshold = 0.22f;

        private InputAction _moveHookAction;
        private bool _autoDropActive;

        public void Configure(
            FishingActionStateMachine stateMachine,
            HookMovementController hookController,
            Transform ship)
        {
            _stateMachine = stateMachine;
            _hookController = hookController;
            _ship = ship;
            ApplyStateImmediate();
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            if (_hookController == null)
            {
                RuntimeServiceRegistry.TryGet(out _hookController);
            }

            if (_ship == null && _hookController != null)
            {
                _ship = _hookController.ShipTransform;
            }
        }

        private void OnEnable()
        {
            if (_stateMachine != null)
            {
                _stateMachine.StateChanged += OnStateChanged;
            }

            ApplyStateImmediate();
        }

        private void OnDisable()
        {
            if (_stateMachine != null)
            {
                _stateMachine.StateChanged -= OnStateChanged;
            }
        }

        private void Update()
        {
            if (_stateMachine == null || _hookController == null || _hookController.transform == null || _ship == null)
            {
                return;
            }

            switch (_stateMachine.State)
            {
                case FishingActionState.Cast:
                    SnapHookToDock();
                    break;
                case FishingActionState.InWater:
                    TickAutoDrop();
                    break;
            }
        }

        private void OnStateChanged(FishingActionState previous, FishingActionState next)
        {
            if (_hookController == null)
            {
                return;
            }

            if (next == FishingActionState.Cast)
            {
                _autoDropActive = false;
                _hookController.SetMovementEnabled(false);
                return;
            }

            if (next == FishingActionState.InWater)
            {
                _autoDropActive = true;
                _hookController.SetMovementEnabled(true);
                return;
            }

            _autoDropActive = false;
            _hookController.SetMovementEnabled(true);
        }

        private void ApplyStateImmediate()
        {
            if (_stateMachine == null || _hookController == null)
            {
                return;
            }

            switch (_stateMachine.State)
            {
                case FishingActionState.Cast:
                    _autoDropActive = false;
                    _hookController.SetMovementEnabled(false);
                    SnapHookToDock();
                    break;
                case FishingActionState.InWater:
                    _autoDropActive = true;
                    _hookController.SetMovementEnabled(true);
                    break;
                default:
                    _autoDropActive = false;
                    _hookController.SetMovementEnabled(true);
                    break;
            }
        }

        private void SnapHookToDock()
        {
            var hookTransform = _hookController.transform;
            if (hookTransform == null || _ship == null)
            {
                return;
            }

            var minY = -Mathf.Abs(_hookController.MaxDepth);
            var targetY = Mathf.Clamp(_ship.position.y - Mathf.Abs(_dockOffsetY), minY, _ship.position.y);
            var blend = 1f - Mathf.Exp(-Mathf.Max(1f, _dockSnapLerp) * Time.deltaTime);

            var position = hookTransform.position;
            position.y = Mathf.Lerp(position.y, targetY, blend);
            hookTransform.position = position;
        }

        private void TickAutoDrop()
        {
            if (!_autoDropActive || _hookController == null)
            {
                return;
            }

            RefreshMoveHookAction();
            if (_moveHookAction != null && Mathf.Abs(_moveHookAction.ReadValue<float>()) > Mathf.Clamp01(_manualOverrideThreshold))
            {
                _autoDropActive = false;
                return;
            }

            var hookTransform = _hookController.transform;
            if (hookTransform == null)
            {
                return;
            }

            var minY = -Mathf.Abs(_hookController.MaxDepth);
            var position = hookTransform.position;
            position.y = Mathf.MoveTowards(position.y, minY, Mathf.Max(0.1f, _autoDropSpeed) * Time.deltaTime);
            hookTransform.position = position;

            if (Mathf.Abs(position.y - minY) <= 0.01f)
            {
                _autoDropActive = false;
            }
        }

        private void RefreshMoveHookAction()
        {
            if (_moveHookAction != null)
            {
                return;
            }

            _moveHookAction = _inputMapController != null
                ? _inputMapController.FindAction("Fishing/MoveHook")
                : null;
        }
    }
}
