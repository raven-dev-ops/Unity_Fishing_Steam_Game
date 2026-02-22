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
        [SerializeField] private float _autoReelSpeed = 7.6f;
        [SerializeField] private float _manualOverrideThreshold = 0.45f;
        [SerializeField] private bool _requireCastHoldForAutoDrop = true;
        [SerializeField] private float _castHoldReleaseGraceSeconds = 0.12f;
        [SerializeField] private float _minimumInitialDropDistance = 0.85f;

        private InputAction _moveHookAction;
        private InputAction _actionInput;
        private bool _autoDropActive;
        private bool _autoReelActive;
        private bool _stateMachineSubscribed;
        private float _inWaterElapsed;
        private float _castStartY;
        private SpriteRenderer _hookRenderer;

        public void Configure(
            FishingActionStateMachine stateMachine,
            HookMovementController hookController,
            Transform ship)
        {
            if (_stateMachine != stateMachine)
            {
                UnsubscribeFromStateMachine();
            }

            _stateMachine = stateMachine;
            SubscribeToStateMachine();
            _hookController = hookController;
            _ship = ship;
            CacheHookRenderer();
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

            CacheHookRenderer();
        }

        private void OnEnable()
        {
            SubscribeToStateMachine();
            ApplyStateImmediate();
        }

        private void OnDisable()
        {
            UnsubscribeFromStateMachine();
        }

        private void Update()
        {
            if (_stateMachine == null || _hookController == null || _hookController.transform == null || _ship == null)
            {
                return;
            }

            TryEnsureCastTransition();
            TryEnsureHeldCastTransition();

            switch (_stateMachine.State)
            {
                case FishingActionState.Cast:
                    if (_autoReelActive)
                    {
                        SetHookVisible(true);
                        TickAutoReelIn();
                    }
                    else if (_autoDropActive)
                    {
                        SetHookVisible(true);
                        TickAutoDrop();
                    }
                    else
                    {
                        SetHookVisible(false);
                        SnapHookToDock();
                    }

                    break;
                case FishingActionState.InWater:
                    SetHookVisible(true);
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
                _inWaterElapsed = 0f;
                _autoReelActive = previous == FishingActionState.InWater || previous == FishingActionState.Reel;
                _hookController.SetMovementEnabled(false);
                SetHookVisible(_autoReelActive);
                return;
            }

            if (next == FishingActionState.InWater)
            {
                _autoDropActive = true;
                _inWaterElapsed = 0f;
                _castStartY = _hookController.transform.position.y;
                _autoReelActive = false;
                _hookController.SetMovementEnabled(false);
                SetHookVisible(true);
                return;
            }

            _autoDropActive = false;
            _inWaterElapsed = 0f;
            _autoReelActive = false;
            _hookController.SetMovementEnabled(true);
            SetHookVisible(true);
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
                    _inWaterElapsed = 0f;
                    _autoReelActive = false;
                    _hookController.SetMovementEnabled(false);
                    SetHookVisible(false);
                    SnapHookToDock();
                    break;
                case FishingActionState.InWater:
                    _autoDropActive = true;
                    _inWaterElapsed = 0f;
                    _castStartY = _hookController.transform.position.y;
                    _autoReelActive = false;
                    _hookController.SetMovementEnabled(false);
                    SetHookVisible(true);
                    break;
                default:
                    _autoDropActive = false;
                    _inWaterElapsed = 0f;
                    _autoReelActive = false;
                    _hookController.SetMovementEnabled(true);
                    SetHookVisible(true);
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

            var targetY = _hookController.GetDockedY(_dockOffsetY);
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

            _hookController.SetMovementEnabled(false);
            _inWaterElapsed += Time.deltaTime;
            RefreshMoveHookAction();
            RefreshActionInput();
            var droppedDistance = Mathf.Max(0f, _castStartY - _hookController.transform.position.y);
            if (_requireCastHoldForAutoDrop
                && _inWaterElapsed > Mathf.Max(0f, _castHoldReleaseGraceSeconds)
                && droppedDistance >= Mathf.Max(0f, _minimumInitialDropDistance)
                && !IsActionHeld())
            {
                _autoDropActive = false;
                _hookController.SetMovementEnabled(true);
                return;
            }

            if (droppedDistance >= Mathf.Max(0f, _minimumInitialDropDistance) && IsManualHookInputActive())
            {
                _autoDropActive = false;
                _hookController.SetMovementEnabled(true);
                return;
            }

            var hookTransform = _hookController.transform;
            if (hookTransform == null)
            {
                return;
            }

            _hookController.GetWorldDepthBounds(out var minY, out _);
            var position = hookTransform.position;
            position.y = Mathf.MoveTowards(position.y, minY, Mathf.Max(0.1f, _autoDropSpeed) * Time.deltaTime);
            hookTransform.position = position;

            if (Mathf.Abs(position.y - minY) <= 0.01f)
            {
                _autoDropActive = false;
                _hookController.SetMovementEnabled(true);
            }
        }

        private void TickAutoReelIn()
        {
            var hookTransform = _hookController.transform;
            if (hookTransform == null || _ship == null)
            {
                return;
            }

            var targetY = _hookController.GetDockedY(_dockOffsetY);

            var position = hookTransform.position;
            position.y = Mathf.MoveTowards(position.y, targetY, Mathf.Max(0.1f, _autoReelSpeed) * Time.deltaTime);
            hookTransform.position = position;

            if (Mathf.Abs(position.y - targetY) <= 0.01f)
            {
                _autoReelActive = false;
                _hookController.SetMovementEnabled(false);
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

        private void RefreshActionInput()
        {
            if (_actionInput != null)
            {
                return;
            }

            _actionInput = _inputMapController != null
                ? _inputMapController.FindAction("Fishing/Action")
                : null;
        }

        private void CacheHookRenderer()
        {
            if (_hookRenderer != null || _hookController == null)
            {
                return;
            }

            _hookRenderer = _hookController.GetComponent<SpriteRenderer>();
        }

        private void SetHookVisible(bool visible)
        {
            CacheHookRenderer();
            if (_hookRenderer == null)
            {
                return;
            }

            _hookRenderer.enabled = visible;
        }

        private bool IsActionHeld()
        {
            if (_actionInput != null && _actionInput.IsPressed())
            {
                return true;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.isPressed)
            {
                return true;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKey(KeyCode.Space))
            {
                return true;
            }
#endif

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.isPressed;
        }

        private void TryEnsureCastTransition()
        {
            if (_stateMachine == null
                || _stateMachine.State != FishingActionState.Cast
                || _autoReelActive
                || !WasActionPressedThisFrame())
            {
                return;
            }

            _stateMachine.AdvanceByAction();
        }

        private void TryEnsureHeldCastTransition()
        {
            if (_stateMachine == null
                || _stateMachine.State != FishingActionState.Cast
                || _autoReelActive
                || !IsActionHeld())
            {
                return;
            }

            _stateMachine.AdvanceByAction();
        }

        private bool WasActionPressedThisFrame()
        {
            RefreshActionInput();
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

        private bool IsManualHookInputActive()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null
                && (keyboard.upArrowKey.isPressed
                    || keyboard.downArrowKey.isPressed
                    || keyboard.wKey.isPressed
                    || keyboard.sKey.isPressed))
            {
                return true;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKey(KeyCode.UpArrow)
                || UnityEngine.Input.GetKey(KeyCode.DownArrow)
                || UnityEngine.Input.GetKey(KeyCode.W)
                || UnityEngine.Input.GetKey(KeyCode.S))
            {
                return true;
            }
#endif

            if (_moveHookAction == null)
            {
                return false;
            }

            return Mathf.Abs(_moveHookAction.ReadValue<float>()) > Mathf.Clamp01(_manualOverrideThreshold);
        }

        private void SubscribeToStateMachine()
        {
            if (_stateMachineSubscribed || _stateMachine == null || !isActiveAndEnabled)
            {
                return;
            }

            _stateMachine.StateChanged += OnStateChanged;
            _stateMachineSubscribed = true;
        }

        private void UnsubscribeFromStateMachine()
        {
            if (!_stateMachineSubscribed || _stateMachine == null)
            {
                _stateMachineSubscribed = false;
                return;
            }

            _stateMachine.StateChanged -= OnStateChanged;
            _stateMachineSubscribed = false;
        }
    }
}
