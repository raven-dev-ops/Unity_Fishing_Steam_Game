using System.Collections;
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
        [SerializeField] private GameObject _tutorialTransitionOverlay;
        [SerializeField] private Image _tutorialTransitionFadeImage;
        [SerializeField] private Text _tutorialTransitionTitleText;
        [SerializeField] private Text _tutorialTransitionSubtitleText;
        [SerializeField] private int _maxRecoveryFailures = 3;
        [SerializeField] private float _promptRefreshIntervalSeconds = 0.85f;
        [SerializeField] private bool _showPromptCardDuringHandsOnTutorial = true;
        [SerializeField] private bool _allowAutoplayInBatchMode = false;
        [SerializeField] private float _demoMessageSeconds = 3f;
        [SerializeField] private float _demoActionPauseSeconds = 0.65f;
        [SerializeField] private float _demoSceneEndPauseSeconds = 3f;
        [SerializeField] private float _tutorialCompletionPauseSeconds = 3f;
        [SerializeField] private string _tutorialCompletionTitle = "Congratulations, Captain!";
        [SerializeField] private float _tutorialCompletionOverlayMaxAlpha = 0.88f;
        [SerializeField] private float _tutorialCompletionFadeSeconds = 0.35f;
        [SerializeField] private float _demoTransitionFadeToBlackSeconds = 0.32f;
        [SerializeField] private float _demoTransitionHoldSeconds = 2f;
        [SerializeField] private float _demoTransitionFadeFromBlackSeconds = 0.32f;
        [SerializeField] private float _demoShipTravelDistance = 2.8f;
        [SerializeField] private float _demoShipMoveSpeed = 4.8f;
        [SerializeField] private float _demoHookMoveSpeed = 7.5f;
        [SerializeField] private float _demoCastDepthMeters = 30f;
        [SerializeField] private float _demoLevel4CastDepthMeters = 4500f;
        [SerializeField] private float _demoLevel5CastDepthMeters = 3300f;
        [SerializeField] private float _demoLevel4ReelTargetDepthMeters = 1000f;
        [SerializeField] private float _demoLevel5ReelTargetDepthMeters = 3000f;
        [SerializeField] private float _demoLevel4DarknessPreviewDepthMeters = 4500f;
        [SerializeField] private float _demoLevel5DeepDarkPreviewDepthMeters = 3300f;
        [SerializeField] private float _demoDeepCastSpeedMultiplier = 85f;
        [SerializeField] private float _demoDeepReelSpeedMultiplier = 110f;
        [SerializeField] private Vector2 _demoLevel4LightRadiiMeters = new Vector2(28f, 14f);
        [SerializeField] private Vector2 _demoLevel5LightRadiiMeters = new Vector2(30f, 15f);
        [SerializeField] private float _demoHookedFishFadeDelayMeters = 20f;
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
        private bool _demoSceneTransitionActive;
        private DemoAutoplayPhase _demoPendingTransitionPhase = DemoAutoplayPhase.None;
        private bool _demoPendingTransitionPhasePrepared;
        private float _demoSceneTransitionStartedAt;
        private string _demoSceneTransitionTitle = string.Empty;
        private string _demoSceneTransitionSubtitle = string.Empty;
        private DemoAutoplayPhase _demoQueuedNextPhase = DemoAutoplayPhase.None;
        private float _demoQueuedNextPhaseAt;
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
        private Coroutine _completeTutorialFlowRoutine;

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

        public void ConfigureTutorialTransitionOverlay(
            GameObject tutorialTransitionOverlay,
            Image tutorialTransitionFadeImage,
            Text tutorialTransitionTitleText,
            Text tutorialTransitionSubtitleText = null)
        {
            _tutorialTransitionOverlay = tutorialTransitionOverlay;
            _tutorialTransitionFadeImage = tutorialTransitionFadeImage;
            _tutorialTransitionTitleText = tutorialTransitionTitleText;
            _tutorialTransitionSubtitleText = tutorialTransitionSubtitleText;
            ClearDemoSceneTransition(forceHideOverlay: true);
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
            if (_completeTutorialFlowRoutine != null)
            {
                StopCoroutine(_completeTutorialFlowRoutine);
                _completeTutorialFlowRoutine = null;
            }

            ClearTutorialCompletionOverlay();

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

            if (Application.isBatchMode && !_allowAutoplayInBatchMode)
            {
                if (_isActive)
                {
                    _isActive = false;
                    _demoActive = false;
                    _step = TutorialStep.None;
                    _failureCount = 0;
                    CleanupDemoState(resetHookToDock: false);
                    UpdateSkipButtonVisibility();
                }

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
                    completionMessage: $"Congratulations! Fishing tutorial complete. You landed {fishLabel}.");
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
                SetTutorialMessage(completionMessage);
            }

            if (_completeTutorialFlowRoutine != null)
            {
                StopCoroutine(_completeTutorialFlowRoutine);
                _completeTutorialFlowRoutine = null;
            }

            var pauseSeconds = skipped
                ? 0f
                : Mathf.Max(0f, _tutorialCompletionPauseSeconds);
            if (pauseSeconds <= 0f)
            {
                ClearTutorialCompletionOverlay();
                SetTutorialMessageBoxVisible(false);
                _orchestrator?.RequestCompleteFishingTutorialFlow();
                return;
            }

            _completeTutorialFlowRoutine = StartCoroutine(CompleteTutorialFlowAfterDelay(pauseSeconds, skipped));
        }

        private IEnumerator CompleteTutorialFlowAfterDelay(float delaySeconds, bool skipped)
        {
            var totalDuration = Mathf.Max(0.05f, delaySeconds);
            var startedAt = Time.unscaledTime;
            var targetTime = startedAt + totalDuration;
            while (Time.unscaledTime < targetTime)
            {
                if (!skipped)
                {
                    var elapsed = Time.unscaledTime - startedAt;
                    UpdateTutorialCompletionOverlay(elapsed, totalDuration);
                }

                yield return null;
            }

            ClearTutorialCompletionOverlay();
            SetTutorialMessageBoxVisible(false);
            _orchestrator?.RequestCompleteFishingTutorialFlow();
            _completeTutorialFlowRoutine = null;
        }

        private void UpdateTutorialCompletionOverlay(float elapsedSeconds, float totalDurationSeconds)
        {
            if (_tutorialTransitionOverlay == null || _tutorialTransitionFadeImage == null)
            {
                return;
            }

            var total = Mathf.Max(0.05f, totalDurationSeconds);
            var fadeWindow = Mathf.Clamp(_tutorialCompletionFadeSeconds, 0.05f, total * 0.45f);
            var normalizedAlpha = 1f;
            if (elapsedSeconds < fadeWindow)
            {
                normalizedAlpha = Mathf.Clamp01(elapsedSeconds / fadeWindow);
            }
            else if (elapsedSeconds > total - fadeWindow)
            {
                normalizedAlpha = Mathf.Clamp01((total - elapsedSeconds) / fadeWindow);
            }

            var maxAlpha = Mathf.Clamp01(_tutorialCompletionOverlayMaxAlpha);
            var overlayAlpha = maxAlpha * normalizedAlpha;
            UpdateTransitionOverlayVisual(
                overlayAlpha,
                _tutorialCompletionTitle,
                string.Empty,
                forceVisible: overlayAlpha > 0.001f);
        }

        private void ClearTutorialCompletionOverlay()
        {
            UpdateTransitionOverlayVisual(0f, string.Empty, string.Empty, forceVisible: false);
        }

        private void PushPrompt()
        {
            var prompt = BuildPromptForStep(_step);
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                _hudOverlay?.SetFishingStatus(prompt);
                if (_showPromptCardDuringHandsOnTutorial && !_demoActive)
                {
                    SetTutorialMessage(prompt);
                }
                else if (!_demoActive)
                {
                    SetTutorialMessageBoxVisible(false);
                }
            }
            else if (!_demoActive)
            {
                SetTutorialMessageBoxVisible(false);
            }

            _nextPromptRefreshAt = Time.unscaledTime + Mathf.Max(0.25f, _promptRefreshIntervalSeconds);
        }

        private string BuildPromptForStep(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.MoveShip:
                    return "Hands-On Step 1/5: Steer left or right (A/D, Arrow keys, or Left Stick/D-Pad). Steering always works, even with the hook down.";
                case TutorialStep.Cast:
                    return "Hands-On Step 2/5: Cast by pressing Down/S (or Left Stick down). The hook drops toward 25m.";
                case TutorialStep.Hook:
                    return "Hands-On Step 3/5: Keep steering until the hook collides with a fish to secure the hook.";
                case TutorialStep.Reel:
                    return "Hands-On Step 4/5: Start reeling with Up/W (or Left Stick up).";
                case TutorialStep.Land:
                    return "Hands-On Step 5/5: Keep reeling. At 25m the catch is secured, then hauled to the boat.";
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
            ClearDemoSceneTransition(forceHideOverlay: true);
            ClearQueuedDemoPhaseTransition();
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

            if (TickDemoSceneTransition())
            {
                return;
            }

            if (TickQueuedDemoPhaseTransition())
            {
                return;
            }

            switch (_demoPhase)
            {
                case DemoAutoplayPhase.IntroInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    TickInfoPhase(DemoAutoplayPhase.MoveShipInfo, pauseBeforeTransition: true);
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
                        QueueDemoPhaseTransition(DemoAutoplayPhase.CastInfo, _demoSceneEndPauseSeconds);
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
                        QueueDemoPhaseTransition(DemoAutoplayPhase.FishHookInfo, _demoSceneEndPauseSeconds);
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
                        QueueDemoPhaseTransition(DemoAutoplayPhase.ReelInfo, _demoSceneEndPauseSeconds);
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
                        if (QueueDemoPhaseTransition(DemoAutoplayPhase.ShipUpgradeInfo, _demoSceneEndPauseSeconds))
                        {
                            ResolveDemoFish(caught: true);
                        }
                    }
                    break;
                case DemoAutoplayPhase.ShipUpgradeInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    TickInfoPhase(DemoAutoplayPhase.HookUpgradeInfo, pauseBeforeTransition: true);
                    break;
                case DemoAutoplayPhase.HookUpgradeInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(false);
                    SnapDemoHookToDock();
                    TickInfoPhase(DemoAutoplayPhase.Level4DarknessInfo, pauseBeforeTransition: true);
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
                    var level4CastComplete = MoveHookTowardDepth(
                        Mathf.Max(1f, _demoLevel4CastDepthMeters),
                        clampToWorldBounds: false,
                        _demoDeepCastSpeedMultiplier);
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    if (level4CastComplete || IsDemoPhaseElapsed(9f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.Level4FishHook);
                    }
                    break;
                case DemoAutoplayPhase.Level4FishHook:
                    MoveShipDuringHookDemo();
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel4CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
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
                    var level4ReelInfoTargetDepth = Mathf.Clamp(
                        _demoLevel4ReelTargetDepthMeters,
                        0f,
                        Mathf.Max(0f, _demoLevel4CastDepthMeters));
                    MoveHookTowardDepth(level4ReelInfoTargetDepth, clampToWorldBounds: false, _demoDeepReelSpeedMultiplier);
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    UpdateDemoHookedFishFade(Mathf.Max(1f, _demoLevel4CastDepthMeters), level4ReelInfoTargetDepth);
                    TickInfoPhase(DemoAutoplayPhase.Level4ReelUp);
                    break;
                case DemoAutoplayPhase.Level4ReelUp:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    var level4ReelUpTargetDepth = Mathf.Clamp(
                        _demoLevel4ReelTargetDepthMeters,
                        0f,
                        Mathf.Max(0f, _demoLevel4CastDepthMeters));
                    MoveHookTowardDepth(level4ReelUpTargetDepth, clampToWorldBounds: false, _demoDeepReelSpeedMultiplier);
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    UpdateDemoHookedFishFade(Mathf.Max(1f, _demoLevel4CastDepthMeters), level4ReelUpTargetDepth);
                    if (Mathf.Abs(ResolveDemoHookDepthMeters() - level4ReelUpTargetDepth) <= 0.08f
                        || IsDemoPhaseElapsed(6.2f))
                    {
                        if (QueueDemoPhaseTransition(DemoAutoplayPhase.Level5DeepDarkInfo, _demoSceneEndPauseSeconds))
                        {
                            ResolveDemoFish(caught: true);
                        }
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
                    var level5CastComplete = MoveHookTowardDepth(
                        Mathf.Max(1f, _demoLevel5CastDepthMeters),
                        clampToWorldBounds: false,
                        _demoDeepCastSpeedMultiplier);
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    if (level5CastComplete || IsDemoPhaseElapsed(7f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.Level5FishHook);
                    }
                    break;
                case DemoAutoplayPhase.Level5FishHook:
                    MoveShipDuringHookDemo();
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel5CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
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
                    var level5ReelInfoTargetDepth = Mathf.Clamp(
                        _demoLevel5ReelTargetDepthMeters,
                        0f,
                        Mathf.Max(0f, _demoLevel5CastDepthMeters));
                    MoveHookTowardDepth(level5ReelInfoTargetDepth, clampToWorldBounds: false, _demoDeepReelSpeedMultiplier);
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    UpdateDemoHookedFishFade(Mathf.Max(1f, _demoLevel5CastDepthMeters), level5ReelInfoTargetDepth);
                    TickInfoPhase(DemoAutoplayPhase.Level5ReelUp);
                    break;
                case DemoAutoplayPhase.Level5ReelUp:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    var level5ReelUpTargetDepth = Mathf.Clamp(
                        _demoLevel5ReelTargetDepthMeters,
                        0f,
                        Mathf.Max(0f, _demoLevel5CastDepthMeters));
                    MoveHookTowardDepth(level5ReelUpTargetDepth, clampToWorldBounds: false, _demoDeepReelSpeedMultiplier);
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    UpdateDemoHookedFishFade(Mathf.Max(1f, _demoLevel5CastDepthMeters), level5ReelUpTargetDepth);
                    if (Mathf.Abs(ResolveDemoHookDepthMeters() - level5ReelUpTargetDepth) <= 0.08f
                        || IsDemoPhaseElapsed(7f))
                    {
                        if (QueueDemoPhaseTransition(DemoAutoplayPhase.FinishInfo, _demoSceneEndPauseSeconds))
                        {
                            ResolveDemoFish(caught: true);
                        }
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
            if (TryBeginDemoSceneTransition(phase))
            {
                return;
            }

            StartDemoPhaseImmediate(phase);
        }

        private void StartDemoPhaseImmediate(DemoAutoplayPhase phase)
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
                ApplyTutorialDepthPreview(enabled: false, 0f);
            }
            else if (phase == DemoAutoplayPhase.Level4CastDrop
                || phase == DemoAutoplayPhase.Level4FishHook
                || phase == DemoAutoplayPhase.Level4ReelInfo)
            {
                ApplyTutorialLightPreview(enabled: true, _demoLevel4LightRadiiMeters);
                ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
            }
            else if (phase == DemoAutoplayPhase.Level4ReelUp)
            {
                var level4ReelTargetDepth = Mathf.Clamp(
                    _demoLevel4ReelTargetDepthMeters,
                    0f,
                    Mathf.Max(0f, _demoLevel4CastDepthMeters));
                ApplyTutorialLightPreview(enabled: true, _demoLevel4LightRadiiMeters);
                ApplyTutorialDepthPreview(enabled: true, level4ReelTargetDepth);
            }
            else if (phase == DemoAutoplayPhase.Level5DeepDarkInfo)
            {
                ApplyTutorialLightPreview(enabled: true, _demoLevel5LightRadiiMeters);
                ApplyTutorialDepthPreview(enabled: false, 0f);
            }
            else if (phase == DemoAutoplayPhase.Level5CastDrop
                || phase == DemoAutoplayPhase.Level5FishHook
                || phase == DemoAutoplayPhase.Level5ReelInfo)
            {
                ApplyTutorialLightPreview(enabled: true, _demoLevel5LightRadiiMeters);
                ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
            }
            else if (phase == DemoAutoplayPhase.Level5ReelUp)
            {
                var level5ReelTargetDepth = Mathf.Clamp(
                    _demoLevel5ReelTargetDepthMeters,
                    0f,
                    Mathf.Max(0f, _demoLevel5CastDepthMeters));
                ApplyTutorialLightPreview(enabled: true, _demoLevel5LightRadiiMeters);
                ApplyTutorialDepthPreview(enabled: true, level5ReelTargetDepth);
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

        private bool TickDemoSceneTransition()
        {
            if (!_demoSceneTransitionActive)
            {
                return false;
            }

            var fadeToBlackSeconds = Mathf.Max(0.05f, _demoTransitionFadeToBlackSeconds);
            var holdSeconds = Mathf.Max(0f, _demoTransitionHoldSeconds);
            var fadeFromBlackSeconds = Mathf.Max(0.05f, _demoTransitionFadeFromBlackSeconds);
            var elapsed = Time.unscaledTime - _demoSceneTransitionStartedAt;
            var fadeOutEndsAt = fadeToBlackSeconds;
            var holdEndsAt = fadeOutEndsAt + holdSeconds;
            var fadeInEndsAt = holdEndsAt + fadeFromBlackSeconds;

            if (elapsed < fadeOutEndsAt)
            {
                var fadeOutProgress = Mathf.Clamp01(elapsed / fadeToBlackSeconds);
                UpdateTransitionOverlayVisual(fadeOutProgress, _demoSceneTransitionTitle, _demoSceneTransitionSubtitle, forceVisible: true);
                return true;
            }

            if (_demoPendingTransitionPhase != DemoAutoplayPhase.None
                && !_demoPendingTransitionPhasePrepared)
            {
                StartDemoPhaseImmediate(_demoPendingTransitionPhase);
                _demoPendingTransitionPhasePrepared = true;
            }

            if (elapsed < holdEndsAt)
            {
                UpdateTransitionOverlayVisual(1f, _demoSceneTransitionTitle, _demoSceneTransitionSubtitle, forceVisible: true);
                return true;
            }

            if (elapsed < fadeInEndsAt)
            {
                var fadeInProgress = Mathf.Clamp01((elapsed - holdEndsAt) / fadeFromBlackSeconds);
                var overlayAlpha = 1f - fadeInProgress;
                UpdateTransitionOverlayVisual(overlayAlpha, _demoSceneTransitionTitle, _demoSceneTransitionSubtitle, forceVisible: true);
                return true;
            }

            if (_demoPendingTransitionPhasePrepared && _demoPhase != DemoAutoplayPhase.None)
            {
                // Reset phase timer once fade-in finishes so scene actions start visibly.
                _demoPhaseStartedAt = Time.unscaledTime;
            }

            ClearDemoSceneTransition(forceHideOverlay: true);
            return false;
        }

        private bool TryBeginDemoSceneTransition(DemoAutoplayPhase phase)
        {
            var title = BuildDemoSceneTitle(phase);
            if (string.IsNullOrWhiteSpace(title)
                || _tutorialTransitionOverlay == null
                || _tutorialTransitionFadeImage == null)
            {
                return false;
            }

            _demoSceneTransitionActive = true;
            _demoPendingTransitionPhase = phase;
            _demoPendingTransitionPhasePrepared = false;
            _demoSceneTransitionStartedAt = Time.unscaledTime;
            _demoSceneTransitionTitle = title;
            _demoSceneTransitionSubtitle = BuildDemoSceneSubtitle(phase);
            SetTutorialMessageBoxVisible(false);
            UpdateTransitionOverlayVisual(0f, title, _demoSceneTransitionSubtitle, forceVisible: true);
            return true;
        }

        private void UpdateTransitionOverlayVisual(float overlayAlpha, string title, string subtitle, bool forceVisible)
        {
            if (_tutorialTransitionFadeImage != null)
            {
                var fadeColor = _tutorialTransitionFadeImage.color;
                fadeColor.a = Mathf.Clamp01(overlayAlpha);
                _tutorialTransitionFadeImage.color = fadeColor;
            }

            if (_tutorialTransitionTitleText != null)
            {
                _tutorialTransitionTitleText.text = title ?? string.Empty;
                var titleColor = _tutorialTransitionTitleText.color;
                titleColor.a = Mathf.Clamp01(overlayAlpha);
                _tutorialTransitionTitleText.color = titleColor;
            }

            if (_tutorialTransitionSubtitleText != null)
            {
                _tutorialTransitionSubtitleText.text = subtitle ?? string.Empty;
                var subtitleColor = _tutorialTransitionSubtitleText.color;
                subtitleColor.a = Mathf.Clamp01(overlayAlpha);
                _tutorialTransitionSubtitleText.color = subtitleColor;
            }

            if (_tutorialTransitionOverlay != null)
            {
                _tutorialTransitionOverlay.SetActive(forceVisible);
            }
        }

        private void ClearDemoSceneTransition(bool forceHideOverlay)
        {
            _demoSceneTransitionActive = false;
            _demoPendingTransitionPhase = DemoAutoplayPhase.None;
            _demoPendingTransitionPhasePrepared = false;
            _demoSceneTransitionStartedAt = 0f;
            _demoSceneTransitionTitle = string.Empty;
            _demoSceneTransitionSubtitle = string.Empty;

            if (!forceHideOverlay)
            {
                return;
            }

            UpdateTransitionOverlayVisual(0f, string.Empty, string.Empty, forceVisible: false);
        }

        private bool TickQueuedDemoPhaseTransition()
        {
            if (_demoQueuedNextPhase == DemoAutoplayPhase.None)
            {
                return false;
            }

            if (Time.unscaledTime < _demoQueuedNextPhaseAt)
            {
                return true;
            }

            var nextPhase = _demoQueuedNextPhase;
            ClearQueuedDemoPhaseTransition();
            StartDemoPhase(nextPhase);
            return true;
        }

        private bool QueueDemoPhaseTransition(DemoAutoplayPhase nextPhase, float delaySeconds)
        {
            if (nextPhase == DemoAutoplayPhase.None)
            {
                return false;
            }

            if (_demoQueuedNextPhase == nextPhase)
            {
                return false;
            }

            _demoQueuedNextPhase = nextPhase;
            _demoQueuedNextPhaseAt = Time.unscaledTime + Mathf.Max(0f, delaySeconds);
            return true;
        }

        private void ClearQueuedDemoPhaseTransition()
        {
            _demoQueuedNextPhase = DemoAutoplayPhase.None;
            _demoQueuedNextPhaseAt = 0f;
        }

        private bool IsDemoPhaseElapsed(float seconds)
        {
            return Time.unscaledTime >= _demoPhaseStartedAt + Mathf.Max(0.05f, seconds);
        }

        private void TickInfoPhase(DemoAutoplayPhase nextPhase, bool pauseBeforeTransition = false)
        {
            if (IsDemoPhaseElapsed(Mathf.Max(0.25f, _demoMessageSeconds)))
            {
                if (pauseBeforeTransition)
                {
                    QueueDemoPhaseTransition(nextPhase, _demoSceneEndPauseSeconds);
                }
                else
                {
                    StartDemoPhase(nextPhase);
                }
            }
        }

        private string BuildDemoMessage(DemoAutoplayPhase phase)
        {
            switch (phase)
            {
                case DemoAutoplayPhase.IntroInfo:
                    return "Fishing tutorial demo. Each scene pauses before transitions so you can track the full loop.";
                case DemoAutoplayPhase.MoveShipInfo:
                    return "Step 1: Steer left/right (A/D, Arrow keys, or Left Stick/D-Pad). You can always steer, even while the hook is down.";
                case DemoAutoplayPhase.CastInfo:
                    return "Step 2: Cast with Down/S (or Left Stick down). The hook descends to 30m.";
                case DemoAutoplayPhase.FishHookInfo:
                    return "Step 3: Keep steering while the hook is down. A fish approaches and is hooked on collision.";
                case DemoAutoplayPhase.ReelInfo:
                    return "Step 4: Reel with Up/W (or Left Stick up). Secure the fish, then haul it to the boat.";
                case DemoAutoplayPhase.ShipUpgradeInfo:
                    return "Ship upgrades expand depth access: Lv3 up to 1,600m, Lv4 from 1,000m to 3,000m, Lv5 from 3,000m to 5,000m.";
                case DemoAutoplayPhase.HookUpgradeInfo:
                    return "Hook upgrades improve drop speed and special effects as hook level increases.";
                case DemoAutoplayPhase.Level4DarknessInfo:
                    return $"Level 4 darkness tutorial: drop to {_demoLevel4CastDepthMeters:0,0}m for a full low-visibility pass. Lv4 light radius is {_demoLevel4LightRadiiMeters.x:0.#}m in darkness and {_demoLevel4LightRadiiMeters.y:0.#}m in deep-dark.";
                case DemoAutoplayPhase.Level4ReelInfo:
                    return $"Level 4 darkness catch: fish is hooked, then reels from {_demoLevel4CastDepthMeters:0,0}m toward the {_demoLevel4ReelTargetDepthMeters:0,0}m line while fading out.";
                case DemoAutoplayPhase.Level5DeepDarkInfo:
                    return $"Level 5 hook demo: cast into deep-dark (3km-5km). Lv5 light radius is {_demoLevel5LightRadiiMeters.x:0.#}m in darkness and {_demoLevel5LightRadiiMeters.y:0.#}m in deep-dark.";
                case DemoAutoplayPhase.Level5ReelInfo:
                    return $"Level 5 deep-dark catch: hook and reel toward the Lv5 ship line at {_demoLevel5ReelTargetDepthMeters:0,0}m.";
                case DemoAutoplayPhase.FinishInfo:
                    return "Demo complete. Your turn next: steer, cast, hook on collision, then reel your fish in.";
                default:
                    return string.Empty;
            }
        }

        private static string BuildDemoSceneTitle(DemoAutoplayPhase phase)
        {
            switch (phase)
            {
                case DemoAutoplayPhase.IntroInfo:
                    return "Scene 1: Tutorial Intro";
                case DemoAutoplayPhase.MoveShipInfo:
                    return "Scene 2: Steering";
                case DemoAutoplayPhase.CastInfo:
                    return "Scene 3: Cast";
                case DemoAutoplayPhase.FishHookInfo:
                    return "Scene 4: Hook";
                case DemoAutoplayPhase.ReelInfo:
                    return "Scene 5: Reel";
                case DemoAutoplayPhase.ShipUpgradeInfo:
                    return "Scene 6: Ship Depth Bands";
                case DemoAutoplayPhase.HookUpgradeInfo:
                    return "Scene 7: Hook Light Tiers";
                case DemoAutoplayPhase.Level4DarknessInfo:
                    return "Scene 8: Darkness Catch";
                case DemoAutoplayPhase.Level5DeepDarkInfo:
                    return "Scene 9: Deep-Dark Catch";
                case DemoAutoplayPhase.FinishInfo:
                    return "Scene 10: Your Turn";
                default:
                    return string.Empty;
            }
        }

        private static string BuildDemoSceneSubtitle(DemoAutoplayPhase phase)
        {
            switch (phase)
            {
                case DemoAutoplayPhase.IntroInfo:
                    return "Tutorial flow overview";
                case DemoAutoplayPhase.MoveShipInfo:
                    return "Core ship movement";
                case DemoAutoplayPhase.CastInfo:
                    return "Hook deployment";
                case DemoAutoplayPhase.FishHookInfo:
                    return "Fish approach and collision";
                case DemoAutoplayPhase.ReelInfo:
                    return "Reel mechanics";
                case DemoAutoplayPhase.ShipUpgradeInfo:
                    return "Depth range progression";
                case DemoAutoplayPhase.HookUpgradeInfo:
                    return "Hook tier abilities";
                case DemoAutoplayPhase.Level4DarknessInfo:
                    return "Low visibility at 4,500m";
                case DemoAutoplayPhase.Level5DeepDarkInfo:
                    return "Deep-dark pass at 3,300m";
                case DemoAutoplayPhase.FinishInfo:
                    return "Transition to player control";
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
                _ambientFishController.SetBoundFishVisualFade(1f);
                _demoFishHookVisualReady = true;
            }
        }

        private float ResolveDemoHookDepthMeters()
        {
            if (_hookMovement != null)
            {
                return Mathf.Max(0f, _hookMovement.CurrentDepth);
            }

            if (_demoShipTransform != null && _demoHookTransform != null)
            {
                return Mathf.Max(0f, _demoShipTransform.position.y - _demoHookTransform.position.y);
            }

            return 0f;
        }

        private void UpdateDemoHookedFishFade(float reelStartDepthMeters, float reelTargetDepthMeters)
        {
            if (_ambientFishController == null)
            {
                return;
            }

            var startDepth = Mathf.Max(0.1f, reelStartDepthMeters);
            var targetDepth = Mathf.Clamp(reelTargetDepthMeters, 0f, startDepth);
            var currentDepth = ResolveDemoHookDepthMeters();
            var fadeDelayMeters = Mathf.Clamp(_demoHookedFishFadeDelayMeters, 0f, startDepth);
            var fadeStartDepth = Mathf.Clamp(startDepth - fadeDelayMeters, targetDepth, startDepth);
            var progress = fadeStartDepth <= targetDepth + 0.001f
                ? 1f
                : Mathf.Clamp01(Mathf.InverseLerp(fadeStartDepth, targetDepth, currentDepth));
            var fade = Mathf.Lerp(1f, 0.06f, progress);
            _ambientFishController.SetBoundFishVisualFade(fade);
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
            ClearDemoSceneTransition(forceHideOverlay: true);
            ClearQueuedDemoPhaseTransition();
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
            ClearDemoSceneTransition(forceHideOverlay: true);
            ClearQueuedDemoPhaseTransition();
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
