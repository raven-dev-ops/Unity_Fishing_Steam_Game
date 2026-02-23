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
        private const float FallbackMinDepthTier1 = 0f;
        private const float FallbackMinDepthTier2 = 0f;
        private const float FallbackMinDepthTier3 = 0f;
        private const float FallbackMinDepthTier4 = 1000f;
        private const float FallbackMinDepthTier5 = 3000f;
        private const float FallbackMaxDepthTier1 = 400f;
        private const float FallbackMaxDepthTier2 = 700f;
        private const float FallbackMaxDepthTier3 = 1600f;
        private const float FallbackMaxDepthTier4 = 3000f;
        private const float FallbackMaxDepthTier5 = 4000f;

        [SerializeField] private float _fallbackSpeed = 6f;
        [SerializeField] private Vector2 _xBounds = new Vector2(-9f, 9f);
        [SerializeField] private bool _clampHorizontalPosition = false;
        [SerializeField] private bool _useTravelDistanceForTier = true;
        [SerializeField] private float _distanceUnitsPerTier = 14f;
        [SerializeField] private bool _disableSteeringWhileHookCast = true;
        [SerializeField] private bool _allowSteeringWhileHookDown = true;
        [SerializeField] private FishingActionStateMachine _fishingActionStateMachine;
        [SerializeField] private int _fallbackCargoCapacityTier1 = 12;
        [SerializeField] private int _fallbackCargoCapacityTier2 = 20;
        [SerializeField] private int _fallbackCargoCapacityTier3 = 32;
        [SerializeField] private int _fallbackCargoCapacityTier4 = 48;
        [SerializeField] private int _fallbackCargoCapacityTier5 = 72;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private float _speedMultiplier = 1f;
        [SerializeField] private float _inputDeadzone = 0.12f;
        [SerializeField] private float _axisSmoothing = 10f;

        public int DistanceTierCap { get; private set; } = 1;
        public int CargoCapacity { get; private set; } = 12;
        public float MinOperationalDepthMeters { get; private set; } = FallbackMinDepthTier1;
        public float MaxOperationalDepthMeters { get; private set; } = FallbackMaxDepthTier1;
        public int CurrentDistanceTier => ResolveDistanceTier(transform.position.x);
        public float DistanceTraveledUnits => _distanceTraveledUnits;
        public bool SteeringLockedForFishingState => IsSteeringLockedByFishingState();
        public float CurrentHorizontalVelocity => _currentHorizontalVelocity;
        private InputAction _moveShipAction;
        private float _smoothedAxis;
        private float _distanceTraveledUnits;
        private float _currentHorizontalVelocity;

        private void Awake()
        {
            // Keep horizontal sailing unconstrained during fishing.
            _clampHorizontalPosition = false;
            // Guarantee steering remains available while hook is down.
            _allowSteeringWhileHookDown = true;

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
            CargoCapacity = Mathf.Max(1, _fallbackCargoCapacityTier1);
            MinOperationalDepthMeters = FallbackMinDepthTier1;
            MaxOperationalDepthMeters = FallbackMaxDepthTier1;
            if (_saveManager == null || _saveManager.Current == null)
            {
                return;
            }

            var equippedId = _saveManager.Current.equippedShipId;
            if (_catalogService != null && _catalogService.TryGetShip(equippedId, out var shipDefinition))
            {
                DistanceTierCap = Mathf.Max(1, shipDefinition.maxDistanceTier);
                CargoCapacity = shipDefinition.cargoCapacity > 0
                    ? shipDefinition.cargoCapacity
                    : ResolveFallbackCargoCapacity(equippedId);
                var minDepth = Mathf.Max(0f, shipDefinition.minDepthMeters);
                var maxDepth = Mathf.Max(minDepth + 0.1f, shipDefinition.maxDepthMeters);
                MinOperationalDepthMeters = minDepth;
                MaxOperationalDepthMeters = maxDepth;
                return;
            }

            CargoCapacity = ResolveFallbackCargoCapacity(equippedId);
            ResolveFallbackDepthBand(equippedId, out var fallbackMinDepth, out var fallbackMaxDepth);
            MinOperationalDepthMeters = fallbackMinDepth;
            MaxOperationalDepthMeters = fallbackMaxDepth;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void ConfigureFishingStateMachine(FishingActionStateMachine fishingActionStateMachine)
        {
            _fishingActionStateMachine = fishingActionStateMachine;
        }

        public int ResolveDistanceTier(float xPosition)
        {
            var tierCap = Mathf.Max(1, DistanceTierCap);
            if (tierCap <= 1)
            {
                return 1;
            }

            if (_useTravelDistanceForTier)
            {
                var unitsPerTier = Mathf.Max(0.5f, _distanceUnitsPerTier);
                var tierByDistance = 1 + Mathf.FloorToInt(Mathf.Max(0f, _distanceTraveledUnits) / unitsPerTier);
                return Mathf.Clamp(tierByDistance, 1, tierCap);
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

        public bool IsDepthWithinOperationalBand(float depthMeters)
        {
            var depth = Mathf.Max(0f, depthMeters);
            var minDepth = Mathf.Max(0f, MinOperationalDepthMeters);
            var maxDepth = Mathf.Max(minDepth + 0.1f, MaxOperationalDepthMeters);
            return depth >= minDepth && depth <= maxDepth;
        }

        public void GetOperationalDepthBand(out float minDepthMeters, out float maxDepthMeters)
        {
            minDepthMeters = Mathf.Max(0f, MinOperationalDepthMeters);
            maxDepthMeters = Mathf.Max(minDepthMeters + 0.1f, MaxOperationalDepthMeters);
        }

        private void Update()
        {
            RefreshActionsIfNeeded();
            if (IsSteeringLockedByFishingState())
            {
                _smoothedAxis = 0f;
                _currentHorizontalVelocity = 0f;
                return;
            }

            var speed = ResolveMoveSpeed();
            var mappedAxis = _moveShipAction != null
                ? Mathf.Clamp(_moveShipAction.ReadValue<float>(), -1f, 1f)
                : 0f;
            var keyboardAxis = ResolveKeyboardShipAxis();
            var gamepadAxis = ResolveGamepadShipAxis();
            var rawAxis = mappedAxis;
            if (Mathf.Abs(keyboardAxis) > Mathf.Abs(rawAxis))
            {
                rawAxis = keyboardAxis;
            }

            if (Mathf.Abs(gamepadAxis) > Mathf.Abs(rawAxis))
            {
                rawAxis = gamepadAxis;
            }

            if (Mathf.Abs(rawAxis) < Mathf.Clamp01(_inputDeadzone))
            {
                rawAxis = 0f;
            }

            _smoothedAxis = Mathf.MoveTowards(_smoothedAxis, rawAxis, Mathf.Max(0.01f, _axisSmoothing) * Time.deltaTime);

            var p = transform.position;
            var previousX = p.x;
            p.x += _smoothedAxis * speed * _speedMultiplier * ResolveInputSensitivity() * Time.deltaTime;
            if (_clampHorizontalPosition)
            {
                p.x = Mathf.Clamp(p.x, Mathf.Min(_xBounds.x, _xBounds.y), Mathf.Max(_xBounds.x, _xBounds.y));
            }

            _currentHorizontalVelocity = (p.x - previousX) / Mathf.Max(0.0001f, Time.deltaTime);
            transform.position = p;
            TrackTravelDistance(previousX, p.x);
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

        private static float ResolveGamepadShipAxis()
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return 0f;
            }

            var leftStickX = gamepad.leftStick.x.ReadValue();
            var dpadX = gamepad.dpad.x.ReadValue();
            var axis = Mathf.Abs(dpadX) > Mathf.Abs(leftStickX) ? dpadX : leftStickX;
            return Mathf.Clamp(axis, -1f, 1f);
        }

        private void TrackTravelDistance(float previousX, float currentX)
        {
            if (!IsFinite(previousX) || !IsFinite(currentX))
            {
                return;
            }

            var delta = Mathf.Abs(currentX - previousX);
            if (delta <= 0f)
            {
                return;
            }

            if (delta > 25f)
            {
                return;
            }

            _distanceTraveledUnits += delta;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private bool IsSteeringLockedByFishingState()
        {
            if (!_disableSteeringWhileHookCast)
            {
                return false;
            }

            ResolveFishingStateMachine();
            if (_fishingActionStateMachine == null)
            {
                return false;
            }

            switch (_fishingActionStateMachine.State)
            {
                case FishingActionState.InWater:
                case FishingActionState.Hooked:
                case FishingActionState.Reel:
                    return !_allowSteeringWhileHookDown;
                default:
                    return false;
            }
        }

        private void ResolveFishingStateMachine()
        {
            if (_fishingActionStateMachine != null)
            {
                return;
            }

            RuntimeServiceRegistry.TryGet(out _fishingActionStateMachine);
            if (_fishingActionStateMachine != null)
            {
                return;
            }

            _fishingActionStateMachine = FindAnyObjectByType<FishingActionStateMachine>(FindObjectsInactive.Include);
        }

        private int ResolveFallbackCargoCapacity(string shipId)
        {
            var id = string.IsNullOrWhiteSpace(shipId)
                ? string.Empty
                : shipId.Trim().ToLowerInvariant();

            if (id.Contains("lv5"))
            {
                return Mathf.Max(1, _fallbackCargoCapacityTier5);
            }

            if (id.Contains("lv4"))
            {
                return Mathf.Max(1, _fallbackCargoCapacityTier4);
            }

            if (id.Contains("lv3"))
            {
                return Mathf.Max(1, _fallbackCargoCapacityTier3);
            }

            if (id.Contains("lv2"))
            {
                return Mathf.Max(1, _fallbackCargoCapacityTier2);
            }

            return Mathf.Max(1, _fallbackCargoCapacityTier1);
        }

        private static void ResolveFallbackDepthBand(string shipId, out float minDepthMeters, out float maxDepthMeters)
        {
            var id = string.IsNullOrWhiteSpace(shipId)
                ? string.Empty
                : shipId.Trim().ToLowerInvariant();

            if (id.Contains("lv5"))
            {
                minDepthMeters = FallbackMinDepthTier5;
                maxDepthMeters = FallbackMaxDepthTier5;
                return;
            }

            if (id.Contains("lv4"))
            {
                minDepthMeters = FallbackMinDepthTier4;
                maxDepthMeters = FallbackMaxDepthTier4;
                return;
            }

            if (id.Contains("lv3"))
            {
                minDepthMeters = FallbackMinDepthTier3;
                maxDepthMeters = FallbackMaxDepthTier3;
                return;
            }

            if (id.Contains("lv2"))
            {
                minDepthMeters = FallbackMinDepthTier2;
                maxDepthMeters = FallbackMaxDepthTier2;
                return;
            }

            minDepthMeters = FallbackMinDepthTier1;
            maxDepthMeters = FallbackMaxDepthTier1;
        }
    }
}
