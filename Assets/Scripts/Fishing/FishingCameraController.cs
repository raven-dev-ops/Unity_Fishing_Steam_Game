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
        [SerializeField] private Vector2 _yBounds = new Vector2(-7f, 6f);
        [SerializeField] private float _minOrthoSize = 5.6f;
        [SerializeField] private float _maxOrthoSize = 11f;
        [SerializeField] private float _maxTrackedDepth = 18f;
        [SerializeField] private float _shipViewportY = 0.84f;
        [SerializeField] private float _hookViewportMinY = 0.12f;
        [SerializeField] private float _hookFollowBlend = 0.22f;
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

            desired.x = Mathf.Clamp(
                _ship.position.x + _offset.x,
                Mathf.Min(_xBounds.x, _xBounds.y),
                Mathf.Max(_xBounds.x, _xBounds.y));
            desired.y = ResolveTargetCameraY(targetSize);
            desired.y = Mathf.Clamp(
                desired.y,
                Mathf.Min(_yBounds.x, _yBounds.y),
                Mathf.Max(_yBounds.x, _yBounds.y));
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

        private float ResolveTargetCameraY(float orthoSize)
        {
            var shipViewportY = Mathf.Clamp(_shipViewportY, 0.55f, 0.95f);
            var hookViewportMinY = Mathf.Clamp(_hookViewportMinY, 0.01f, shipViewportY - 0.1f);

            var anchorY = _ship.position.y - ((shipViewportY - 0.5f) * 2f * orthoSize);
            var hookAlignedY = _hook.position.y + ((0.5f - hookViewportMinY) * 2f * orthoSize);
            var depthRatio = Mathf.Clamp01(Mathf.Max(0f, _ship.position.y - _hook.position.y) / Mathf.Max(0.1f, _maxTrackedDepth));
            var blend = Mathf.Clamp01(_hookFollowBlend) * depthRatio;
            return Mathf.Lerp(anchorY, hookAlignedY, blend);
        }
    }
}
