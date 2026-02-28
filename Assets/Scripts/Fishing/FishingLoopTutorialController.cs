using System;
using System.Collections;
using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using TMPro;
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
        [SerializeField] private FishingCameraController _fishingCameraController;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private InputRebindingService _inputRebindingService;
        [SerializeField] private MonoBehaviour _hudOverlayBehaviour;
        [SerializeField] private Button _skipTutorialButton;
        [SerializeField] private Button _skipAllTutorialButton;
        [SerializeField] private GameObject _tutorialMessageBox;
        [SerializeField] private TMP_Text _tutorialMessageText;
        [SerializeField] private GameObject _tutorialTransitionOverlay;
        [SerializeField] private Image _tutorialTransitionFadeImage;
        [SerializeField] private TMP_Text _tutorialTransitionTitleText;
        [SerializeField] private TMP_Text _tutorialTransitionSubtitleText;
        [SerializeField] private string _skipTutorialButtonText = "Skip Tutorial";
        [SerializeField] private string _skipSceneButtonText = "Next Scene";
        [SerializeField] private string _skipAllButtonText = "Skip All";
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
        [SerializeField] private float _demoShipMoveSpeed = 9.6f;
        [SerializeField] private float _demoHookMoveSpeed = 7.5f;
        [SerializeField] private float _demoCastDepthMeters = 30f;
        [SerializeField] private float _demoShipUpgradePreviewDepthMinMeters = 1200f;
        [SerializeField] private float _demoShipUpgradePreviewDepthMaxMeters = 3200f;
        [SerializeField] private float _demoShipUpgradeDepthCycleSeconds = 2.4f;
        [SerializeField] private float _demoHookUpgradePreviewDepthMeters = 2800f;
        [SerializeField] private float _demoHookUpgradeLightPulseSeconds = 1.6f;
        [SerializeField] private float _demoLevel4CastDepthMeters = 4500f;
        [SerializeField] private float _demoLevel5CastDepthMeters = 3300f;
        [SerializeField] private float _demoLevel4ReelTargetDepthMeters = 1000f;
        [SerializeField] private float _demoLevel5ReelTargetDepthMeters = 3000f;
        [SerializeField] private float _demoLevel4DarknessPreviewDepthMeters = 4500f;
        [SerializeField] private float _demoLevel5DeepDarkPreviewDepthMeters = 3300f;
        [SerializeField] private float _demoDeepCastSpeedMultiplier = 85f;
        [SerializeField] private float _demoDeepReelSpeedMultiplier = 110f;
        [SerializeField] private float _demoLevel4ReelSpeedMultiplier = 1f;
        [SerializeField] private float _demoLevel5ReelSpeedMultiplier = 1f;
        [SerializeField] private float _demoLevel4ReelDistanceBeforeTransitionMeters = 20f;
        [SerializeField] private float _demoLevel5ReelDistanceBeforeTransitionMeters = 30f;
        [SerializeField] private float _demoLevel4ReelMaxPhaseSeconds = 4.5f;
        [SerializeField] private float _demoLevel4ReelCameraFollowLerpScale = 1.35f;
        [SerializeField] private float _demoLevel4ReelCameraHookViewportY = 0.3f;
        [SerializeField] private float _demoCameraZoomOutScale = 1.5f;
        [SerializeField] private float _demoHookCollisionRadius = 0.28f;
        [SerializeField] private float _demoHookPhaseSailSpeedScale = 0.32f;
        [SerializeField] private float _demoHookPhaseMaxWaitSeconds = 6.5f;
        [SerializeField] private float _demoLevel5ReelMaxPhaseSeconds = 6.5f;
        [SerializeField] private float _demoReelTransitionLeadSeconds = 0.05f;
        [SerializeField] private Vector2 _demoLevel4LightRadiiMeters = new Vector2(16f, 8f);
        [SerializeField] private Vector2 _demoLevel5LightRadiiMeters = new Vector2(30f, 15f);
        [SerializeField] private float _demoHookedFishFadeDelayMeters = 20f;
        [SerializeField] private float _demoHookedFishFadeDelaySeconds = 0.65f;
        [SerializeField] private float _demoDockOffsetY = 0.65f;

        public sealed class DependencyBundle
        {
            public SaveManager SaveManager { get; set; }
            public GameFlowOrchestrator Orchestrator { get; set; }
            public FishingActionStateMachine StateMachine { get; set; }
            public CatchResolver CatchResolver { get; set; }
            public ShipMovementController ShipMovement { get; set; }
            public HookMovementController HookMovement { get; set; }
            public FishingHookCastDropController HookCastDropController { get; set; }
            public FishingDepthDarknessController DepthDarknessController { get; set; }
            public FishingAmbientFishSwimController AmbientFishController { get; set; }
            public FishingCameraController FishingCameraController { get; set; }
            public InputActionMapController InputMapController { get; set; }
            public InputRebindingService InputRebindingService { get; set; }
            public IFishingHudOverlay HudOverlay { get; set; }
        }

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
        private bool _demoCurrentTransitionStartsBlack;
        private DemoAutoplayPhase _demoQueuedNextPhase = DemoAutoplayPhase.None;
        private float _demoQueuedNextPhaseAt;
        private IFishingHudOverlay _hudOverlay;
        private FishingActionStateMachine _subscribedStateMachine;
        private CatchResolver _subscribedCatchResolver;
        private SaveManager _subscribedSaveManager;
        private InputAction _moveShipAction;
        private InputAction _moveHookAction;
        private Transform _demoShipTransform;
        private Transform _demoHookTransform;
        private SpriteRenderer _demoHookRenderer;
        private bool _hookCastDropWasEnabledBeforeDemo;
        private bool _hookCastDropDisabledByTutorial;
        private Coroutine _completeTutorialFlowRoutine;
        private float _demoLevel4ReelStartDepthMeters;
        private float _demoLevel5ReelStartDepthMeters;
        private bool _demoHookDepthOverrideApplied;
        private float _demoHookPreviousMaxDepthMeters;
        private int _demoCollisionLaneIndex;
        private bool _dependenciesInitialized;
        private bool _missingDependencyLogEmitted;

        public void ConfigureDependencies(DependencyBundle dependencies)
        {
            if (dependencies == null)
            {
                return;
            }

            _saveManager = dependencies.SaveManager ?? _saveManager;
            _orchestrator = dependencies.Orchestrator ?? _orchestrator;
            _stateMachine = dependencies.StateMachine ?? _stateMachine;
            _catchResolver = dependencies.CatchResolver ?? _catchResolver;
            _shipMovement = dependencies.ShipMovement ?? _shipMovement;
            _hookMovement = dependencies.HookMovement ?? _hookMovement;
            _hookCastDropController = dependencies.HookCastDropController ?? _hookCastDropController;
            _depthDarknessController = dependencies.DepthDarknessController ?? _depthDarknessController;
            _ambientFishController = dependencies.AmbientFishController ?? _ambientFishController;
            _fishingCameraController = dependencies.FishingCameraController ?? _fishingCameraController;
            _inputMapController = dependencies.InputMapController ?? _inputMapController;
            _inputRebindingService = dependencies.InputRebindingService ?? _inputRebindingService;

            if (dependencies.HudOverlay != null)
            {
                _hudOverlay = dependencies.HudOverlay;
                if (dependencies.HudOverlay is MonoBehaviour hudOverlayBehaviour)
                {
                    _hudOverlayBehaviour = hudOverlayBehaviour;
                }
            }

            _dependenciesInitialized = false;
            _missingDependencyLogEmitted = false;
            TryInitializeDependencies(emitMissingDependencyError: true);
            SubscribeToDependencies();
            if (isActiveAndEnabled)
            {
                EvaluateActivation();
            }
        }

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

        public void ConfigureSkipAllButton(Button skipAllTutorialButton)
        {
            if (_skipAllTutorialButton != null)
            {
                _skipAllTutorialButton.onClick.RemoveListener(SkipAllTutorial);
            }

            _skipAllTutorialButton = skipAllTutorialButton;
            if (_skipAllTutorialButton != null)
            {
                _skipAllTutorialButton.onClick.AddListener(SkipAllTutorial);
            }

            UpdateSkipButtonVisibility();
        }

        public void ConfigureTutorialMessageBox(GameObject tutorialMessageBox, TMP_Text tutorialMessageText)
        {
            _tutorialMessageBox = tutorialMessageBox;
            _tutorialMessageText = tutorialMessageText;
            SetTutorialMessageBoxVisible(false);
        }

        public void ConfigureTutorialTransitionOverlay(
            GameObject tutorialTransitionOverlay,
            Image tutorialTransitionFadeImage,
            TMP_Text tutorialTransitionTitleText,
            TMP_Text tutorialTransitionSubtitleText = null)
        {
            _tutorialTransitionOverlay = tutorialTransitionOverlay;
            _tutorialTransitionFadeImage = tutorialTransitionFadeImage;
            _tutorialTransitionTitleText = tutorialTransitionTitleText;
            _tutorialTransitionSubtitleText = tutorialTransitionSubtitleText;
            ClearDemoSceneTransition(forceHideOverlay: true);
        }

        private void Awake()
        {
            TryInitializeDependencies();
        }

        private void OnEnable()
        {
            TryInitializeDependencies();
            if (_dependenciesInitialized)
            {
                SubscribeToDependencies();
            }

            if (_skipTutorialButton != null)
            {
                _skipTutorialButton.onClick.RemoveListener(SkipActiveTutorial);
                _skipTutorialButton.onClick.AddListener(SkipActiveTutorial);
            }

            if (_skipAllTutorialButton != null)
            {
                _skipAllTutorialButton.onClick.RemoveListener(SkipAllTutorial);
                _skipAllTutorialButton.onClick.AddListener(SkipAllTutorial);
            }

            if (_dependenciesInitialized)
            {
                EvaluateActivation();
            }
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

            if (_skipAllTutorialButton != null)
            {
                _skipAllTutorialButton.onClick.RemoveListener(SkipAllTutorial);
            }
        }

        private void Update()
        {
            if (!_dependenciesInitialized || !_isActive)
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

            if (_demoActive && TrySkipToNextDemoScene())
            {
                return;
            }

            CompleteTutorial(
                skipped: true,
                completionMessage: "Fishing tutorial skipped.");
        }

        public void SkipAllTutorial()
        {
            if (!_isActive)
            {
                return;
            }

            CompleteTutorial(
                skipped: true,
                completionMessage: "All fishing tutorial scenes skipped.");
        }

        internal void SetAutoplayInBatchModeForTests(bool enabled)
        {
            _allowAutoplayInBatchMode = enabled;
        }

        internal void BeginTutorialForTests()
        {
            BeginTutorial();
        }

        internal bool AreDependenciesInitializedForTests()
        {
            return _dependenciesInitialized;
        }

        internal bool IsDemoActiveForTests()
        {
            return _demoActive;
        }

        internal bool IsDemoSceneTransitionActiveForTests()
        {
            return _demoSceneTransitionActive;
        }

        internal string GetDemoPhaseNameForTests()
        {
            return _demoPhase.ToString();
        }

        internal void SetDemoActiveForTests(bool active)
        {
            _demoActive = active;
        }

        internal void SimulateFishingStateChangedForTests(FishingActionState previous, FishingActionState next)
        {
            OnFishingStateChanged(previous, next);
        }

        internal void SimulateCatchResolvedForTests(bool success, FishingFailReason failReason, string fishId)
        {
            OnCatchResolved(success, failReason, fishId);
        }

        internal bool IsActiveForTests()
        {
            return _isActive;
        }

        internal string GetStepNameForTests()
        {
            return _step.ToString();
        }

        private void EvaluateActivation()
        {
            if (!TryInitializeDependencies(emitMissingDependencyError: true))
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
            if (!TryInitializeDependencies(emitMissingDependencyError: true))
            {
                return;
            }

            _isActive = true;
            _step = TutorialStep.Cast;
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
                    return "Hands-On Setup: Ship auto-sails left in this build. No steering input is required.";
                case TutorialStep.Cast:
                    return $"Hands-On Step 1/4: Lower the hook with {ResolveMoveHookDownControlHint()} until fish lanes become active.";
                case TutorialStep.Hook:
                    return "Hands-On Step 2/4: Let the ship drag the hook through fish lanes until a collision secures a hook.";
                case TutorialStep.Reel:
                    return $"Hands-On Step 3/4: Start reeling with {ResolveMoveHookUpControlHint()}.";
                case TutorialStep.Land:
                    return "Hands-On Step 4/4: Keep reeling. At 25m the catch secures, then auto-hauls to the boat.";
                default:
                    return string.Empty;
            }
        }

        private static string BuildFailureHint(FishingFailReason failReason)
        {
            switch (failReason)
            {
                case FishingFailReason.MissedHook:
                    return "Keep the hook in fish paths to trigger a collision hook, then start reeling immediately.";
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
            if (!TryInitializeDependencies(emitMissingDependencyError: true))
            {
                return;
            }

            RefreshDemoAnchors();
            ApplyDemoHookDepthOverride();
            SuppressRuntimeHookCastControllerForDemo();
            ClearDemoSceneTransition(forceHideOverlay: true);
            ClearQueuedDemoPhaseTransition();
            _demoActive = true;
            _demoFishApproachStarted = false;
            _demoFishBound = false;
            _demoFishHookVisualReady = false;
            _demoCollisionLaneIndex = 0;
            _demoLevel4ReelStartDepthMeters = 0f;
            _demoLevel5ReelStartDepthMeters = 0f;
            _demoShipStartX = _demoShipTransform != null ? _demoShipTransform.position.x : 0f;
            if (_demoShipTransform == null || _demoHookTransform == null)
            {
                EndDemoSequence();
                return;
            }

            ApplyDemoCameraZoomOverride(active: true);
            StartDemoPhase(DemoAutoplayPhase.IntroInfo);
        }

        private void TickDemoAutoplay()
        {
            if (!_demoActive)
            {
                return;
            }

            RefreshDemoAnchors();
            if (_demoShipTransform == null || _demoHookTransform == null)
            {
                EndDemoSequence();
                return;
            }

            var depthOverrideReapplied = EnsureDemoHookDepthOverrideActive();
            if (depthOverrideReapplied)
            {
                RecoverDemoHookDepthAfterOverrideReset();
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
                    TickInfoPhase(DemoAutoplayPhase.CastInfo, pauseBeforeTransition: true);
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
                    SailShipLeftDuringHookDemo();
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoCastDepthMeters), clampToWorldBounds: false);
                    TickDemoFishHookVisual();
                    var fishHookTimeoutElapsed = IsDemoPhaseElapsed(Mathf.Max(2f, _demoHookPhaseMaxWaitSeconds));
                    if (fishHookTimeoutElapsed && !_demoFishHookVisualReady)
                    {
                        if (SeedDemoCollisionFish(forceNearHook: true))
                        {
                            _demoPhaseStartedAt = Time.unscaledTime - 0.9f;
                        }
                    }

                    if (_demoFishHookVisualReady && IsDemoPhaseElapsed(1.2f))
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
                    if (MoveHookToY(ResolveDemoDockY()))
                    {
                        SetDemoHookVisible(false);
                        if (QueueDemoPhaseTransition(DemoAutoplayPhase.ShipUpgradeInfo, _demoSceneEndPauseSeconds))
                        {
                            ResolveDemoFish(caught: true);
                        }
                    }
                    break;
                case DemoAutoplayPhase.ShipUpgradeInfo:
                    MoveShipTowardX(_demoShipStartX);
                    TickShipUpgradeInfoVisual();
                    TickInfoPhase(DemoAutoplayPhase.HookUpgradeInfo, pauseBeforeTransition: true);
                    break;
                case DemoAutoplayPhase.HookUpgradeInfo:
                    MoveShipTowardX(_demoShipStartX);
                    TickHookUpgradeInfoVisual();
                    TickInfoPhase(DemoAutoplayPhase.Level4DarknessInfo, pauseBeforeTransition: true);
                    break;
                case DemoAutoplayPhase.Level4DarknessInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel4CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    ApplyTutorialLightPreview(enabled: true, ResolveScene8LightRadiiMeters());
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    TickInfoPhase(DemoAutoplayPhase.Level4FishHook);
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
                    SailShipLeftDuringHookDemo();
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel4CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    ApplyTutorialLightPreview(enabled: true, ResolveScene8LightRadiiMeters());
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    TickDemoFishHookVisual();
                    var level4FishHookTimeoutElapsed = IsDemoPhaseElapsed(Mathf.Max(2.5f, _demoHookPhaseMaxWaitSeconds));
                    if (level4FishHookTimeoutElapsed && !_demoFishHookVisualReady)
                    {
                        if (SeedDemoCollisionFish(forceNearHook: true))
                        {
                            _demoPhaseStartedAt = Time.unscaledTime - 0.9f;
                        }
                    }

                    if (_demoFishHookVisualReady && IsDemoPhaseElapsed(1.2f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.Level4ReelInfo);
                    }
                    break;
                case DemoAutoplayPhase.Level4ReelInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel4CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    ApplyTutorialLightPreview(enabled: true, ResolveScene8LightRadiiMeters());
                    EnsureDemoFishHookedVisual();
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    TickInfoPhase(DemoAutoplayPhase.Level4ReelUp);
                    break;
                case DemoAutoplayPhase.Level4ReelUp:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    EnsureDemoFishHookedVisual();
                    var level4ReelDistanceBeforeTransition = Mathf.Max(1f, _demoLevel4ReelDistanceBeforeTransitionMeters);
                    var level4ReelStartDepth = Mathf.Max(
                        level4ReelDistanceBeforeTransition + 0.1f,
                        _demoLevel4ReelStartDepthMeters);
                    var level4ReelUpTargetDepth = Mathf.Clamp(
                        level4ReelStartDepth - level4ReelDistanceBeforeTransition,
                        0f,
                        Mathf.Max(0f, _demoLevel4CastDepthMeters));
                    var level4ReelSpeedMultiplier = ResolveDeepReelSpeedMultiplier(_demoLevel4ReelSpeedMultiplier);
                    MoveHookTowardDepth(level4ReelUpTargetDepth, clampToWorldBounds: false, level4ReelSpeedMultiplier);
                    ApplyScene8CameraFollowOverride(active: true);
                    ApplyTutorialLightPreview(enabled: true, ResolveScene8LightRadiiMeters());
                    var level4CurrentDepth = ResolveDemoHookDepthMeters();
                    ApplyTutorialDepthPreview(enabled: true, level4CurrentDepth);
                    var level4ReeledDistance = Mathf.Max(0f, level4ReelStartDepth - level4CurrentDepth);
                    var level4TransitionLeadMeters = ResolveDemoReelTransitionLeadMeters(level4ReelSpeedMultiplier);
                    if (level4ReeledDistance >= Mathf.Max(0f, level4ReelDistanceBeforeTransition - level4TransitionLeadMeters)
                        || IsDemoPhaseElapsed(Mathf.Max(0.25f, _demoLevel4ReelMaxPhaseSeconds)))
                    {
                        StartDemoPhase(DemoAutoplayPhase.Level5DeepDarkInfo);
                    }
                    break;
                case DemoAutoplayPhase.Level5DeepDarkInfo:
                    MoveShipTowardX(_demoShipStartX);
                    ApplyScene8CameraFollowOverride(active: false);
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel5CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    ApplyTutorialLightPreview(enabled: true, ResolveScene9LightRadiiMeters());
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    TickInfoPhase(DemoAutoplayPhase.Level5FishHook);
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
                    SailShipLeftDuringHookDemo();
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel5CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    ApplyTutorialLightPreview(enabled: true, ResolveScene9LightRadiiMeters());
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    TickDemoFishHookVisual();
                    var level5FishHookTimeoutElapsed = IsDemoPhaseElapsed(Mathf.Max(2.75f, _demoHookPhaseMaxWaitSeconds));
                    if (level5FishHookTimeoutElapsed && !_demoFishHookVisualReady)
                    {
                        if (SeedDemoCollisionFish(forceNearHook: true))
                        {
                            _demoPhaseStartedAt = Time.unscaledTime - 0.9f;
                        }
                    }

                    if (_demoFishHookVisualReady && IsDemoPhaseElapsed(1.25f))
                    {
                        StartDemoPhase(DemoAutoplayPhase.Level5ReelInfo);
                    }
                    break;
                case DemoAutoplayPhase.Level5ReelInfo:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    MoveHookTowardDepth(Mathf.Max(1f, _demoLevel5CastDepthMeters), clampToWorldBounds: false, _demoDeepCastSpeedMultiplier);
                    ApplyTutorialLightPreview(enabled: true, ResolveScene9LightRadiiMeters());
                    EnsureDemoFishHookedVisual();
                    ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                    TickInfoPhase(DemoAutoplayPhase.Level5ReelUp);
                    break;
                case DemoAutoplayPhase.Level5ReelUp:
                    MoveShipTowardX(_demoShipStartX);
                    SetDemoHookVisible(true);
                    EnsureDemoFishHookedVisual();
                    var level5ReelDistanceBeforeTransition = Mathf.Max(1f, _demoLevel5ReelDistanceBeforeTransitionMeters);
                    var level5ReelStartDepth = Mathf.Max(
                        level5ReelDistanceBeforeTransition + 0.1f,
                        _demoLevel5ReelStartDepthMeters);
                    var level5ReelUpTargetDepth = Mathf.Clamp(
                        level5ReelStartDepth - level5ReelDistanceBeforeTransition,
                        0f,
                        Mathf.Max(0f, _demoLevel5CastDepthMeters));
                    var level5ReelSpeedMultiplier = ResolveDeepReelSpeedMultiplier(_demoLevel5ReelSpeedMultiplier);
                    MoveHookTowardDepth(level5ReelUpTargetDepth, clampToWorldBounds: false, level5ReelSpeedMultiplier);
                    ApplyScene8CameraFollowOverride(active: true);
                    ApplyTutorialLightPreview(enabled: true, ResolveScene9LightRadiiMeters());
                    var level5CurrentDepth = ResolveDemoHookDepthMeters();
                    ApplyTutorialDepthPreview(enabled: true, level5CurrentDepth);
                    UpdateDemoHookedFishFade(level5ReelStartDepth, level5ReelUpTargetDepth, level5ReelSpeedMultiplier);
                    var level5ReeledDistance = Mathf.Max(0f, level5ReelStartDepth - level5CurrentDepth);
                    var level5TransitionLeadMeters = ResolveDemoReelTransitionLeadMeters(level5ReelSpeedMultiplier);
                    if (level5ReeledDistance >= Mathf.Max(0f, level5ReelDistanceBeforeTransition - level5TransitionLeadMeters)
                        || IsDemoPhaseElapsed(Mathf.Max(0.25f, _demoLevel5ReelMaxPhaseSeconds)))
                    {
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
            ApplyScene8CameraFollowOverride(active: phase == DemoAutoplayPhase.Level4ReelUp || phase == DemoAutoplayPhase.Level5ReelUp);

            if (phase == DemoAutoplayPhase.FishHook
                || phase == DemoAutoplayPhase.Level4FishHook
                || phase == DemoAutoplayPhase.Level5FishHook)
            {
                ResetDemoFishVisualState();
            }
            else if (phase == DemoAutoplayPhase.Level5DeepDarkInfo
                || phase == DemoAutoplayPhase.FinishInfo)
            {
                // Resolve previous scene's hooked fish once the next scene has been
                // prepared under transition so it does not visibly pop off-hook.
                ResolveDemoFish(caught: true);
            }

            if (phase == DemoAutoplayPhase.ShipUpgradeInfo)
            {
                SetDemoHookVisible(true);
                var minimumDepth = Mathf.Max(1f, Mathf.Min(_demoShipUpgradePreviewDepthMinMeters, _demoShipUpgradePreviewDepthMaxMeters));
                SnapDemoHookToDepth(minimumDepth, clampToWorldBounds: false);
                ApplyTutorialLightPreview(enabled: false, Vector2.zero);
                ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
            }
            else if (phase == DemoAutoplayPhase.HookUpgradeInfo)
            {
                SetDemoHookVisible(true);
                SnapDemoHookToDepth(Mathf.Max(1f, _demoHookUpgradePreviewDepthMeters), clampToWorldBounds: false);
                ApplyTutorialLightPreview(enabled: true, ResolveScene8LightRadiiMeters());
                ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
            }
            else if (phase == DemoAutoplayPhase.Level4DarknessInfo)
            {
                SetDemoHookVisible(true);
                SnapDemoHookToDepth(Mathf.Max(1f, _demoLevel4CastDepthMeters), clampToWorldBounds: false);
                ApplyTutorialLightPreview(enabled: true, ResolveScene8LightRadiiMeters());
                ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
            }
            else if (phase == DemoAutoplayPhase.Level4CastDrop
                || phase == DemoAutoplayPhase.Level4FishHook
                || phase == DemoAutoplayPhase.Level4ReelInfo)
            {
                ApplyTutorialLightPreview(enabled: true, ResolveScene8LightRadiiMeters());
                ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                if (phase == DemoAutoplayPhase.Level4ReelInfo)
                {
                    _ambientFishController?.SetBoundFishVisualFade(1f);
                }
            }
            else if (phase == DemoAutoplayPhase.Level4ReelUp)
            {
                _demoLevel4ReelStartDepthMeters = ResolveDemoHookDepthMeters();
                ApplyTutorialLightPreview(enabled: true, ResolveScene8LightRadiiMeters());
                ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
            }
            else if (phase == DemoAutoplayPhase.Level5DeepDarkInfo)
            {
                SetDemoHookVisible(true);
                SnapDemoHookToDepth(Mathf.Max(1f, _demoLevel5CastDepthMeters), clampToWorldBounds: false);
                ApplyTutorialLightPreview(enabled: true, ResolveScene9LightRadiiMeters());
                ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
            }
            else if (phase == DemoAutoplayPhase.Level5CastDrop
                || phase == DemoAutoplayPhase.Level5FishHook
                || phase == DemoAutoplayPhase.Level5ReelInfo)
            {
                ApplyTutorialLightPreview(enabled: true, ResolveScene9LightRadiiMeters());
                ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
                if (phase == DemoAutoplayPhase.Level5ReelInfo)
                {
                    _ambientFishController?.SetBoundFishVisualFade(1f);
                }
            }
            else if (phase == DemoAutoplayPhase.Level5ReelUp)
            {
                _demoLevel5ReelStartDepthMeters = ResolveDemoHookDepthMeters();
                ApplyTutorialLightPreview(enabled: true, ResolveScene9LightRadiiMeters());
                ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
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
            var fadeOutEndsAt = _demoCurrentTransitionStartsBlack ? 0f : fadeToBlackSeconds;
            var holdEndsAt = fadeOutEndsAt + holdSeconds;
            var fadeInEndsAt = holdEndsAt + fadeFromBlackSeconds;

            if (fadeOutEndsAt > 0f && elapsed < fadeOutEndsAt)
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
            var startFromBlack = phase == DemoAutoplayPhase.IntroInfo;
            _demoCurrentTransitionStartsBlack = startFromBlack;
            _demoSceneTransitionStartedAt = Time.unscaledTime;
            _demoSceneTransitionTitle = title;
            _demoSceneTransitionSubtitle = BuildDemoSceneSubtitle(phase);
            SetTutorialMessageBoxVisible(false);
            UpdateTransitionOverlayVisual(startFromBlack ? 1f : 0f, title, _demoSceneTransitionSubtitle, forceVisible: true);
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
            _demoCurrentTransitionStartsBlack = false;

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
                    return "Step 1: Ship auto-sails left continuously and drags the hook line for you.";
                case DemoAutoplayPhase.CastInfo:
                    return $"Step 2: Cast with {ResolveMoveHookDownControlHint()}. The hook descends to 30m.";
                case DemoAutoplayPhase.FishHookInfo:
                    return "Step 3: Keep the hook in fish lanes while the ship sails. Fish hook on collision.";
                case DemoAutoplayPhase.ReelInfo:
                    return $"Step 4: Reel with {ResolveMoveHookUpControlHint()}. Secure the fish, then haul it to the boat.";
                case DemoAutoplayPhase.ShipUpgradeInfo:
                    return "Ship upgrades expand depth access. This scene sweeps the hook through deeper ship bands to preview operating ranges.";
                case DemoAutoplayPhase.HookUpgradeInfo:
                    return "Hook upgrades increase light radius in dark and deep-dark water. This scene pulses Lv4 and Lv5 light tiers.";
                case DemoAutoplayPhase.Level4DarknessInfo:
                    var scene8Light = ResolveScene8LightRadiiMeters();
                    return $"Level 4 darkness tutorial: scene starts at {_demoLevel4CastDepthMeters:0,0}m for a full low-visibility pass. Scene 8 light radius is {scene8Light.x:0.#}m in darkness and {scene8Light.y:0.#}m in deep-dark.";
                case DemoAutoplayPhase.Level4ReelInfo:
                    return $"Level 4 darkness catch: fish is hooked, then reels up about {_demoLevel4ReelDistanceBeforeTransitionMeters:0.#}m with camera follow before transitioning.";
                case DemoAutoplayPhase.Level5DeepDarkInfo:
                    var scene9Light = ResolveScene9LightRadiiMeters();
                    return $"Level 5 hook demo: scene starts in deep-dark at {_demoLevel5CastDepthMeters:0,0}m. Scene 9 light radius is {scene9Light.x:0.#}m in darkness and {scene9Light.y:0.#}m in deep-dark.";
                case DemoAutoplayPhase.Level5ReelInfo:
                    return $"Level 5 deep-dark catch: fish stays hooked while reeling up about {_demoLevel5ReelDistanceBeforeTransitionMeters:0.#}m, then transitions before the hook fully stops.";
                case DemoAutoplayPhase.FinishInfo:
                    return "Demo complete. Your turn next: lower hook, collide to hook fish, then reel it in.";
                default:
                    return string.Empty;
            }
        }

        private static string BuildDemoSceneTitle(DemoAutoplayPhase phase)
        {
            switch (phase)
            {
                case DemoAutoplayPhase.IntroInfo:
                    return "Scene 1: How to Play";
                case DemoAutoplayPhase.MoveShipInfo:
                    return "Scene 2: Auto Sail";
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
                    return "Baseline ship motion";
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
                _demoFishBound = SeedDemoCollisionFish(forceNearHook: false);
                return;
            }

            if (_demoFishHookVisualReady)
            {
                return;
            }

            var collisionRadius = Mathf.Max(0.05f, _demoHookCollisionRadius);
            if (_ambientFishController.TryBindCollidingFishToHook(_demoHookTransform, collisionRadius, out _))
            {
                _demoFishBound = true;
                _ambientFishController.SetBoundFishHooked(_demoHookTransform);
                _ambientFishController.SetBoundFishVisualFade(1f);
                _demoFishHookVisualReady = true;
                return;
            }

            if (_demoFishBound && _ambientFishController.IsBoundFishCollidingWithHook(_demoHookTransform, collisionRadius))
            {
                _ambientFishController.SetBoundFishHooked(_demoHookTransform);
                _ambientFishController.SetBoundFishVisualFade(1f);
                _demoFishHookVisualReady = true;
            }
        }

        private bool SeedDemoCollisionFish(bool forceNearHook)
        {
            if (_ambientFishController == null || _demoHookTransform == null)
            {
                return false;
            }

            if (!_ambientFishController.TryBindFish(string.Empty, out var fishTransform) || fishTransform == null)
            {
                return false;
            }

            var laneCount = 5;
            var laneIndex = Mathf.Abs(_demoCollisionLaneIndex++);
            var laneRatio = laneCount <= 1
                ? 0.5f
                : (laneIndex % laneCount) / (float)(laneCount - 1);

            var runtimeCamera = Camera.main;
            var hookViewportRatio = 0.5f;
            if (runtimeCamera != null)
            {
                var hookViewportPosition = runtimeCamera.WorldToViewportPoint(_demoHookTransform.position);
                if (hookViewportPosition.z > 0f)
                {
                    hookViewportRatio = Mathf.Clamp01(hookViewportPosition.y);
                }
            }

            var laneSpread = forceNearHook ? 0.015f : 0.05f;
            var laneOffset = Mathf.Lerp(-laneSpread, laneSpread, laneRatio);
            var spawnViewportRatio = Mathf.Clamp01(hookViewportRatio + laneOffset);
            var spawnFromLeft = true;
            var speedMultiplier = forceNearHook ? 1.25f : 1.1f;
            if (_ambientFishController.PositionBoundFishForDemo(spawnViewportRatio, spawnFromLeft, speedMultiplier))
            {
                return true;
            }

            var hookPosition = _demoHookTransform.position;
            var fallbackVerticalOffset = Mathf.Lerp(-0.45f, 0.45f, laneRatio);
            var fallbackSpawnX = hookPosition.x - 18f;
            if (runtimeCamera != null && runtimeCamera.orthographic)
            {
                var halfWidth = Mathf.Max(0.5f, runtimeCamera.orthographicSize * Mathf.Max(0.1f, runtimeCamera.aspect));
                fallbackSpawnX = runtimeCamera.transform.position.x - halfWidth - 1.8f;
            }

            fishTransform.position = new Vector3(
                fallbackSpawnX,
                hookPosition.y + fallbackVerticalOffset,
                fishTransform.position.z);
            return true;
        }

        private float ResolveDemoHookDepthMeters()
        {
            if (_demoShipTransform != null && _demoHookTransform != null)
            {
                return Mathf.Max(0f, _demoShipTransform.position.y - _demoHookTransform.position.y);
            }

            if (_hookMovement != null)
            {
                return Mathf.Max(0f, _hookMovement.CurrentDepth);
            }

            return 0f;
        }

        private void EnsureDemoFishHookedVisual()
        {
            if (_ambientFishController == null || _demoHookTransform == null)
            {
                return;
            }

            if (!_demoFishBound && _ambientFishController.TryBindFish(string.Empty, out _))
            {
                _demoFishBound = true;
            }

            if (_demoFishBound)
            {
                _ambientFishController.SetBoundFishHooked(_demoHookTransform);
                _ambientFishController.SetBoundFishVisualFade(1f);
                _demoFishHookVisualReady = true;
            }
        }

        private void UpdateDemoHookedFishFade(float reelStartDepthMeters, float reelTargetDepthMeters, float reelSpeedMultiplier = 1f)
        {
            if (_ambientFishController == null)
            {
                return;
            }

            var targetDepth = Mathf.Max(0f, reelTargetDepthMeters);
            var minimumStartDepth = targetDepth + Mathf.Max(1f, _demoHookedFishFadeDelayMeters);
            var startDepth = Mathf.Max(minimumStartDepth, reelStartDepthMeters);
            var currentDepth = ResolveDemoHookDepthMeters();
            var reelSpeedMetersPerSecond = Mathf.Max(0.1f, _demoHookMoveSpeed * Mathf.Max(0.1f, reelSpeedMultiplier));
            var delayBySecondsMeters = reelSpeedMetersPerSecond * Mathf.Max(0f, _demoHookedFishFadeDelaySeconds);
            var fadeDelayMeters = Mathf.Clamp(
                Mathf.Max(_demoHookedFishFadeDelayMeters, delayBySecondsMeters),
                0f,
                startDepth);
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
            RestoreDemoHookDepthOverride();
            RestoreRuntimeHookCastController();
            ApplyScene8CameraFollowOverride(active: false);
            ApplyDemoCameraZoomOverride(active: false);
            ApplyTutorialLightPreview(enabled: false, Vector2.zero);
            ApplyTutorialDepthPreview(enabled: false, 0f);
            SetTutorialMessageBoxVisible(false);
            SetDemoHookVisible(false);
            _step = TutorialStep.MoveShip;
            UpdateSkipButtonVisibility();
            PushPrompt();
        }

        private void CleanupDemoState(bool resetHookToDock)
        {
            _demoActive = false;
            _demoPhase = DemoAutoplayPhase.None;
            ClearDemoSceneTransition(forceHideOverlay: true);
            ClearQueuedDemoPhaseTransition();
            ResolveDemoFish(caught: false);
            RestoreDemoHookDepthOverride();
            RestoreRuntimeHookCastController();
            ApplyScene8CameraFollowOverride(active: false);
            ApplyDemoCameraZoomOverride(active: false);
            ApplyTutorialLightPreview(enabled: false, Vector2.zero);
            ApplyTutorialDepthPreview(enabled: false, 0f);
            SetTutorialMessageBoxVisible(false);
            if (resetHookToDock)
            {
                SnapDemoHookToDock();
            }

            SetDemoHookVisible(false);
        }

        private void RefreshDemoAnchors()
        {
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

            if (_hookMovement != null && _demoShipTransform != null && _hookMovement.ShipTransform != _demoShipTransform)
            {
                _hookMovement.ConfigureShipTransform(_demoShipTransform);
            }

            if (_demoHookRenderer == null && _demoHookTransform != null)
            {
                _demoHookRenderer = _demoHookTransform.GetComponent<SpriteRenderer>();
            }
        }

        private void ApplyDemoHookDepthOverride()
        {
            if (_hookMovement == null || _demoHookDepthOverrideApplied)
            {
                return;
            }

            _demoHookDepthOverrideApplied = true;
            _demoHookPreviousMaxDepthMeters = Mathf.Max(0.5f, _hookMovement.MaxDepth);
            EnsureDemoHookDepthOverrideActive();
        }

        private bool EnsureDemoHookDepthOverrideActive()
        {
            if (_hookMovement == null || !_demoHookDepthOverrideApplied)
            {
                return false;
            }

            var overrideReapplied = false;
            var requiredMaxDepth = Mathf.Clamp(
                Mathf.Max(
                    Mathf.Max(_demoLevel4CastDepthMeters, _demoLevel5CastDepthMeters),
                    Mathf.Max(_demoLevel4DarknessPreviewDepthMeters, _demoLevel5DeepDarkPreviewDepthMeters)),
                0.5f,
                5000f);
            if (Mathf.Abs(_hookMovement.MaxDepth - requiredMaxDepth) > 0.05f)
            {
                _hookMovement.MaxDepth = requiredMaxDepth;
                overrideReapplied = true;
            }

            // Demo drives hook movement directly; disable player input/clamping side-effects.
            _hookMovement.SetMovementEnabled(false);
            return overrideReapplied;
        }

        private void RecoverDemoHookDepthAfterOverrideReset()
        {
            if (_demoHookTransform == null || _demoShipTransform == null)
            {
                return;
            }

            var recoveryDepth = ResolveDemoRecoveryDepthMeters(_demoPhase);
            if (recoveryDepth <= 0f)
            {
                return;
            }

            var currentDepth = ResolveDemoHookDepthMeters();
            if (currentDepth + 0.5f >= recoveryDepth)
            {
                return;
            }

            SnapDemoHookToDepth(recoveryDepth, clampToWorldBounds: false);
            ApplyTutorialDepthPreview(enabled: true, recoveryDepth);
        }

        private float ResolveDemoRecoveryDepthMeters(DemoAutoplayPhase phase)
        {
            switch (phase)
            {
                case DemoAutoplayPhase.Level4DarknessInfo:
                case DemoAutoplayPhase.Level4CastDrop:
                case DemoAutoplayPhase.Level4FishHook:
                case DemoAutoplayPhase.Level4ReelInfo:
                    return Mathf.Max(1f, _demoLevel4CastDepthMeters);
                case DemoAutoplayPhase.Level4ReelUp:
                    return Mathf.Max(
                        Mathf.Max(1f, _demoLevel4CastDepthMeters),
                        Mathf.Max(_demoLevel4ReelStartDepthMeters, _demoLevel4ReelTargetDepthMeters + 20f));
                case DemoAutoplayPhase.Level5DeepDarkInfo:
                case DemoAutoplayPhase.Level5CastDrop:
                case DemoAutoplayPhase.Level5FishHook:
                case DemoAutoplayPhase.Level5ReelInfo:
                    return Mathf.Max(1f, _demoLevel5CastDepthMeters);
                case DemoAutoplayPhase.Level5ReelUp:
                    return Mathf.Max(
                        Mathf.Max(1f, _demoLevel5CastDepthMeters),
                        Mathf.Max(_demoLevel5ReelStartDepthMeters, _demoLevel5ReelTargetDepthMeters + 20f));
                default:
                    return 0f;
            }
        }

        private float ResolveDeepReelSpeedMultiplier(float sceneReelSpeedMultiplier)
        {
            if (sceneReelSpeedMultiplier > 0.01f)
            {
                return Mathf.Max(0.1f, sceneReelSpeedMultiplier);
            }

            return Mathf.Max(0.1f, _demoDeepReelSpeedMultiplier);
        }

        private Vector2 ResolveScene8LightRadiiMeters()
        {
            // Scene 8 intentionally uses the larger illumination profile.
            return _demoLevel5LightRadiiMeters;
        }

        private Vector2 ResolveScene9LightRadiiMeters()
        {
            // Scene 9 intentionally uses the smaller illumination profile.
            return _demoLevel4LightRadiiMeters;
        }

        private float ResolveDemoReelTransitionLeadMeters(float reelSpeedMultiplier)
        {
            var reelSpeedMetersPerSecond = Mathf.Max(0.1f, _demoHookMoveSpeed * Mathf.Max(0.1f, reelSpeedMultiplier));
            var leadBySpeed = reelSpeedMetersPerSecond * Mathf.Max(0f, _demoReelTransitionLeadSeconds);
            return Mathf.Max(0.35f, leadBySpeed);
        }

        private void ApplyScene8CameraFollowOverride(bool active)
        {
            if (_fishingCameraController == null)
            {
                return;
            }

            var followLerpScale = active
                ? Mathf.Max(0.1f, _demoLevel4ReelCameraFollowLerpScale)
                : 1f;
            var hookViewportY = active
                ? Mathf.Clamp01(_demoLevel4ReelCameraHookViewportY)
                : -1f;
            _fishingCameraController.SetTutorialHookFollowOverride(active, followLerpScale, hookViewportY);
        }

        private void ApplyDemoCameraZoomOverride(bool active)
        {
            if (_fishingCameraController == null)
            {
                return;
            }

            var zoomScale = active
                ? Mathf.Max(1f, _demoCameraZoomOutScale)
                : 1f;
            _fishingCameraController.SetTutorialZoomOverride(active, zoomScale);
        }

        private void RestoreDemoHookDepthOverride()
        {
            if (_hookMovement == null || !_demoHookDepthOverrideApplied)
            {
                return;
            }

            _demoHookDepthOverrideApplied = false;
            _hookMovement.MaxDepth = Mathf.Clamp(_demoHookPreviousMaxDepthMeters, 0.5f, 5000f);
            _hookMovement.RefreshHookStats();
            _hookMovement.SetMovementEnabled(true);
            _demoHookPreviousMaxDepthMeters = 0f;
        }

        private bool MoveShipTowardX(float targetX)
        {
            if (_demoShipTransform == null)
            {
                return true;
            }

            var position = _demoShipTransform.position;
            // Demo ship should only sail left; never move right while autoplay is active.
            var constrainedTargetX = Mathf.Min(targetX, position.x);
            position.x = Mathf.MoveTowards(
                position.x,
                constrainedTargetX,
                Mathf.Max(0.1f, _demoShipMoveSpeed) * Time.deltaTime);
            _demoShipTransform.position = position;
            return Mathf.Abs(position.x - constrainedTargetX) <= 0.02f;
        }

        private void MoveShipDuringHookDemo()
        {
            var travelSpan = Mathf.Max(0.5f, _demoShipTravelDistance * 0.55f);
            var wave = Mathf.Sin((Time.unscaledTime - _demoPhaseStartedAt) * 2.8f);
            var targetX = _demoShipStartX + (travelSpan * wave);
            MoveShipTowardX(targetX);
        }

        private void TickShipUpgradeInfoVisual()
        {
            SetDemoHookVisible(true);
            var minimumDepth = Mathf.Max(1f, Mathf.Min(_demoShipUpgradePreviewDepthMinMeters, _demoShipUpgradePreviewDepthMaxMeters));
            var maximumDepth = Mathf.Max(minimumDepth + 1f, Mathf.Max(_demoShipUpgradePreviewDepthMinMeters, _demoShipUpgradePreviewDepthMaxMeters));
            var cycleSeconds = Mathf.Max(0.35f, _demoShipUpgradeDepthCycleSeconds);
            var elapsed = Mathf.Max(0f, Time.unscaledTime - _demoPhaseStartedAt);
            var blend = Mathf.PingPong(elapsed / cycleSeconds, 1f);
            blend = blend * blend * (3f - (2f * blend));
            var targetDepth = Mathf.Lerp(minimumDepth, maximumDepth, blend);
            MoveHookTowardDepth(targetDepth, clampToWorldBounds: false, _demoDeepCastSpeedMultiplier * 0.55f);
            ApplyTutorialLightPreview(enabled: false, Vector2.zero);
            ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
        }

        private void TickHookUpgradeInfoVisual()
        {
            SetDemoHookVisible(true);
            MoveHookTowardDepth(
                Mathf.Max(1f, _demoHookUpgradePreviewDepthMeters),
                clampToWorldBounds: false,
                _demoDeepCastSpeedMultiplier * 0.75f);
            var pulseSeconds = Mathf.Max(0.4f, _demoHookUpgradeLightPulseSeconds);
            var elapsed = Mathf.Max(0f, Time.unscaledTime - _demoPhaseStartedAt);
            var pulse = 0.5f + (0.5f * Mathf.Sin((elapsed / pulseSeconds) * Mathf.PI * 2f));
            var pulsedRadii = Vector2.Lerp(ResolveScene8LightRadiiMeters(), ResolveScene9LightRadiiMeters(), pulse);
            ApplyTutorialLightPreview(enabled: true, pulsedRadii);
            ApplyTutorialDepthPreview(enabled: true, ResolveDemoHookDepthMeters());
        }

        private void SailShipLeftDuringHookDemo()
        {
            if (_demoShipTransform == null)
            {
                return;
            }

            var elapsed = Mathf.Max(0f, Time.unscaledTime - _demoPhaseStartedAt);
            var travelBase = Mathf.Max(0.5f, _demoShipTravelDistance);
            var sailSpeed = Mathf.Max(0.1f, _demoShipMoveSpeed) * Mathf.Clamp(_demoHookPhaseSailSpeedScale, 0.1f, 1f);
            var targetX = _demoShipStartX - travelBase - (elapsed * sailSpeed);
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

        private void SnapDemoHookToDepth(float depthMeters, bool clampToWorldBounds)
        {
            if (_demoShipTransform == null || _demoHookTransform == null)
            {
                return;
            }

            var targetY = _demoShipTransform.position.y - Mathf.Max(0f, depthMeters);
            if (clampToWorldBounds && _hookMovement != null)
            {
                _hookMovement.GetWorldDepthBounds(out var minY, out var maxY);
                targetY = Mathf.Clamp(targetY, minY, maxY);
            }

            var position = _demoHookTransform.position;
            position.y = targetY;
            _demoHookTransform.position = position;
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

        private void RefreshPromptActions()
        {
            RefreshMoveShipAction();
            RefreshMoveHookAction();
        }

        private string ResolveMoveShipControlHint()
        {
            var keyboardLeft = ResolveKeyboardCompositeBinding("Fishing/MoveShip", "negative", "Left Arrow");
            var keyboardRight = ResolveKeyboardCompositeBinding("Fishing/MoveShip", "positive", "Right Arrow");
            var gamepad = ResolveGamepadBindingSet("Fishing/MoveShip", "Left Stick or D-Pad");
            return $"{keyboardLeft}/{keyboardRight} or {gamepad}";
        }

        private string ResolveMoveHookDownControlHint()
        {
            var keyboardDown = ResolveKeyboardCompositeBinding("Fishing/MoveHook", "negative", "Down Arrow");
            var gamepad = ResolveGamepadBindingSet("Fishing/MoveHook", "Right Stick or D-Pad");
            return $"{keyboardDown} or {gamepad} down";
        }

        private string ResolveMoveHookUpControlHint()
        {
            var keyboardUp = ResolveKeyboardCompositeBinding("Fishing/MoveHook", "positive", "Up Arrow");
            var gamepad = ResolveGamepadBindingSet("Fishing/MoveHook", "Right Stick or D-Pad");
            return $"{keyboardUp} or {gamepad} up";
        }

        private string ResolveKeyboardCompositeBinding(string actionPath, string compositePart, string fallback)
        {
            if (_inputRebindingService != null)
            {
                var fromService = _inputRebindingService.GetDisplayBindingForCompositePart(actionPath, compositePart, "Keyboard");
                if (!string.IsNullOrWhiteSpace(fromService))
                {
                    return fromService;
                }
            }

            var action = ResolvePromptAction(actionPath);
            if (action == null)
            {
                return fallback;
            }

            for (var i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (!binding.isPartOfComposite
                    || !string.Equals(binding.name, compositePart, StringComparison.OrdinalIgnoreCase)
                    || !IsBindingOnDevice(binding, "Keyboard"))
                {
                    continue;
                }

                var display = action.GetBindingDisplayString(i);
                if (!string.IsNullOrWhiteSpace(display))
                {
                    return display;
                }
            }

            return fallback;
        }

        private string ResolveGamepadBindingSet(string actionPath, string fallback)
        {
            if (_inputRebindingService != null)
            {
                var fromService = _inputRebindingService.GetDisplayBindingsForAction(actionPath, "Gamepad", " or ", 2);
                if (!string.IsNullOrWhiteSpace(fromService))
                {
                    return fromService;
                }
            }

            var action = ResolvePromptAction(actionPath);
            if (action == null)
            {
                return fallback;
            }

            var displayBindings = new List<string>(2);
            for (var i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite || !IsBindingOnDevice(binding, "Gamepad"))
                {
                    continue;
                }

                var display = action.GetBindingDisplayString(i);
                if (string.IsNullOrWhiteSpace(display) || ContainsDisplayIgnoreCase(displayBindings, display))
                {
                    continue;
                }

                displayBindings.Add(display);
                if (displayBindings.Count >= 2)
                {
                    break;
                }
            }

            return displayBindings.Count > 0 ? string.Join(" or ", displayBindings) : fallback;
        }

        private InputAction ResolvePromptAction(string actionPath)
        {
            RefreshPromptActions();
            return actionPath switch
            {
                "Fishing/MoveShip" => _moveShipAction,
                "Fishing/MoveHook" => _moveHookAction,
                _ => _inputMapController != null
                    ? _inputMapController.FindAction(actionPath)
                    : null
            };
        }

        private static bool IsBindingOnDevice(InputBinding binding, string requestedDevice)
        {
            var requested = string.IsNullOrWhiteSpace(requestedDevice)
                ? string.Empty
                : requestedDevice.Trim().Trim('<', '>');
            if (string.IsNullOrWhiteSpace(requested))
            {
                return true;
            }

            var path = string.IsNullOrWhiteSpace(binding.effectivePath)
                ? binding.path
                : binding.effectivePath;
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("<", StringComparison.Ordinal))
            {
                return false;
            }

            var layoutEnd = path.IndexOf('>');
            if (layoutEnd <= 1)
            {
                return false;
            }

            var actual = path.Substring(1, layoutEnd - 1);
            if (string.Equals(actual, requested, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return requested switch
            {
                "Gamepad" => actual.IndexOf("Gamepad", StringComparison.OrdinalIgnoreCase) >= 0,
                "Keyboard" => actual.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0,
                _ => false
            };
        }

        private static bool ContainsDisplayIgnoreCase(List<string> displayBindings, string candidate)
        {
            for (var i = 0; i < displayBindings.Count; i++)
            {
                if (string.Equals(displayBindings[i], candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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

        private bool TryInitializeDependencies(bool emitMissingDependencyError = false)
        {
            if (_dependenciesInitialized)
            {
                return true;
            }

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
            RuntimeServiceRegistry.Resolve(ref _inputRebindingService, this, warnIfMissing: false);

            _saveManager ??= GetComponent<SaveManager>();
            _orchestrator ??= GetComponent<GameFlowOrchestrator>();
            _stateMachine ??= GetComponent<FishingActionStateMachine>();
            _catchResolver ??= GetComponent<CatchResolver>();
            _shipMovement ??= GetComponent<ShipMovementController>();
            _hookMovement ??= GetComponent<HookMovementController>();
            _hookCastDropController ??= GetComponent<FishingHookCastDropController>();
            _depthDarknessController ??= GetComponent<FishingDepthDarknessController>();
            _ambientFishController ??= GetComponent<FishingAmbientFishSwimController>();
            _inputMapController ??= GetComponent<InputActionMapController>();
            _inputRebindingService ??= GetComponent<InputRebindingService>();
            _hudOverlay ??= _hudOverlayBehaviour as IFishingHudOverlay;

            if (_fishingCameraController == null)
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    _fishingCameraController = mainCamera.GetComponent<FishingCameraController>();
                }
            }

            RefreshDemoAnchors();
            if (!ValidateRequiredDependencies(out var missingDependencies))
            {
                if (emitMissingDependencyError && !_missingDependencyLogEmitted)
                {
                    _missingDependencyLogEmitted = true;
                    Debug.LogError(
                        $"FishingLoopTutorialController missing required dependencies: {missingDependencies}. Configure dependencies from scene composition before tutorial activation.");
                }

                _dependenciesInitialized = false;
                return false;
            }

            _missingDependencyLogEmitted = false;
            _dependenciesInitialized = true;
            return true;
        }

        private bool ValidateRequiredDependencies(out string missingDependencies)
        {
            var missing = new List<string>(12);
            if (_saveManager == null)
            {
                missing.Add(nameof(_saveManager));
            }

            if (_orchestrator == null)
            {
                missing.Add(nameof(_orchestrator));
            }

            if (_stateMachine == null)
            {
                missing.Add(nameof(_stateMachine));
            }

            if (_catchResolver == null)
            {
                missing.Add(nameof(_catchResolver));
            }

            if (_shipMovement == null)
            {
                missing.Add(nameof(_shipMovement));
            }

            if (_hookMovement == null)
            {
                missing.Add(nameof(_hookMovement));
            }

            if (_hookCastDropController == null)
            {
                missing.Add(nameof(_hookCastDropController));
            }

            if (_depthDarknessController == null)
            {
                missing.Add(nameof(_depthDarknessController));
            }

            if (_ambientFishController == null)
            {
                missing.Add(nameof(_ambientFishController));
            }

            if (_inputMapController == null)
            {
                missing.Add(nameof(_inputMapController));
            }

            if (_inputRebindingService == null)
            {
                missing.Add(nameof(_inputRebindingService));
            }

            if (_hudOverlay == null)
            {
                missing.Add(nameof(_hudOverlay));
            }

            missingDependencies = string.Join(", ", missing);
            return missing.Count == 0;
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
                if (_skipAllTutorialButton != null)
                {
                    _skipAllTutorialButton.gameObject.SetActive(_isActive);
                }

                return;
            }

            _skipTutorialButton.gameObject.SetActive(_isActive);
            var skipButtonLabel = _skipTutorialButton.GetComponentInChildren<TMP_Text>();
            if (skipButtonLabel != null)
            {
                skipButtonLabel.text = _demoActive ? _skipSceneButtonText : _skipTutorialButtonText;
            }

            if (_skipAllTutorialButton == null)
            {
                return;
            }

            _skipAllTutorialButton.gameObject.SetActive(_isActive);
            var skipAllLabel = _skipAllTutorialButton.GetComponentInChildren<TMP_Text>();
            if (skipAllLabel != null)
            {
                skipAllLabel.text = _skipAllButtonText;
            }
        }

        private bool TrySkipToNextDemoScene()
        {
            var anchorPhase = ResolveDemoSceneSkipAnchorPhase();
            if (anchorPhase == DemoAutoplayPhase.None)
            {
                return false;
            }

            var nextSceneStartPhase = ResolveNextDemoSceneStartPhase(anchorPhase);
            if (nextSceneStartPhase == DemoAutoplayPhase.None)
            {
                EndDemoSequence();
                return true;
            }

            ClearQueuedDemoPhaseTransition();
            if (_demoSceneTransitionActive)
            {
                ClearDemoSceneTransition(forceHideOverlay: true);
            }

            ResolveDemoFish(caught: false);
            StartDemoPhase(nextSceneStartPhase);
            _hudOverlay?.SetFishingStatus("Skipped to next tutorial scene.");
            return true;
        }

        private DemoAutoplayPhase ResolveDemoSceneSkipAnchorPhase()
        {
            if (_demoSceneTransitionActive && _demoPendingTransitionPhase != DemoAutoplayPhase.None)
            {
                return _demoPendingTransitionPhase;
            }

            // Anchor skip routing to the scene currently on screen. Using the queued
            // scene here can skip two scenes when the current scene is in its end pause.
            if (_demoPhase != DemoAutoplayPhase.None)
            {
                return _demoPhase;
            }

            return _demoQueuedNextPhase;
        }

        private static DemoAutoplayPhase ResolveNextDemoSceneStartPhase(DemoAutoplayPhase phase)
        {
            switch (ResolveDemoSceneStartPhase(phase))
            {
                case DemoAutoplayPhase.IntroInfo:
                    return DemoAutoplayPhase.MoveShipInfo;
                case DemoAutoplayPhase.MoveShipInfo:
                    return DemoAutoplayPhase.CastInfo;
                case DemoAutoplayPhase.CastInfo:
                    return DemoAutoplayPhase.FishHookInfo;
                case DemoAutoplayPhase.FishHookInfo:
                    return DemoAutoplayPhase.ReelInfo;
                case DemoAutoplayPhase.ReelInfo:
                    return DemoAutoplayPhase.ShipUpgradeInfo;
                case DemoAutoplayPhase.ShipUpgradeInfo:
                    return DemoAutoplayPhase.HookUpgradeInfo;
                case DemoAutoplayPhase.HookUpgradeInfo:
                    return DemoAutoplayPhase.Level4DarknessInfo;
                case DemoAutoplayPhase.Level4DarknessInfo:
                    return DemoAutoplayPhase.Level5DeepDarkInfo;
                case DemoAutoplayPhase.Level5DeepDarkInfo:
                    return DemoAutoplayPhase.FinishInfo;
                default:
                    return DemoAutoplayPhase.None;
            }
        }

        private static DemoAutoplayPhase ResolveDemoSceneStartPhase(DemoAutoplayPhase phase)
        {
            switch (phase)
            {
                case DemoAutoplayPhase.IntroInfo:
                    return DemoAutoplayPhase.IntroInfo;
                case DemoAutoplayPhase.MoveShipInfo:
                case DemoAutoplayPhase.SteerRight:
                case DemoAutoplayPhase.SteerLeft:
                    return DemoAutoplayPhase.MoveShipInfo;
                case DemoAutoplayPhase.CastInfo:
                case DemoAutoplayPhase.CastDrop:
                    return DemoAutoplayPhase.CastInfo;
                case DemoAutoplayPhase.FishHookInfo:
                case DemoAutoplayPhase.FishHook:
                    return DemoAutoplayPhase.FishHookInfo;
                case DemoAutoplayPhase.ReelInfo:
                case DemoAutoplayPhase.ReelUp:
                    return DemoAutoplayPhase.ReelInfo;
                case DemoAutoplayPhase.ShipUpgradeInfo:
                    return DemoAutoplayPhase.ShipUpgradeInfo;
                case DemoAutoplayPhase.HookUpgradeInfo:
                    return DemoAutoplayPhase.HookUpgradeInfo;
                case DemoAutoplayPhase.Level4DarknessInfo:
                case DemoAutoplayPhase.Level4CastDrop:
                case DemoAutoplayPhase.Level4FishHook:
                case DemoAutoplayPhase.Level4ReelInfo:
                case DemoAutoplayPhase.Level4ReelUp:
                    return DemoAutoplayPhase.Level4DarknessInfo;
                case DemoAutoplayPhase.Level5DeepDarkInfo:
                case DemoAutoplayPhase.Level5CastDrop:
                case DemoAutoplayPhase.Level5FishHook:
                case DemoAutoplayPhase.Level5ReelInfo:
                case DemoAutoplayPhase.Level5ReelUp:
                    return DemoAutoplayPhase.Level5DeepDarkInfo;
                case DemoAutoplayPhase.FinishInfo:
                case DemoAutoplayPhase.Finish:
                    return DemoAutoplayPhase.FinishInfo;
                default:
                    return DemoAutoplayPhase.None;
            }
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
