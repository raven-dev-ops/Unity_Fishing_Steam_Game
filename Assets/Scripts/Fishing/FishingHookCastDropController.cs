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
        [SerializeField] private float _initialAutoCastDepth = 25f;
        [SerializeField] private float _autoRetractDepth = 20f;
        [SerializeField] private float _downDoubleTapWindowSeconds = 0.35f;
        [SerializeField] private float _upDoubleTapWindowSeconds = 0.35f;
        [SerializeField] private float _autoLowerSpeed = 7f;
        [SerializeField] private float _autoDropSpeed = 4.2f;
        [SerializeField] private float _autoReelSpeed = 7.6f;
        [SerializeField] private bool _matchAutoReelSpeedToDropSpeed = true;
        [SerializeField] private float _manualOverrideThreshold = 0.45f;

        private InputAction _moveHookAction;
        private bool _autoDropActive;
        private bool _autoReelActive;
        private bool _autoLowerActive;
        private bool _autoRaiseActive;
        private bool _stateMachineSubscribed;
        private float _lastDownPressTime = -10f;
        private float _lastUpPressTime = -10f;
        private bool _axisDownHeldLastFrame;
        private bool _axisUpHeldLastFrame;
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
                    TickInWaterControl();
                    break;
                case FishingActionState.Reel:
                    SetHookVisible(true);
                    TickAutoReelIn();
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
                _autoReelActive = previous == FishingActionState.InWater || previous == FishingActionState.Reel;
                _autoLowerActive = false;
                _autoRaiseActive = false;
                _axisDownHeldLastFrame = false;
                _axisUpHeldLastFrame = false;
                _hookController.SetMovementEnabled(false);
                SetHookVisible(_autoReelActive);
                return;
            }

            if (next == FishingActionState.InWater)
            {
                _autoDropActive = true;
                _autoReelActive = false;
                _autoLowerActive = false;
                _autoRaiseActive = false;
                _lastDownPressTime = -10f;
                _lastUpPressTime = -10f;
                _axisDownHeldLastFrame = false;
                _axisUpHeldLastFrame = false;
                _hookController.SetMovementEnabled(false);
                SetHookVisible(true);
                return;
            }

            if (next == FishingActionState.Reel)
            {
                _autoDropActive = false;
                _autoReelActive = true;
                _autoLowerActive = false;
                _autoRaiseActive = false;
                _hookController.SetMovementEnabled(false);
                SetHookVisible(true);
                return;
            }

            _autoDropActive = false;
            _autoReelActive = false;
            _autoLowerActive = false;
            _autoRaiseActive = false;
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
                    _autoReelActive = false;
                    _autoLowerActive = false;
                    _autoRaiseActive = false;
                    _axisDownHeldLastFrame = false;
                    _axisUpHeldLastFrame = false;
                    _hookController.SetMovementEnabled(false);
                    SetHookVisible(false);
                    SnapHookToDock();
                    break;
                case FishingActionState.InWater:
                    _autoDropActive = true;
                    _autoReelActive = false;
                    _autoLowerActive = false;
                    _autoRaiseActive = false;
                    _lastDownPressTime = -10f;
                    _lastUpPressTime = -10f;
                    _axisDownHeldLastFrame = false;
                    _axisUpHeldLastFrame = false;
                    _hookController.SetMovementEnabled(false);
                    SetHookVisible(true);
                    break;
                case FishingActionState.Reel:
                    _autoDropActive = false;
                    _autoReelActive = true;
                    _autoLowerActive = false;
                    _autoRaiseActive = false;
                    _hookController.SetMovementEnabled(false);
                    SetHookVisible(true);
                    break;
                default:
                    _autoDropActive = false;
                    _autoReelActive = false;
                    _autoLowerActive = false;
                    _autoRaiseActive = false;
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
            RefreshMoveHookAction();

            var hookTransform = _hookController.transform;
            if (hookTransform == null)
            {
                return;
            }

            var targetY = ResolveWorldYForDepth(_initialAutoCastDepth);
            var position = hookTransform.position;
            position.y = Mathf.MoveTowards(position.y, targetY, Mathf.Max(0.1f, _autoDropSpeed) * Time.deltaTime);
            hookTransform.position = position;

            if (Mathf.Abs(position.y - targetY) <= 0.01f)
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
            var reelSpeed = ResolveAutoReelSpeed();

            var position = hookTransform.position;
            position.y = Mathf.MoveTowards(position.y, targetY, reelSpeed * Time.deltaTime);
            hookTransform.position = position;

            if (Mathf.Abs(position.y - targetY) <= 0.01f)
            {
                _autoReelActive = false;
                _hookController.SetMovementEnabled(false);
            }
        }

        private void TickInWaterControl()
        {
            if (_stateMachine == null || _stateMachine.State != FishingActionState.InWater || _hookController == null)
            {
                return;
            }

            if (_autoDropActive)
            {
                return;
            }

            if (_autoRaiseActive)
            {
                TickAutoRaiseToRetractDepth();
            }
            else if (_autoLowerActive)
            {
                TickAutoLower();
            }
            else
            {
                if (IsDownPressedThisFrame())
                {
                    var now = Time.unscaledTime;
                    if (now - _lastDownPressTime <= Mathf.Max(0.1f, _downDoubleTapWindowSeconds))
                    {
                        StartAutoLower();
                    }

                    _lastDownPressTime = now;
                }

                if (IsUpPressedThisFrame())
                {
                    var now = Time.unscaledTime;
                    if (now - _lastUpPressTime <= Mathf.Max(0.1f, _upDoubleTapWindowSeconds))
                    {
                        StartAutoRaise();
                    }

                    _lastUpPressTime = now;
                }
            }

            TryAutoRetractAtThreshold();
        }

        private void TickAutoLower()
        {
            if (_hookController == null || _hookController.transform == null)
            {
                _autoLowerActive = false;
                return;
            }

            if (IsUpInputHeld())
            {
                _autoLowerActive = false;
                _hookController.SetMovementEnabled(true);
                return;
            }

            _hookController.SetMovementEnabled(false);
            _hookController.GetWorldDepthBounds(out var minY, out _);
            var hookTransform = _hookController.transform;
            var position = hookTransform.position;
            position.y = Mathf.MoveTowards(position.y, minY, Mathf.Max(0.1f, _autoLowerSpeed) * Time.deltaTime);
            hookTransform.position = position;

            if (Mathf.Abs(position.y - minY) <= 0.01f)
            {
                _autoLowerActive = false;
                _hookController.SetMovementEnabled(true);
            }
        }

        private void StartAutoLower()
        {
            if (_autoDropActive || _autoReelActive || _stateMachine == null || _stateMachine.State != FishingActionState.InWater)
            {
                return;
            }

            _autoLowerActive = true;
            _autoRaiseActive = false;
            _hookController.SetMovementEnabled(false);
        }

        private void StartAutoRaise()
        {
            if (_autoDropActive || _autoReelActive || _stateMachine == null || _stateMachine.State != FishingActionState.InWater)
            {
                return;
            }

            _autoRaiseActive = true;
            _autoLowerActive = false;
            _hookController.SetMovementEnabled(false);
        }

        private void TickAutoRaiseToRetractDepth()
        {
            if (_hookController == null || _hookController.transform == null)
            {
                _autoRaiseActive = false;
                return;
            }

            if (IsDownInputHeld())
            {
                _autoRaiseActive = false;
                _hookController.SetMovementEnabled(true);
                return;
            }

            _hookController.SetMovementEnabled(false);
            var hookTransform = _hookController.transform;
            var targetY = ResolveWorldYForDepth(_autoRetractDepth);
            var position = hookTransform.position;
            position.y = Mathf.MoveTowards(position.y, targetY, ResolveAutoReelSpeed() * Time.deltaTime);
            hookTransform.position = position;

            if (_hookController.CurrentDepth <= Mathf.Max(0.1f, _autoRetractDepth) + 0.05f
                || Mathf.Abs(position.y - targetY) <= 0.01f)
            {
                _autoRaiseActive = false;
                _hookController.SetMovementEnabled(false);
                _stateMachine?.ResetToCast();
            }
        }

        private void TryAutoRetractAtThreshold()
        {
            if (_stateMachine == null
                || _hookController == null
                || _stateMachine.State != FishingActionState.InWater
                || _autoDropActive
                || _autoRaiseActive)
            {
                return;
            }

            if (!IsUpInputHeld())
            {
                return;
            }

            if (_hookController.CurrentDepth > Mathf.Max(0.1f, _autoRetractDepth))
            {
                return;
            }

            _autoLowerActive = false;
            _autoRaiseActive = false;
            _hookController.SetMovementEnabled(false);
            _stateMachine.ResetToCast();
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

        private void TryEnsureCastTransition()
        {
            if (_stateMachine == null
                || _stateMachine.State != FishingActionState.Cast
                || _autoReelActive
                || !WasCastInputPressedThisFrame())
            {
                return;
            }

            _stateMachine.AdvanceByAction();
        }

        private bool WasCastInputPressedThisFrame()
        {
            return IsDownPressedThisFrame();
        }

        private bool IsDownPressedThisFrame()
        {
            RefreshMoveHookAction();

            var keyboardPressed = false;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                keyboardPressed = keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            keyboardPressed = keyboardPressed
                || UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)
                || UnityEngine.Input.GetKeyDown(KeyCode.S);
#endif

            var threshold = Mathf.Clamp01(_manualOverrideThreshold);
            var axisDown = _moveHookAction != null && _moveHookAction.ReadValue<float>() < -threshold;
            var axisPressed = axisDown && !_axisDownHeldLastFrame;
            _axisDownHeldLastFrame = axisDown;
            return keyboardPressed || axisPressed;
        }

        private bool IsUpPressedThisFrame()
        {
            RefreshMoveHookAction();

            var keyboardPressed = false;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                keyboardPressed = keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            keyboardPressed = keyboardPressed
                || UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)
                || UnityEngine.Input.GetKeyDown(KeyCode.W);
#endif

            var threshold = Mathf.Clamp01(_manualOverrideThreshold);
            var axisUp = _moveHookAction != null && _moveHookAction.ReadValue<float>() > threshold;
            var axisPressed = axisUp && !_axisUpHeldLastFrame;
            _axisUpHeldLastFrame = axisUp;
            return keyboardPressed || axisPressed;
        }

        private bool IsUpInputHeld()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed))
            {
                return true;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKey(KeyCode.UpArrow) || UnityEngine.Input.GetKey(KeyCode.W))
            {
                return true;
            }
#endif

            RefreshMoveHookAction();
            if (_moveHookAction == null)
            {
                return false;
            }

            var threshold = Mathf.Clamp01(_manualOverrideThreshold);
            return _moveHookAction.ReadValue<float>() > threshold;
        }

        private bool IsDownInputHeld()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed))
            {
                return true;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKey(KeyCode.DownArrow) || UnityEngine.Input.GetKey(KeyCode.S))
            {
                return true;
            }
#endif

            if (_moveHookAction == null)
            {
                return false;
            }

            var threshold = Mathf.Clamp01(_manualOverrideThreshold);
            return _moveHookAction.ReadValue<float>() < -threshold;
        }

        private float ResolveAutoReelSpeed()
        {
            if (_matchAutoReelSpeedToDropSpeed)
            {
                return Mathf.Max(0.1f, _autoDropSpeed);
            }

            return Mathf.Max(0.1f, _autoReelSpeed);
        }

        private float ResolveWorldYForDepth(float depth)
        {
            if (_hookController == null || _ship == null)
            {
                return _hookController != null ? _hookController.transform.position.y : 0f;
            }

            _hookController.GetWorldDepthBounds(out var minY, out var maxY);
            var targetY = _ship.position.y - Mathf.Max(0.1f, depth);
            return Mathf.Clamp(targetY, minY, maxY);
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
