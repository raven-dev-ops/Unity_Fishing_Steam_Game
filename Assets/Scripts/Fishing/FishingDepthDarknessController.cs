using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.Rendering;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingDepthDarknessController : MonoBehaviour
    {
        private static readonly int DarknessAlphaId = Shader.PropertyToID("_DarknessAlpha");
        private static readonly int HookWorldPosId = Shader.PropertyToID("_HookWorldPos");
        private static readonly int LightRadiusId = Shader.PropertyToID("_LightRadius");
        private static readonly int LightSoftnessId = Shader.PropertyToID("_LightSoftness");
        private static Mesh s_OverlayQuadMesh;

        [SerializeField] private HookMovementController _hookController;
        [SerializeField] private Transform _hook;
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private string _darknessShaderName = "Raven/Fishing/DepthDarkness";
        [SerializeField] private float _darkZoneStartDepthMeters = 1000f;
        [SerializeField] private float _deepDarkZoneStartDepthMeters = 2500f;
        [SerializeField] private float _deepDarkZoneGradientDepthMeters = 500f;
        [SerializeField] private float _maxDepthMeters = 5000f;
        [SerializeField, Range(0f, 1f)] private float _darkZoneAlphaAtDeepEdge = 0.78f;
        [SerializeField, Range(0f, 1f)] private float _deepDarkZoneAlphaAtMaxDepth = 0.95f;
        [SerializeField] private float _lightSoftnessMeters = 3f;
        [SerializeField] private float _overlayPlaneOffset = 0.8f;
        [SerializeField] private Vector2 _hookLv4LightRadiiMeters = new Vector2(15f, 5f);
        [SerializeField] private Vector2 _hookLv5LightRadiiMeters = new Vector2(30f, 15f);

        private GameObject _overlayObject;
        private MeshRenderer _overlayRenderer;
        private Material _overlayMaterial;
        private bool _tutorialLightPreviewActive;
        private Vector2 _tutorialLightPreviewRadiiMeters;
        private bool _tutorialDepthPreviewActive;
        private float _tutorialDepthPreviewMeters;

        public void SetTutorialLightPreview(Vector2 lightRadiiMeters)
        {
            _tutorialLightPreviewActive = true;
            _tutorialLightPreviewRadiiMeters = new Vector2(
                Mathf.Max(0f, lightRadiiMeters.x),
                Mathf.Max(0f, lightRadiiMeters.y));
        }

        public void ClearTutorialLightPreview()
        {
            _tutorialLightPreviewActive = false;
            _tutorialLightPreviewRadiiMeters = Vector2.zero;
        }

        public void SetTutorialDepthPreview(float depthMeters)
        {
            _tutorialDepthPreviewActive = true;
            _tutorialDepthPreviewMeters = Mathf.Max(0f, depthMeters);
        }

        public void ClearTutorialDepthPreview()
        {
            _tutorialDepthPreviewActive = false;
            _tutorialDepthPreviewMeters = 0f;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _hookController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            _targetCamera ??= Camera.main;
            if (_hookController != null)
            {
                _hook ??= _hookController.transform;
            }
        }

        private void LateUpdate()
        {
            EnsureReferences();
            EnsureOverlay();
            UpdateOverlay();
        }

        private void EnsureReferences()
        {
            if (_hookController == null && RuntimeServiceRegistry.TryGet<HookMovementController>(out var hookController))
            {
                _hookController = hookController;
            }

            if (_hook == null && _hookController != null)
            {
                _hook = _hookController.transform;
            }

            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
                if (_targetCamera == null)
                {
                    _targetCamera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
                }
            }
        }

        private void EnsureOverlay()
        {
            if (_overlayRenderer != null && _overlayMaterial != null)
            {
                return;
            }

            if (_targetCamera == null)
            {
                return;
            }

            var shader = Shader.Find(_darknessShaderName);
            if (shader == null)
            {
                return;
            }

            if (_overlayObject == null)
            {
                _overlayObject = new GameObject("DepthDarknessOverlay");
                _overlayObject.transform.SetParent(_targetCamera.transform, worldPositionStays: false);
                var filter = _overlayObject.AddComponent<MeshFilter>();
                filter.sharedMesh = GetOverlayQuadMesh();
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
                    name = "DepthDarknessOverlayMaterial"
                };
                _overlayRenderer.sharedMaterial = _overlayMaterial;
            }
        }

        private void UpdateOverlay()
        {
            if (_overlayRenderer == null || _overlayMaterial == null || _targetCamera == null || _hookController == null || _hook == null)
            {
                if (_overlayRenderer != null)
                {
                    _overlayRenderer.enabled = false;
                }

                return;
            }

            var currentDepth = _tutorialDepthPreviewActive
                ? Mathf.Max(0f, _tutorialDepthPreviewMeters)
                : Mathf.Max(0f, _hookController.CurrentDepth);
            var darknessAlpha = ResolveDarknessAlpha(currentDepth, out var deepDarkBlend);
            if (darknessAlpha <= 0.001f)
            {
                _overlayRenderer.enabled = false;
                return;
            }

            _overlayRenderer.enabled = true;
            var lightRadii = ResolveCurrentHookLightRadii();
            var lightRadius = ResolveActiveLightRadius(currentDepth, lightRadii, deepDarkBlend);

            _overlayMaterial.SetFloat(DarknessAlphaId, darknessAlpha);
            _overlayMaterial.SetVector(HookWorldPosId, _hook.position);
            _overlayMaterial.SetFloat(LightRadiusId, Mathf.Max(0f, lightRadius));
            _overlayMaterial.SetFloat(LightSoftnessId, Mathf.Max(0.01f, _lightSoftnessMeters));

            var overlayTransform = _overlayObject.transform;
            overlayTransform.localPosition = new Vector3(0f, 0f, Mathf.Max(0.01f, _overlayPlaneOffset));
            overlayTransform.localRotation = Quaternion.identity;
            if (_targetCamera.orthographic)
            {
                var viewportHeight = Mathf.Max(0.5f, _targetCamera.orthographicSize * 2f);
                var viewportWidth = viewportHeight * Mathf.Max(0.01f, _targetCamera.aspect);
                overlayTransform.localScale = new Vector3(viewportWidth * 1.12f, viewportHeight * 1.12f, 1f);
            }
            else
            {
                overlayTransform.localScale = new Vector3(120f, 120f, 1f);
            }
        }

        private float ResolveDarknessAlpha(float depthMeters, out float deepDarkBlend)
        {
            deepDarkBlend = 0f;
            var darkStartDepth = Mathf.Max(0f, _darkZoneStartDepthMeters);
            var deepStartDepth = Mathf.Max(darkStartDepth + 1f, _deepDarkZoneStartDepthMeters);
            var maxDepth = Mathf.Max(deepStartDepth + 1f, _maxDepthMeters);
            var deepGradientDepth = Mathf.Clamp(
                _deepDarkZoneGradientDepthMeters,
                1f,
                Mathf.Max(1f, maxDepth - deepStartDepth));
            var deepEndDepth = Mathf.Min(maxDepth, deepStartDepth + deepGradientDepth);

            if (depthMeters <= darkStartDepth)
            {
                return 0f;
            }

            var darkZoneAlpha = Mathf.Clamp01(_darkZoneAlphaAtDeepEdge);
            var deepDarkAlpha = Mathf.Clamp01(Mathf.Max(darkZoneAlpha, _deepDarkZoneAlphaAtMaxDepth));
            if (depthMeters < deepStartDepth)
            {
                var darkBlend = Mathf.Clamp01((depthMeters - darkStartDepth) / (deepStartDepth - darkStartDepth));
                return Mathf.Lerp(0f, darkZoneAlpha, darkBlend);
            }

            if (depthMeters < deepEndDepth)
            {
                deepDarkBlend = Mathf.Clamp01((depthMeters - deepStartDepth) / (deepEndDepth - deepStartDepth));
                return Mathf.Lerp(darkZoneAlpha, deepDarkAlpha, deepDarkBlend);
            }

            deepDarkBlend = 1f;
            return Mathf.Lerp(darkZoneAlpha, deepDarkAlpha, deepDarkBlend);
        }

        private float ResolveActiveLightRadius(float depthMeters, Vector2 hookLightRadiiMeters, float deepDarkBlend)
        {
            var darkStartDepth = Mathf.Max(0f, _darkZoneStartDepthMeters);
            var deepStartDepth = Mathf.Max(darkStartDepth + 1f, _deepDarkZoneStartDepthMeters);
            if (depthMeters <= darkStartDepth)
            {
                return 0f;
            }

            var darkRadius = Mathf.Max(0f, hookLightRadiiMeters.x);
            var deepDarkRadius = Mathf.Max(0f, hookLightRadiiMeters.y);
            if (depthMeters < deepStartDepth)
            {
                return darkRadius;
            }

            return Mathf.Lerp(darkRadius, deepDarkRadius, Mathf.Clamp01(deepDarkBlend));
        }

        private Vector2 ResolveCurrentHookLightRadii()
        {
            if (_tutorialLightPreviewActive)
            {
                return _tutorialLightPreviewRadiiMeters;
            }

            var hookId = _saveManager != null && _saveManager.Current != null
                ? _saveManager.Current.equippedHookId
                : string.Empty;
            var normalizedId = string.IsNullOrWhiteSpace(hookId)
                ? string.Empty
                : hookId.Trim().ToLowerInvariant();

            if (_catalogService != null
                && !string.IsNullOrEmpty(normalizedId)
                && _catalogService.TryGetHook(normalizedId, out var hookDefinition)
                && hookDefinition != null)
            {
                var darkRadius = Mathf.Max(0f, hookDefinition.darkZoneLightRadiusMeters);
                var deepDarkRadius = Mathf.Max(0f, hookDefinition.deepDarkZoneLightRadiusMeters);
                if (darkRadius > 0f || deepDarkRadius > 0f)
                {
                    return new Vector2(darkRadius, deepDarkRadius);
                }
            }

            if (normalizedId.Contains("hook_lv5"))
            {
                return new Vector2(Mathf.Max(0f, _hookLv5LightRadiiMeters.x), Mathf.Max(0f, _hookLv5LightRadiiMeters.y));
            }

            if (normalizedId.Contains("hook_lv4"))
            {
                return new Vector2(Mathf.Max(0f, _hookLv4LightRadiiMeters.x), Mathf.Max(0f, _hookLv4LightRadiiMeters.y));
            }

            return Vector2.zero;
        }

        private static Mesh GetOverlayQuadMesh()
        {
            if (s_OverlayQuadMesh != null)
            {
                return s_OverlayQuadMesh;
            }

            s_OverlayQuadMesh = new Mesh
            {
                name = "DepthDarknessOverlayQuad"
            };
            s_OverlayQuadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            s_OverlayQuadMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            s_OverlayQuadMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            s_OverlayQuadMesh.RecalculateBounds();
            s_OverlayQuadMesh.UploadMeshData(markNoLongerReadable: true);
            return s_OverlayQuadMesh;
        }
    }
}
