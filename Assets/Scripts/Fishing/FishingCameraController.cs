using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    [RequireComponent(typeof(Camera))]
    public sealed class FishingCameraController : MonoBehaviour
    {
        [SerializeField] private Transform _ship;
        [SerializeField] private Transform _hook;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.85f, -10f);
        [SerializeField] private float _followLerp = 9f;
        [SerializeField] private float _xPadding = 2.4f;
        [SerializeField] private float _yPadding = 2.8f;
        [SerializeField] private Vector2 _xBounds = new Vector2(-10f, 10f);
        [SerializeField] private Vector2 _yBounds = new Vector2(-7f, 6f);
        [SerializeField] private float _minOrthoSize = 5.4f;
        [SerializeField] private float _maxOrthoSize = 7.4f;
        [SerializeField] private float _maxTrackedDepth = 10f;
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

            var focus = (_ship.position + _hook.position) * 0.5f;
            focus.x = Mathf.Clamp(focus.x, Mathf.Min(_xBounds.x, _xBounds.y), Mathf.Max(_xBounds.x, _xBounds.y));
            focus.y = Mathf.Clamp(focus.y, Mathf.Min(_yBounds.x, _yBounds.y), Mathf.Max(_yBounds.x, _yBounds.y));

            var desired = focus + _offset;
            var reducedMotion = _settingsService != null && _settingsService.ReducedMotion;
            var followLerp = reducedMotion
                ? Mathf.Max(0.01f, _followLerp * Mathf.Clamp(_reducedMotionFollowScale, 0.1f, 1f))
                : Mathf.Max(0.01f, _followLerp);
            transform.position = Vector3.Lerp(
                transform.position,
                desired,
                1f - Mathf.Exp(-followLerp * Time.unscaledDeltaTime));

            if (_camera.orthographic)
            {
                var verticalSpan = Mathf.Abs(_ship.position.y - _hook.position.y) + _yPadding;
                var horizontalSpan = Mathf.Abs(_ship.position.x - _hook.position.x) + _xPadding;
                var depthRatio = Mathf.Clamp01(Mathf.Abs(_hook.position.y) / Mathf.Max(0.1f, _maxTrackedDepth));
                var depthSize = Mathf.Lerp(_minOrthoSize, _maxOrthoSize, depthRatio);

                var halfHorizontalAsVertical = horizontalSpan / Mathf.Max(0.01f, _camera.aspect);
                var targetSize = Mathf.Max(_minOrthoSize, verticalSpan * 0.5f, halfHorizontalAsVertical * 0.5f, depthSize);
                var sizeLerp = reducedMotion ? Mathf.Clamp(_reducedMotionSizeLerpScale, 0.1f, 1f) * 8f : 8f;
                _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, Mathf.Min(_maxOrthoSize, targetSize), 1f - Mathf.Exp(-sizeLerp * Time.unscaledDeltaTime));
            }
        }

        public void Configure(Transform ship, Transform hook)
        {
            _ship = ship;
            _hook = hook;
        }
    }
}
