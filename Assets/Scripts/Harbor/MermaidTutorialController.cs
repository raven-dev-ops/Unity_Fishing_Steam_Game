using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Harbor
{
    public sealed class MermaidTutorialController : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private DialogueBubbleController _dialogue;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private Button _skipIntroButton;

        [SerializeField] private bool _isBlockingInteractions;
        private InputAction _cancelAction;

        public bool IsBlockingInteractions => _isBlockingInteractions;

        public void Configure(DialogueBubbleController dialogue, Button skipIntroButton = null)
        {
            _dialogue = dialogue;
            ConfigureSkipButton(skipIntroButton);
            EvaluateTutorialState();
        }

        public void ConfigureSkipButton(Button skipIntroButton)
        {
            if (_skipIntroButton != null)
            {
                _skipIntroButton.onClick.RemoveListener(CompleteTutorial);
            }

            _skipIntroButton = skipIntroButton;
            if (_skipIntroButton != null)
            {
                _skipIntroButton.onClick.AddListener(CompleteTutorial);
            }

            UpdateSkipButtonVisibility();
        }

        private void Awake()
        {
            EnsureDependencies();
        }

        private void Start()
        {
            EvaluateTutorialState();
        }

        private void OnEnable()
        {
            EnsureDependencies();
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged += OnSaveDataChanged;
            }

            if (_skipIntroButton != null)
            {
                _skipIntroButton.onClick.RemoveListener(CompleteTutorial);
                _skipIntroButton.onClick.AddListener(CompleteTutorial);
            }

            EvaluateTutorialState();
        }

        private void OnDisable()
        {
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged -= OnSaveDataChanged;
            }

            if (_skipIntroButton != null)
            {
                _skipIntroButton.onClick.RemoveListener(CompleteTutorial);
            }
        }

        private void Update()
        {
            if (!_isBlockingInteractions)
            {
                return;
            }

            RefreshActionsIfNeeded();
            if (_cancelAction != null && _cancelAction.WasPressedThisFrame())
            {
                CompleteTutorial();
                return;
            }

            if (_dialogue != null && !_dialogue.IsRunning)
            {
                CompleteTutorial();
            }
        }

        public void CompleteTutorial()
        {
            _saveManager?.SetTutorialSeen(true);

            _isBlockingInteractions = false;
            _dialogue?.Stop();
            UpdateSkipButtonVisibility();
        }

        private void OnSaveDataChanged(SaveDataV1 _)
        {
            EvaluateTutorialState();
        }

        private void EvaluateTutorialState()
        {
            EnsureDependencies();
            if (_saveManager == null || _saveManager.Current == null)
            {
                return;
            }

            var shouldRunTutorial = _saveManager.ShouldRunIntroTutorial();
            if (!shouldRunTutorial)
            {
                _isBlockingInteractions = false;
                UpdateSkipButtonVisibility();
                return;
            }

            BeginTutorialIfNeeded();
        }

        private void BeginTutorialIfNeeded()
        {
            if (_dialogue == null)
            {
                _saveManager?.SetTutorialSeen(true);
                _isBlockingInteractions = false;
                UpdateSkipButtonVisibility();
                return;
            }

            _isBlockingInteractions = true;
            _saveManager?.MarkIntroTutorialStarted();
            if (!_dialogue.IsRunning)
            {
                _dialogue.Play();
            }

            UpdateSkipButtonVisibility();
        }

        private void RefreshActionsIfNeeded()
        {
            if (_cancelAction != null)
            {
                return;
            }

            _cancelAction = _inputMapController != null
                ? _inputMapController.FindAction("Harbor/Pause")
                : null;
        }

        private void EnsureDependencies()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
        }

        private void UpdateSkipButtonVisibility()
        {
            if (_skipIntroButton == null)
            {
                return;
            }

            _skipIntroButton.gameObject.SetActive(_isBlockingInteractions);
        }
    }
}
