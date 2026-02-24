using System.Collections;
using RavenDevOps.Fishing.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Core
{
    public sealed class CinematicSceneFlowController : MonoBehaviour
    {
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private InputContextRouter _inputContextRouter;
        [SerializeField] private Button _skipIntroButton;
        [SerializeField] private CanvasGroup _titleCardOverlay;
        [SerializeField] private Text _titleCardText;
        [SerializeField] private string _titleCardLabel = "Raven DevOps Fishing";
        [SerializeField, Min(0f)] private float _minimumIntroWatchSeconds = 3.5f;
        [SerializeField, Min(0f)] private float _introAutoAdvanceSeconds = 6.5f;
        [SerializeField, Min(0.05f)] private float _titleFadeToBlackSeconds = 0.8f;
        [SerializeField, Min(0f)] private float _titleHoldSeconds = 1.75f;
        [SerializeField, Min(0f)] private float _inputDebounceSeconds = 0.25f;
        [SerializeField, Min(0f)] private float _batchModeAutoAdvanceSeconds = 3f;

        private InputAction _submitAction;
        private InputAction _cancelAction;
        private Coroutine _transitionRoutine;
        private float _inputEnabledAt;
        private float _skipEnabledAt;
        private float _autoAdvanceAt;
        private float _sceneEnabledAt;
        private bool _awaitingInitialInputRelease;
        private bool _advanced;

        public void Configure(
            Button skipIntroButton,
            CanvasGroup titleCardOverlay,
            Text titleCardText)
        {
            ConfigureSkipButton(skipIntroButton);
            _titleCardOverlay = titleCardOverlay;
            _titleCardText = titleCardText;
            ResetTitleCardOverlayVisual();
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputContextRouter, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            _advanced = false;
            _sceneEnabledAt = Time.unscaledTime;
            _inputEnabledAt = _sceneEnabledAt + Mathf.Max(0f, _inputDebounceSeconds);
            _skipEnabledAt = _sceneEnabledAt + Mathf.Max(0f, _minimumIntroWatchSeconds);
            var requestedAutoAdvance = Mathf.Max(0f, _introAutoAdvanceSeconds);
            _autoAdvanceAt = requestedAutoAdvance > 0f
                ? _sceneEnabledAt + Mathf.Max(requestedAutoAdvance, Mathf.Max(0f, _minimumIntroWatchSeconds))
                : -1f;
            _awaitingInitialInputRelease = true;
            _inputContextRouter?.SetContext(InputContext.UI);
            _inputMapController?.ApplyContext(InputContext.UI);
            ResetTitleCardOverlayVisual();
            SetSkipButtonVisible(true);
            SetSkipButtonInteractable(false);
        }

        private void OnDisable()
        {
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }

            if (_skipIntroButton != null)
            {
                _skipIntroButton.onClick.RemoveListener(OnSkipIntroPressed);
            }
        }

        private void Update()
        {
            if (_advanced)
            {
                return;
            }

            if (Application.isBatchMode
                && Time.unscaledTime >= _sceneEnabledAt + Mathf.Max(0f, _batchModeAutoAdvanceSeconds))
            {
                BeginTransitionToMainMenu();
                return;
            }

            var skipEnabled = Time.unscaledTime >= _skipEnabledAt;
            SetSkipButtonInteractable(skipEnabled);

            if (_autoAdvanceAt >= 0f && Time.unscaledTime >= _autoAdvanceAt)
            {
                BeginTransitionToMainMenu();
                return;
            }

            if (_awaitingInitialInputRelease)
            {
                if (AreAnyIntroInputsHeld())
                {
                    return;
                }

                _awaitingInitialInputRelease = false;
            }

            if (Time.unscaledTime < _inputEnabledAt)
            {
                return;
            }

            if (!skipEnabled)
            {
                return;
            }

            if (IsAnyIntroInputPressed())
            {
                BeginTransitionToMainMenu();
            }
        }

        private void RefreshActionsIfNeeded()
        {
            if (_submitAction == null)
            {
                _submitAction = _inputMapController != null
                    ? _inputMapController.FindAction("UI/Submit")
                    : null;
            }

            if (_cancelAction == null)
            {
                _cancelAction = _inputMapController != null
                    ? _inputMapController.FindAction("UI/Cancel")
                    : null;
            }
        }

        private bool IsAnyIntroInputPressed()
        {
            RefreshActionsIfNeeded();
            if ((_submitAction != null && _submitAction.WasPressedThisFrame())
                || (_cancelAction != null && _cancelAction.WasPressedThisFrame()))
            {
                return true;
            }

            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null
                && (Mouse.current.leftButton.wasPressedThisFrame
                    || Mouse.current.rightButton.wasPressedThisFrame
                    || Mouse.current.middleButton.wasPressedThisFrame))
            {
                return true;
            }

            return false;
        }

        private bool AreAnyIntroInputsHeld()
        {
            RefreshActionsIfNeeded();
            if ((_submitAction != null && _submitAction.IsPressed())
                || (_cancelAction != null && _cancelAction.IsPressed()))
            {
                return true;
            }

            if (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
            {
                return true;
            }

            if (Mouse.current != null
                && (Mouse.current.leftButton.isPressed
                    || Mouse.current.rightButton.isPressed
                    || Mouse.current.middleButton.isPressed))
            {
                return true;
            }

            return false;
        }

        private void ConfigureSkipButton(Button skipIntroButton)
        {
            if (_skipIntroButton != null)
            {
                _skipIntroButton.onClick.RemoveListener(OnSkipIntroPressed);
            }

            _skipIntroButton = skipIntroButton;
            if (_skipIntroButton != null)
            {
                _skipIntroButton.onClick.AddListener(OnSkipIntroPressed);
            }
        }

        private void OnSkipIntroPressed()
        {
            if (Time.unscaledTime < _skipEnabledAt)
            {
                return;
            }

            BeginTransitionToMainMenu();
        }

        private void BeginTransitionToMainMenu()
        {
            if (_advanced)
            {
                return;
            }

            _advanced = true;
            SetSkipButtonVisible(false);

            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
            }

            _transitionRoutine = StartCoroutine(RunTitleCardTransition());
        }

        private IEnumerator RunTitleCardTransition()
        {
            if (_titleCardOverlay != null)
            {
                _titleCardOverlay.gameObject.SetActive(true);
                _titleCardOverlay.blocksRaycasts = true;
                _titleCardOverlay.interactable = false;
                _titleCardOverlay.alpha = 0f;

                if (_titleCardText != null)
                {
                    _titleCardText.text = _titleCardLabel ?? string.Empty;
                }

                var fadeDuration = Mathf.Max(0.05f, _titleFadeToBlackSeconds);
                var elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _titleCardOverlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                    yield return null;
                }

                _titleCardOverlay.alpha = 1f;

                var holdSeconds = Mathf.Max(0f, _titleHoldSeconds);
                if (holdSeconds > 0f)
                {
                    yield return new WaitForSecondsRealtime(holdSeconds);
                }
            }

            if (_orchestrator != null)
            {
                _orchestrator.RequestCompleteIntroFlow();
            }
            else
            {
                _gameFlowManager?.SetState(GameFlowState.MainMenu);
            }

            _transitionRoutine = null;
        }

        private void ResetTitleCardOverlayVisual()
        {
            if (_titleCardText != null)
            {
                _titleCardText.text = string.Empty;
            }

            if (_titleCardOverlay == null)
            {
                return;
            }

            _titleCardOverlay.alpha = 0f;
            _titleCardOverlay.blocksRaycasts = false;
            _titleCardOverlay.interactable = false;
            _titleCardOverlay.gameObject.SetActive(false);
        }

        private void SetSkipButtonVisible(bool visible)
        {
            if (_skipIntroButton == null)
            {
                return;
            }

            _skipIntroButton.gameObject.SetActive(visible);
        }

        private void SetSkipButtonInteractable(bool interactable)
        {
            if (_skipIntroButton == null)
            {
                return;
            }

            if (_skipIntroButton.interactable == interactable)
            {
                return;
            }

            _skipIntroButton.interactable = interactable;
        }
    }
}
