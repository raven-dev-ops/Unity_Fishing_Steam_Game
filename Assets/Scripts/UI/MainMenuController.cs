using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _startButton;
        [SerializeField] private GameObject _profileButton;
        [SerializeField] private GameObject _settingsButton;
        [SerializeField] private GameObject _exitButton;

        [SerializeField] private GameObject _profilePanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameFlowOrchestrator _orchestrator;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            SetSelected(_startButton);
            SetPanel(_profilePanel, false);
            SetPanel(_settingsPanel, false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame)
            {
                SubmitCurrentSelection();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetPanel(_profilePanel, false);
                SetPanel(_settingsPanel, false);
                SetSelected(_startButton);
            }
        }

        public void SubmitCurrentSelection()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == _startButton)
            {
                StartGame();
                return;
            }

            if (selected == _profileButton)
            {
                OpenProfile();
                return;
            }

            if (selected == _settingsButton)
            {
                OpenSettings();
                return;
            }

            if (selected == _exitButton)
            {
                ExitGame();
            }
        }

        public void StartGame()
        {
            _orchestrator?.RequestStartGame();
        }

        public void OpenProfile()
        {
            SetPanel(_profilePanel, true);
            SetPanel(_settingsPanel, false);
        }

        public void OpenSettings()
        {
            SetPanel(_settingsPanel, true);
            SetPanel(_profilePanel, false);
        }

        public void ExitGame()
        {
            _orchestrator?.RequestExitGame();
        }

        private static void SetPanel(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        private static void SetSelected(GameObject target)
        {
            if (EventSystem.current == null || target == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
