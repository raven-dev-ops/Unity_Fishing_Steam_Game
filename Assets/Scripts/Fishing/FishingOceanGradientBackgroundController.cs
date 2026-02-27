using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingOceanGradientBackgroundController : MonoBehaviour
    {
        private static readonly int SurfaceYId = Shader.PropertyToID("_SurfaceY");
        private static readonly int SkyHeightMetersId = Shader.PropertyToID("_SkyHeightMeters");
        private static readonly int OceanDepthMetersId = Shader.PropertyToID("_OceanDepthMeters");
        private static readonly int SkyLightColorId = Shader.PropertyToID("_SkyLightColor");
        private static readonly int SkyDarkColorId = Shader.PropertyToID("_SkyDarkColor");
        private static readonly int SkyHorizontalInfluenceId = Shader.PropertyToID("_SkyHorizontalInfluence");
        private static readonly int SkyWavelengthMetersId = Shader.PropertyToID("_SkyWavelengthMeters");
        private static readonly int SkyCycleSecondsId = Shader.PropertyToID("_SkyCycleSeconds");
        private static readonly int OceanShallowColorId = Shader.PropertyToID("_OceanShallowColor");
        private static readonly int OceanDeepColorId = Shader.PropertyToID("_OceanDeepColor");
        private static Mesh s_BackgroundQuadMesh;

        [SerializeField] private Camera _targetCamera;
        [SerializeField] private string _gradientShaderName = "Raven/Fishing/OceanSkyGradient";
        [SerializeField] private float _surfaceY = 0f;
        [SerializeField] private float _skyHeightMeters = 50f;
        [SerializeField] private float _oceanDepthMeters = 5000f;
        [SerializeField] private float _skyCycleSeconds = 600f;
        [SerializeField] private float _skyWavelengthMeters = 260f;
        [SerializeField, Range(0f, 1f)] private float _skyHorizontalInfluence = 0.95f;
        [SerializeField] private Color _skyLightColor = new Color(0.28f, 0.34f, 0.46f, 1f);
        [SerializeField] private Color _skyDarkColor = new Color(0.015f, 0.02f, 0.07f, 1f);
        [SerializeField] private Color _oceanShallowColor = new Color(0.08f, 0.16f, 0.28f, 1f);
        [SerializeField] private Color _oceanDeepColor = new Color(0f, 0.005f, 0.015f, 1f);
        [SerializeField] private float _overlayDepthOffset = 18f;
        [SerializeField] private Vector2 _overlayPadding = new Vector2(4f, 4f);
        [SerializeField] private Transform _legacyBackdropFar;
        [SerializeField] private Transform _legacyBackdropVeil;

        private GameObject _overlayObject;
        private MeshRenderer _overlayRenderer;
        private Material _overlayMaterial;

        public void Configure(
            Camera targetCamera,
            Transform legacyBackdropFar,
            Transform legacyBackdropVeil,
            float surfaceY = 0f,
            float skyHeightMeters = 50f,
            float oceanDepthMeters = 5000f,
            float skyCycleSeconds = 600f)
        {
            _targetCamera = targetCamera;
            _legacyBackdropFar = legacyBackdropFar;
            _legacyBackdropVeil = legacyBackdropVeil;
            _surfaceY = surfaceY;
            _skyHeightMeters = Mathf.Max(0.01f, skyHeightMeters);
            _oceanDepthMeters = Mathf.Max(0.01f, oceanDepthMeters);
            _skyCycleSeconds = Mathf.Max(1f, skyCycleSeconds);
            HideLegacyBackdropSprites();
            EnsureOverlay();
            UpdateOverlay();
        }

        private void Awake()
        {
            EnsureCamera();
        }

        private void LateUpdate()
        {
            EnsureCamera();
            HideLegacyBackdropSprites();
            EnsureOverlay();
            UpdateOverlay();
        }

        private void OnDestroy()
        {
            if (_overlayMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_overlayMaterial);
                }
                else
                {
                    DestroyImmediate(_overlayMaterial);
                }
            }

            if (_overlayObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_overlayObject);
                }
                else
                {
                    DestroyImmediate(_overlayObject);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _skyHeightMeters = Mathf.Max(0.01f, _skyHeightMeters);
            _oceanDepthMeters = Mathf.Max(0.01f, _oceanDepthMeters);
            _skyCycleSeconds = Mathf.Max(1f, _skyCycleSeconds);
            _skyWavelengthMeters = Mathf.Max(1f, _skyWavelengthMeters);
            _overlayDepthOffset = Mathf.Max(0.01f, _overlayDepthOffset);
            _overlayPadding = new Vector2(
                Mathf.Max(0f, _overlayPadding.x),
                Mathf.Max(0f, _overlayPadding.y));
            _skyHorizontalInfluence = Mathf.Clamp01(_skyHorizontalInfluence);
        }
#endif

        private void EnsureCamera()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera == null)
            {
                _targetCamera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
            }
        }

        private void HideLegacyBackdropSprites()
        {
            HideLegacyBackdropSprite(_legacyBackdropFar);
            HideLegacyBackdropSprite(_legacyBackdropVeil);
        }

        private static void HideLegacyBackdropSprite(Transform backdropRoot)
        {
            if (backdropRoot == null)
            {
                return;
            }

            var renderer = backdropRoot.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = null;
                renderer.enabled = false;
            }

            var fit = backdropRoot.GetComponent<SceneBackdropFit2D>();
            if (fit != null)
            {
                fit.enabled = false;
            }
        }

        private void EnsureOverlay()
        {
            if (_overlayRenderer != null && _overlayMaterial != null)
            {
                if (_targetCamera != null && _overlayObject != null && _overlayObject.transform.parent != _targetCamera.transform)
                {
                    _overlayObject.transform.SetParent(_targetCamera.transform, worldPositionStays: false);
                }

                return;
            }

            if (_targetCamera == null)
            {
                return;
            }

            var shader = Shader.Find(_gradientShaderName);
            if (shader == null)
            {
                return;
            }

            if (_overlayObject == null)
            {
                _overlayObject = new GameObject("FishingOceanGradientOverlay");
                _overlayObject.transform.SetParent(_targetCamera.transform, worldPositionStays: false);
                var meshFilter = _overlayObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = GetOverlayQuadMesh();
                _overlayRenderer = _overlayObject.AddComponent<MeshRenderer>();
                _overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
                _overlayRenderer.receiveShadows = false;
                _overlayRenderer.lightProbeUsage = LightProbeUsage.Off;
                _overlayRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            if (_overlayMaterial == null)
            {
                _overlayMaterial = new Material(shader)
                {
                    name = "FishingOceanGradientMaterial"
                };
            }

            if (_overlayRenderer != null)
            {
                _overlayRenderer.sharedMaterial = _overlayMaterial;
            }
        }

        private void UpdateOverlay()
        {
            if (_overlayRenderer == null || _overlayMaterial == null || _overlayObject == null || _targetCamera == null)
            {
                if (_overlayRenderer != null)
                {
                    _overlayRenderer.enabled = false;
                }

                return;
            }

            _overlayRenderer.enabled = true;
            _overlayMaterial.SetFloat(SurfaceYId, _surfaceY);
            _overlayMaterial.SetFloat(SkyHeightMetersId, Mathf.Max(0.01f, _skyHeightMeters));
            _overlayMaterial.SetFloat(OceanDepthMetersId, Mathf.Max(0.01f, _oceanDepthMeters));
            _overlayMaterial.SetColor(SkyLightColorId, _skyLightColor);
            _overlayMaterial.SetColor(SkyDarkColorId, _skyDarkColor);
            _overlayMaterial.SetFloat(SkyHorizontalInfluenceId, Mathf.Clamp01(_skyHorizontalInfluence));
            _overlayMaterial.SetFloat(SkyWavelengthMetersId, Mathf.Max(1f, _skyWavelengthMeters));
            _overlayMaterial.SetFloat(SkyCycleSecondsId, Mathf.Max(1f, _skyCycleSeconds));
            _overlayMaterial.SetColor(OceanShallowColorId, _oceanShallowColor);
            _overlayMaterial.SetColor(OceanDeepColorId, _oceanDeepColor);

            var overlayTransform = _overlayObject.transform;
            overlayTransform.localPosition = new Vector3(0f, 0f, Mathf.Max(0.01f, _overlayDepthOffset));
            overlayTransform.localRotation = Quaternion.identity;

            var paddingX = Mathf.Max(0f, _overlayPadding.x);
            var paddingY = Mathf.Max(0f, _overlayPadding.y);
            if (_targetCamera.orthographic)
            {
                var viewportHeight = Mathf.Max(0.5f, (_targetCamera.orthographicSize * 2f) + paddingY);
                var viewportWidth = Mathf.Max(0.5f, (viewportHeight * Mathf.Max(0.01f, _targetCamera.aspect)) + paddingX);
                overlayTransform.localScale = new Vector3(viewportWidth, viewportHeight, 1f);
            }
            else
            {
                overlayTransform.localScale = new Vector3(180f, 120f, 1f);
            }
        }

        private static Mesh GetOverlayQuadMesh()
        {
            if (s_BackgroundQuadMesh != null)
            {
                return s_BackgroundQuadMesh;
            }

            s_BackgroundQuadMesh = new Mesh
            {
                name = "FishingOceanGradientQuad"
            };
            s_BackgroundQuadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            s_BackgroundQuadMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            s_BackgroundQuadMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            s_BackgroundQuadMesh.RecalculateBounds();
            s_BackgroundQuadMesh.UploadMeshData(markNoLongerReadable: true);
            return s_BackgroundQuadMesh;
        }
    }
}
