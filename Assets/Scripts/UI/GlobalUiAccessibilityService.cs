using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.UI
{
    public sealed class GlobalUiAccessibilityService : MonoBehaviour
    {
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private float _refreshIntervalSeconds = 2f;

        private float _refreshElapsed;

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

            ApplyUiScale();
        }

        private void OnDisable()
        {
            if (_settingsService != null)
            {
                _settingsService.SettingsChanged -= OnSettingsChanged;
            }
        }

        private void Update()
        {
            _refreshElapsed += Time.unscaledDeltaTime;
            if (_refreshElapsed < Mathf.Max(0.5f, _refreshIntervalSeconds))
            {
                return;
            }

            _refreshElapsed = 0f;
            ApplyUiScale();
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        private void OnSettingsChanged()
        {
            ApplyUiScale();
        }

        private void ApplyUiScale()
        {
            var uiScale = _settingsService != null ? _settingsService.UiScale : 1f;
            var rootCanvases = Object.FindObjectsOfType<Canvas>(true);
            for (var i = 0; i < rootCanvases.Length; i++)
            {
                var canvas = rootCanvases[i];
                if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                canvas.transform.localScale = Vector3.one * uiScale;
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    scaler.scaleFactor = uiScale;
                }
            }
        }
    }
}
