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
        [SerializeField] private Vector2 _depthBounds = new Vector2(-4000f, -1f);
        [SerializeField] private float _absoluteMaxDepthMeters = 4000f;
        [SerializeField] private float _distanceTierDepthStep = 0.5f;
        [SerializeField] private int _distanceTier = 1;
        [SerializeField] private float _minimumOperationalMaxDepth = 0f;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private float _speedMultiplier = 1f;
        [SerializeField] private float _inputDeadzone = 0.12f;
        [SerializeField] private float _axisSmoothing = 10f;
        [SerializeField] private bool _movementEnabled = true;
        [SerializeField] private SpriteSwayMotion2D _swayMotion;
        [SerializeField] private bool _enableHorizontalLag = true;
        [SerializeField] private float _horizontalLagPerVelocitySeconds = 0.5f;
        [SerializeField] private float _maxHorizontalLagDistance = 6f;
        [SerializeField] private float _horizontalLagSmoothing = 5f;
        [SerializeField] private float _horizontalFollowSmoothing = 8f;

        public float MaxDepth
        {
            get => Mathf.Abs(_depthBounds.x);
            set
            {
                var maxDepthCap = Mathf.Max(0.5f, _absoluteMaxDepthMeters);
                _baseMaxDepth = Mathf.Clamp(Mathf.Abs(value), 0.5f, maxDepthCap);
                ApplyDepthBoundsFromTier();
            }
        }

        public float CurrentDepth
        {
            get
            {
                var surfaceY = ResolveSurfaceY();
                return Mathf.Max(0f, surfaceY - transform.position.y);
            }
        }
        public Transform ShipTransform => _shipTransform;
        private InputAction _moveHookAction;
        private float _smoothedAxis;
        private float _baseMaxDepth;
        private float _minDepthBelowSurface;
        private ShipMovementController _shipMovement;
        private bool _hasRecordedShipXForLag;
        private float _lastShipXForLag;
        private float _horizontalLagOffset;

        public void ConfigureShipTransform(Transform shipTransform)
        {
            _shipTransform = shipTransform;
            ResetHorizontalLagState();
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            _absoluteMaxDepthMeters = Mathf.Max(0.5f, _absoluteMaxDepthMeters);
            _minimumOperationalMaxDepth = Mathf.Clamp(_minimumOperationalMaxDepth, 0f, _absoluteMaxDepthMeters);
            _swayMotion ??= GetComponent<SpriteSwayMotion2D>();
            if (_swayMotion != null)
            {
                _swayMotion.enabled = false;
            }

            _minDepthBelowSurface = Mathf.Min(Mathf.Abs(_depthBounds.x), Mathf.Abs(_depthBounds.y));
            _baseMaxDepth = Mathf.Max(Mathf.Abs(_depthBounds.x), Mathf.Abs(_depthBounds.y));
            _distanceTier = Mathf.Max(1, _distanceTier);
            RefreshHookStats();
            ResetHorizontalLagState();
        }

        private void OnValidate()
        {
            _absoluteMaxDepthMeters = Mathf.Max(0.5f, _absoluteMaxDepthMeters);
            _minimumOperationalMaxDepth = Mathf.Clamp(_minimumOperationalMaxDepth, 0f, _absoluteMaxDepthMeters);
            _horizontalLagPerVelocitySeconds = Mathf.Max(0f, _horizontalLagPerVelocitySeconds);
            _maxHorizontalLagDistance = Mathf.Max(0f, _maxHorizontalLagDistance);
            _horizontalLagSmoothing = Mathf.Max(0.1f, _horizontalLagSmoothing);
            _horizontalFollowSmoothing = Mathf.Max(0.1f, _horizontalFollowSmoothing);
        }

        private void OnEnable()
        {
            if (_swayMotion != null)
            {
                _swayMotion.enabled = false;
            }
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public void RefreshHookStats()
        {
            var maxDepthCap = Mathf.Max(0.5f, _absoluteMaxDepthMeters);
            var resolvedMaxDepth = Mathf.Clamp(_baseMaxDepth, 0.5f, maxDepthCap);
            if (_saveManager == null || _saveManager.Current == null || _catalogService == null)
            {
                _baseMaxDepth = resolvedMaxDepth;
                ApplyDepthBoundsFromTier();
                return;
            }

            var save = _saveManager.Current;
            var equippedShipId = save.equippedShipId;
            if (_catalogService.TryGetShip(equippedShipId, out var shipDefinition))
            {
                // Ship depth is authoritative; hooks should descend to the active ship's limit.
                resolvedMaxDepth = Mathf.Clamp(shipDefinition.maxDepthMeters, 0.5f, maxDepthCap);
            }
            else
            {
                var equippedHookId = save.equippedHookId;
                if (_catalogService.TryGetHook(equippedHookId, out var hookDefinition))
                {
                    resolvedMaxDepth = Mathf.Clamp(hookDefinition.maxDepth, 0.5f, maxDepthCap);
                }
            }

            _baseMaxDepth = resolvedMaxDepth;
            ApplyDepthBoundsFromTier();
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

        public void SetDistanceTier(int distanceTier)
        {
            var clampedTier = Mathf.Max(1, distanceTier);
            if (_distanceTier == clampedTier)
            {
                return;
            }

            _distanceTier = clampedTier;
            ApplyDepthBoundsFromTier();
        }

        public void GetWorldDepthBounds(out float minY, out float maxY)
        {
            var surfaceY = ResolveSurfaceY();
            var maxDepth = Mathf.Max(_minDepthBelowSurface + 0.1f, Mathf.Abs(_depthBounds.x));
            var minDepth = Mathf.Clamp(Mathf.Abs(_depthBounds.y), 0f, maxDepth - 0.01f);
            minY = surfaceY - maxDepth;
            maxY = surfaceY - minDepth;
        }

        public float GetDockedY(float dockOffsetY)
        {
            GetWorldDepthBounds(out var minY, out var maxY);
            var target = ResolveSurfaceY() - Mathf.Abs(dockOffsetY);
            return Mathf.Clamp(target, minY, maxY);
        }

        private void LateUpdate()
        {
            if (_shipTransform != null)
            {
                var p = transform.position;
                if (!_enableHorizontalLag)
                {
                    p.x = _shipTransform.position.x;
                    transform.position = p;
                    return;
                }

                var shipVelocityX = ResolveShipVelocityXForLag();
                var maxLag = Mathf.Max(0f, _maxHorizontalLagDistance);
                var targetLagOffset = Mathf.Clamp(
                    -shipVelocityX * Mathf.Max(0f, _horizontalLagPerVelocitySeconds),
                    -maxLag,
                    maxLag);

                var lagBlend = 1f - Mathf.Exp(-Mathf.Max(0.1f, _horizontalLagSmoothing) * Time.deltaTime);
                _horizontalLagOffset = Mathf.Lerp(_horizontalLagOffset, targetLagOffset, lagBlend);

                var targetX = _shipTransform.position.x + _horizontalLagOffset;
                var followBlend = 1f - Mathf.Exp(-Mathf.Max(0.1f, _horizontalFollowSmoothing) * Time.deltaTime);
                p.x = Mathf.Lerp(p.x, targetX, followBlend);
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

            var mappedAxis = _moveHookAction != null
                ? Mathf.Clamp(_moveHookAction.ReadValue<float>(), -1f, 1f)
                : 0f;
            var keyboardAxis = ResolveKeyboardHookAxis();
            var rawAxis = Mathf.Abs(keyboardAxis) > Mathf.Abs(mappedAxis)
                ? keyboardAxis
                : mappedAxis;
            if (Mathf.Abs(rawAxis) < Mathf.Clamp01(_inputDeadzone))
            {
                rawAxis = 0f;
            }

            _smoothedAxis = Mathf.MoveTowards(_smoothedAxis, rawAxis, Mathf.Max(0.01f, _axisSmoothing) * Time.deltaTime);

            var p = transform.position;
            p.y += _smoothedAxis * _verticalSpeed * _speedMultiplier * ResolveInputSensitivity() * Time.deltaTime;
            GetWorldDepthBounds(out var minY, out var maxY);
            p.y = Mathf.Clamp(p.y, minY, maxY);
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

        private static float ResolveKeyboardHookAxis()
        {
            var axis = 0f;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
                {
                    axis -= 1f;
                }

                if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
                {
                    axis += 1f;
                }
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Mathf.Abs(axis) < 0.01f)
            {
                if (UnityEngine.Input.GetKey(KeyCode.DownArrow) || UnityEngine.Input.GetKey(KeyCode.S))
                {
                    axis -= 1f;
                }

                if (UnityEngine.Input.GetKey(KeyCode.UpArrow) || UnityEngine.Input.GetKey(KeyCode.W))
                {
                    axis += 1f;
                }
            }
#endif

            return Mathf.Clamp(axis, -1f, 1f);
        }

        private float ResolveSurfaceY()
        {
            if (_shipTransform != null)
            {
                return _shipTransform.position.y;
            }

            return transform.position.y + Mathf.Max(0.1f, _minDepthBelowSurface);
        }

        private void ApplyDepthBoundsFromTier()
        {
            _minDepthBelowSurface = Mathf.Max(0.1f, _minDepthBelowSurface);
            var tierScale = 1f + (Mathf.Max(1, _distanceTier) - 1f) * Mathf.Max(0f, _distanceTierDepthStep);
            var baseDepthCap = Mathf.Max(_minDepthBelowSurface + 0.1f, _baseMaxDepth);
            var maxDepth = Mathf.Max(
                _minDepthBelowSurface + 0.1f,
                _baseMaxDepth * tierScale);

            var enforcedMinimumDepth = Mathf.Max(0f, _minimumOperationalMaxDepth);
            if (enforcedMinimumDepth > 0f)
            {
                maxDepth = Mathf.Max(maxDepth, enforcedMinimumDepth);
            }

            // Never exceed the authoritative equipped-depth cap resolved in RefreshHookStats.
            maxDepth = Mathf.Min(maxDepth, baseDepthCap);
            var maxDepthCap = Mathf.Max(_minDepthBelowSurface + 0.1f, _absoluteMaxDepthMeters);
            maxDepth = Mathf.Min(maxDepth, maxDepthCap);

            _depthBounds = new Vector2(-maxDepth, -_minDepthBelowSurface);
            ClampTransformToDepthBounds();
        }

        private void ClampTransformToDepthBounds()
        {
            if (transform == null)
            {
                return;
            }

            GetWorldDepthBounds(out var minY, out var maxY);
            var p = transform.position;
            p.y = Mathf.Clamp(p.y, minY, maxY);
            transform.position = p;
        }

        private float ResolveShipVelocityXForLag()
        {
            if (_shipTransform == null)
            {
                ResetHorizontalLagState();
                return 0f;
            }

            if (_shipMovement == null)
            {
                _shipMovement = _shipTransform.GetComponent<ShipMovementController>();
            }

            if (_shipMovement != null)
            {
                return _shipMovement.CurrentHorizontalVelocity;
            }

            var shipX = _shipTransform.position.x;
            if (!_hasRecordedShipXForLag)
            {
                _hasRecordedShipXForLag = true;
                _lastShipXForLag = shipX;
                return 0f;
            }

            var deltaTime = Mathf.Max(0.0001f, Time.deltaTime);
            var velocityX = (shipX - _lastShipXForLag) / deltaTime;
            _lastShipXForLag = shipX;
            return velocityX;
        }

        private void ResetHorizontalLagState()
        {
            _hasRecordedShipXForLag = false;
            _lastShipXForLag = _shipTransform != null ? _shipTransform.position.x : 0f;
            _horizontalLagOffset = 0f;
            _shipMovement = null;
        }
    }
}
