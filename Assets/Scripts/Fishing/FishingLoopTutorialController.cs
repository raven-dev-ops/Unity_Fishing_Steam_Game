using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingLoopTutorialController : MonoBehaviour
    {
        private enum TutorialStep
        {
            None = 0,
            Cast = 1,
            Hook = 2,
            Reel = 3,
            Complete = 4
        }

        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private FishingActionStateMachine _stateMachine;
        [SerializeField] private CatchResolver _catchResolver;
        [SerializeField] private MonoBehaviour _hudOverlayBehaviour;
        [SerializeField] private Button _skipTutorialButton;
        [SerializeField] private int _maxRecoveryFailures = 3;

        private TutorialStep _step = TutorialStep.None;
        private bool _isActive;
        private int _failureCount;
        private IFishingHudOverlay _hudOverlay;
        private FishingActionStateMachine _subscribedStateMachine;
        private CatchResolver _subscribedCatchResolver;
        private SaveManager _subscribedSaveManager;

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

            if (_skipTutorialButton != null)
            {
                _skipTutorialButton.onClick.RemoveListener(SkipActiveTutorial);
            }
        }

        private void Update()
        {
            EnsureDependencies();
            SubscribeToDependencies();
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
                _step = TutorialStep.None;
                _failureCount = 0;
                UpdateSkipButtonVisibility();
            }
        }

        private void BeginTutorial()
        {
            _isActive = true;
            _step = TutorialStep.Cast;
            _failureCount = 0;
            _saveManager?.MarkFishingLoopTutorialStarted();
            UpdateSkipButtonVisibility();
            PushPrompt();
        }

        private void OnFishingStateChanged(FishingActionState previous, FishingActionState next)
        {
            if (!_isActive)
            {
                return;
            }

            if (next == FishingActionState.InWater && _step == TutorialStep.Cast)
            {
                _step = TutorialStep.Hook;
                PushPrompt();
                return;
            }

            if ((next == FishingActionState.Hooked || next == FishingActionState.Reel) && _step <= TutorialStep.Hook)
            {
                _step = TutorialStep.Reel;
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

            var reasonHint = BuildFailureHint(failReason);
            _hudOverlay?.SetFishingStatus($"Tutorial hint: {reasonHint} Retry {_failureCount}/{_maxRecoveryFailures}.");
        }

        private void CompleteTutorial(bool skipped, string completionMessage)
        {
            _isActive = false;
            _step = TutorialStep.Complete;
            _failureCount = 0;
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
        }

        private string BuildPromptForStep(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.Cast:
                    return "Tutorial: Press Down Arrow or S to cast to 25m.";
                case TutorialStep.Hook:
                    return "Tutorial: Steer left/right and collide the hook with a fish, then press Up Arrow or W to reel.";
                case TutorialStep.Reel:
                    return "Tutorial: Press Up Arrow or W to start auto-reeling toward 20m.";
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

        private void OnSaveDataChanged(SaveDataV1 data)
        {
            EvaluateActivation();
        }

        private void EnsureDependencies()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _stateMachine, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catchResolver, this, warnIfMissing: false);

            _saveManager ??= GetComponent<SaveManager>();
            _saveManager ??= FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);

            _stateMachine ??= GetComponent<FishingActionStateMachine>();
            _stateMachine ??= FindAnyObjectByType<FishingActionStateMachine>(FindObjectsInactive.Include);

            _catchResolver ??= GetComponent<CatchResolver>();
            _catchResolver ??= FindAnyObjectByType<CatchResolver>(FindObjectsInactive.Include);

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
