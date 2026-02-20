using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
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
        [SerializeField] private InputActionMapController _inputMapController;

        private InputAction _submitAction;
        private InputAction _cancelAction;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            SetSelected(_startButton);
            SetPanel(_profilePanel, false);
            SetPanel(_settingsPanel, false);
        }

        private void Update()
        {
            RefreshActionsIfNeeded();

            if (_submitAction != null && _submitAction.WasPressedThisFrame())
            {
                SubmitCurrentSelection();
            }

            if (_cancelAction != null && _cancelAction.WasPressedThisFrame())
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
    }
}
