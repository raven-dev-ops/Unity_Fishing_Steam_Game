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
        [SerializeField] private FishingActionStateMachine _stateMachine;
        [SerializeField] private FishSpawner _spawner;
        [SerializeField] private HookMovementController _hook;
        [SerializeField] private MonoBehaviour _hudOverlayBehaviour;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private MetaLoopRuntimeService _metaLoopService;

        [SerializeField] private int _currentDistanceTier = 1;
        [SerializeField] private float _hookReactionWindowSeconds = 1.3f;
        [SerializeField] private bool _enableNoBitePity = true;
        [SerializeField] private int _noBitePityThresholdCasts = 3;
        [SerializeField] private float _pityBiteDelayScale = 0.55f;
        [SerializeField] private bool _enableAdaptiveHookWindowAssist = true;
        [SerializeField] private int _adaptiveHookWindowFailureThreshold = 3;
        [SerializeField] private float _adaptiveHookWindowBonusSeconds = 0.35f;
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

        [NonSerialized] private IFishingRandomSource _randomSource;
        private InputAction _reelAction;
        private bool _toggleReelActive;

        public event System.Action<bool, FishingFailReason, string> CatchResolved;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _metaLoopService, this, warnIfMissing: false);
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
            if (_stateMachine != null)
            {
                _stateMachine.StateChanged += OnStateChanged;
            }
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
            UpdateHudTelemetry();
            EnsureCameraController();
            EnsureTutorialController();
            EnsureEnvironmentController();
            EnsureConditionController();
            RefreshReelAction();

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
            _catchSucceeded = false;
            _pendingFailReason = FishingFailReason.None;
            _inWaterElapsedSeconds = 0f;
            _hookedElapsedSeconds = 0f;
            _biteTimerSeconds = 0f;
            _lastTensionState = FishingTensionState.None;
            _activeHookReactionWindowSeconds = _hookReactionWindowSeconds;
            _toggleReelActive = false;

            _hudOverlay?.SetFishingFailure(string.Empty);
            _hudOverlay?.SetFishingTension(0f, FishingTensionState.None);

            if (_spawner == null || _hook == null)
            {
                _hudOverlay?.SetFishingStatus("Missing fishing dependencies.");
                return;
            }

            _targetFish = _spawner.RollFish(_currentDistanceTier, _hook.CurrentDepth);
            var pityActivated = false;
            if (_targetFish == null && _spawner != null && _assistService.TryActivateNoBitePity())
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
                _hudOverlay?.SetFishingStatus("No bite in this zone. Recast or change depth.");
                return;
            }

            var minBite = Mathf.Max(0f, _targetFish.minBiteDelaySeconds);
            var maxBite = Mathf.Max(minBite, _targetFish.maxBiteDelaySeconds);
            _biteTimerSeconds = Random.Range(minBite, maxBite);
            _biteTimerSeconds = _assistService.ApplyPityDelayScale(_biteTimerSeconds, pityActivated);
            if (pityActivated)
            {
                _hudOverlay?.SetFishingStatus("Fishing assist active: activity increased. Waiting for a bite...");
                return;
            }

            _hudOverlay?.SetFishingStatus("Waiting for a bite...");
        }

        private void BeginHookedPhase()
        {
            if (_targetFish == null)
            {
                return;
            }

            _hookedFish = _targetFish;
            _hookedElapsedSeconds = 0f;
            _activeHookReactionWindowSeconds = _assistService.ResolveHookWindow(_hookReactionWindowSeconds, out var adaptiveWindowActivated);
            _audioManager?.PlaySfx(_hookSfx);
            if (adaptiveWindowActivated)
            {
                StructuredLogService.LogInfo(
                    "fishing-assist",
                    $"assist=adaptive_hook_window status=activated base_window={_hookReactionWindowSeconds:0.00} resolved_window={_activeHookReactionWindowSeconds:0.00}");
                _hudOverlay?.SetFishingStatus("Fish hooked! Assist active: extra hook timing window.");
            }
            else
            {
                _hudOverlay?.SetFishingStatus("Fish hooked! Press and hold Action to reel.");
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

            _encounterModel.Begin(_hookedFish, initialTension: 0.25f);
            _hudOverlay?.SetFishingStatus("Reel steadily. Keep line tension out of critical.");
            _hudOverlay?.SetFishingFailure(string.Empty);
            _lastTensionState = FishingTensionState.Safe;
        }

        private void TickInWater()
        {
            if (_targetFish == null)
            {
                return;
            }

            _inWaterElapsedSeconds += Time.deltaTime;
            if (_inWaterElapsedSeconds >= _biteTimerSeconds)
            {
                _stateMachine?.SetHooked();
            }
        }

        private void TickHookedWindow()
        {
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
            var isReeling = ResolveIsReeling();
            var tensionState = _encounterModel.Step(Time.deltaTime, isReeling, out var landed, out var failReason);
            _hudOverlay?.SetFishingTension(_encounterModel.TensionNormalized, tensionState);
            _hudOverlay?.SetFishingStatus(isReeling ? "Reeling..." : ResolveReelHintText());

            if (tensionState != _lastTensionState)
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

            if (landed)
            {
                _catchSucceeded = true;
                _pendingFailReason = FishingFailReason.None;
                _stateMachine?.SetResolve();
                return;
            }

            if (failReason != FishingFailReason.None)
            {
                _catchSucceeded = false;
                _pendingFailReason = failReason;
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
                _hudOverlay?.SetFishingStatus($"Caught {_hookedFish.id} ({weightKg:0.0}kg, {valueCopecs} copecs).");
            }
            else
            {
                var reason = _outcomeDomainService.BuildFailureReasonText(_pendingFailReason);
                _hudOverlay?.SetFishingFailure(reason);
                _hudOverlay?.SetFishingStatus("Press Action to cast again.");
                _saveManager?.RecordCatchFailure(_hookedFish != null ? _hookedFish.id : string.Empty, _currentDistanceTier, reason);
                PlayFailureSfx(_pendingFailReason);
            }

            InvokeCatchResolved(_catchSucceeded, resolvedFailReason, resolvedFishId);
            _assistService.RecordCatchOutcome(_catchSucceeded);

            _encounterModel.End();
            _targetFish = null;
            _hookedFish = null;
            _catchSucceeded = false;
            _pendingFailReason = FishingFailReason.None;
            _lastTensionState = FishingTensionState.None;
            _toggleReelActive = false;
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

        private void RefreshReelAction()
        {
            if (_reelAction != null)
            {
                return;
            }

            _reelAction = _inputMapController != null
                ? _inputMapController.FindAction("Fishing/Action")
                : null;
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
            var candidates = FindObjectsOfType<MonoBehaviour>(true);
            for (var i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is IFishingHudOverlay overlay)
                {
                    return overlay;
                }
            }

            return null;
        }

        private bool ResolveIsReeling()
        {
            if (_reelAction == null)
            {
                return false;
            }

            var toggleMode = _settingsService != null && _settingsService.ReelInputToggle;
            if (!toggleMode)
            {
                _toggleReelActive = false;
                return _reelAction.IsPressed();
            }

            if (_reelAction.WasPressedThisFrame())
            {
                _toggleReelActive = !_toggleReelActive;
            }

            return _toggleReelActive;
        }

        private string ResolveReelHintText()
        {
            var toggleMode = _settingsService != null && _settingsService.ReelInputToggle;
            return toggleMode
                ? "Press Action to toggle reel."
                : "Hold Action to reel.";
        }
    }
}
