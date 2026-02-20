using RavenDevOps.Fishing.Core;
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

        [SerializeField] private bool _isBlockingInteractions;

        public bool IsBlockingInteractions => _isBlockingInteractions;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
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

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
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
    }
}
