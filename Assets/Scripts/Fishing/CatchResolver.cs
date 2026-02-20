using RavenDevOps.Fishing.Audio;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class CatchResolver : MonoBehaviour
    {
        [SerializeField] private FishingActionStateMachine _stateMachine;
        [SerializeField] private FishSpawner _spawner;
        [SerializeField] private HookMovementController _hook;
        [SerializeField] private HudOverlayController _hudOverlay;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private InputActionMapController _inputMapController;

        [SerializeField] private int _currentDistanceTier = 1;
        [SerializeField] private float _hookReactionWindowSeconds = 1.3f;
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

        private FishDefinition _targetFish;
        private FishDefinition _hookedFish;
        private float _biteTimerSeconds;
        private float _inWaterElapsedSeconds;
        private float _hookedElapsedSeconds;
        private bool _catchSucceeded;
        private FishingFailReason _pendingFailReason = FishingFailReason.None;
        private FishingTensionState _lastTensionState = FishingTensionState.None;
        private bool _cameraConfigured;
        private bool _tutorialConfigured;
        private bool _environmentConfigured;
        private bool _conditionConfigured;

        private InputAction _reelAction;

        public event System.Action<bool, FishingFailReason, string> CatchResolved;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
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

            _hudOverlay?.SetFishingFailure(string.Empty);
            _hudOverlay?.SetFishingTension(0f, FishingTensionState.None);

            if (_spawner == null || _hook == null)
            {
                _hudOverlay?.SetFishingStatus("Missing fishing dependencies.");
                return;
            }

            _targetFish = _spawner.RollFish(_currentDistanceTier, _hook.CurrentDepth);
            if (_targetFish == null)
            {
                _hudOverlay?.SetFishingStatus("No bite in this zone. Recast or change depth.");
                return;
            }

            var minBite = Mathf.Max(0f, _targetFish.minBiteDelaySeconds);
            var maxBite = Mathf.Max(minBite, _targetFish.maxBiteDelaySeconds);
            _biteTimerSeconds = Random.Range(minBite, maxBite);
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
            _audioManager?.PlaySfx(_hookSfx);
            _hudOverlay?.SetFishingStatus("Fish hooked! Press and hold Action to reel.");
            _hudOverlay?.SetFishingFailure(string.Empty);
            _hudOverlay?.SetFishingTension(0.2f, FishingTensionState.Safe);
            _lastTensionState = FishingTensionState.Safe;
        }

        private void BeginReelPhase()
        {
            if (_hookedFish == null)
            {
                _pendingFailReason = FishingFailReason.MissedHook;
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
            if (_hookedFish == null)
            {
                return;
            }

            _hookedElapsedSeconds += Time.deltaTime;
            if (_hookedElapsedSeconds >= Mathf.Max(0.2f, _hookReactionWindowSeconds))
            {
                _pendingFailReason = FishingFailReason.MissedHook;
                _stateMachine?.SetResolve();
            }
        }

        private void TickReelFight()
        {
            var isReeling = _reelAction != null && _reelAction.IsPressed();
            var tensionState = _encounterModel.Step(Time.deltaTime, isReeling, out var landed, out var failReason);
            _hudOverlay?.SetFishingTension(_encounterModel.TensionNormalized, tensionState);
            _hudOverlay?.SetFishingStatus(isReeling ? "Reeling..." : "Hold Action to reel.");

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
                var weightKg = Random.Range(
                    Mathf.Max(0.1f, _hookedFish.minCatchWeightKg),
                    Mathf.Max(_hookedFish.minCatchWeightKg, _hookedFish.maxCatchWeightKg));
                var valueCopecs = Mathf.Max(1, Mathf.RoundToInt(_hookedFish.baseValue * Random.Range(0.9f, 1.3f)));

                _saveManager?.RecordCatch(_hookedFish.id, _currentDistanceTier, weightKg, valueCopecs);
                _audioManager?.PlaySfx(_catchSfx);
                _hudOverlay?.SetFishingFailure(string.Empty);
                _hudOverlay?.SetFishingStatus($"Caught {_hookedFish.id} ({weightKg:0.0}kg, {valueCopecs} copecs).");
            }
            else
            {
                var reason = BuildFailureReasonText(_pendingFailReason);
                _hudOverlay?.SetFishingFailure(reason);
                _hudOverlay?.SetFishingStatus("Press Action to cast again.");
                _saveManager?.RecordCatchFailure(_hookedFish != null ? _hookedFish.id : string.Empty, _currentDistanceTier, reason);
                PlayFailureSfx(_pendingFailReason);
            }

            InvokeCatchResolved(_catchSucceeded, resolvedFailReason, resolvedFishId);

            _encounterModel.End();
            _targetFish = null;
            _hookedFish = null;
            _catchSucceeded = false;
            _pendingFailReason = FishingFailReason.None;
            _lastTensionState = FishingTensionState.None;
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

        private static string BuildFailureReasonText(FishingFailReason failReason)
        {
            switch (failReason)
            {
                case FishingFailReason.MissedHook:
                    return "Missed hook: the fish slipped away before you started reeling.";
                case FishingFailReason.LineSnap:
                    return "Line snapped: tension stayed too high.";
                case FishingFailReason.FishEscaped:
                    return "Fish escaped: stamina was not depleted before escape.";
                default:
                    return "Catch failed.";
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
                _hudOverlay.SetFishingConditions(_spawner.GetActiveConditionSummary());
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
    }
}
