using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingLoopTutorialController : MonoBehaviour
    {
        private enum TutorialStep
        {
            None = 0,
            MoveShip = 1,
            Cast = 2,
            Hook = 3,
            Reel = 4,
            Land = 5,
            Complete = 6
        }

        private enum DemoAutoplayPhase
        {
            None = 0,
            SteerRight = 1,
            SteerLeft = 2,
            CastDrop = 3,
            FishHook = 4,
            ReelUp = 5,
            Finish = 6
        }

        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private FishingActionStateMachine _stateMachine;
        [SerializeField] private CatchResolver _catchResolver;
        [SerializeField] private ShipMovementController _shipMovement;
        [SerializeField] private HookMovementController _hookMovement;
        [SerializeField] private FishingAmbientFishSwimController _ambientFishController;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private MonoBehaviour _hudOverlayBehaviour;
        [SerializeField] private Button _skipTutorialButton;
        [SerializeField] private int _maxRecoveryFailures = 3;
        [SerializeField] private float _promptRefreshIntervalSeconds = 0.85f;
        [SerializeField] private float _demoShipTravelDistance = 2.8f;
        [SerializeField] private float _demoShipMoveSpeed = 4.8f;
        [SerializeField] private float _demoHookMoveSpeed = 7.5f;
        [SerializeField] private float _demoCastDepthMeters = 25f;
        [SerializeField] private float _demoDockOffsetY = 0.65f;

        private TutorialStep _step = TutorialStep.None;
        private bool _isActive;
        private bool _demoActive;
        private int _failureCount;
        private float _nextPromptRefreshAt;
        private DemoAutoplayPhase _demoPhase = DemoAutoplayPhase.None;
        private float _demoPhaseStartedAt;
        private float _demoShipStartX;
        private bool _demoFishApproachStarted;
        private bool _demoFishBound;
        private IFishingHudOverlay _hudOverlay;
        private FishingActionStateMachine _subscribedStateMachine;
        private CatchResolver _subscribedCatchResolver;
        private SaveManager _subscribedSaveManager;
        private InputAction _moveShipAction;
        private Transform _demoShipTransform;
        private Transform _demoHookTransform;
        private SpriteRenderer _demoHookRenderer;

        public void ConfigureSkipButton(Button skipTutorialButton)
        {
            if (_skipTutorialButton != null)
            {
                _skipTutorialButton.onClick.RemoveListener(SkipActiveTutorial);
            }

            _skipTutorialButton = skipTutorialButton;
            if (_skipTutorialButton != null)
            {
                _skipTutorialButton.onClick.AddListener(SkipActiveTutorial);
            }

            UpdateSkipButtonVisibility();
        }

        private void Awake()
        {
            EnsureDependencies();
        }

        private void OnEnable()
        {
            EnsureDependencies();
            SubscribeToDependencies();

            if (_skipTutorialButton != null)
            {
                _skipTutorialButton.onClick.RemoveListener(SkipActiveTutorial);
                _skipTutorialButton.onClick.AddListener(SkipActiveTutorial);
            }

            EvaluateActivation();
        }

        private void OnDisable()
        {
            UnsubscribeFromDependencies();
            CleanupDemoState(resetHookToDock: false);

            if (_skipTutorialButton != null)
            {
                _skipTutorialButton.onClick.RemoveListener(SkipActiveTutorial);
            }
        }

        private void Update()
        {
            EnsureDependencies();
            SubscribeToDependencies();

            if (!_isActive)
            {
                return;
            }

            if (_demoActive)
            {
                return;
            }

            RefreshMoveShipAction();
            if (_step == TutorialStep.MoveShip && IsShipMovementInputActive())
            {
                _step = TutorialStep.Cast;
                PushPrompt();
            }

            MaintainPrompt();
        }

        private void LateUpdate()
        {
            if (!_isActive || !_demoActive)
            {
                return;
            }

            TickDemoAutoplay();
        }

        public void SkipActiveTutorial()
        {
            if (!_isActive)
            {
                return;
            }

            CompleteTutorial(
                skipped: true,
                completionMessage: "Fishing tutorial skipped.");
            _orchestrator?.RequestOpenMainMenu();
        }

        private void EvaluateActivation()
        {
            EnsureDependencies();
            if (_saveManager == null)
            {
                return;
            }

            var shouldRun = _saveManager.ShouldRunFishingLoopTutorial();
            if (shouldRun && !_isActive)
            {
                BeginTutorial();
            }
            else if (!shouldRun && _isActive)
            {
                _isActive = false;
                _demoActive = false;
                _step = TutorialStep.None;
                _failureCount = 0;
                CleanupDemoState(resetHookToDock: false);
                UpdateSkipButtonVisibility();
            }
        }

        private void BeginTutorial()
        {
            _isActive = true;
            _step = TutorialStep.MoveShip;
            _demoActive = true;
            _failureCount = 0;
            _nextPromptRefreshAt = 0f;
            _saveManager?.MarkFishingLoopTutorialStarted();
            UpdateSkipButtonVisibility();
            _stateMachine?.ResetToCast();
            StartDemoSequence();
        }

        private void OnFishingStateChanged(FishingActionState previous, FishingActionState next)
        {
            if (!_isActive || _demoActive)
            {
                return;
            }

            if (next == FishingActionState.InWater && _step <= TutorialStep.Cast)
            {
                _step = TutorialStep.Hook;
                PushPrompt();
                return;
            }

            if (next == FishingActionState.Hooked && _step <= TutorialStep.Hook)
            {
                _step = TutorialStep.Reel;
                PushPrompt();
                return;
            }

            if (next == FishingActionState.Reel && _step <= TutorialStep.Reel)
            {
                _step = TutorialStep.Land;
                PushPrompt();
            }
        }

        private void OnCatchResolved(bool success, FishingFailReason failReason, string fishId)
        {
            if (!_isActive)
            {
                return;
            }

            if (success)
            {
                var fishLabel = string.IsNullOrWhiteSpace(fishId) ? "your catch" : fishId;
                CompleteTutorial(
                    skipped: false,
                    completionMessage: $"Fishing tutorial complete. You landed {fishLabel}.");
                return;
            }

            _failureCount += 1;
            if (_failureCount >= Mathf.Max(1, _maxRecoveryFailures))
            {
                CompleteTutorial(
                    skipped: false,
                    completionMessage: "Tutorial auto-completed after repeated failures.");
                return;
            }

            _step = ResolveRecoveryStep(failReason);
            var reasonHint = BuildFailureHint(failReason);
            _hudOverlay?.SetFishingStatus($"Tutorial hint: {reasonHint} Retry {_failureCount}/{_maxRecoveryFailures}.");
            _nextPromptRefreshAt = Time.unscaledTime + 2.2f;
        }

        private void CompleteTutorial(bool skipped, string completionMessage)
        {
            _isActive = false;
            _demoActive = false;
            _step = TutorialStep.Complete;
            _failureCount = 0;
            CleanupDemoState(resetHookToDock: false);
            _saveManager?.CompleteFishingLoopTutorial(skipped);
            UpdateSkipButtonVisibility();
            if (!string.IsNullOrWhiteSpace(completionMessage))
            {
                _hudOverlay?.SetFishingStatus(completionMessage);
            }
        }

        private void PushPrompt()
        {
            var prompt = BuildPromptForStep(_step);
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                _hudOverlay?.SetFishingStatus(prompt);
            }

            _nextPromptRefreshAt = Time.unscaledTime + Mathf.Max(0.25f, _promptRefreshIntervalSeconds);
        }

        private string BuildPromptForStep(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.MoveShip:
                    return "Fishing Tutorial Step 1/5: Move the ship left/right (A/D or Arrow keys). You can always steer, even while hook is down.";
                case TutorialStep.Cast:
                    return "Fishing Tutorial Step 2/5: Press Down Arrow or S to cast. The hook auto-drops toward 25m.";
                case TutorialStep.Hook:
                    return "Fishing Tutorial Step 3/5: Steer the ship so the hook collides with a fish to hook it.";
                case TutorialStep.Reel:
                    return "Fishing Tutorial Step 4/5: Press Up Arrow or W to start reeling.";
                case TutorialStep.Land:
                    return "Fishing Tutorial Step 5/5: Keep reeling. At 25m the catch is secured and then hauled to the boat.";
                default:
                    return string.Empty;
            }
        }

        private static string BuildFailureHint(FishingFailReason failReason)
        {
            switch (failReason)
            {
                case FishingFailReason.MissedHook:
                    return "Steer into a fish to hook it, then start reeling immediately.";
                case FishingFailReason.LineSnap:
                    return "Keep steady reeling and avoid abrupt depth changes.";
                case FishingFailReason.FishEscaped:
                    return "Start reeling earlier and keep steady pressure.";
                default:
                    return "Recast and try again.";
            }
        }

        private static TutorialStep ResolveRecoveryStep(FishingFailReason failReason)
        {
            switch (failReason)
            {
                case FishingFailReason.MissedHook:
                    return TutorialStep.Hook;
                case FishingFailReason.LineSnap:
                    return TutorialStep.Cast;
                case FishingFailReason.FishEscaped:
                    return TutorialStep.Hook;
                default:
                    return TutorialStep.Cast;
            }
        }

        private void OnSaveDataChanged(SaveDataV1 data)
        {
            EvaluateActivation();
        }

        private void StartDemoSequence()
        {
            EnsureDependencies();
            EnsureDemoAnchors();
            _demoActive = true;
            _demoFishApproachStarted = false;
            _demoFishBound = false;
            _demoShipStartX = _demoShipTransform != null ? _demoShipTransform.position.x : 0f;
            if (_demoShipTransform == null || _demoHookTransform == null)
            {
                EndDemoSequence();
                return;
            }

            StartDemoPhase(DemoAutoplayPhase.SteerRight);
        }

        private void TickDemoAutoplay()
        {
            if (!_demoActive)
            {
                return;
            }

            EnsureDemoAnchors();
            if (_demoShipTransform == null || _demoHookTransform == null)
            {
                EndDemoSequence();
                return;
            }

            switch (_demoPhase)
            {
                case DemoAutoplayPhase.SteerRight:
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    if (MoveShipTowardX(_demoShipStartX + Mathf.Max(0.5f, _demoShipTravelDistance))
                        || IsDemoPhaseElapsed(1.35f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.SteerLeft);
                    }
                    break;
                case DemoAutoplayPhase.SteerLeft:
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    if (MoveShipTowardX(_demoShipStartX - Mathf.Max(0.5f, _demoShipTravelDistance))
                        || IsDemoPhaseElapsed(1.6f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.CastDrop);
                    }
                    break;
                case DemoAutoplayPhase.CastDrop:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    if (MoveHookTowardDepth(Mathf.Max(1f, _demoCastDepthMeters))
                        || IsDemoPhaseElapsed(2.5f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.FishHook);
                    }
                    break;
                case DemoAutoplayPhase.FishHook:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoCastDepthMeters));
                    TickDemoFishHookVisual();
                    if (IsDemoPhaseElapsed(3f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.ReelUp);
                    }
                    break;
                case DemoAutoplayPhase.ReelUp:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    if (MoveHookToY(ResolveDemoDockY()) || IsDemoPhaseElapsed(2.2f))
                    {
                        ResolveDemoFish(caught: true);
                        StartDemoPhase(DemoAutoplayPhase.Finish);
                    }
                    break;
                case DemoAutoplayPhase.Finish:
                    MoveShipTowardX(_demoShipStartX);
                    MoveHookToY(ResolveDemoDockY());
                    SetDemoHookVisible(false);
                    if (IsDemoPhaseElapsed(0.6f))
                    {
                        EndDemoSequence();
                    }
                    break;
                default:
                    EndDemoSequence();
                    break;
            }
        }

        private void StartDemoPhase(DemoAutoplayPhase phase)
        {
            _demoPhase = phase;
            _demoPhaseStartedAt = Time.unscaledTime;
            string message;
            switch (phase)
            {
                case DemoAutoplayPhase.SteerRight:
                case DemoAutoplayPhase.SteerLeft:
                    message = "Fishing Tutorial Demo 1/4: Steer with A/D or Left/Right. The ship can move while the hook is down.";
                    break;
                case DemoAutoplayPhase.CastDrop:
                    message = "Fishing Tutorial Demo 2/4: Press Down/S to cast. The hook auto-drops toward 25m.";
                    break;
                case DemoAutoplayPhase.FishHook:
                    message = "Fishing Tutorial Demo 3/4: Move the ship so the hook collides with a fish to hook it.";
                    break;
                case DemoAutoplayPhase.ReelUp:
                    message = "Fishing Tutorial Demo 4/4: Press Up/W to reel. Secure at 25m, then haul to the boat.";
                    break;
                case DemoAutoplayPhase.Finish:
                    message = "Fishing Tutorial Demo complete. Your turn.";
                    break;
                default:
                    message = string.Empty;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                _hudOverlay?.SetFishingStatus(message);
            }
        }

        private bool IsDemoPhaseElapsed(float seconds)
        {
            return Time.unscaledTime >= _demoPhaseStartedAt + Mathf.Max(0.05f, seconds);
        }

        private void TickDemoFishHookVisual()
        {
            if (_ambientFishController == null || _demoHookTransform == null)
            {
                return;
            }

            if (!_demoFishApproachStarted)
            {
                _demoFishApproachStarted = true;
                if (_ambientFishController.TryBindFish(string.Empty, out _))
                {
                    _demoFishBound = true;
                    _ambientFishController.BeginBoundFishApproach(_demoHookTransform);
                }

                return;
            }

            if (_demoFishBound && _ambientFishController.IsBoundFishApproachComplete())
            {
                _ambientFishController.SetBoundFishHooked(_demoHookTransform);
            }
        }

        private void ResolveDemoFish(bool caught)
        {
            if (_ambientFishController == null)
            {
                _demoFishBound = false;
                _demoFishApproachStarted = false;
                return;
            }

            _ambientFishController.ResolveBoundFish(caught);
            _demoFishBound = false;
            _demoFishApproachStarted = false;
        }

        private void EndDemoSequence()
        {
            _demoActive = false;
            _demoPhase = DemoAutoplayPhase.None;
            ResolveDemoFish(caught: false);
            SetDemoHookVisible(false);
            _step = TutorialStep.MoveShip;
            PushPrompt();
        }

        private void CleanupDemoState(bool resetHookToDock)
        {
            _demoActive = false;
            _demoPhase = DemoAutoplayPhase.None;
            ResolveDemoFish(caught: false);
            if (resetHookToDock)
            {
                SnapDemoHookToDock();
            }

            SetDemoHookVisible(false);
        }

        private void EnsureDemoAnchors()
        {
            _hookMovement ??= GetComponent<HookMovementController>();
            _hookMovement ??= FindAnyObjectByType<HookMovementController>(FindObjectsInactive.Include);
            _shipMovement ??= GetComponent<ShipMovementController>();
            _shipMovement ??= FindAnyObjectByType<ShipMovementController>(FindObjectsInactive.Include);
            _ambientFishController ??= GetComponent<FishingAmbientFishSwimController>();
            _ambientFishController ??= FindAnyObjectByType<FishingAmbientFishSwimController>(FindObjectsInactive.Include);

            if (_hookMovement != null)
            {
                _demoHookTransform = _hookMovement.transform;
                _demoShipTransform = _hookMovement.ShipTransform != null
                    ? _hookMovement.ShipTransform
                    : _demoShipTransform;
            }

            if (_shipMovement != null)
            {
                _demoShipTransform = _shipMovement.transform;
            }

            if (_demoHookRenderer == null && _demoHookTransform != null)
            {
                _demoHookRenderer = _demoHookTransform.GetComponent<SpriteRenderer>();
            }
        }

        private bool MoveShipTowardX(float targetX)
        {
            if (_demoShipTransform == null)
            {
                return true;
            }

            var position = _demoShipTransform.position;
            position.x = Mathf.MoveTowards(
                position.x,
                targetX,
                Mathf.Max(0.1f, _demoShipMoveSpeed) * Time.deltaTime);
            _demoShipTransform.position = position;
            return Mathf.Abs(position.x - targetX) <= 0.02f;
        }

        private bool MoveHookTowardDepth(float depthMeters)
        {
            if (_demoShipTransform == null || _hookMovement == null)
            {
                return true;
            }

            _hookMovement.GetWorldDepthBounds(out var minY, out var maxY);
            var targetY = _demoShipTransform.position.y - Mathf.Max(0f, depthMeters);
            targetY = Mathf.Clamp(targetY, minY, maxY);
            return MoveHookToY(targetY);
        }

        private bool MoveHookToY(float targetY)
        {
            if (_demoHookTransform == null)
            {
                return true;
            }

            var position = _demoHookTransform.position;
            position.y = Mathf.MoveTowards(
                position.y,
                targetY,
                Mathf.Max(0.1f, _demoHookMoveSpeed) * Time.deltaTime);
            _demoHookTransform.position = position;
            return Mathf.Abs(position.y - targetY) <= 0.02f;
        }

        private float ResolveDemoDockY()
        {
            if (_hookMovement != null)
            {
                return _hookMovement.GetDockedY(_demoDockOffsetY);
            }

            if (_demoShipTransform != null)
            {
                return _demoShipTransform.position.y - Mathf.Abs(_demoDockOffsetY);
            }

            return _demoHookTransform != null ? _demoHookTransform.position.y : 0f;
        }

        private void SnapDemoHookToDock()
        {
            if (_demoHookTransform == null)
            {
                return;
            }

            var position = _demoHookTransform.position;
            position.y = ResolveDemoDockY();
            _demoHookTransform.position = position;
        }

        private void SetDemoHookVisible(bool visible)
        {
            if (_demoHookRenderer == null && _demoHookTransform != null)
            {
                _demoHookRenderer = _demoHookTransform.GetComponent<SpriteRenderer>();
            }

            if (_demoHookRenderer != null)
            {
                _demoHookRenderer.enabled = visible;
            }
        }

        private void MaintainPrompt()
        {
            if (Time.unscaledTime < _nextPromptRefreshAt)
            {
                return;
            }

            PushPrompt();
        }

        private void RefreshMoveShipAction()
        {
            if (_moveShipAction != null)
            {
                return;
            }

            _moveShipAction = _inputMapController != null
                ? _inputMapController.FindAction("Fishing/MoveShip")
                : null;
        }

        private bool IsShipMovementInputActive()
        {
            var axis = _moveShipAction != null
                ? Mathf.Clamp(_moveShipAction.ReadValue<float>(), -1f, 1f)
                : 0f;
            if (Mathf.Abs(axis) > 0.2f)
            {
                return true;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed
                    || keyboard.rightArrowKey.isPressed
                    || keyboard.aKey.isPressed
                    || keyboard.dKey.isPressed)
                {
                    return true;
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                var leftStickX = gamepad.leftStick.x.ReadValue();
                var dpadX = gamepad.dpad.x.ReadValue();
                if (Mathf.Abs(leftStickX) > 0.2f || Mathf.Abs(dpadX) > 0.2f)
                {
                    return true;
                }
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)
                || UnityEngine.Input.GetKey(KeyCode.RightArrow)
                || UnityEngine.Input.GetKey(KeyCode.A)
                || UnityEngine.Input.GetKey(KeyCode.D))
            {
                return true;
            }
#endif

            return false;
        }

        private void EnsureDependencies()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _stateMachine, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catchResolver, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _shipMovement, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _hookMovement, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _ambientFishController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);

            _saveManager ??= GetComponent<SaveManager>();
            _saveManager ??= FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);

            _orchestrator ??= GetComponent<GameFlowOrchestrator>();
            _orchestrator ??= FindAnyObjectByType<GameFlowOrchestrator>(FindObjectsInactive.Include);

            _stateMachine ??= GetComponent<FishingActionStateMachine>();
            _stateMachine ??= FindAnyObjectByType<FishingActionStateMachine>(FindObjectsInactive.Include);

            _catchResolver ??= GetComponent<CatchResolver>();
            _catchResolver ??= FindAnyObjectByType<CatchResolver>(FindObjectsInactive.Include);

            _shipMovement ??= GetComponent<ShipMovementController>();
            _shipMovement ??= FindAnyObjectByType<ShipMovementController>(FindObjectsInactive.Include);

            _hookMovement ??= GetComponent<HookMovementController>();
            _hookMovement ??= FindAnyObjectByType<HookMovementController>(FindObjectsInactive.Include);

            _ambientFishController ??= GetComponent<FishingAmbientFishSwimController>();
            _ambientFishController ??= FindAnyObjectByType<FishingAmbientFishSwimController>(FindObjectsInactive.Include);

            _inputMapController ??= GetComponent<InputActionMapController>();
            _inputMapController ??= FindAnyObjectByType<InputActionMapController>(FindObjectsInactive.Include);

            if (_hudOverlay == null)
            {
                _hudOverlay = _hudOverlayBehaviour as IFishingHudOverlay;
            }

            if (_hudOverlay == null)
            {
                _hudOverlay = FindFishingHudOverlay();
            }
        }

        private void SubscribeToDependencies()
        {
            if (_subscribedStateMachine != _stateMachine)
            {
                if (_subscribedStateMachine != null)
                {
                    _subscribedStateMachine.StateChanged -= OnFishingStateChanged;
                }

                if (_stateMachine != null)
                {
                    _stateMachine.StateChanged += OnFishingStateChanged;
                }

                _subscribedStateMachine = _stateMachine;
            }

            if (_subscribedCatchResolver != _catchResolver)
            {
                if (_subscribedCatchResolver != null)
                {
                    _subscribedCatchResolver.CatchResolved -= OnCatchResolved;
                }

                if (_catchResolver != null)
                {
                    _catchResolver.CatchResolved += OnCatchResolved;
                }

                _subscribedCatchResolver = _catchResolver;
            }

            if (_subscribedSaveManager != _saveManager)
            {
                if (_subscribedSaveManager != null)
                {
                    _subscribedSaveManager.SaveDataChanged -= OnSaveDataChanged;
                }

                if (_saveManager != null)
                {
                    _saveManager.SaveDataChanged += OnSaveDataChanged;
                }

                _subscribedSaveManager = _saveManager;
            }
        }

        private void UnsubscribeFromDependencies()
        {
            if (_subscribedStateMachine != null)
            {
                _subscribedStateMachine.StateChanged -= OnFishingStateChanged;
                _subscribedStateMachine = null;
            }

            if (_subscribedCatchResolver != null)
            {
                _subscribedCatchResolver.CatchResolved -= OnCatchResolved;
                _subscribedCatchResolver = null;
            }

            if (_subscribedSaveManager != null)
            {
                _subscribedSaveManager.SaveDataChanged -= OnSaveDataChanged;
                _subscribedSaveManager = null;
            }
        }

        private void UpdateSkipButtonVisibility()
        {
            if (_skipTutorialButton == null)
            {
                return;
            }

            _skipTutorialButton.gameObject.SetActive(_isActive);
        }

        private static IFishingHudOverlay FindFishingHudOverlay()
        {
            var candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is IFishingHudOverlay overlay)
                {
                    return overlay;
                }
            }

            return null;
        }
    }
}
