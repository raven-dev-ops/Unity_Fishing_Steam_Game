using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    [RequireComponent(typeof(Camera))]
    public sealed class FishingCameraController : MonoBehaviour
    {
        [SerializeField] private Transform _ship;
        [SerializeField] private Transform _hook;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);
        [SerializeField] private float _followLerp = 9f;
        [SerializeField] private float _xPadding = 2.4f;
        [SerializeField] private float _yPadding = 1.25f;
        [SerializeField] private Vector2 _xBounds = new Vector2(-10f, 10f);
        [SerializeField] private bool _limitHorizontalRange = false;
        [SerializeField] private Vector2 _yBounds = new Vector2(-7f, 6f);
        [SerializeField] private float _minOrthoSize = 5.6f;
        [SerializeField] private float _maxOrthoSize = 11f;
        [SerializeField] private float _maxTrackedDepth = 18f;
        [SerializeField] private float _shipViewportY = 0.84f;
        [SerializeField] private float _hookViewportMinY = 0.12f;
        [SerializeField] private float _hookFollowBlend = 0.22f;
        [SerializeField] private float _hookFollowStartDepth = 15f;
        [SerializeField] private float _hookFollowTransitionDepth = 4f;
        [SerializeField] private float _deepFollowHookViewportY = 0.3f;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private float _reducedMotionFollowScale = 0.45f;
        [SerializeField] private float _reducedMotionSizeLerpScale = 0.4f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                return;
            }

            if (_ship == null || _hook == null)
            {
                return;
            }

            var reducedMotion = _settingsService != null && _settingsService.ReducedMotion;
            var followLerp = reducedMotion
                ? Mathf.Max(0.01f, _followLerp * Mathf.Clamp(_reducedMotionFollowScale, 0.1f, 1f))
                : Mathf.Max(0.01f, _followLerp);
            var desired = transform.position;
            var targetSize = _camera.orthographic ? ResolveTargetOrthoSize() : _camera.orthographicSize;
            if (_camera.orthographic)
            {
                var sizeLerp = reducedMotion ? Mathf.Clamp(_reducedMotionSizeLerpScale, 0.1f, 1f) * 8f : 8f;
                _camera.orthographicSize = Mathf.Lerp(
                    _camera.orthographicSize,
                    targetSize,
                    1f - Mathf.Exp(-sizeLerp * Time.unscaledDeltaTime));
            }

            desired.x = _ship.position.x + _offset.x;
            if (_limitHorizontalRange)
            {
                desired.x = Mathf.Clamp(
                    desired.x,
                    Mathf.Min(_xBounds.x, _xBounds.y),
                    Mathf.Max(_xBounds.x, _xBounds.y));
            }
            desired.y = ResolveTargetCameraY(targetSize, out var hookAlignedY);
            var minYBound = Mathf.Min(_yBounds.x, _yBounds.y);
            var maxYBound = Mathf.Max(_yBounds.x, _yBounds.y);
            var dynamicMinY = Mathf.Min(minYBound, hookAlignedY);
            desired.y = Mathf.Clamp(desired.y, dynamicMinY, maxYBound);
            desired.z = _offset.z;

            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followLerp * Time.unscaledDeltaTime));
        }

        public void Configure(Transform ship, Transform hook)
        {
            _ship = ship;
            _hook = hook;
        }

        private float ResolveTargetOrthoSize()
        {
            var depthFromShip = Mathf.Max(0f, _ship.position.y - _hook.position.y);
            var viewportSpan = Mathf.Max(0.2f, Mathf.Clamp01(_shipViewportY) - Mathf.Clamp01(_hookViewportMinY));
            var requiredByDepth = depthFromShip / Mathf.Max(0.2f, viewportSpan * 2f);
            var requiredByDepthWithPadding = requiredByDepth + Mathf.Max(0f, _yPadding);

            var horizontalSpan = Mathf.Abs(_ship.position.x - _hook.position.x) + _xPadding;
            var requiredByHorizontal = horizontalSpan / Mathf.Max(0.01f, _camera.aspect * 2f);

            var depthRatio = Mathf.Clamp01(depthFromShip / Mathf.Max(0.1f, _maxTrackedDepth));
            var depthSize = Mathf.Lerp(_minOrthoSize, _maxOrthoSize, depthRatio);
            var targetSize = Mathf.Max(_minOrthoSize, requiredByDepthWithPadding, requiredByHorizontal, depthSize);
            return Mathf.Clamp(targetSize, _minOrthoSize, _maxOrthoSize);
        }

        private float ResolveTargetCameraY(float orthoSize, out float hookAlignedY)
        {
            var shipViewportY = Mathf.Clamp(_shipViewportY, 0.55f, 0.95f);
            var hookViewportMinY = Mathf.Clamp(_hookViewportMinY, 0.01f, shipViewportY - 0.1f);
            var deepFollowHookViewportY = Mathf.Clamp(_deepFollowHookViewportY, hookViewportMinY, shipViewportY - 0.08f);
            var depthFromShip = Mathf.Max(0f, _ship.position.y - _hook.position.y);
            var depthRatio = Mathf.Clamp01(depthFromShip / Mathf.Max(0.1f, _maxTrackedDepth));
            var baseBlend = Mathf.Clamp01(_hookFollowBlend) * depthRatio;

            var deepFollowStartDepth = Mathf.Max(0f, _hookFollowStartDepth);
            var deepFollowTransitionDepth = Mathf.Max(0.1f, _hookFollowTransitionDepth);
            var deepFollowRatio = Mathf.Clamp01((depthFromShip - deepFollowStartDepth) / deepFollowTransitionDepth);
            deepFollowRatio = Mathf.SmoothStep(0f, 1f, deepFollowRatio);

            var targetHookViewportY = Mathf.Lerp(hookViewportMinY, deepFollowHookViewportY, deepFollowRatio);

            var anchorY = _ship.position.y - ((shipViewportY - 0.5f) * 2f * orthoSize);
            hookAlignedY = _hook.position.y + ((0.5f - targetHookViewportY) * 2f * orthoSize);
            var blend = Mathf.Clamp01(baseBlend + ((1f - baseBlend) * deepFollowRatio));
            return Mathf.Lerp(anchorY, hookAlignedY, blend);
        }
    }
}
