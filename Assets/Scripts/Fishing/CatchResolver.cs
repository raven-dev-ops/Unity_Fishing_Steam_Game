using RavenDevOps.Fishing.Audio;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Core.Logging;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class CatchResolver : MonoBehaviour
    {
        private const float FallbackShipMinDepthTier1 = 0f;
        private const float FallbackShipMinDepthTier2 = 0f;
        private const float FallbackShipMinDepthTier3 = 0f;
        private const float FallbackShipMinDepthTier4 = 1000f;
        private const float FallbackShipMinDepthTier5 = 3000f;
        private const float FallbackShipMaxDepthTier1 = 400f;
        private const float FallbackShipMaxDepthTier2 = 700f;
        private const float FallbackShipMaxDepthTier3 = 1600f;
        private const float FallbackShipMaxDepthTier4 = 3000f;
        private const float FallbackShipMaxDepthTier5 = 5000f;

        private const int FallbackCargoCapacityTier1 = 12;
        private const int FallbackCargoCapacityTier2 = 20;
        private const int FallbackCargoCapacityTier3 = 32;

        [SerializeField] private FishingActionStateMachine _stateMachine;
        [SerializeField] private FishSpawner _spawner;
        [SerializeField] private HookMovementController _hook;
        [SerializeField] private MonoBehaviour _hudOverlayBehaviour;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private InputRebindingService _inputRebindingService;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private MetaLoopRuntimeService _metaLoopService;
        [SerializeField] private FishingAmbientFishSwimController _ambientFishController;

        [SerializeField] private int _currentDistanceTier = 1;
        [SerializeField] private float _minimumFishSpawnDepth = 20f;
        [SerializeField] private bool _requireCollisionHook = true;
        [SerializeField] private Vector2 _hookStationaryAttractionDelayRangeSeconds = new Vector2(5f, 15f);
        [SerializeField] private float _hookStationaryMovementThreshold = 0.015f;
        [SerializeField] private float _hookCollisionRadius = 0.22f;
        [SerializeField] private float _haulCompletionDepthThreshold = 25f;
        [SerializeField] private float _boatArrivalDepthThreshold = 1.1f;
        [SerializeField] private float _hookReactionWindowSeconds = 1.3f;
        [SerializeField] private float _hookedDoubleTapWindowSeconds = 0.35f;
        [SerializeField] private float _levelOneReelPulseDurationSeconds = 0.2f;
        [SerializeField] private bool _enableReelStruggleEscape = true;
        [SerializeField] private bool _useFishTypeEscapeChance = true;
        [SerializeField, Range(0f, 1f)] private float _fishTypeEscapeChanceFloor = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _fishTypeEscapeChanceCeiling = 0.85f;
        [SerializeField] private float _reelEscapeDrainWhileReeling = 0.45f;
        [SerializeField] private float _reelEscapeDrainWhileIdle = 1f;
        [SerializeField] private float _minimumReelEscapeWindowSeconds = 6f;
        [SerializeField] private float _reelTensionBase = 0.22f;
        [SerializeField] private float _reelTensionAmplitude = 0.2f;
        [SerializeField] private float _reelTensionFrequency = 5.6f;
        [SerializeField] private bool _enableNoBitePity = true;
        [SerializeField] private int _noBitePityThresholdCasts = 2;
        [SerializeField] private float _pityBiteDelayScale = 0.5f;
        [SerializeField] private bool _enableAdaptiveHookWindowAssist = true;
        [SerializeField] private int _adaptiveHookWindowFailureThreshold = 2;
        [SerializeField] private float _adaptiveHookWindowBonusSeconds = 0.4f;
        [SerializeField] private int _assistCooldownCatches = 2;
        [SerializeField] private bool _autoAttachFishingCameraController = true;
        [SerializeField] private bool _autoAttachFishingTutorialController = true;
        [SerializeField] private bool _autoAttachEnvironmentSliceController = true;
        [SerializeField] private bool _autoAttachConditionController = true;

        [SerializeField] private AudioClip _castSfx;
        [SerializeField] private AudioClip _hookSfx;
        [SerializeField] private AudioClip _catchSfx;
        [SerializeField] private AudioClip _tensionWarningSfx;
        [SerializeField] private AudioClip _tensionCriticalSfx;
        [SerializeField] private AudioClip _lineSnapSfx;
        [SerializeField] private AudioClip _escapeSfx;
        [SerializeField] private AudioClip _missHookSfx;

        private readonly FishEncounterModel _encounterModel = new FishEncounterModel();
        private readonly FishingOutcomeDomainService _outcomeDomainService = new FishingOutcomeDomainService();
        private readonly FishingAssistService _assistService = new FishingAssistService();
        private IFishingHudOverlay _hudOverlay;

        private FishDefinition _targetFish;
        private FishDefinition _hookedFish;
        private float _biteTimerSeconds;
        private float _hookStationaryElapsedSeconds;
        private float _hookStationaryDelaySeconds;
        private Vector3 _lastHookPosition;
        private bool _hasRecordedHookPositionForStationaryCheck;
        private float _inWaterElapsedSeconds;
        private float _hookedElapsedSeconds;
        private float _hookDepthAtHookMeters;
        private bool _catchSucceeded;
        private FishingFailReason _pendingFailReason = FishingFailReason.None;
        private FishingTensionState _lastTensionState = FishingTensionState.None;
        private float _activeHookReactionWindowSeconds;
        private bool _cameraConfigured;
        private bool _tutorialConfigured;
        private bool _environmentConfigured;
        private bool _conditionConfigured;
        private bool _biteSelectionResolvedForCurrentDrop;
        private bool _biteApproachStarted;
        private bool _targetFishBoundToAmbient;
        private string _pendingCollisionFishId = string.Empty;
        private bool _hasPendingCollisionHookCandidate;
        private bool _resumeInWaterAfterFishEscaped;
        private string _resumeInWaterStatusOverride = string.Empty;
        private bool _haulCatchInProgress;
        private bool _catchSecuredAtThresholdDepth;
        private float _reelEscapeTimeRemaining;
        private float _lastHookedUpPressTime = -10f;
        private float _levelOneReelPulseTimeRemaining;
        private bool _upAxisHeldLastFrameForPress;
        private int _cachedUpPressFrame = -1;
        private bool _cachedUpPressResult;
        private string _cachedHudConditionBase = string.Empty;
        private string _cachedHudConditionFishId = string.Empty;
        private string _cachedHudConditionShipId = string.Empty;
        private string _cachedHudConditionHookId = string.Empty;
        private string _cachedHudConditionSummary = string.Empty;

        [NonSerialized] private IFishingRandomSource _randomSource;
        private InputAction _moveHookAction;
        private bool _stateMachineSubscribed;
        private ShipMovementController _shipMovement;
        private bool _dependenciesInitialized;
        private bool _dependencyErrorLogged;

        public sealed class DependencyBundle
        {
            public SaveManager SaveManager { get; set; }
            public AudioManager AudioManager { get; set; }
            public InputActionMapController InputMapController { get; set; }
            public InputRebindingService InputRebindingService { get; set; }
            public UserSettingsService SettingsService { get; set; }
            public MetaLoopRuntimeService MetaLoopService { get; set; }
            public FishingAmbientFishSwimController AmbientFishController { get; set; }
            public IFishingHudOverlay HudOverlay { get; set; }
            public ShipMovementController ShipMovement { get; set; }
            public FishingCameraController FishingCameraController { get; set; }
            public FishingLoopTutorialController TutorialController { get; set; }
            public FishingEnvironmentSliceController EnvironmentSliceController { get; set; }
            public FishingConditionController ConditionController { get; set; }
        }

        public event System.Action<bool, FishingFailReason, string> CatchResolved;

        public void ConfigureDependencies(DependencyBundle dependencies)
        {
            if (dependencies == null)
            {
                return;
            }

            _saveManager = dependencies.SaveManager ?? _saveManager;
            _audioManager = dependencies.AudioManager ?? _audioManager;
            _inputMapController = dependencies.InputMapController ?? _inputMapController;
            _inputRebindingService = dependencies.InputRebindingService ?? _inputRebindingService;
            _settingsService = dependencies.SettingsService ?? _settingsService;
            _metaLoopService = dependencies.MetaLoopService ?? _metaLoopService;
            _ambientFishController = dependencies.AmbientFishController ?? _ambientFishController;
            _shipMovement = dependencies.ShipMovement ?? _shipMovement;

            if (dependencies.HudOverlay != null)
            {
                _hudOverlay = dependencies.HudOverlay;
                if (dependencies.HudOverlay is MonoBehaviour hudOverlayBehaviour)
                {
                    _hudOverlayBehaviour = hudOverlayBehaviour;
                }
            }

            _dependenciesInitialized = false;
            _dependencyErrorLogged = false;
            TryInitializeDependencies(emitError: true);
            ApplyAutoSetupIfEnabled(
                dependencies.FishingCameraController,
                dependencies.TutorialController,
                dependencies.EnvironmentSliceController,
                dependencies.ConditionController);
        }

        public void Configure(
            FishingActionStateMachine stateMachine,
            FishSpawner spawner,
            HookMovementController hook,
            MonoBehaviour hudOverlayBehaviour)
        {
            if (_stateMachine != stateMachine)
            {
                UnsubscribeFromStateMachine();
            }

            _stateMachine = stateMachine;
            SubscribeToStateMachine();
            _spawner = spawner;
            _hook = hook;
            _hudOverlayBehaviour = hudOverlayBehaviour;
            _hudOverlay = hudOverlayBehaviour as IFishingHudOverlay;
            _dependenciesInitialized = false;
            _dependencyErrorLogged = false;
            TryInitializeDependencies();
            ApplyAutoSetupIfEnabled();
        }

        internal void ConfigureReelInputEvaluationForTests(SaveManager saveManager, float levelOneReelPulseDurationSeconds)
        {
            _saveManager = saveManager;
            _levelOneReelPulseDurationSeconds = Mathf.Max(0.01f, levelOneReelPulseDurationSeconds);
            _dependenciesInitialized = false;
            _dependencyErrorLogged = false;
            TryInitializeDependencies();
        }

        internal void PrimeUpPressForTests(bool pressedThisFrame)
        {
            _cachedUpPressFrame = Time.frameCount;
            _cachedUpPressResult = pressedThisFrame;
        }

        internal bool IsReelEffortActiveForTests()
        {
            return IsReelEffortActive();
        }

        internal bool ShouldStartReelFromHookedInputForTests()
        {
            return ShouldStartReelFromHookedInput();
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputRebindingService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _metaLoopService, this, warnIfMissing: false);
            _randomSource ??= new UnityFishingRandomSource();
            _hudOverlay = _hudOverlayBehaviour as IFishingHudOverlay;
            ConfigureAssistService();
            _dependenciesInitialized = false;
            _dependencyErrorLogged = false;
            TryInitializeDependencies();
        }

        private void OnValidate()
        {
            ConfigureAssistService();
        }

        private void EnsureAmbientFishControllerReference()
        {
            if (_ambientFishController != null)
            {
                return;
            }

            _ambientFishController = GetComponent<FishingAmbientFishSwimController>();
            if (_ambientFishController != null)
            {
                return;
            }
        }

        private void OnEnable()
        {
            SubscribeToStateMachine();
            TryInitializeDependencies();
        }

        private void OnDisable()
        {
            UnsubscribeFromStateMachine();
        }

        private bool TryInitializeDependencies(bool emitError = false)
        {
            if (_dependenciesInitialized)
            {
                return true;
            }

            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputRebindingService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _metaLoopService, this, warnIfMissing: false);

            EnsureAmbientFishControllerReference();
            ResolveShipMovementReference();
            _hudOverlay ??= _hudOverlayBehaviour as IFishingHudOverlay;
            RefreshInputActions();

            if (!ValidateRequiredDependencies(out var missingDependencies))
            {
                if (emitError && !_dependencyErrorLogged)
                {
                    _dependencyErrorLogged = true;
                    Debug.LogError(
                        $"CatchResolver missing required dependencies: {missingDependencies}. Configure dependencies from scene composition before gameplay update.");
                }

                _dependenciesInitialized = false;
                return false;
            }

            _dependencyErrorLogged = false;
            _dependenciesInitialized = true;
            return true;
        }

        private bool ValidateRequiredDependencies(out string missingDependencies)
        {
            var missing = new System.Collections.Generic.List<string>(2);
            if (_stateMachine == null)
            {
                missing.Add(nameof(_stateMachine));
            }

            if (_hook == null)
            {
                missing.Add(nameof(_hook));
            }

            missingDependencies = string.Join(", ", missing);
            return missing.Count == 0;
        }

        private void ResolveShipMovementReference()
        {
            if (_shipMovement != null || _hook == null)
            {
                return;
            }

            var shipTransform = _hook.ShipTransform;
            if (shipTransform != null)
            {
                _shipMovement = shipTransform.GetComponent<ShipMovementController>();
            }

            if (_shipMovement == null)
            {
                RuntimeServiceRegistry.TryGet(out _shipMovement);
            }
        }

        private void ApplyAutoSetupIfEnabled(
            FishingCameraController cameraController = null,
            FishingLoopTutorialController tutorialController = null,
            FishingEnvironmentSliceController environmentController = null,
            FishingConditionController conditionController = null)
        {
            if (_autoAttachFishingCameraController && !_cameraConfigured)
            {
                if (cameraController != null && _hook != null)
                {
                    cameraController.Configure(_hook.ShipTransform, _hook.transform);
                    _cameraConfigured = true;
                }
                else
                {
                    EnsureCameraController();
                }
            }

            if (_autoAttachFishingTutorialController && !_tutorialConfigured)
            {
                if (tutorialController != null)
                {
                    _tutorialConfigured = true;
                }
                else
                {
                    EnsureTutorialController();
                }
            }

            if (_autoAttachEnvironmentSliceController && !_environmentConfigured)
            {
                if (environmentController != null)
                {
                    if (_hook != null)
                    {
                        environmentController.Configure(_hook.ShipTransform, _hook.transform);
                    }

                    _environmentConfigured = true;
                }
                else
                {
                    EnsureEnvironmentController();
                }
            }

            if (_autoAttachConditionController && !_conditionConfigured)
            {
                if (conditionController != null && _spawner != null)
                {
                    _spawner.SetConditionController(conditionController);
                    _conditionConfigured = true;
                }
                else
                {
                    EnsureConditionController();
                }
            }
        }

        private void Update()
        {
            if (!_dependenciesInitialized)
            {
                return;
            }

            RefreshDistanceTier();
            UpdateHudTelemetry();

            if (_stateMachine == null)
            {
                return;
            }

            switch (_stateMachine.State)
            {
                case FishingActionState.InWater:
                    TickInWater();
                    break;
                case FishingActionState.Hooked:
                    TickHookedWindow();
                    break;
                case FishingActionState.Reel:
                    TickReelFight();
                    break;
            }
        }

        private void OnStateChanged(FishingActionState previous, FishingActionState next)
        {
            if (!TryInitializeDependencies(emitError: true))
            {
                return;
            }

            switch (next)
            {
                case FishingActionState.InWater:
                    if (_resumeInWaterAfterFishEscaped)
                    {
                        var resumeStatusOverride = _resumeInWaterStatusOverride;
                        _resumeInWaterAfterFishEscaped = false;
                        _resumeInWaterStatusOverride = string.Empty;
                        BeginCastPhase(
                            playCastSfx: false,
                            recoveredFromEscapedFish: true,
                            recoveredStatusOverride: resumeStatusOverride);
                    }
                    else
                    {
                        BeginCastPhase();
                    }

                    break;
                case FishingActionState.Hooked:
                    BeginHookedPhase();
                    break;
                case FishingActionState.Reel:
                    BeginReelPhase();
                    break;
                case FishingActionState.Resolve:
                    ResolveOutcome();
                    break;
            }
        }

        private void BeginCastPhase(
            bool playCastSfx = true,
            bool recoveredFromEscapedFish = false,
            string recoveredStatusOverride = null)
        {
            if (playCastSfx)
            {
                _audioManager?.PlaySfx(_castSfx);
            }

            _encounterModel.End();

            _targetFish = null;
            _hookedFish = null;
            _haulCatchInProgress = false;
            _catchSecuredAtThresholdDepth = false;
            _catchSucceeded = false;
            _pendingFailReason = FishingFailReason.None;
            _inWaterElapsedSeconds = 0f;
            _hookedElapsedSeconds = 0f;
            _hookDepthAtHookMeters = 0f;
            _biteTimerSeconds = 0f;
            _lastTensionState = FishingTensionState.None;
            _activeHookReactionWindowSeconds = _hookReactionWindowSeconds;
            _reelEscapeTimeRemaining = 0f;
            _biteSelectionResolvedForCurrentDrop = false;
            _biteApproachStarted = false;
            _targetFishBoundToAmbient = false;
            _pendingCollisionFishId = string.Empty;
            _hasPendingCollisionHookCandidate = false;
            ClearHudConditionCache();
            ResetHookStationaryAttractionTimer(reseedDelay: true);
            _ambientFishController?.ResolveBoundFish(caught: false);

            _hudOverlay?.SetFishingFailure(string.Empty);
            _hudOverlay?.SetFishingTension(0f, FishingTensionState.None);

            if (_spawner == null || _hook == null)
            {
                _hudOverlay?.SetFishingStatus("Missing fishing dependencies.");
                return;
            }

            RefreshDistanceTier();
            if (IsCargoFull(out var cargoFishCount, out var cargoCapacity))
            {
                _hudOverlay?.SetFishingStatus($"Cargo full ({cargoFishCount}/{cargoCapacity}). Return to harbor and sell fish.");
                return;
            }

            if (recoveredFromEscapedFish)
            {
                if (!string.IsNullOrWhiteSpace(recoveredStatusOverride))
                {
                    _hudOverlay?.SetFishingStatus(recoveredStatusOverride);
                }
                else
                {
                    _hudOverlay?.SetFishingStatus("Fish escaped. Keep the hook down and collide with another fish.");
                }
            }
            else
            {
                var secureDepth = Mathf.Max(0.1f, _haulCompletionDepthThreshold);
                _hudOverlay?.SetFishingStatus(
                    $"Ship auto-sails left. Use hook controls: lower to search, hook fish on collision, then reel to {secureDepth:0}m to secure and haul.");
            }
        }

        private void BeginHookedPhase()
        {
            if (_targetFish == null)
            {
                EnsureTargetFishForCollisionHook(
                    _pendingCollisionFishId,
                    _hasPendingCollisionHookCandidate,
                    overwriteExistingTarget: false);
                if (_targetFish == null)
                {
                    _pendingFailReason = FishingFailReason.MissedHook;
                    _stateMachine?.SetResolve();
                    return;
                }
            }

            _hookedFish = _targetFish;
            _pendingCollisionFishId = _hookedFish != null ? _hookedFish.id : string.Empty;
            _hasPendingCollisionHookCandidate = false;
            _hookedElapsedSeconds = 0f;
            _hookDepthAtHookMeters = _hook != null ? Mathf.Max(0f, _hook.CurrentDepth) : 0f;
            _lastHookedUpPressTime = -10f;
            _levelOneReelPulseTimeRemaining = 0f;
            _upAxisHeldLastFrameForPress = false;
            _biteApproachStarted = false;
            _ambientFishController?.SetBoundFishHooked(_hook != null ? _hook.transform : null);
            _activeHookReactionWindowSeconds = _assistService.ResolveHookWindow(_hookReactionWindowSeconds, out var adaptiveWindowActivated);
            _audioManager?.PlaySfx(_hookSfx);
            if (adaptiveWindowActivated)
            {
                StructuredLogService.LogInfo(
                    "fishing-assist",
                    $"assist=adaptive_hook_window status=activated base_window={_hookReactionWindowSeconds:0.00} resolved_window={_activeHookReactionWindowSeconds:0.00}");
                _hudOverlay?.SetFishingStatus($"Fish hooked! Assist active: extra hook window. {ResolveHookedPrompt()}");
            }
            else
            {
                _hudOverlay?.SetFishingStatus($"Fish hooked! {ResolveHookedPrompt()}");
            }

            _hudOverlay?.SetFishingFailure(string.Empty);
            _hudOverlay?.SetFishingTension(0.2f, FishingTensionState.Safe);
            _lastTensionState = FishingTensionState.Safe;
        }

        private void BeginReelPhase()
        {
            if (_hookedFish == null)
            {
                _pendingFailReason = _outcomeDomainService.ResolveHookWindowFailure(
                    hasHookedFish: false,
                    elapsedSeconds: _hookedElapsedSeconds,
                    reactionWindowSeconds: _activeHookReactionWindowSeconds);
                _stateMachine?.SetResolve();
                return;
            }

            _haulCatchInProgress = true;
            _catchSecuredAtThresholdDepth = false;
            _catchSucceeded = false;
            _pendingFailReason = FishingFailReason.None;
            if (ResolveHookReelInputMode() == HookReelInputMode.Level1Tap)
            {
                _levelOneReelPulseTimeRemaining = Mathf.Max(0.05f, _levelOneReelPulseDurationSeconds);
            }

            _reelEscapeTimeRemaining = ResolveInitialReelEscapeTimer(_hookedFish);
            _encounterModel.Begin(_hookedFish, Mathf.Clamp01(_reelTensionBase));
            var reelInstruction = ResolveReelInstruction();
            _hudOverlay?.SetFishingStatus(reelInstruction);
            _hudOverlay?.SetFishingFailure(string.Empty);
            _hudOverlay?.SetFishingTension(0.12f, FishingTensionState.Safe);
            _lastTensionState = FishingTensionState.Safe;
        }

        private void TickInWater()
        {
            if (IsCargoFull(out var cargoFishCount, out var cargoCapacity))
            {
                _hudOverlay?.SetFishingStatus($"Cargo full ({cargoFishCount}/{cargoCapacity}). Return to harbor and sell fish.");
                return;
            }

            if (IsActionHeldForCastDepth())
            {
                if (_targetFish != null)
                {
                    _ambientFishController?.ResolveBoundFish(caught: false);
                    _targetFish = null;
                }

                _biteSelectionResolvedForCurrentDrop = false;
                _biteApproachStarted = false;
                _targetFishBoundToAmbient = false;
                _inWaterElapsedSeconds = 0f;
                ResetHookStationaryAttractionTimer(reseedDelay: true);
                _hudOverlay?.SetFishingStatus($"Adjusting hook depth. Use {ResolveCastDepthControlPrompt()} to lower deeper.");
                return;
            }

            if (!_biteSelectionResolvedForCurrentDrop)
            {
                _biteSelectionResolvedForCurrentDrop = true;
                if (!TryResolveTargetFishForCurrentDepth())
                {
                    // Even without a rolled target fish (for example, empty runtime catalogs),
                    // collisions should still be able to hook ambient fish.
                    if (TryHookTargetFishOnCollision())
                    {
                        _stateMachine?.SetHooked();
                    }

                    return;
                }
            }

            if (_targetFish == null)
            {
                // Allow direct collision hooks even when fish definitions are unavailable.
                if (TryHookTargetFishOnCollision())
                {
                    _stateMachine?.SetHooked();
                }

                return;
            }

            // Collision hooks should always take priority while the hook is in the water,
            // even when a timed bite path is active.
            if (TryHookTargetFishOnCollision())
            {
                _stateMachine?.SetHooked();
                return;
            }

            if (_requireCollisionHook)
            {
                if (!_targetFishBoundToAmbient)
                {
                    BindAmbientFishToTarget();
                }

                return;
            }

            var baitAttractionEnabled = IsBaitAttractionEnabled() && _targetFishBoundToAmbient;
            if (baitAttractionEnabled)
            {
                UpdateHookStationaryAttractionTimer();
                _inWaterElapsedSeconds += Time.deltaTime;
                if (_inWaterElapsedSeconds >= _biteTimerSeconds && HasSatisfiedHookStationaryAttractionDelay())
                {
                    if (!_biteApproachStarted)
                    {
                        _biteApproachStarted = true;
                        if (TryBeginBiteApproach())
                        {
                            _hudOverlay?.SetFishingStatus("Bait attracts a fish toward the hook...");
                        }
                    }
                }
            }
            else if (!_targetFishBoundToAmbient)
            {
                // Fallback path for tests or scenes without ambient fish visuals.
                UpdateHookStationaryAttractionTimer();
                _inWaterElapsedSeconds += Time.deltaTime;
                if (_inWaterElapsedSeconds < _biteTimerSeconds)
                {
                    return;
                }

                if (!HasSatisfiedHookStationaryAttractionDelay())
                {
                    return;
                }

                if (!_biteApproachStarted)
                {
                    _biteApproachStarted = true;
                    if (TryBeginBiteApproach())
                    {
                        _hudOverlay?.SetFishingStatus("A fish is circling the hook...");
                        return;
                    }
                }

                if (!IsBiteApproachComplete())
                {
                    return;
                }
            }

            if (!_targetFishBoundToAmbient)
            {
                _stateMachine?.SetHooked();
            }
        }

        private void TickHookedWindow()
        {
            if (ShouldStartReelFromHookedInput())
            {
                _stateMachine?.AdvanceByAction();
                return;
            }

            _hookedElapsedSeconds += Time.deltaTime;
            var failReason = _outcomeDomainService.ResolveHookWindowFailure(
                hasHookedFish: _hookedFish != null,
                elapsedSeconds: _hookedElapsedSeconds,
                reactionWindowSeconds: _activeHookReactionWindowSeconds);
            if (failReason != FishingFailReason.None)
            {
                _pendingFailReason = failReason;
                _stateMachine?.SetResolve();
            }
        }

        private void TickReelFight()
        {
            if (!_haulCatchInProgress || _hookedFish == null)
            {
                _pendingFailReason = FishingFailReason.MissedHook;
                _catchSucceeded = false;
                _stateMachine?.SetResolve();
                return;
            }

            var isReeling = IsReelEffortActive();

            if (!_catchSecuredAtThresholdDepth)
            {
                UpdateReelEscapeTimer(isReeling);
                var hasActiveEscapeRisk = _enableReelStruggleEscape && !float.IsInfinity(_reelEscapeTimeRemaining);
                if (hasActiveEscapeRisk && _reelEscapeTimeRemaining <= 0f)
                {
                    _pendingFailReason = FishingFailReason.FishEscaped;
                    _catchSucceeded = false;
                    _stateMachine?.SetResolve();
                    return;
                }

                var remainingDepth = _hook != null ? Mathf.Max(0f, _hook.CurrentDepth) : 0f;
                var catchDepthThreshold = Mathf.Max(0.1f, _haulCompletionDepthThreshold);
                var remainingDepthUntilSecured = Mathf.Max(0f, remainingDepth - catchDepthThreshold);
                var tension = ResolveReelTension(isReeling);
                var tensionState = FishEncounterModel.ResolveTensionState(tension);
                if (_lastTensionState != tensionState)
                {
                    if (tensionState == FishingTensionState.Warning)
                    {
                        _audioManager?.PlaySfx(_tensionWarningSfx);
                    }
                    else if (tensionState == FishingTensionState.Critical)
                    {
                        _audioManager?.PlaySfx(_tensionCriticalSfx);
                    }

                    _lastTensionState = tensionState;
                }

                _hudOverlay?.SetFishingTension(tension, tensionState);
                var escapeStatus = hasActiveEscapeRisk
                    ? $"Escape in {Mathf.Max(0f, _reelEscapeTimeRemaining):0.0}s."
                    : "Escape risk low.";
                if (!isReeling && !IsReelToggleModeEnabled())
                {
                    _hudOverlay?.SetFishingStatus(
                        $"Fish struggling... {ResolveReelFailPrompt()} {escapeStatus}");
                }
                else
                {
                    _hudOverlay?.SetFishingStatus(
                        $"Reeling catch... {remainingDepthUntilSecured:0.0}m until secured | {escapeStatus}");
                }

                if (IsHookAtCatchSecureDepth())
                {
                    _catchSecuredAtThresholdDepth = true;
                    _reelEscapeTimeRemaining = float.PositiveInfinity;
                    _ambientFishController?.SetBoundFishSettled(_hook != null ? _hook.transform : null);
                    _lastTensionState = FishingTensionState.Safe;
                    _hudOverlay?.SetFishingTension(0.08f, FishingTensionState.Safe);
                    _hudOverlay?.SetFishingStatus(
                        $"Catch secured at {Mathf.Max(0.1f, _haulCompletionDepthThreshold):0}m. Keep reeling to bring it aboard.");
                }
            }
            else
            {
                var currentDepth = _hook != null ? Mathf.Max(0f, _hook.CurrentDepth) : 0f;
                _lastTensionState = FishingTensionState.Safe;
                _hudOverlay?.SetFishingTension(0.06f, FishingTensionState.Safe);
                _hudOverlay?.SetFishingStatus(
                    $"Catch secured at {Mathf.Max(0.1f, _haulCompletionDepthThreshold):0}m. Reeling to boat... current depth {currentDepth:0.0}m");
            }

            if (_catchSecuredAtThresholdDepth && IsHookAtBoat())
            {
                _haulCatchInProgress = false;
                _catchSucceeded = true;
                _pendingFailReason = FishingFailReason.None;
                _stateMachine?.SetResolve();
            }
        }

        private void ResolveOutcome()
        {
            var resolvedFishId = _hookedFish != null ? _hookedFish.id : string.Empty;
            var resolvedFailReason = _catchSucceeded ? FishingFailReason.None : _pendingFailReason;
            var shouldResumeInWaterAfterOutcome = !_catchSucceeded
                && (resolvedFailReason == FishingFailReason.FishEscaped
                    || resolvedFailReason == FishingFailReason.LineSnap);

            if (_catchSucceeded && _hookedFish != null)
            {
                var reward = _outcomeDomainService.BuildCatchReward(_hookedFish, _randomSource);
                var weightKg = reward.WeightKg;
                var valueCopecs = reward.ValueCopecs;

                _saveManager?.RecordCatch(
                    _hookedFish.id,
                    _currentDistanceTier,
                    weightKg,
                    valueCopecs,
                    depthMeters: _hookDepthAtHookMeters);
                _audioManager?.PlaySfx(_catchSfx);
                _hudOverlay?.SetFishingFailure(string.Empty);
                if (IsCargoFull(out var cargoFishCount, out var cargoCapacity))
                {
                    _hudOverlay?.SetFishingStatus(
                        $"Caught {_hookedFish.id} ({weightKg:0.0}kg, {valueCopecs} copecs). Cargo full ({cargoFishCount}/{cargoCapacity}) - return to harbor to sell.");
                }
                else
                {
                    _hudOverlay?.SetFishingStatus($"Caught {_hookedFish.id} ({weightKg:0.0}kg, {valueCopecs} copecs).");
                }
            }
            else
            {
                var reason = _outcomeDomainService.BuildFailureReasonText(_pendingFailReason);
                _hudOverlay?.SetFishingFailure(reason);
                if (_pendingFailReason == FishingFailReason.FishEscaped)
                {
                    _hudOverlay?.SetFishingStatus("Fish slipped away. Keep fishing at this depth.");
                }
                else
                {
                    _hudOverlay?.SetFishingStatus($"Use {ResolveCastDepthControlPrompt()} to lower and continue fishing.");
                }

                _saveManager?.RecordCatchFailure(_hookedFish != null ? _hookedFish.id : string.Empty, _currentDistanceTier, reason);
                PlayFailureSfx(_pendingFailReason);
            }

            InvokeCatchResolved(_catchSucceeded, resolvedFailReason, resolvedFishId);
            _assistService.RecordCatchOutcome(_catchSucceeded);
            _ambientFishController?.ResolveBoundFish(_catchSucceeded);

            _encounterModel.End();
            _targetFish = null;
            _hookedFish = null;
            _pendingCollisionFishId = string.Empty;
            _hasPendingCollisionHookCandidate = false;
            _haulCatchInProgress = false;
            _catchSecuredAtThresholdDepth = false;
            _catchSucceeded = false;
            _hookDepthAtHookMeters = 0f;
            _pendingFailReason = FishingFailReason.None;
            _lastTensionState = FishingTensionState.None;
            _reelEscapeTimeRemaining = 0f;
            _biteApproachStarted = false;
            _targetFishBoundToAmbient = false;
            if (shouldResumeInWaterAfterOutcome)
            {
                _resumeInWaterAfterFishEscaped = true;
                _resumeInWaterStatusOverride = resolvedFailReason == FishingFailReason.LineSnap
                    ? "Line snapped. Keep the hook down and collide with another fish."
                    : "Fish escaped. Keep the hook down and collide with another fish.";
                _stateMachine?.SetInWater();
            }
            else
            {
                _resumeInWaterAfterFishEscaped = false;
                _resumeInWaterStatusOverride = string.Empty;
                _stateMachine?.ResetToCast();
            }
        }

        private void InvokeCatchResolved(bool success, FishingFailReason failReason, string fishId)
        {
            try
            {
                CatchResolved?.Invoke(success, failReason, fishId ?? string.Empty);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CatchResolver: CatchResolved listener failed ({ex.Message}).");
            }
        }

        private void PlayFailureSfx(FishingFailReason failReason)
        {
            switch (failReason)
            {
                case FishingFailReason.MissedHook:
                    _audioManager?.PlaySfx(_missHookSfx);
                    break;
                case FishingFailReason.LineSnap:
                    _audioManager?.PlaySfx(_lineSnapSfx);
                    break;
                case FishingFailReason.FishEscaped:
                    _audioManager?.PlaySfx(_escapeSfx);
                    break;
            }
        }

        private void UpdateHudTelemetry()
        {
            if (_hudOverlay == null || _hook == null)
            {
                return;
            }

            _hudOverlay.SetFishingTelemetry(_currentDistanceTier, _hook.CurrentDepth);
            if (_spawner != null)
            {
                var conditionSummary = _spawner.GetActiveConditionSummary() ?? string.Empty;
                _hudOverlay.SetFishingConditions(BuildHudConditionSummary(conditionSummary));
            }
        }

        private void ClearHudConditionCache()
        {
            _cachedHudConditionBase = string.Empty;
            _cachedHudConditionFishId = string.Empty;
            _cachedHudConditionShipId = string.Empty;
            _cachedHudConditionHookId = string.Empty;
            _cachedHudConditionSummary = string.Empty;
        }

        private string BuildHudConditionSummary(string baseConditionSummary)
        {
            var resolvedBaseSummary = baseConditionSummary ?? string.Empty;
            var activeFishId = _hookedFish != null ? _hookedFish.id : (_targetFish != null ? _targetFish.id : string.Empty);
            var hasActiveFishId = !string.IsNullOrWhiteSpace(activeFishId);
            var shipId = _saveManager != null && _saveManager.Current != null ? (_saveManager.Current.equippedShipId ?? string.Empty) : string.Empty;
            var hookId = _saveManager != null && _saveManager.Current != null ? (_saveManager.Current.equippedHookId ?? string.Empty) : string.Empty;

            if (_cachedHudConditionBase == resolvedBaseSummary
                && _cachedHudConditionFishId == activeFishId
                && _cachedHudConditionShipId == shipId
                && _cachedHudConditionHookId == hookId)
            {
                return _cachedHudConditionSummary;
            }

            var resolvedSummary = resolvedBaseSummary;
            if (_metaLoopService != null && hasActiveFishId)
            {
                var modifierLabel = _metaLoopService.BuildModifierLabel(activeFishId, shipId, hookId);
                if (!string.IsNullOrWhiteSpace(modifierLabel))
                {
                    resolvedSummary = string.IsNullOrWhiteSpace(resolvedBaseSummary)
                        ? modifierLabel
                        : $"{resolvedBaseSummary} | {modifierLabel}";
                }
            }

            _cachedHudConditionBase = resolvedBaseSummary;
            _cachedHudConditionFishId = activeFishId;
            _cachedHudConditionShipId = shipId;
            _cachedHudConditionHookId = hookId;
            _cachedHudConditionSummary = resolvedSummary ?? string.Empty;
            return _cachedHudConditionSummary;
        }

        private void RefreshInputActions()
        {
            if (_moveHookAction != null)
            {
                return;
            }

            _moveHookAction = _inputMapController != null
                ? _inputMapController.FindAction("Fishing/MoveHook")
                : null;
        }

        private void RefreshDistanceTier()
        {
            if (_hook == null)
            {
                _currentDistanceTier = 1;
                return;
            }

            if (_shipMovement == null)
            {
                var shipTransform = _hook.ShipTransform;
                if (shipTransform != null)
                {
                    _shipMovement = shipTransform.GetComponent<ShipMovementController>();
                }
            }

            var resolvedTier = _shipMovement != null
                ? _shipMovement.CurrentDistanceTier
                : 1;
            _currentDistanceTier = Mathf.Max(1, resolvedTier);
            _hook.SetDistanceTier(_currentDistanceTier);
        }

        private void EnsureCameraController()
        {
            if (_cameraConfigured || !_autoAttachFishingCameraController)
            {
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera == null || _hook == null)
            {
                return;
            }

            var controller = mainCamera.GetComponent<FishingCameraController>();
            if (controller == null)
            {
                controller = mainCamera.gameObject.AddComponent<FishingCameraController>();
            }

            var ship = _hook.ShipTransform;
            controller.Configure(ship, _hook.transform);
            _cameraConfigured = true;
        }

        private void EnsureTutorialController()
        {
            if (_tutorialConfigured || !_autoAttachFishingTutorialController)
            {
                return;
            }

            var tutorial = GetComponent<FishingLoopTutorialController>();
            if (tutorial == null)
            {
                tutorial = gameObject.AddComponent<FishingLoopTutorialController>();
            }

            _tutorialConfigured = tutorial != null;
        }

        private void EnsureEnvironmentController()
        {
            if (_environmentConfigured || !_autoAttachEnvironmentSliceController)
            {
                return;
            }

            var controller = GetComponent<FishingEnvironmentSliceController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<FishingEnvironmentSliceController>();
            }

            if (controller != null && _hook != null)
            {
                controller.Configure(_hook.ShipTransform, _hook.transform);
            }

            _environmentConfigured = controller != null;
        }

        private void EnsureConditionController()
        {
            if (_conditionConfigured || !_autoAttachConditionController || _spawner == null)
            {
                return;
            }

            var controller = GetComponent<FishingConditionController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<FishingConditionController>();
            }

            _spawner.SetConditionController(controller);
            _conditionConfigured = controller != null;
        }

        private void ConfigureAssistService()
        {
            _assistService.Configure(new FishingAssistSettings
            {
                EnableNoBitePity = _enableNoBitePity,
                NoBitePityThresholdCasts = Mathf.Max(1, _noBitePityThresholdCasts),
                PityBiteDelayScale = Mathf.Clamp(_pityBiteDelayScale, 0.25f, 1f),
                EnableAdaptiveHookWindow = _enableAdaptiveHookWindowAssist,
                AdaptiveFailureThreshold = Mathf.Max(1, _adaptiveHookWindowFailureThreshold),
                AdaptiveHookWindowBonusSeconds = Mathf.Clamp(_adaptiveHookWindowBonusSeconds, 0f, 0.75f),
                AssistCooldownCatches = Mathf.Max(0, _assistCooldownCatches)
            });
        }

        private bool TryResolveTargetFishForCurrentDepth()
        {
            if (_spawner == null || _hook == null)
            {
                _hudOverlay?.SetFishingStatus("Missing fishing dependencies.");
                return false;
            }

            if (IsCargoFull(out var cargoFishCount, out var cargoCapacity))
            {
                _hudOverlay?.SetFishingStatus($"Cargo full ({cargoFishCount}/{cargoCapacity}). Return to harbor and sell fish.");
                return false;
            }

            RefreshDistanceTier();
            var depth = Mathf.Max(0f, _hook.CurrentDepth);
            if (!IsDepthWithinShipOperationalBand(depth, out var shipMinDepth, out var shipMaxDepth))
            {
                _targetFish = null;
                SetShipDepthBandStatus(depth, shipMinDepth, shipMaxDepth);
                return false;
            }

            var minimumSpawnDepth = Mathf.Max(0f, _minimumFishSpawnDepth);
            if (depth < minimumSpawnDepth)
            {
                _targetFish = null;
                _hudOverlay?.SetFishingStatus(
                    $"No fish above {minimumSpawnDepth:0}m. Lower with {ResolveCastDepthControlPrompt()} to {minimumSpawnDepth:0}m+.");
                return false;
            }

            _targetFish = _spawner.RollFish(_currentDistanceTier, depth);

            var pityActivated = false;
            if (_targetFish == null && _assistService.TryActivateNoBitePity())
            {
                _targetFish = _spawner.RollFishByDistanceOnly(_currentDistanceTier);
                pityActivated = _targetFish != null;
                if (pityActivated)
                {
                    StructuredLogService.LogInfo(
                        "fishing-assist",
                        $"assist=no_bite_pity status=activated distance_tier={_currentDistanceTier}");
                }
            }

            _assistService.RecordCastResult(_targetFish != null);
            if (_targetFish == null)
            {
                _hudOverlay?.SetFishingStatus(
                    $"No fish at {depth:0}m. Lower with {ResolveCastDepthControlPrompt()} or reel with {ResolveReelUpControlPrompt()}.");
                return false;
            }

            BindAmbientFishToTarget();
            var minBite = Mathf.Max(0f, _targetFish.minBiteDelaySeconds);
            var maxBite = Mathf.Max(minBite, _targetFish.maxBiteDelaySeconds);
            _biteTimerSeconds = UnityEngine.Random.Range(minBite, maxBite);
            _biteTimerSeconds = _assistService.ApplyPityDelayScale(_biteTimerSeconds, pityActivated);
            _inWaterElapsedSeconds = 0f;
            _biteApproachStarted = false;
            ResetHookStationaryAttractionTimer(reseedDelay: true);
            if (_requireCollisionHook)
            {
                if (_targetFishBoundToAmbient)
                {
                    _hudOverlay?.SetFishingStatus($"Fish spotted at {depth:0}m. Keep the hook in lane so collision hooks the fish.");
                }
                else
                {
                    _hudOverlay?.SetFishingStatus($"Fish detected at {depth:0}m. Keep searching and collide with fish to hook.");
                }

                return true;
            }

            var baitAttractionEnabled = IsBaitAttractionEnabled() && _targetFishBoundToAmbient;
            if (pityActivated)
            {
                if (baitAttractionEnabled)
                {
                    _hudOverlay?.SetFishingStatus($"Fishing assist active at {depth:0}m: bait attracts fish faster.");
                }
                else if (_targetFishBoundToAmbient)
                {
                    _hudOverlay?.SetFishingStatus($"Fishing assist active at {depth:0}m: keep hook in fish lanes for collision hooks.");
                }
                else
                {
                    _hudOverlay?.SetFishingStatus($"Fishing assist active at {depth:0}m: waiting for a bite...");
                }

                return true;
            }

            if (baitAttractionEnabled)
            {
                _hudOverlay?.SetFishingStatus($"Bait ready at {depth:0}m. Keep hook steady to attract fish.");
            }
            else if (_targetFishBoundToAmbient)
            {
                _hudOverlay?.SetFishingStatus($"Fish spotted at {depth:0}m. Keep the hook in lane so collision hooks the fish.");
            }
            else
            {
                _hudOverlay?.SetFishingStatus($"Waiting for a bite at {depth:0}m...");
            }

            return true;
        }

        private void BindAmbientFishToTarget()
        {
            _targetFishBoundToAmbient = false;
            if (_ambientFishController == null || _targetFish == null)
            {
                return;
            }

            _targetFishBoundToAmbient = _ambientFishController.TryBindFish(_targetFish.id, out _);
        }

        private bool TryBeginBiteApproach()
        {
            if (_ambientFishController == null || _hook == null)
            {
                return false;
            }

            return _ambientFishController.BeginBoundFishApproach(_hook.transform);
        }

        private bool IsBiteApproachComplete()
        {
            if (_ambientFishController == null)
            {
                return true;
            }

            return _ambientFishController.IsBoundFishApproachComplete();
        }

        private bool TryHookTargetFishOnCollision()
        {
            if (_ambientFishController == null || _hook == null)
            {
                return false;
            }

            var depth = Mathf.Max(0f, _hook.CurrentDepth);
            if (!IsDepthWithinShipOperationalBand(depth, out var shipMinDepth, out var shipMaxDepth))
            {
                SetShipDepthBandStatus(depth, shipMinDepth, shipMaxDepth);
                return false;
            }

            string collidedFishId = string.Empty;
            var collisionRadius = Mathf.Max(0.02f, _hookCollisionRadius);
            if (_targetFishBoundToAmbient
                && _ambientFishController.IsBoundFishCollidingWithHook(_hook.transform, collisionRadius))
            {
                collidedFishId = _ambientFishController.GetBoundFishId();
                _hasPendingCollisionHookCandidate = true;
                EnsureTargetFishForCollisionHook(
                    collidedFishId,
                    allowFallbackDefinition: true,
                    overwriteExistingTarget: true);
                return true;
            }

            if (!TryPromoteCollidingAmbientFishToTarget(collisionRadius, out collidedFishId))
            {
                return false;
            }

            _hasPendingCollisionHookCandidate = true;
            EnsureTargetFishForCollisionHook(
                collidedFishId,
                allowFallbackDefinition: true,
                overwriteExistingTarget: true);
            return true;
        }

        private bool IsDepthWithinShipOperationalBand(
            float depthMeters,
            out float minDepthMeters,
            out float maxDepthMeters)
        {
            ResolveShipOperationalDepthBand(out minDepthMeters, out maxDepthMeters);
            var depth = Mathf.Max(0f, depthMeters);
            return depth >= minDepthMeters && depth <= maxDepthMeters;
        }

        private void ResolveShipOperationalDepthBand(out float minDepthMeters, out float maxDepthMeters)
        {
            if (_shipMovement == null)
            {
                RuntimeServiceRegistry.TryGet(out _shipMovement);
                if (_shipMovement == null && _hook != null && _hook.ShipTransform != null)
                {
                    _shipMovement = _hook.ShipTransform.GetComponent<ShipMovementController>();
                }
            }

            if (_shipMovement != null)
            {
                _shipMovement.GetOperationalDepthBand(out minDepthMeters, out maxDepthMeters);
                return;
            }

            ResolveFallbackShipDepthBand(
                _saveManager != null && _saveManager.Current != null ? _saveManager.Current.equippedShipId : string.Empty,
                out minDepthMeters,
                out maxDepthMeters);
        }

        private void SetShipDepthBandStatus(float depthMeters, float minDepthMeters, float maxDepthMeters)
        {
            if (depthMeters < minDepthMeters)
            {
                _hudOverlay?.SetFishingStatus(
                    $"Ship depth band is {minDepthMeters:0}-{maxDepthMeters:0}m. Lower to {minDepthMeters:0}m+.");
                return;
            }

            _hudOverlay?.SetFishingStatus(
                $"Ship depth band is {minDepthMeters:0}-{maxDepthMeters:0}m. Reel up shallower.");
        }

        private static void ResolveFallbackShipDepthBand(
            string shipId,
            out float minDepthMeters,
            out float maxDepthMeters)
        {
            var normalizedId = string.IsNullOrWhiteSpace(shipId)
                ? string.Empty
                : shipId.Trim().ToLowerInvariant();
            if (normalizedId.Contains("lv5"))
            {
                minDepthMeters = FallbackShipMinDepthTier5;
                maxDepthMeters = FallbackShipMaxDepthTier5;
                return;
            }

            if (normalizedId.Contains("lv4"))
            {
                minDepthMeters = FallbackShipMinDepthTier4;
                maxDepthMeters = FallbackShipMaxDepthTier4;
                return;
            }

            if (normalizedId.Contains("lv3"))
            {
                minDepthMeters = FallbackShipMinDepthTier3;
                maxDepthMeters = FallbackShipMaxDepthTier3;
                return;
            }

            if (normalizedId.Contains("lv2"))
            {
                minDepthMeters = FallbackShipMinDepthTier2;
                maxDepthMeters = FallbackShipMaxDepthTier2;
                return;
            }

            minDepthMeters = FallbackShipMinDepthTier1;
            maxDepthMeters = FallbackShipMaxDepthTier1;
        }

        private bool TryPromoteCollidingAmbientFishToTarget(float collisionRadius, out string fishId)
        {
            fishId = string.Empty;
            if (_ambientFishController == null || _hook == null)
            {
                return false;
            }

            if (!_ambientFishController.TryBindCollidingFishToHook(_hook.transform, collisionRadius, out fishId))
            {
                return false;
            }

            _targetFishBoundToAmbient = true;
            _pendingCollisionFishId = !string.IsNullOrWhiteSpace(fishId)
                ? fishId
                : _pendingCollisionFishId;
            return true;
        }

        private void EnsureTargetFishForCollisionHook(
            string fishId,
            bool allowFallbackDefinition,
            bool overwriteExistingTarget)
        {
            if (_targetFish != null && !overwriteExistingTarget)
            {
                return;
            }

            if (overwriteExistingTarget)
            {
                _targetFish = null;
            }

            if (!string.IsNullOrWhiteSpace(fishId))
            {
                _pendingCollisionFishId = fishId;
            }

            if (_spawner != null
                && !string.IsNullOrWhiteSpace(_pendingCollisionFishId)
                && _spawner.TryGetFishDefinitionById(_pendingCollisionFishId, out var resolvedFish)
                && resolvedFish != null)
            {
                _targetFish = resolvedFish;
                return;
            }

            if (allowFallbackDefinition)
            {
                _targetFish = BuildFallbackCollisionFishDefinition(_pendingCollisionFishId);
            }
        }

        private static FishDefinition BuildFallbackCollisionFishDefinition(string fishId)
        {
            var resolvedId = string.IsNullOrWhiteSpace(fishId)
                ? "fish_collision_fallback"
                : fishId.Trim().ToLowerInvariant();
            return new FishDefinition
            {
                id = resolvedId,
                minDistanceTier = 1,
                maxDistanceTier = 5,
                minDepth = 0f,
                maxDepth = 200f,
                rarityWeight = 1,
                baseValue = 8,
                minBiteDelaySeconds = 0.1f,
                maxBiteDelaySeconds = 0.2f,
                fightStamina = 3f,
                pullIntensity = 1f,
                escapeSeconds = 5f,
                minCatchWeightKg = 0.4f,
                maxCatchWeightKg = 1.6f
            };
        }

        private bool IsBaitAttractionEnabled()
        {
            return ResolveHookReelInputMode() == HookReelInputMode.Level3Auto;
        }

        private void UpdateHookStationaryAttractionTimer()
        {
            if (_hook == null)
            {
                _hookStationaryElapsedSeconds = 0f;
                _hasRecordedHookPositionForStationaryCheck = false;
                return;
            }

            var currentPosition = _hook.transform.position;
            if (!_hasRecordedHookPositionForStationaryCheck)
            {
                _lastHookPosition = currentPosition;
                _hasRecordedHookPositionForStationaryCheck = true;
                return;
            }

            var movementThreshold = Mathf.Max(0.0001f, _hookStationaryMovementThreshold);
            var movedDistance = Vector3.Distance(currentPosition, _lastHookPosition);
            if (movedDistance > movementThreshold)
            {
                ResetHookStationaryAttractionTimer(reseedDelay: true);
                _lastHookPosition = currentPosition;
                _hasRecordedHookPositionForStationaryCheck = true;
                return;
            }

            _hookStationaryElapsedSeconds += Time.deltaTime;
            _lastHookPosition = currentPosition;
        }

        private bool HasSatisfiedHookStationaryAttractionDelay()
        {
            return _hookStationaryElapsedSeconds >= Mathf.Max(0f, _hookStationaryDelaySeconds);
        }

        private void ResetHookStationaryAttractionTimer(bool reseedDelay)
        {
            if (reseedDelay || _hookStationaryDelaySeconds <= 0f)
            {
                _hookStationaryDelaySeconds = ResolveHookStationaryAttractionDelaySeconds();
            }

            _hookStationaryElapsedSeconds = 0f;
            _hasRecordedHookPositionForStationaryCheck = false;
        }

        private float ResolveHookStationaryAttractionDelaySeconds()
        {
            var minDelaySeconds = Mathf.Max(
                0f,
                Mathf.Min(_hookStationaryAttractionDelayRangeSeconds.x, _hookStationaryAttractionDelayRangeSeconds.y));
            var maxDelaySeconds = Mathf.Max(
                minDelaySeconds,
                Mathf.Max(_hookStationaryAttractionDelayRangeSeconds.x, _hookStationaryAttractionDelayRangeSeconds.y));
            _randomSource ??= new UnityFishingRandomSource();
            return _randomSource.Range(minDelaySeconds, maxDelaySeconds);
        }

        private bool IsCargoFull(out int fishCount, out int cargoCapacity)
        {
            fishCount = _saveManager != null ? _saveManager.GetFishInventoryCount() : 0;
            cargoCapacity = ResolveCurrentCargoCapacity();
            return fishCount >= cargoCapacity;
        }

        private bool IsHookAtCatchSecureDepth()
        {
            if (_hook == null)
            {
                return true;
            }

            var catchDepth = Mathf.Max(0.1f, _haulCompletionDepthThreshold);
            return _hook.CurrentDepth <= catchDepth;
        }

        private bool IsHookAtBoat()
        {
            if (_hook == null)
            {
                return true;
            }

            var completionDepth = Mathf.Max(0.05f, _boatArrivalDepthThreshold);
            return _hook.CurrentDepth <= completionDepth;
        }

        private float ResolveReelEscapeWindowSeconds(FishDefinition fish)
        {
            if (fish == null)
            {
                return Mathf.Max(0.5f, _minimumReelEscapeWindowSeconds);
            }

            return Mathf.Max(
                Mathf.Max(0.5f, _minimumReelEscapeWindowSeconds),
                Mathf.Max(0.5f, fish.escapeSeconds));
        }

        private float ResolveInitialReelEscapeTimer(FishDefinition fish)
        {
            if (!_enableReelStruggleEscape)
            {
                return float.PositiveInfinity;
            }

            if (!_useFishTypeEscapeChance)
            {
                return ResolveReelEscapeWindowSeconds(fish);
            }

            if (fish == null)
            {
                return ResolveReelEscapeWindowSeconds(fish);
            }

            var escapeChance = ResolveFishTypeEscapeChance(fish);
            var roll = (_randomSource ?? new UnityFishingRandomSource()).Range(0f, 1f);
            if (roll <= escapeChance)
            {
                return ResolveReelEscapeWindowSeconds(fish);
            }

            return float.PositiveInfinity;
        }

        private float ResolveFishTypeEscapeChance(FishDefinition fish)
        {
            if (fish == null)
            {
                return Mathf.Clamp01(Mathf.Min(_fishTypeEscapeChanceFloor, _fishTypeEscapeChanceCeiling));
            }

            var minChance = Mathf.Clamp01(Mathf.Min(_fishTypeEscapeChanceFloor, _fishTypeEscapeChanceCeiling));
            var maxChance = Mathf.Clamp01(Mathf.Max(_fishTypeEscapeChanceFloor, _fishTypeEscapeChanceCeiling));

            var escapeSeconds = Mathf.Max(0.1f, fish.escapeSeconds);
            if (escapeSeconds <= 1f)
            {
                return 1f;
            }

            var escapePressure = 1f - Mathf.Clamp01((escapeSeconds - 3f) / 9f);
            var staminaPressure = Mathf.Clamp01((Mathf.Max(0.1f, fish.fightStamina) - 3f) / 9f);
            var pullPressure = Mathf.Clamp01((Mathf.Max(0.1f, fish.pullIntensity) - 0.8f) / 1.6f);
            var rarityPressure = Mathf.Clamp01((Mathf.Max(1, fish.rarityWeight) - 1f) / 12f);

            var weightedDifficulty = Mathf.Clamp01(
                (escapePressure * 0.5f) +
                (staminaPressure * 0.28f) +
                (pullPressure * 0.17f) +
                (rarityPressure * 0.05f));

            return Mathf.Lerp(minChance, maxChance, weightedDifficulty);
        }

        private void UpdateReelEscapeTimer(bool isReeling)
        {
            if (!_enableReelStruggleEscape || float.IsInfinity(_reelEscapeTimeRemaining))
            {
                return;
            }

            var drainPerSecond = isReeling
                ? Mathf.Max(0.05f, _reelEscapeDrainWhileReeling)
                : Mathf.Max(0.05f, _reelEscapeDrainWhileIdle);
            _reelEscapeTimeRemaining = Mathf.Max(0f, _reelEscapeTimeRemaining - (drainPerSecond * Time.deltaTime));
        }

        private float ResolveReelTension(bool isReeling)
        {
            var frequency = Mathf.Max(0.1f, _reelTensionFrequency);
            var amplitude = Mathf.Abs(_reelTensionAmplitude);
            var oscillation = Mathf.Sin(Time.time * frequency) * amplitude;
            var reelingBias = isReeling ? 0.08f : -0.05f;
            return Mathf.Clamp01(_reelTensionBase + oscillation + reelingBias);
        }

        private bool IsReelEffortActive()
        {
            switch (ResolveHookReelInputMode())
            {
                case HookReelInputMode.Level1Tap:
                    return IsLevelOneReelPulseActive();
                case HookReelInputMode.Level2Hold:
                    return IsUpInputHeldForReelStart();
                case HookReelInputMode.Level3Auto:
                    return true;
                default:
                    if (IsReelToggleModeEnabled())
                    {
                        return true;
                    }

                    return IsUpInputHeldForReelStart();
            }
        }

        private bool IsReelToggleModeEnabled()
        {
            return ResolveHookReelInputMode() == HookReelInputMode.Legacy
                && _settingsService != null
                && _settingsService.ReelInputToggle;
        }

        private int ResolveCurrentCargoCapacity()
        {
            if (_shipMovement != null)
            {
                return Mathf.Max(1, _shipMovement.CargoCapacity);
            }

            var shipId = _saveManager != null && _saveManager.Current != null
                ? _saveManager.Current.equippedShipId
                : string.Empty;
            var normalizedId = string.IsNullOrWhiteSpace(shipId)
                ? string.Empty
                : shipId.Trim().ToLowerInvariant();
            if (normalizedId.Contains("lv3"))
            {
                return FallbackCargoCapacityTier3;
            }

            if (normalizedId.Contains("lv2"))
            {
                return FallbackCargoCapacityTier2;
            }

            return FallbackCargoCapacityTier1;
        }

        private HookReelInputMode ResolveHookReelInputMode()
        {
            var equippedHookId = _saveManager != null && _saveManager.Current != null
                ? _saveManager.Current.equippedHookId
                : string.Empty;
            return HookReelInputProfile.Resolve(equippedHookId);
        }

        private bool ShouldStartReelFromHookedInput()
        {
            switch (ResolveHookReelInputMode())
            {
                case HookReelInputMode.Level1Tap:
                    return IsUpPressedThisFrame();
                case HookReelInputMode.Level2Hold:
                    return IsUpInputHeldForReelStart();
                case HookReelInputMode.Level3Auto:
                    return IsUpPressedThisFrame();
                default:
                    return IsUpInputHeldForReelStart();
            }
        }

        private bool IsHookedAutoReelDoubleTapTriggered()
        {
            if (!IsUpPressedThisFrame())
            {
                return false;
            }

            var now = Time.unscaledTime;
            var isDoubleTap = now - _lastHookedUpPressTime <= Mathf.Max(0.1f, _hookedDoubleTapWindowSeconds);
            _lastHookedUpPressTime = now;
            return isDoubleTap;
        }

        private bool IsLevelOneReelPulseActive()
        {
            if (IsUpPressedThisFrame())
            {
                _levelOneReelPulseTimeRemaining = Mathf.Max(
                    _levelOneReelPulseTimeRemaining,
                    Mathf.Max(0.05f, _levelOneReelPulseDurationSeconds));
            }

            if (_levelOneReelPulseTimeRemaining <= 0f)
            {
                return false;
            }

            _levelOneReelPulseTimeRemaining = Mathf.Max(0f, _levelOneReelPulseTimeRemaining - Time.deltaTime);
            return true;
        }

        private string ResolveHookedPrompt()
        {
            var reelPrompt = ResolveReelUpControlPrompt();
            switch (ResolveHookReelInputMode())
            {
                case HookReelInputMode.Level1Tap:
                    return $"Tap {reelPrompt} repeatedly to reel in.";
                case HookReelInputMode.Level2Hold:
                    return $"Hold {reelPrompt} to reel in at double speed.";
                case HookReelInputMode.Level3Auto:
                    return $"Press {reelPrompt} to start auto reel.";
                default:
                    return $"Reel up with {reelPrompt}.";
            }
        }

        private string ResolveReelInstruction()
        {
            var secureDepth = Mathf.Max(0.1f, _haulCompletionDepthThreshold);
            var reelPrompt = ResolveReelUpControlPrompt();
            switch (ResolveHookReelInputMode())
            {
                case HookReelInputMode.Level1Tap:
                    return $"Fish hooked. Tap {reelPrompt} repeatedly to reel to {secureDepth:0}m before it escapes.";
                case HookReelInputMode.Level2Hold:
                    return $"Fish hooked. Hold {reelPrompt} to reel to {secureDepth:0}m at double speed.";
                case HookReelInputMode.Level3Auto:
                    return $"Fish hooked. Auto reel engaged to {secureDepth:0}m.";
                default:
                    return IsReelToggleModeEnabled()
                        ? $"Fish hooked. Auto reeling to {secureDepth:0}m..."
                        : $"Fish hooked. Hold {reelPrompt} to reel to {secureDepth:0}m before it escapes.";
            }
        }

        private string ResolveReelFailPrompt()
        {
            var reelPrompt = ResolveReelUpControlPrompt();
            switch (ResolveHookReelInputMode())
            {
                case HookReelInputMode.Level1Tap:
                    return $"tap {reelPrompt} to reel.";
                case HookReelInputMode.Level2Hold:
                    return $"hold {reelPrompt} to reel.";
                case HookReelInputMode.Level3Auto:
                    return $"press {reelPrompt} to start auto reel.";
                default:
                    return $"hold {reelPrompt} to reel.";
            }
        }

        private string ResolveCastDepthControlPrompt()
        {
            var keyboardDown = ResolveMoveHookKeyboardBinding("negative", "Down Arrow");
            var gamepad = ResolveMoveHookGamepadBindings();
            return $"{keyboardDown} or {gamepad} down";
        }

        private string ResolveReelUpControlPrompt()
        {
            var keyboardUp = ResolveMoveHookKeyboardBinding("positive", "Up Arrow");
            var gamepad = ResolveMoveHookGamepadBindings();
            return $"{keyboardUp} or {gamepad} up";
        }

        private string ResolveMoveHookKeyboardBinding(string compositePartName, string fallback)
        {
            var display = _inputRebindingService != null
                ? _inputRebindingService.GetDisplayBindingForCompositePart("Fishing/MoveHook", compositePartName, "Keyboard")
                : string.Empty;
            return string.IsNullOrWhiteSpace(display) ? fallback : display;
        }

        private string ResolveMoveHookGamepadBindings()
        {
            var display = _inputRebindingService != null
                ? _inputRebindingService.GetDisplayBindingsForAction("Fishing/MoveHook", "Gamepad", " or ", 2)
                : string.Empty;
            return string.IsNullOrWhiteSpace(display) ? "Right Stick or D-Pad" : display;
        }

        private bool IsActionHeldForCastDepth()
        {
            var axis = _moveHookAction != null ? _moveHookAction.ReadValue<float>() : 0f;
            if (axis < -0.25f)
            {
                return true;
            }

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

            return false;
        }

        private bool IsUpInputHeldForReelStart()
        {
            var moveAxis = _moveHookAction != null ? _moveHookAction.ReadValue<float>() : 0f;
            if (moveAxis > 0.25f)
            {
                return true;
            }

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

            return false;
        }

        private bool IsUpPressedThisFrame()
        {
            var frame = Time.frameCount;
            if (_cachedUpPressFrame == frame)
            {
                return _cachedUpPressResult;
            }

            RefreshInputActions();
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

            var axisUp = _moveHookAction != null && _moveHookAction.ReadValue<float>() > 0.25f;
            var axisPressed = axisUp && !_upAxisHeldLastFrameForPress;
            _upAxisHeldLastFrameForPress = axisUp;

            _cachedUpPressFrame = frame;
            _cachedUpPressResult = keyboardPressed || axisPressed;
            return _cachedUpPressResult;
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
