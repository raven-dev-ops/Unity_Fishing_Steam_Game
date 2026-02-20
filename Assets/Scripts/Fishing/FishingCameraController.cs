using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    [RequireComponent(typeof(Camera))]
    public sealed class FishingCameraController : MonoBehaviour
    {
        [SerializeField] private Transform _ship;
        [SerializeField] private Transform _hook;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 4f, -7f);
        [SerializeField] private float _followLerp = 8f;
        [SerializeField] private float _xPadding = 1.75f;
        [SerializeField] private float _yPadding = 1.25f;
        [SerializeField] private Vector2 _xBounds = new Vector2(-12f, 12f);
        [SerializeField] private Vector2 _yBounds = new Vector2(-14f, 14f);
        [SerializeField] private float _minOrthoSize = 4.5f;
        [SerializeField] private float _maxOrthoSize = 7f;
        [SerializeField] private float _maxTrackedDepth = 14f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
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
            transform.position = Vector3.Lerp(
                transform.position,
                desired,
                1f - Mathf.Exp(-Mathf.Max(0.01f, _followLerp) * Time.unscaledDeltaTime));

            if (_camera.orthographic)
            {
                var verticalSpan = Mathf.Abs(_ship.position.y - _hook.position.y) + _yPadding;
                var horizontalSpan = Mathf.Abs(_ship.position.x - _hook.position.x) + _xPadding;
                var depthRatio = Mathf.Clamp01(Mathf.Abs(_hook.position.y) / Mathf.Max(0.1f, _maxTrackedDepth));
                var depthSize = Mathf.Lerp(_minOrthoSize, _maxOrthoSize, depthRatio);

                var halfHorizontalAsVertical = horizontalSpan / Mathf.Max(0.01f, _camera.aspect);
                var targetSize = Mathf.Max(_minOrthoSize, verticalSpan * 0.5f, halfHorizontalAsVertical * 0.5f, depthSize);
                _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, Mathf.Min(_maxOrthoSize, targetSize), 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            }
        }

        public void Configure(Transform ship, Transform hook)
        {
            _ship = ship;
            _hook = hook;
        }
    }
}
