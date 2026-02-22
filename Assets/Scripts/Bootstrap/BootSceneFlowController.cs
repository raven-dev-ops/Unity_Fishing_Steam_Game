using RavenDevOps.Fishing.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Core
{
    public sealed class BootSceneFlowController : MonoBehaviour
    {
        [SerializeField, Min(0.25f)] private float _autoAdvanceSeconds = 2.25f;
        [SerializeField] private Text _statusText;
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private InputContextRouter _inputContextRouter;
        [SerializeField] private InputActionMapController _inputMapController;

        private InputAction _submitAction;
        private float _startedAtTime;
        private bool _advanced;

        public void Configure(Text statusText)
        {
            _statusText = statusText;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputContextRouter, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            _advanced = false;
            _startedAtTime = Time.unscaledTime;

            _inputContextRouter?.SetContext(InputContext.UI);
            _inputMapController?.ApplyContext(InputContext.UI);
            SetStatus("Boot: Press Enter to continue.");
        }

        private void Update()
        {
            if (_advanced)
            {
                return;
            }

            RefreshActionIfNeeded();
            if (_submitAction != null && _submitAction.WasPressedThisFrame())
            {
                AdvanceToCinematic();
                return;
            }

            if (Time.unscaledTime - _startedAtTime >= _autoAdvanceSeconds)
            {
                AdvanceToCinematic();
            }
        }

        private void RefreshActionIfNeeded()
        {
            if (_submitAction != null)
            {
                return;
            }

            _submitAction = _inputMapController != null
                ? _inputMapController.FindAction("UI/Submit")
                : null;
        }

        private void AdvanceToCinematic()
        {
            if (_advanced)
            {
                return;
            }

            _advanced = true;
            SetStatus("Loading cinematic...");
            _gameFlowManager?.SetState(GameFlowState.Cinematic);
        }

        private void SetStatus(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text ?? string.Empty;
            }
        }
    }
}
