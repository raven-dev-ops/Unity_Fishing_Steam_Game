using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class ShipMovementController : MonoBehaviour
    {
        [SerializeField] private float _fallbackSpeed = 6f;
        [SerializeField] private Vector2 _xBounds = new Vector2(-9f, 9f);
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private float _speedMultiplier = 1f;
        [SerializeField] private float _inputDeadzone = 0.12f;
        [SerializeField] private float _axisSmoothing = 10f;

        public int DistanceTierCap { get; private set; } = 1;
        public int CurrentDistanceTier => ResolveDistanceTier(transform.position.x);
        private InputAction _moveShipAction;
        private float _smoothedAxis;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            RefreshShipStats();
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public void RefreshShipStats()
        {
            DistanceTierCap = 1;
            if (_saveManager == null || _catalogService == null)
            {
                return;
            }

            var equippedId = _saveManager.Current.equippedShipId;
            if (_catalogService.TryGetShip(equippedId, out var shipDefinition))
            {
                DistanceTierCap = Mathf.Max(1, shipDefinition.maxDistanceTier);
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public int ResolveDistanceTier(float xPosition)
        {
            var tierCap = Mathf.Max(1, DistanceTierCap);
            if (tierCap <= 1)
            {
                return 1;
            }

            var minX = Mathf.Min(_xBounds.x, _xBounds.y);
            var maxX = Mathf.Max(_xBounds.x, _xBounds.y);
            if (Mathf.Approximately(minX, maxX))
            {
                return 1;
            }

            var normalized = Mathf.Clamp01(Mathf.InverseLerp(minX, maxX, xPosition));
            var tierIndex = Mathf.FloorToInt(normalized * tierCap);
            return Mathf.Clamp(tierIndex + 1, 1, tierCap);
        }

        private void Update()
        {
            RefreshActionsIfNeeded();

            var speed = ResolveMoveSpeed();
            var mappedAxis = _moveShipAction != null
                ? Mathf.Clamp(_moveShipAction.ReadValue<float>(), -1f, 1f)
                : 0f;
            var keyboardAxis = ResolveKeyboardShipAxis();
            var rawAxis = Mathf.Abs(keyboardAxis) > Mathf.Abs(mappedAxis)
                ? keyboardAxis
                : mappedAxis;
            if (Mathf.Abs(rawAxis) < Mathf.Clamp01(_inputDeadzone))
            {
                rawAxis = 0f;
            }

            _smoothedAxis = Mathf.MoveTowards(_smoothedAxis, rawAxis, Mathf.Max(0.01f, _axisSmoothing) * Time.deltaTime);

            var p = transform.position;
            p.x += _smoothedAxis * speed * _speedMultiplier * ResolveInputSensitivity() * Time.deltaTime;
            p.x = Mathf.Clamp(p.x, Mathf.Min(_xBounds.x, _xBounds.y), Mathf.Max(_xBounds.x, _xBounds.y));
            transform.position = p;
        }

        private float ResolveMoveSpeed()
        {
            if (_saveManager == null || _catalogService == null)
            {
                return _fallbackSpeed;
            }

            var equippedId = _saveManager.Current.equippedShipId;
            if (_catalogService.TryGetShip(equippedId, out var shipDefinition))
            {
                return Mathf.Max(0.1f, shipDefinition.moveSpeed);
            }

            return _fallbackSpeed;
        }

        private float ResolveInputSensitivity()
        {
            return _settingsService != null ? _settingsService.InputSensitivity : 1f;
        }

        private void RefreshActionsIfNeeded()
        {
            if (_moveShipAction != null)
            {
                return;
            }

            _moveShipAction = _inputMapController != null
                ? _inputMapController.FindAction("Fishing/MoveShip")
                : null;
        }

        private static float ResolveKeyboardShipAxis()
        {
            var axis = 0f;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
                {
                    axis -= 1f;
                }

                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
                {
                    axis += 1f;
                }
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Mathf.Abs(axis) < 0.01f)
            {
                if (UnityEngine.Input.GetKey(KeyCode.LeftArrow) || UnityEngine.Input.GetKey(KeyCode.A))
                {
                    axis -= 1f;
                }

                if (UnityEngine.Input.GetKey(KeyCode.RightArrow) || UnityEngine.Input.GetKey(KeyCode.D))
                {
                    axis += 1f;
                }
            }
#endif

            return Mathf.Clamp(axis, -1f, 1f);
        }
    }
}
