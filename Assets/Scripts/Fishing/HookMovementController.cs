using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class HookMovementController : MonoBehaviour
    {
        [SerializeField] private Transform _shipTransform;
        [SerializeField] private float _verticalSpeed = 4f;
        [SerializeField] private Vector2 _depthBounds = new Vector2(-8f, -1f);
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private float _speedMultiplier = 1f;
        [SerializeField] private float _inputDeadzone = 0.12f;
        [SerializeField] private float _axisSmoothing = 10f;
        [SerializeField] private bool _movementEnabled = true;

        public float MaxDepth
        {
            get => Mathf.Abs(_depthBounds.x);
            set => _depthBounds.x = -Mathf.Abs(value);
        }

        public float CurrentDepth => Mathf.Abs(transform.position.y);
        public Transform ShipTransform => _shipTransform;
        private InputAction _moveHookAction;
        private float _smoothedAxis;

        public void ConfigureShipTransform(Transform shipTransform)
        {
            _shipTransform = shipTransform;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            RefreshHookStats();
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public void RefreshHookStats()
        {
            if (_saveManager == null || _catalogService == null)
            {
                return;
            }

            var equippedId = _saveManager.Current.equippedHookId;
            if (_catalogService.TryGetHook(equippedId, out var hookDefinition))
            {
                MaxDepth = hookDefinition.maxDepth;
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;
            if (!enabled)
            {
                _smoothedAxis = 0f;
            }
        }

        private void LateUpdate()
        {
            if (_shipTransform != null)
            {
                var p = transform.position;
                p.x = _shipTransform.position.x;
                transform.position = p;
            }
        }

        private void Update()
        {
            RefreshActionsIfNeeded();
            if (!_movementEnabled)
            {
                return;
            }

            if (_moveHookAction == null)
            {
                return;
            }

            var rawAxis = Mathf.Clamp(_moveHookAction.ReadValue<float>(), -1f, 1f);
            if (Mathf.Abs(rawAxis) < Mathf.Clamp01(_inputDeadzone))
            {
                rawAxis = 0f;
            }

            _smoothedAxis = Mathf.MoveTowards(_smoothedAxis, rawAxis, Mathf.Max(0.01f, _axisSmoothing) * Time.deltaTime);

            var p = transform.position;
            p.y += _smoothedAxis * _verticalSpeed * _speedMultiplier * ResolveInputSensitivity() * Time.deltaTime;
            p.y = Mathf.Clamp(p.y, Mathf.Min(_depthBounds.x, _depthBounds.y), Mathf.Max(_depthBounds.x, _depthBounds.y));
            transform.position = p;
        }

        private float ResolveInputSensitivity()
        {
            return _settingsService != null ? _settingsService.InputSensitivity : 1f;
        }

        private void RefreshActionsIfNeeded()
        {
            if (_moveHookAction != null)
            {
                return;
            }

            _moveHookAction = _inputMapController != null
                ? _inputMapController.FindAction("Fishing/MoveHook")
                : null;
        }
    }
}
