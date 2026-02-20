using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingEnvironmentSliceController : MonoBehaviour
    {
        [SerializeField] private Transform _ship;
        [SerializeField] private Transform _hook;
        [SerializeField] private Vector2 _shipXBounds = new Vector2(-9f, 9f);
        [SerializeField] private Vector2 _hookYBounds = new Vector2(-8f, -1f);
        [SerializeField] private Material _fallbackSkybox;
        [SerializeField] private bool _ensureDirectionalLight = true;
        [SerializeField] private bool _autoCreateBoundaryColliders = true;
        [SerializeField] private string _boundaryRootName = "__FishingBoundaries";
        [SerializeField] private Vector3 _playAreaCenter = new Vector3(0f, -4.5f, 0f);
        [SerializeField] private Vector3 _playAreaSize = new Vector3(20f, 9f, 1f);
        [SerializeField] private float _boundaryThickness = 0.5f;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private bool _usePhaseTwoEnvironmentOverride = true;
        [SerializeField] private string _phaseTwoSkyboxKey = "fishing_skybox";

        private bool _visualBaselineApplied;
        private bool _boundariesReady;
        private bool _phaseTwoEnvironmentAttempted;

        public void Configure(Transform ship, Transform hook)
        {
            _ship = ship;
            _hook = hook;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
        }

        private void Start()
        {
            EnsureReferences();
            EnsureVisualBaseline();
            TryApplyPhaseTwoEnvironmentOverride();
            EnsureBoundaryColliders();
        }

        private void LateUpdate()
        {
            EnsureReferences();
            TryApplyPhaseTwoEnvironmentOverride();
            ClampTransformsToPlayArea();
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        private void EnsureReferences()
        {
            if (_ship == null && RuntimeServiceRegistry.TryGet<ShipMovementController>(out var shipController))
            {
                _ship = shipController.transform;
            }

            if (_hook == null && RuntimeServiceRegistry.TryGet<HookMovementController>(out var hookController))
            {
                _hook = hookController.transform;
            }
        }

        private void ClampTransformsToPlayArea()
        {
            var minX = Mathf.Min(_shipXBounds.x, _shipXBounds.y);
            var maxX = Mathf.Max(_shipXBounds.x, _shipXBounds.y);
            var minHookY = Mathf.Min(_hookYBounds.x, _hookYBounds.y);
            var maxHookY = Mathf.Max(_hookYBounds.x, _hookYBounds.y);

            if (_ship != null)
            {
                var shipPos = _ship.position;
                shipPos.x = Mathf.Clamp(shipPos.x, minX, maxX);
                if (!IsFinite(shipPos))
                {
                    shipPos = new Vector3(Mathf.Clamp(transform.position.x, minX, maxX), transform.position.y, transform.position.z);
                }

                _ship.position = shipPos;
            }

            if (_hook != null)
            {
                var hookPos = _hook.position;
                hookPos.x = Mathf.Clamp(hookPos.x, minX, maxX);
                hookPos.y = Mathf.Clamp(hookPos.y, minHookY, maxHookY);
                if (!IsFinite(hookPos))
                {
                    hookPos = new Vector3(Mathf.Clamp(transform.position.x, minX, maxX), Mathf.Clamp(transform.position.y, minHookY, maxHookY), transform.position.z);
                }

                _hook.position = hookPos;
            }
        }

        private void EnsureVisualBaseline()
        {
            if (_visualBaselineApplied)
            {
                return;
            }

            if (RenderSettings.skybox == null && _fallbackSkybox != null)
            {
                RenderSettings.skybox = _fallbackSkybox;
            }

            if (_ensureDirectionalLight && !HasDirectionalLight())
            {
                var go = new GameObject("FishingDirectionalLight");
                var light = go.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            _visualBaselineApplied = true;
        }

        private void TryApplyPhaseTwoEnvironmentOverride()
        {
            if (_phaseTwoEnvironmentAttempted || !_usePhaseTwoEnvironmentOverride || _catalogService == null || string.IsNullOrWhiteSpace(_phaseTwoSkyboxKey))
            {
                return;
            }

            if (_catalogService.TryGetPhaseTwoEnvironmentMaterial(_phaseTwoSkyboxKey, out var skyboxMaterial))
            {
                RenderSettings.skybox = skyboxMaterial;
                _phaseTwoEnvironmentAttempted = true;
                Debug.Log($"FishingEnvironmentSliceController: applied phase-two skybox override '{_phaseTwoSkyboxKey}'.");
                return;
            }

            if (_catalogService.PhaseTwoEnvironmentLoadCompleted)
            {
                _phaseTwoEnvironmentAttempted = true;
                Debug.Log($"FishingEnvironmentSliceController: phase-two skybox key '{_phaseTwoSkyboxKey}' not found. Keeping fallback skybox.");
            }
        }

        private void EnsureBoundaryColliders()
        {
            if (_boundariesReady || !_autoCreateBoundaryColliders)
            {
                return;
            }

            var root = GameObject.Find(_boundaryRootName);
            if (root == null)
            {
                root = new GameObject(_boundaryRootName);
            }

            var half = _playAreaSize * 0.5f;
            CreateOrUpdateBoundary(root.transform, "Left", new Vector3(_playAreaCenter.x - half.x, _playAreaCenter.y, _playAreaCenter.z), new Vector3(_boundaryThickness, _playAreaSize.y, 1f));
            CreateOrUpdateBoundary(root.transform, "Right", new Vector3(_playAreaCenter.x + half.x, _playAreaCenter.y, _playAreaCenter.z), new Vector3(_boundaryThickness, _playAreaSize.y, 1f));
            CreateOrUpdateBoundary(root.transform, "Top", new Vector3(_playAreaCenter.x, _playAreaCenter.y + half.y, _playAreaCenter.z), new Vector3(_playAreaSize.x, _boundaryThickness, 1f));
            CreateOrUpdateBoundary(root.transform, "Bottom", new Vector3(_playAreaCenter.x, _playAreaCenter.y - half.y, _playAreaCenter.z), new Vector3(_playAreaSize.x, _boundaryThickness, 1f));

            _boundariesReady = true;
        }

        private static void CreateOrUpdateBoundary(Transform root, string name, Vector3 center, Vector3 size)
        {
            var child = root.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, worldPositionStays: false);
                child = go.transform;
            }

            child.position = center;
            var collider = child.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = child.gameObject.AddComponent<BoxCollider>();
            }

            collider.size = size;
            collider.center = Vector3.zero;
            collider.isTrigger = false;
        }

        private static bool HasDirectionalLight()
        {
            var lights = Object.FindObjectsOfType<Light>(true);
            for (var i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_playAreaCenter, _playAreaSize);
        }
    }
}
