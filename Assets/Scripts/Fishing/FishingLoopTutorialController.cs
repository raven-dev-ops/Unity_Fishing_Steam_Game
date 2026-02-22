using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using UnityEngine;

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
        [SerializeField] private int _maxRecoveryFailures = 3;

        private TutorialStep _step = TutorialStep.None;
        private bool _isActive;
        private int _failureCount;
        private IFishingHudOverlay _hudOverlay;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _stateMachine, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catchResolver, this, warnIfMissing: false);
            _hudOverlay = _hudOverlayBehaviour as IFishingHudOverlay;
            if (_hudOverlay == null)
            {
                _hudOverlay = FindFishingHudOverlay();
            }
        }

        private void OnEnable()
        {
            if (_stateMachine != null)
            {
                _stateMachine.StateChanged += OnFishingStateChanged;
            }

            if (_catchResolver != null)
            {
                _catchResolver.CatchResolved += OnCatchResolved;
            }

            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged += OnSaveDataChanged;
            }

            EvaluateActivation();
        }

        private void OnDisable()
        {
            if (_stateMachine != null)
            {
                _stateMachine.StateChanged -= OnFishingStateChanged;
            }

            if (_catchResolver != null)
            {
                _catchResolver.CatchResolved -= OnCatchResolved;
            }

            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged -= OnSaveDataChanged;
            }
        }

        public void SkipActiveTutorial()
        {
            if (!_isActive)
            {
                return;
            }

            CompleteTutorial(
                skipped: true,
                completionMessage: "Fishing tutorial skipped. You can replay it from Tutorial Controls.");
        }

        private void EvaluateActivation()
        {
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
            }
        }

        private void BeginTutorial()
        {
            _isActive = true;
            _step = TutorialStep.Cast;
            _failureCount = 0;
            _saveManager?.MarkFishingLoopTutorialStarted();
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
                    completionMessage: "Tutorial auto-completed after repeated failures. Replay anytime from Tutorial Controls.");
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
                    return "Tutorial: Press Down Arrow or S to cast to depth 25.";
                case TutorialStep.Hook:
                    return "Tutorial: Steer left/right and collide the hook with a fish, then press Up Arrow or W to reel.";
                case TutorialStep.Reel:
                    return "Tutorial: Double-tap Up Arrow or W to auto-reel toward depth 20.";
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
