using RavenDevOps.Fishing.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.UI
{
    public sealed class GlobalUiAccessibilityService : MonoBehaviour
    {
        [SerializeField] private UserSettingsService _settingsService;

        private readonly HashSet<Canvas> _registeredCanvases = new HashSet<Canvas>();
        private readonly List<Canvas> _staleCanvases = new List<Canvas>();

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Register(this);
        }

        private void OnEnable()
        {
            if (_settingsService != null)
            {
                _settingsService.SettingsChanged += OnSettingsChanged;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            RegisterLoadedSceneCanvases();
            ApplyUiScale();
        }

        private void OnDisable()
        {
            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= OnSettingsChanged;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            _registeredCanvases.Clear();
            RuntimeServiceRegistry.Unregister(this);
        }

        public void RegisterCanvas(Canvas canvas)
        {
            if (!IsEligibleCanvas(canvas))
            {
                return;
            }

            if (_registeredCanvases.Add(canvas))
            {
                ApplyUiScaleToCanvas(canvas, _settingsService != null ? _settingsService.UiScale : 1f);
            }
        }

        public void UnregisterCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            _registeredCanvases.Remove(canvas);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterSceneCanvases(scene);
            ApplyUiScale();
        }

        private void OnSettingsChanged()
        {
            ApplyUiScale();
        }

        private void RegisterLoadedSceneCanvases()
        {
            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                RegisterSceneCanvases(SceneManager.GetSceneAt(i));
            }
        }

        private void RegisterSceneCanvases(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var canvases = root.GetComponentsInChildren<Canvas>(true);
                for (var j = 0; j < canvases.Length; j++)
                {
                    RegisterCanvas(canvases[j]);
                }
            }
        }

        private void ApplyUiScale()
        {
            var uiScale = _settingsService != null ? _settingsService.UiScale : 1f;
            if (_settingsService != null && _settingsService.ReadabilityBoost)
            {
                uiScale = Mathf.Max(uiScale, 1.1f);
            }

            PruneStaleCanvases();
            foreach (var canvas in _registeredCanvases)
            {
                ApplyUiScaleToCanvas(canvas, uiScale);
            }
        }

        private void ApplyUiScaleToCanvas(Canvas canvas, float uiScale)
        {
            if (!IsEligibleCanvas(canvas))
            {
                return;
            }

            canvas.transform.localScale = Vector3.one * uiScale;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
            {
                scaler.scaleFactor = uiScale;
            }
        }

        private void PruneStaleCanvases()
        {
            _staleCanvases.Clear();
            foreach (var canvas in _registeredCanvases)
            {
                if (canvas == null)
                {
                    _staleCanvases.Add(canvas);
                }
            }

            for (var i = 0; i < _staleCanvases.Count; i++)
            {
                _registeredCanvases.Remove(_staleCanvases[i]);
            }
        }

        private static bool IsEligibleCanvas(Canvas canvas)
        {
            return canvas != null && canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace;
        }
    }
}
