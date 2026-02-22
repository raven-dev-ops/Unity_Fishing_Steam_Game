using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SceneBackdropFit2D : MonoBehaviour
    {
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private bool _useMainCamera = true;
        [SerializeField] private bool _coverViewport = true;
        [SerializeField] private bool _followCameraPosition = true;
        [SerializeField] private bool _preserveDepth = true;
        [SerializeField, Min(0.1f)] private float _scaleMultiplier = 1f;
        [SerializeField] private bool _preserveZScale = true;

        private SpriteRenderer _spriteRenderer;
        private Camera _cachedFallbackCamera;
        private float _lastAspect = -1f;
        private float _lastOrthographicSize = -1f;
        private Sprite _lastSprite;

        public void Configure(
            float scaleMultiplier,
            bool coverViewport = true,
            bool followCameraPosition = true)
        {
            _scaleMultiplier = Mathf.Max(0.1f, scaleMultiplier);
            _coverViewport = coverViewport;
            _followCameraPosition = followCameraPosition;
            FitToCamera(force: true);
        }

        private void Awake()
        {
            CacheReferences();
            FitToCamera(force: true);
        }

        private void OnEnable()
        {
            CacheReferences();
            FitToCamera(force: true);
        }

        private void Update()
        {
            FitToCamera(force: false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _scaleMultiplier = Mathf.Max(0.1f, _scaleMultiplier);
            CacheReferences();
            FitToCamera(force: true);
        }
#endif

        private void CacheReferences()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void FitToCamera(bool force)
        {
            CacheReferences();
            if (_spriteRenderer == null || _spriteRenderer.sprite == null)
            {
                return;
            }

            var camera = ResolveTargetCamera();
            if (camera == null || !camera.orthographic || camera.aspect <= 0.0001f)
            {
                return;
            }

            if (!force
                && Mathf.Approximately(_lastAspect, camera.aspect)
                && Mathf.Approximately(_lastOrthographicSize, camera.orthographicSize)
                && _lastSprite == _spriteRenderer.sprite)
            {
                return;
            }

            var sprite = _spriteRenderer.sprite;
            var spriteWorldWidth = sprite.rect.width / sprite.pixelsPerUnit;
            var spriteWorldHeight = sprite.rect.height / sprite.pixelsPerUnit;
            if (spriteWorldWidth <= 0.0001f || spriteWorldHeight <= 0.0001f)
            {
                return;
            }

            var viewportHeight = camera.orthographicSize * 2f;
            var viewportWidth = viewportHeight * camera.aspect;
            var fitScaleX = viewportWidth / spriteWorldWidth;
            var fitScaleY = viewportHeight / spriteWorldHeight;
            var baseScale = _coverViewport ? Mathf.Max(fitScaleX, fitScaleY) : Mathf.Min(fitScaleX, fitScaleY);
            var targetScale = Mathf.Max(0.01f, baseScale * _scaleMultiplier);

            var currentScale = transform.localScale;
            transform.localScale = new Vector3(
                targetScale,
                targetScale,
                _preserveZScale ? currentScale.z : targetScale);

            if (_followCameraPosition)
            {
                var currentPosition = transform.position;
                transform.position = new Vector3(
                    camera.transform.position.x,
                    camera.transform.position.y,
                    _preserveDepth ? currentPosition.z : camera.transform.position.z);
            }

            _lastAspect = camera.aspect;
            _lastOrthographicSize = camera.orthographicSize;
            _lastSprite = sprite;
        }

        private Camera ResolveTargetCamera()
        {
            if (_targetCamera != null)
            {
                return _targetCamera;
            }

            if (_useMainCamera && Camera.main != null)
            {
                return Camera.main;
            }

            if (_cachedFallbackCamera == null)
            {
                var cameras = Camera.allCameras;
                if (cameras != null && cameras.Length > 0)
                {
                    _cachedFallbackCamera = cameras[0];
                }
            }

            return _cachedFallbackCamera;
        }
    }
}
