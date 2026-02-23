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
            IntroInfo = 1,
            MoveShipInfo = 2,
            SteerRight = 3,
            SteerLeft = 4,
            CastInfo = 5,
            CastDrop = 6,
            FishHookInfo = 7,
            FishHook = 8,
            ReelInfo = 9,
            ReelUp = 10,
            ShipUpgradeInfo = 11,
            HookUpgradeInfo = 12,
            Level4DarknessInfo = 13,
            Level4CastDrop = 14,
            Level4FishHook = 15,
            Level4ReelInfo = 16,
            Level4ReelUp = 17,
            Level5DeepDarkInfo = 18,
            Level5CastDrop = 19,
            Level5FishHook = 20,
            Level5ReelInfo = 21,
            Level5ReelUp = 22,
            FinishInfo = 23,
            Finish = 24
        }

        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private FishingActionStateMachine _stateMachine;
        [SerializeField] private CatchResolver _catchResolver;
        [SerializeField] private ShipMovementController _shipMovement;
        [SerializeField] private HookMovementController _hookMovement;
        [SerializeField] private FishingHookCastDropController _hookCastDropController;
        [SerializeField] private FishingDepthDarknessController _depthDarknessController;
        [SerializeField] private FishingAmbientFishSwimController _ambientFishController;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private MonoBehaviour _hudOverlayBehaviour;
        [SerializeField] private Button _skipTutorialButton;
        [SerializeField] private GameObject _tutorialMessageBox;
        [SerializeField] private Text _tutorialMessageText;
        [SerializeField] private int _maxRecoveryFailures = 3;
        [SerializeField] private float _promptRefreshIntervalSeconds = 0.85f;
        [SerializeField] private float _demoMessageSeconds = 3f;
        [SerializeField] private float _demoActionPauseSeconds = 0.65f;
        [SerializeField] private float _demoShipTravelDistance = 2.8f;
        [SerializeField] private float _demoShipMoveSpeed = 4.8f;
        [SerializeField] private float _demoHookMoveSpeed = 7.5f;
        [SerializeField] private float _demoCastDepthMeters = 30f;
        [SerializeField] private float _demoLevel4CastDepthMeters = 1500f;
        [SerializeField] private float _demoLevel5CastDepthMeters = 3300f;
        [SerializeField] private float _demoLevel4DarknessPreviewDepthMeters = 1700f;
        [SerializeField] private float _demoLevel5DeepDarkPreviewDepthMeters = 3300f;
        [SerializeField] private float _demoDeepCastSpeedMultiplier = 85f;
        [SerializeField] private float _demoDeepReelSpeedMultiplier = 110f;
        [SerializeField] private Vector2 _demoLevel4LightRadiiMeters = new Vector2(15f, 5f);
        [SerializeField] private Vector2 _demoLevel5LightRadiiMeters = new Vector2(30f, 15f);
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
        private bool _demoFishHookVisualReady;
        private IFishingHudOverlay _hudOverlay;
        private FishingActionStateMachine _subscribedStateMachine;
        private CatchResolver _subscribedCatchResolver;
        private SaveManager _subscribedSaveManager;
        private InputAction _moveShipAction;
        private Transform _demoShipTransform;
        private Transform _demoHookTransform;
        private SpriteRenderer _demoHookRenderer;
        private bool _hookCastDropWasEnabledBeforeDemo;
        private bool _hookCastDropDisabledByTutorial;

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

        public void ConfigureTutorialMessageBox(GameObject tutorialMessageBox, Text tutorialMessageText)
        {
            _tutorialMessageBox = tutorialMessageBox;
            _tutorialMessageText = tutorialMessageText;
            SetTutorialMessageBoxVisible(false);
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

            _orchestrator?.RequestCompleteFishingTutorialFlow();
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
            SuppressRuntimeHookCastControllerForDemo();
            _demoActive = true;
            _demoFishApproachStarted = false;
            _demoFishBound = false;
            _demoFishHookVisualReady = false;
            _demoShipStartX = _demoShipTransform != null ? _demoShipTransform.position.x : 0f;
            if (_demoShipTransform == null || _demoHookTransform == null)
            {
                EndDemoSequence();
                return;
            }

            StartDemoPhase(DemoAutoplayPhase.IntroInfo);
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
                case DemoAutoplayPhase.IntroInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    TickInfoPhase(DemoAutoplayPhase.MoveShipInfo);
                    break;
                case DemoAutoplayPhase.MoveShipInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    TickInfoPhase(DemoAutoplayPhase.SteerRight);
                    break;
                case DemoAutoplayPhase.SteerRight:
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    if (MoveShipTowardX(_demoShipStartX + Mathf.Max(0.5f, _demoShipTravelDistance))
                        || IsDemoPhaseElapsed(2.4f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.SteerLeft);
                    }
                    break;
                case DemoAutoplayPhase.SteerLeft:
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    if (MoveShipTowardX(_demoShipStartX - Mathf.Max(0.5f, _demoShipTravelDistance))
                        || IsDemoPhaseElapsed(2.4f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.CastInfo);
                    }
                    break;
                case DemoAutoplayPhase.CastInfo:
                    MoveShipTowardX(_demoShipStartX);
                    TickInfoPhase(DemoAutoplayPhase.CastDrop);
                    break;
                case DemoAutoplayPhase.CastDrop:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    if (MoveHookTowardDepth(Mathf.Max(1f, _demoCastDepthMeters), clampToWorldBounds: false)
                        || IsDemoPhaseElapsed(4f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.FishHookInfo);
                    }
                    break;
                case DemoAutoplayPhase.FishHookInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoCastDepthMeters), clampToWorldBounds: false);
                    TickInfoPhase(DemoAutoplayPhase.FishHook);
                    break;
                case DemoAutoplayPhase.FishHook:
                    MoveShipDuringHookDemo();
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoCastDepthMeters), clampToWorldBounds: false);
                    TickDemoFishHookVisual();
                    if ((_demoFishHookVisualReady && IsDemoPhaseElapsed(1.2f))
                        || IsDemoPhaseElapsed(3.4f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.ReelInfo);
                    }
                    break;
                case DemoAutoplayPhase.ReelInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoCastDepthMeters), clampToWorldBounds: false);
                    TickInfoPhase(DemoAutoplayPhase.ReelUp);
                    break;
                case DemoAutoplayPhase.ReelUp:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    if (MoveHookToY(ResolveDemoDockY()) || IsDemoPhaseElapsed(3.2f))
                    {
                        ResolveDemoFish(caught: true);
                        StartDemoPhase(DemoAutoplayPhase.ShipUpgradeInfo);
                    }
                    break;
                case DemoAutoplayPhase.ShipUpgradeInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    TickInfoPhase(DemoAutoplayPhase.HookUpgradeInfo);
                    break;
                case DemoAutoplayPhase.HookUpgradeInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    TickInfoPhase(DemoAutoplayPhase.Level4DarknessInfo);
                    break;
                case DemoAutoplayPhase.Level4DarknessInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    TickInfoPhase(DemoAutoplayPhase.Level4CastDrop);
                    break;
                case DemoAutoplayPhase.Level4CastDrop:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    if (MoveHookTowardDepth(Mathf.Max(1f, _demoLevel4CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier)
                        || IsDemoPhaseElapsed(6.2f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.Level4FishHook);
                    }
                    break;
                case DemoAutoplayPhase.Level4FishHook:
                    MoveShipDuringHookDemo();
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel4CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    TickDemoFishHookVisual();
                    if ((_demoFishHookVisualReady && IsDemoPhaseElapsed(1.2f))
                        || IsDemoPhaseElapsed(3.8f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.Level4ReelInfo);
                    }
                    break;
                case DemoAutoplayPhase.Level4ReelInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel4CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    TickInfoPhase(DemoAutoplayPhase.Level4ReelUp);
                    break;
                case DemoAutoplayPhase.Level4ReelUp:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    if (MoveHookToY(ResolveDemoDockY(), _demoDeepReelSpeedMultiplier) || IsDemoPhaseElapsed(6.2f))
                    {
                        ResolveDemoFish(caught: true);
                        StartDemoPhase(DemoAutoplayPhase.Level5DeepDarkInfo);
                    }
                    break;
                case DemoAutoplayPhase.Level5DeepDarkInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    TickInfoPhase(DemoAutoplayPhase.Level5CastDrop);
                    break;
                case DemoAutoplayPhase.Level5CastDrop:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    if (MoveHookTowardDepth(Mathf.Max(1f, _demoLevel5CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier)
                        || IsDemoPhaseElapsed(7f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.Level5FishHook);
                    }
                    break;
                case DemoAutoplayPhase.Level5FishHook:
                    MoveShipDuringHookDemo();
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel5CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    TickDemoFishHookVisual();
                    if ((_demoFishHookVisualReady && IsDemoPhaseElapsed(1.25f))
                        || IsDemoPhaseElapsed(3.9f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.Level5ReelInfo);
                    }
                    break;
                case DemoAutoplayPhase.Level5ReelInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel5CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    TickInfoPhase(DemoAutoplayPhase.Level5ReelUp);
                    break;
                case DemoAutoplayPhase.Level5ReelUp:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    if (MoveHookToY(ResolveDemoDockY(), _demoDeepReelSpeedMultiplier) || IsDemoPhaseElapsed(7f))
                    {
                        ResolveDemoFish(caught: true);
                        StartDemoPhase(DemoAutoplayPhase.FinishInfo);
                    }
                    break;
                case DemoAutoplayPhase.FinishInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    TickInfoPhase(DemoAutoplayPhase.Finish);
                    break;
                case DemoAutoplayPhase.Finish:
                    MoveShipTowardX(_demoShipStartX);
                    MoveHookToY(ResolveDemoDockY());
                    SetDemoHookVisible(false);
                    if (IsDemoPhaseElapsed(Mathf.Max(0.2f, _demoActionPauseSeconds)))
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
            _demoFishHookVisualReady = false;

            if (phase == DemoAutoplayPhase.FishHook
                || phase == DemoAutoplayPhase.Level4FishHook
                || phase == DemoAutoplayPhase.Level5FishHook)
            {
                ResetDemoFishVisualState();
            }

            if (phase == DemoAutoplayPhase.Level4DarknessInfo)
            {
                ApplyTutorialLightPreview(enabled: true, _demoLevel4LightRadiiMeters);
                ApplyTutorialDepthPreview(enabled: true, _demoLevel4DarknessPreviewDepthMeters);
            }
            else if (phase == DemoAutoplayPhase.Level4CastDrop
                || phase == DemoAutoplayPhase.Level4FishHook
                || phase == DemoAutoplayPhase.Level4ReelInfo
                || phase == DemoAutoplayPhase.Level4ReelUp)
            {
                ApplyTutorialLightPreview(enabled: true, _demoLevel4LightRadiiMeters);
                ApplyTutorialDepthPreview(enabled: false, 0f);
            }
            else if (phase == DemoAutoplayPhase.Level5DeepDarkInfo)
            {
                ApplyTutorialLightPreview(enabled: true, _demoLevel5LightRadiiMeters);
                ApplyTutorialDepthPreview(enabled: true, _demoLevel5DeepDarkPreviewDepthMeters);
            }
            else if (phase == DemoAutoplayPhase.Level5CastDrop
                || phase == DemoAutoplayPhase.Level5FishHook
                || phase == DemoAutoplayPhase.Level5ReelInfo
                || phase == DemoAutoplayPhase.Level5ReelUp)
            {
                ApplyTutorialLightPreview(enabled: true, _demoLevel5LightRadiiMeters);
                ApplyTutorialDepthPreview(enabled: false, 0f);
            }
            else
            {
                ApplyTutorialLightPreview(enabled: false, Vector2.zero);
                ApplyTutorialDepthPreview(enabled: false, 0f);
            }

            var message = BuildDemoMessage(phase);
            if (!string.IsNullOrWhiteSpace(message))
            {
                SetTutorialMessage(message);
            }
            else
            {
                SetTutorialMessageBoxVisible(false);
            }
        }

        private bool IsDemoPhaseElapsed(float seconds)
        {
            return Time.unscaledTime >= _demoPhaseStartedAt + Mathf.Max(0.05f, seconds);
        }

        private void TickInfoPhase(DemoAutoplayPhase nextPhase)
        {
            if (IsDemoPhaseElapsed(Mathf.Max(0.25f, _demoMessageSeconds)))
            {
                StartDemoPhase(nextPhase);
            }
        }

        private string BuildDemoMessage(DemoAutoplayPhase phase)
        {
            switch (phase)
            {
                case DemoAutoplayPhase.IntroInfo:
                    return "Fishing tutorial demo. Each card stays up for 3 seconds so you can follow every step.";
                case DemoAutoplayPhase.MoveShipInfo:
                    return "Step 1: Move the ship left and right. You can always steer, even while the hook is down.";
                case DemoAutoplayPhase.CastInfo:
                    return "Step 2: Cast. The hook descends to 30m.";
                case DemoAutoplayPhase.FishHookInfo:
                    return "Step 3: Keep steering while the hook is down. A fish approaches and is hooked on collision.";
                case DemoAutoplayPhase.ReelInfo:
                    return "Step 4: Reel in. Secure the fish, then haul it to the boat.";
                case DemoAutoplayPhase.ShipUpgradeInfo:
                    return "Ship upgrades expand depth access: Lv3 up to 1,600m, Lv4 from 1,000m to 3,000m, Lv5 from 3,000m to 5,000m.";
                case DemoAutoplayPhase.HookUpgradeInfo:
                    return "Hook upgrades improve drop speed and special effects as hook level increases.";
                case DemoAutoplayPhase.Level4DarknessInfo:
                    return "Level 4 hook demo: cast into darkness (1km-3km). Lv4 light radius is 15m in darkness and 5m in deep-dark.";
                case DemoAutoplayPhase.Level4ReelInfo:
                    return "Level 4 darkness catch: hook and reel.";
                case DemoAutoplayPhase.Level5DeepDarkInfo:
                    return "Level 5 hook demo: cast into deep-dark (3km-5km). Lv5 light radius is 30m in darkness and 15m in deep-dark.";
                case DemoAutoplayPhase.Level5ReelInfo:
                    return "Level 5 deep-dark catch: hook and reel.";
                case DemoAutoplayPhase.FinishInfo:
                    return "Demo complete. Your turn: steer, cast, hook on collision, then reel.";
                default:
                    return string.Empty;
            }
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

            if (_demoFishBound
                && !_demoFishHookVisualReady
                && _ambientFishController.IsBoundFishApproachComplete())
            {
                _ambientFishController.SetBoundFishHooked(_demoHookTransform);
                _demoFishHookVisualReady = true;
            }
        }

        private void ResetDemoFishVisualState()
        {
            ResolveDemoFish(caught: false);
            _demoFishApproachStarted = false;
            _demoFishBound = false;
            _demoFishHookVisualReady = false;
        }

        private void ResolveDemoFish(bool caught)
        {
            if (_ambientFishController == null)
            {
                _demoFishBound = false;
                _demoFishApproachStarted = false;
                _demoFishHookVisualReady = false;
                return;
            }

            _ambientFishController.ResolveBoundFish(caught);
            _demoFishBound = false;
            _demoFishApproachStarted = false;
            _demoFishHookVisualReady = false;
        }

        private void EndDemoSequence()
        {
            _demoActive = false;
            _demoPhase = DemoAutoplayPhase.None;
            ResolveDemoFish(caught: false);
            RestoreRuntimeHookCastController();
            ApplyTutorialLightPreview(enabled: false, Vector2.zero);
            ApplyTutorialDepthPreview(enabled: false, 0f);
            SetTutorialMessageBoxVisible(false);
            SetDemoHookVisible(false);
            _step = TutorialStep.MoveShip;
            PushPrompt();
        }

        private void CleanupDemoState(bool resetHookToDock)
        {
            _demoActive = false;
            _demoPhase = DemoAutoplayPhase.None;
            ResolveDemoFish(caught: false);
            RestoreRuntimeHookCastController();
            ApplyTutorialLightPreview(enabled: false, Vector2.zero);
            ApplyTutorialDepthPreview(enabled: false, 0f);
            SetTutorialMessageBoxVisible(false);
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

        private void MoveShipDuringHookDemo()
        {
            var travelSpan = Mathf.Max(0.5f, _demoShipTravelDistance * 0.55f);
            var wave = Mathf.Sin((Time.unscaledTime - _demoPhaseStartedAt) * 2.8f);
            var targetX = _demoShipStartX + (travelSpan * wave);
            MoveShipTowardX(targetX);
        }

        private bool MoveHookTowardDepth(float depthMeters)
        {
            return MoveHookTowardDepth(depthMeters, clampToWorldBounds: true);
        }

        private bool MoveHookTowardDepth(float depthMeters, bool clampToWorldBounds, float speedMultiplier = 1f)
        {
            if (_demoShipTransform == null)
            {
                return true;
            }

            var targetY = _demoShipTransform.position.y - Mathf.Max(0f, depthMeters);
            if (clampToWorldBounds && _hookMovement != null)
            {
                _hookMovement.GetWorldDepthBounds(out var minY, out var maxY);
                targetY = Mathf.Clamp(targetY, minY, maxY);
            }

            return MoveHookToY(targetY, speedMultiplier);
        }

        private bool MoveHookToY(float targetY)
        {
            return MoveHookToY(targetY, 1f);
        }

        private bool MoveHookToY(float targetY, float speedMultiplier)
        {
            if (_demoHookTransform == null)
            {
                return true;
            }

            var position = _demoHookTransform.position;
            var resolvedSpeed = Mathf.Max(0.1f, _demoHookMoveSpeed * Mathf.Max(0.1f, speedMultiplier));
            position.y = Mathf.MoveTowards(
                position.y,
                targetY,
                resolvedSpeed * Time.deltaTime);
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

        private void SetTutorialMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _hudOverlay?.SetFishingStatus(message);
            }

            if (_tutorialMessageText != null)
            {
                _tutorialMessageText.text = message ?? string.Empty;
            }

            SetTutorialMessageBoxVisible(!string.IsNullOrWhiteSpace(message));
        }

        private void SetTutorialMessageBoxVisible(bool visible)
        {
            if (_tutorialMessageBox != null)
            {
                _tutorialMessageBox.SetActive(visible);
            }
        }

        private void ApplyTutorialLightPreview(bool enabled, Vector2 lightRadiiMeters)
        {
            if (_depthDarknessController == null)
            {
                return;
            }

            if (enabled)
            {
                _depthDarknessController.SetTutorialLightPreview(lightRadiiMeters);
            }
            else
            {
                _depthDarknessController.ClearTutorialLightPreview();
            }
        }

        private void ApplyTutorialDepthPreview(bool enabled, float depthMeters)
        {
            if (_depthDarknessController == null)
            {
                return;
            }

            if (enabled)
            {
                _depthDarknessController.SetTutorialDepthPreview(depthMeters);
            }
            else
            {
                _depthDarknessController.ClearTutorialDepthPreview();
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
            RuntimeServiceRegistry.Resolve(ref _hookCastDropController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _depthDarknessController, this, warnIfMissing: false);
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

            _hookCastDropController ??= GetComponent<FishingHookCastDropController>();
            _hookCastDropController ??= FindAnyObjectByType<FishingHookCastDropController>(FindObjectsInactive.Include);

            _depthDarknessController ??= GetComponent<FishingDepthDarknessController>();
            _depthDarknessController ??= FindAnyObjectByType<FishingDepthDarknessController>(FindObjectsInactive.Include);

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

        private void SuppressRuntimeHookCastControllerForDemo()
        {
            if (_hookCastDropController == null)
            {
                _hookCastDropDisabledByTutorial = false;
                _hookCastDropWasEnabledBeforeDemo = false;
                return;
            }

            _hookCastDropWasEnabledBeforeDemo = _hookCastDropController.enabled;
            if (_hookCastDropWasEnabledBeforeDemo)
            {
                _hookCastDropController.enabled = false;
                _hookCastDropDisabledByTutorial = true;
                return;
            }

            _hookCastDropDisabledByTutorial = false;
        }

        private void RestoreRuntimeHookCastController()
        {
            if (_hookCastDropController == null)
            {
                _hookCastDropDisabledByTutorial = false;
                return;
            }

            if (_hookCastDropDisabledByTutorial && _hookCastDropWasEnabledBeforeDemo)
            {
                _hookCastDropController.enabled = true;
            }

            _hookCastDropDisabledByTutorial = false;
            _hookCastDropWasEnabledBeforeDemo = false;
        }
    }
}
