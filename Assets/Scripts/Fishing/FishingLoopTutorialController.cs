using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.UI;
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
        [SerializeField] private HudOverlayController _hudOverlay;
        [SerializeField] private InputRebindingService _rebindService;
        [SerializeField] private int _maxRecoveryFailures = 3;

        private TutorialStep _step = TutorialStep.None;
        private bool _isActive;
        private int _failureCount;
        private string _actionLabel = "Action";

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _stateMachine, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catchResolver, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _hudOverlay, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _rebindService, this, warnIfMissing: false);
            RefreshBindings();
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
            RefreshBindings();
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
                    return $"Tutorial: Press {_actionLabel} to cast your line.";
                case TutorialStep.Hook:
                    return $"Tutorial: Wait for a bite, then press {_actionLabel} quickly to set the hook.";
                case TutorialStep.Reel:
                    return $"Tutorial: Hold {_actionLabel} to reel. Ease off when tension reaches critical.";
                default:
                    return string.Empty;
            }
        }

        private static string BuildFailureHint(FishingFailReason failReason)
        {
            switch (failReason)
            {
                case FishingFailReason.MissedHook:
                    return "Press Action right when the bite happens.";
                case FishingFailReason.LineSnap:
                    return "Release reel briefly when tension is critical.";
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

        private void RefreshBindings()
        {
            if (_rebindService == null)
            {
                _actionLabel = "Action";
                return;
            }

            var display = _rebindService.GetDisplayBindingForAction("Fishing/Action");
            _actionLabel = string.IsNullOrWhiteSpace(display) ? "Action" : display;
        }
    }
}
