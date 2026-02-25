using RavenDevOps.Fishing.Audio;
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
        [SerializeField] private GameObject _exitPanel;
        [SerializeField] private GameObject _exitConfirmButton;
        [SerializeField] private GameObject _exitCancelButton;
        [SerializeField] private GameObject _profileDefaultSelection;
        [SerializeField] private GameObject _settingsDefaultSelection;
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private InputActionMapController _inputMapController;

        private InputAction _submitAction;
        private InputAction _cancelAction;

        public void Configure(
            GameObject startButton,
            GameObject profileButton,
            GameObject settingsButton,
            GameObject exitButton,
            GameObject profilePanel,
            GameObject settingsPanel,
            GameObject exitPanel = null,
            GameObject exitConfirmButton = null,
            GameObject exitCancelButton = null,
            GameObject profileDefaultSelection = null,
            GameObject settingsDefaultSelection = null)
        {
            _startButton = startButton;
            _profileButton = profileButton;
            _settingsButton = settingsButton;
            _exitButton = exitButton;
            _profilePanel = profilePanel;
            _settingsPanel = settingsPanel;
            _exitPanel = exitPanel;
            _exitConfirmButton = exitConfirmButton;
            _exitCancelButton = exitCancelButton;
            _profileDefaultSelection = profileDefaultSelection;
            _settingsDefaultSelection = settingsDefaultSelection;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            SetSelected(_startButton);
            HideSubmenus();
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
                PlaySfx(SfxEvent.UiCancel);
                HideSubmenus();
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
                OpenExitPanel();
                return;
            }

            if (selected == _exitConfirmButton)
            {
                ConfirmExit();
                return;
            }

            if (selected == _exitCancelButton)
            {
                CancelExit();
            }
        }

        public void StartGame()
        {
            PlaySfx(SfxEvent.UiSelect);
            _orchestrator?.RequestStartGame();
        }

        public void OpenProfile()
        {
            PlaySfx(SfxEvent.UiSelect);
            ShowSingleSubmenu(_profilePanel);
            SetSelected(_profileDefaultSelection != null ? _profileDefaultSelection : _profileButton);
        }

        public void OpenSettings()
        {
            PlaySfx(SfxEvent.UiSelect);
            ShowSingleSubmenu(_settingsPanel);
            SetSelected(_settingsDefaultSelection != null ? _settingsDefaultSelection : _settingsButton);
        }

        public void OpenExitPanel()
        {
            PlaySfx(SfxEvent.UiSelect);
            ShowSingleSubmenu(_exitPanel);
            if (_exitCancelButton != null)
            {
                SetSelected(_exitCancelButton);
            }
        }

        public void ExitGame()
        {
            ConfirmExit();
        }

        public void ConfirmExit()
        {
            PlaySfx(SfxEvent.UiSelect);
            _orchestrator?.RequestExitGame();
        }

        public void CancelExit()
        {
            PlaySfx(SfxEvent.UiCancel);
            HideSubmenus();
            SetSelected(_exitButton);
        }

        public void CloseProfilePanel()
        {
            PlaySfx(SfxEvent.UiCancel);
            HideSubmenus();
            SetSelected(_profileButton);
        }

        public void CloseSettingsPanel()
        {
            PlaySfx(SfxEvent.UiCancel);
            HideSubmenus();
            SetSelected(_settingsButton);
        }

        private void HideSubmenus()
        {
            SetPanel(_profilePanel, false);
            SetPanel(_settingsPanel, false);
            SetPanel(_exitPanel, false);
        }

        private void ShowSingleSubmenu(GameObject panel)
        {
            HideSubmenus();
            SetPanel(panel, true);
        }

        private static void SetPanel(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
                if (active)
                {
                    panel.transform.SetAsLastSibling();
                }
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

        private static void PlaySfx(SfxEvent eventType)
        {
            RuntimeServiceRegistry.Get<SfxTriggerRouter>()?.Play(eventType);
        }
    }
}
