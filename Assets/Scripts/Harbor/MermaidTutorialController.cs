using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Harbor
{
    public sealed class MermaidTutorialController : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private DialogueBubbleController _dialogue;
        [SerializeField] private InputActionMapController _inputMapController;

        [SerializeField] private bool _isBlockingInteractions;
        private InputAction _cancelAction;

        public bool IsBlockingInteractions => _isBlockingInteractions;

        public void Configure(DialogueBubbleController dialogue)
        {
            _dialogue = dialogue;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
        }

        private void Start()
        {
            if (_saveManager == null)
            {
                return;
            }

            if (_saveManager.Current.tutorialFlags.tutorialSeen)
            {
                _isBlockingInteractions = false;
                return;
            }

            _isBlockingInteractions = true;
            _dialogue?.Play();
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
    }
}
