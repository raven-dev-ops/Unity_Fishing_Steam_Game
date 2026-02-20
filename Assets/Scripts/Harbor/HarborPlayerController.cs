using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Harbor
{
    public sealed class HarborPlayerController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private Vector2 _xBounds = new Vector2(-8f, 8f);
        [SerializeField] private Vector2 _zBounds = new Vector2(-4f, 4f);
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private InputActionMapController _inputMapController;

        private InputAction _moveAction;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
        }

        private void Update()
        {
            RefreshMoveActionIfNeeded();
            if (_moveAction == null)
            {
                return;
            }

            var moveInput = _moveAction.ReadValue<Vector2>();
            var move = new Vector3(moveInput.x, 0f, moveInput.y);

            var sensitivity = _settingsService != null ? _settingsService.InputSensitivity : 1f;
            move = move.normalized * (_moveSpeed * sensitivity * Time.deltaTime);
            transform.position += move;

            var p = transform.position;
            p.x = Mathf.Clamp(p.x, Mathf.Min(_xBounds.x, _xBounds.y), Mathf.Max(_xBounds.x, _xBounds.y));
            p.z = Mathf.Clamp(p.z, Mathf.Min(_zBounds.x, _zBounds.y), Mathf.Max(_zBounds.x, _zBounds.y));
            transform.position = p;
        }

        private void RefreshMoveActionIfNeeded()
        {
            if (_moveAction != null)
            {
                return;
            }

            _moveAction = _inputMapController != null
                ? _inputMapController.FindAction("Harbor/Move")
                : null;
        }
    }
}
