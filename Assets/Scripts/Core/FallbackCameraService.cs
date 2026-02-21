using UnityEngine;
using UnityEngine.SceneManagement;

namespace RavenDevOps.Fishing.Core
{
    public sealed class FallbackCameraService : MonoBehaviour
    {
        private const string FallbackCameraName = "__FallbackMainCamera";
        private const float ScanIntervalSeconds = 0.5f;

        [SerializeField] private bool _verboseLogs;

        private Camera _fallbackCamera;
        private float _nextScanTime;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            RefreshCameraState(forceScan: true);
        }

        private void Update()
        {
            RefreshCameraState(forceScan: false);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _nextScanTime = 0f;
            RefreshCameraState(forceScan: true);
        }

        private void RefreshCameraState(bool forceScan)
        {
            if (!forceScan && Time.unscaledTime < _nextScanTime)
            {
                return;
            }

            _nextScanTime = Time.unscaledTime + ScanIntervalSeconds;
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return;
            }

            var hasNonFallbackCamera = HasEnabledSceneCamera(activeScene);
            if (hasNonFallbackCamera)
            {
                DestroyFallbackCamera();
                return;
            }

            EnsureFallbackCamera(activeScene);
        }

        private bool HasEnabledSceneCamera(Scene activeScene)
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
            {
                var candidate = cameras[i];
                if (candidate == null || candidate == _fallbackCamera)
                {
                    continue;
                }

                if (!candidate.enabled || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (candidate.gameObject.scene != activeScene)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private void EnsureFallbackCamera(Scene activeScene)
        {
            if (_fallbackCamera != null && _fallbackCamera.gameObject != null)
            {
                if (_fallbackCamera.gameObject.scene == activeScene && _fallbackCamera.enabled)
                {
                    return;
                }

                DestroyFallbackCamera();
            }

            var cameraGo = new GameObject(FallbackCameraName);
            cameraGo.tag = "MainCamera";
            SceneManager.MoveGameObjectToScene(cameraGo, activeScene);

            _fallbackCamera = cameraGo.AddComponent<Camera>();
            _fallbackCamera.clearFlags = CameraClearFlags.SolidColor;
            _fallbackCamera.backgroundColor = Color.black;
            _fallbackCamera.fieldOfView = 60f;
            _fallbackCamera.nearClipPlane = 0.01f;
            _fallbackCamera.farClipPlane = 1000f;
            _fallbackCamera.depth = -100f;

            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 1f, -10f);
            cameraGo.transform.rotation = Quaternion.identity;

            if (_verboseLogs)
            {
                Debug.Log($"FallbackCameraService: created fallback camera in scene '{activeScene.path}'.");
            }
        }

        private void DestroyFallbackCamera()
        {
            if (_fallbackCamera == null)
            {
                return;
            }

            var go = _fallbackCamera.gameObject;
            _fallbackCamera = null;
            if (go != null)
            {
                Destroy(go);
            }
        }
    }
}
