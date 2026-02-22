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
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private MetaLoopRuntimeService _metaLoopService;
        [SerializeField] private FishingAmbientFishSwimController _ambientFishController;

        [SerializeField] private int _currentDistanceTier = 1;
        [SerializeField] private float _minimumFishSpawnDepth = 20f;
        [SerializeField] private float _haulCompletionDepthThreshold = 0.85f;
        [SerializeField] private float _hookReactionWindowSeconds = 1.3f;
        [SerializeField] private bool _enableReelStruggleEscape = true;
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
        private float _inWaterElapsedSeconds;
        private float _hookedElapsedSeconds;
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
        private bool _haulCatchInProgress;
        private float _reelEscapeTimeRemaining;

        [NonSerialized] private IFishingRandomSource _randomSource;
        private InputAction _moveHookAction;
        private bool _stateMachineSubscribed;
        private ShipMovementController _shipMovement;

        public event System.Action<bool, FishingFailReason, string> CatchResolved;

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
            if (_hudOverlay == null)
            {
                _hudOverlay = FindFishingHudOverlay();
            }
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _metaLoopService, this, warnIfMissing: false);
            if (_ambientFishController == null)
            {
                _ambientFishController = GetComponent<FishingAmbientFishSwimController>();
                if (_ambientFishController == null)
                {
                    _ambientFishController = FindAnyObjectByType<FishingAmbientFishSwimController>(FindObjectsInactive.Include);
                }
            }

            _randomSource ??= new UnityFishingRandomSource();
            _hudOverlay = _hudOverlayBehaviour as IFishingHudOverlay;
            if (_hudOverlay == null)
            {
                _hudOverlay = FindFishingHudOverlay();
            }

            ConfigureAssistService();
        }

        private void OnValidate()
        {
            ConfigureAssistService();
        }

        private void OnEnable()
        {
            SubscribeToStateMachine();
        }

        private void OnDisable()
        {
            UnsubscribeFromStateMachine();
        }

        private void Update()
        {
            RefreshDistanceTier();
            UpdateHudTelemetry();
            EnsureCameraController();
            EnsureTutorialController();
            EnsureEnvironmentController();
            EnsureConditionController();
            RefreshInputActions();

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
            switch (next)
            {
                case FishingActionState.InWater:
                    BeginCastPhase();
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

        private void BeginCastPhase()
        {
            _audioManager?.PlaySfx(_castSfx);
            _encounterModel.End();

            _targetFish = null;
            _hookedFish = null;
            _haulCatchInProgress = false;
            _catchSucceeded = false;
            _pendingFailReason = FishingFailReason.None;
            _inWaterElapsedSeconds = 0f;
            _hookedElapsedSeconds = 0f;
            _biteTimerSeconds = 0f;
            _lastTensionState = FishingTensionState.None;
            _activeHookReactionWindowSeconds = _hookReactionWindowSeconds;
            _reelEscapeTimeRemaining = 0f;
            _biteSelectionResolvedForCurrentDrop = false;
            _biteApproachStarted = false;
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
                _stateMachine?.ResetToCast();
                return;
            }

            _hudOverlay?.SetFishingStatus("Casting to depth 25. Use Down/S to lower deeper and Up/W to reel up.");
        }

        private void BeginHookedPhase()
        {
            if (_targetFish == null)
            {
                return;
            }

            _hookedFish = _targetFish;
            _hookedElapsedSeconds = 0f;
            _biteApproachStarted = false;
            _ambientFishController?.SetBoundFishHooked(_hook != null ? _hook.transform : null);
            _activeHookReactionWindowSeconds = _assistService.ResolveHookWindow(_hookReactionWindowSeconds, out var adaptiveWindowActivated);
            _audioManager?.PlaySfx(_hookSfx);
            if (adaptiveWindowActivated)
            {
                StructuredLogService.LogInfo(
                    "fishing-assist",
                    $"assist=adaptive_hook_window status=activated base_window={_hookReactionWindowSeconds:0.00} resolved_window={_activeHookReactionWindowSeconds:0.00}");
                _hudOverlay?.SetFishingStatus("Fish hooked! Assist active: extra hook window. Start reeling with Up/W.");
            }
            else
            {
                _hudOverlay?.SetFishingStatus("Fish hooked! Reel up with Up/W.");
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
            _catchSucceeded = false;
            _pendingFailReason = FishingFailReason.None;
            _reelEscapeTimeRemaining = _enableReelStruggleEscape
                ? ResolveReelEscapeWindowSeconds(_hookedFish)
                : float.PositiveInfinity;
            _encounterModel.Begin(_hookedFish, Mathf.Clamp01(_reelTensionBase));
            var reelInstruction = IsReelToggleModeEnabled()
                ? "Fish secured. Auto reeling to boat..."
                : "Fish secured. Hold Up/W to reel before it escapes.";
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
                _stateMachine?.ResetToCast();
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
                _inWaterElapsedSeconds = 0f;
                _hudOverlay?.SetFishingStatus("Adjusting cast depth. Use Down/S to lower deeper.");
                return;
            }

            if (!_biteSelectionResolvedForCurrentDrop)
            {
                _biteSelectionResolvedForCurrentDrop = true;
                if (!TryResolveTargetFishForCurrentDepth())
                {
                    return;
                }
            }

            if (_targetFish == null)
            {
                return;
            }

            _inWaterElapsedSeconds += Time.deltaTime;
            if (_inWaterElapsedSeconds < _biteTimerSeconds)
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

            _stateMachine?.SetHooked();
        }

        private void TickHookedWindow()
        {
            if (IsUpInputHeldForReelStart())
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
            UpdateReelEscapeTimer(isReeling);
            if (_enableReelStruggleEscape && _reelEscapeTimeRemaining <= 0f)
            {
                _pendingFailReason = FishingFailReason.FishEscaped;
                _catchSucceeded = false;
                _stateMachine?.SetResolve();
                return;
            }

            var remainingDepth = _hook != null ? Mathf.Max(0f, _hook.CurrentDepth) : 0f;
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
            var escapeSuffix = _enableReelStruggleEscape
                ? $"{Mathf.Max(0f, _reelEscapeTimeRemaining):0.0}s"
                : "n/a";
            if (!isReeling && !IsReelToggleModeEnabled())
            {
                _hudOverlay?.SetFishingStatus(
                    $"Fish struggling... hold Up/W to reel. Escape in {escapeSuffix}.");
            }
            else
            {
                _hudOverlay?.SetFishingStatus(
                    $"Reeling catch... {remainingDepth:0.0} depth to boat | Escape {escapeSuffix}.");
            }

            if (IsHookAtBoat())
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

            if (_catchSucceeded && _hookedFish != null)
            {
                var reward = _outcomeDomainService.BuildCatchReward(_hookedFish, _randomSource);
                var weightKg = reward.WeightKg;
                var valueCopecs = reward.ValueCopecs;

                _saveManager?.RecordCatch(_hookedFish.id, _currentDistanceTier, weightKg, valueCopecs);
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
                _hudOverlay?.SetFishingStatus("Press Down/S to cast again.");
                _saveManager?.RecordCatchFailure(_hookedFish != null ? _hookedFish.id : string.Empty, _currentDistanceTier, reason);
                PlayFailureSfx(_pendingFailReason);
            }

            InvokeCatchResolved(_catchSucceeded, resolvedFailReason, resolvedFishId);
            _assistService.RecordCatchOutcome(_catchSucceeded);
            _ambientFishController?.ResolveBoundFish(_catchSucceeded);

            _encounterModel.End();
            _targetFish = null;
            _hookedFish = null;
            _haulCatchInProgress = false;
            _catchSucceeded = false;
            _pendingFailReason = FishingFailReason.None;
            _lastTensionState = FishingTensionState.None;
            _reelEscapeTimeRemaining = 0f;
            _biteApproachStarted = false;
            _stateMachine?.ResetToCast();
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
                var conditionSummary = _spawner.GetActiveConditionSummary();
                if (_metaLoopService != null && _saveManager != null && _saveManager.Current != null)
                {
                    var activeFishId = _hookedFish != null ? _hookedFish.id : (_targetFish != null ? _targetFish.id : string.Empty);
                    if (!string.IsNullOrWhiteSpace(activeFishId))
                    {
                        var modifierLabel = _metaLoopService.BuildModifierLabel(
                            activeFishId,
                            _saveManager.Current.equippedShipId,
                            _saveManager.Current.equippedHookId);
                        conditionSummary = $"{conditionSummary} | {modifierLabel}";
                    }
                }

                _hudOverlay.SetFishingConditions(conditionSummary);
            }
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

                if (_shipMovement == null)
                {
                    RuntimeServiceRegistry.TryGet(out _shipMovement);
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
            var minimumSpawnDepth = Mathf.Max(0f, _minimumFishSpawnDepth);
            if (depth < minimumSpawnDepth)
            {
                _targetFish = null;
                _hudOverlay?.SetFishingStatus(
                    $"No fish above depth {minimumSpawnDepth:0}. Lower with Down/S to {minimumSpawnDepth:0}+.");
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
                _hudOverlay?.SetFishingStatus($"No fish at depth {depth:0}. Lower with Down/S or reel up with Up/W.");
                return false;
            }

            BindAmbientFishToTarget();
            var minBite = Mathf.Max(0f, _targetFish.minBiteDelaySeconds);
            var maxBite = Mathf.Max(minBite, _targetFish.maxBiteDelaySeconds);
            _biteTimerSeconds = UnityEngine.Random.Range(minBite, maxBite);
            _biteTimerSeconds = _assistService.ApplyPityDelayScale(_biteTimerSeconds, pityActivated);
            _inWaterElapsedSeconds = 0f;
            _biteApproachStarted = false;
            if (pityActivated)
            {
                _hudOverlay?.SetFishingStatus($"Fishing assist active at depth {depth:0}: activity increased. Waiting for a bite...");
                return true;
            }

            _hudOverlay?.SetFishingStatus($"Waiting for a bite at depth {depth:0}...");
            return true;
        }

        private void BindAmbientFishToTarget()
        {
            if (_ambientFishController == null || _targetFish == null)
            {
                return;
            }

            _ambientFishController.TryBindFish(_targetFish.id, out _);
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

        private bool IsCargoFull(out int fishCount, out int cargoCapacity)
        {
            fishCount = _saveManager != null ? _saveManager.GetFishInventoryCount() : 0;
            cargoCapacity = ResolveCurrentCargoCapacity();
            return fishCount >= cargoCapacity;
        }

        private bool IsHookAtBoat()
        {
            if (_hook == null)
            {
                return true;
            }

            var completionDepth = Mathf.Max(0.05f, _haulCompletionDepthThreshold);
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
            if (IsReelToggleModeEnabled())
            {
                return true;
            }

            return IsUpInputHeldForReelStart();
        }

        private bool IsReelToggleModeEnabled()
        {
            return _settingsService != null && _settingsService.ReelInputToggle;
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
