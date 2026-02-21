using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class UiAccessibilityCanvasRegistrant : MonoBehaviour
    {
        [SerializeField] private GlobalUiAccessibilityService _accessibilityService;
        [SerializeField] private Canvas _canvas;

        private void Awake()
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }

            RuntimeServiceRegistry.Resolve(ref _accessibilityService, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }

            if (_accessibilityService == null)
            {
                RuntimeServiceRegistry.Resolve(ref _accessibilityService, this, warnIfMissing: false);
            }

            _accessibilityService?.RegisterCanvas(_canvas);
        }

        private void Start()
        {
            if (_accessibilityService == null)
            {
                RuntimeServiceRegistry.Resolve(ref _accessibilityService, this, warnIfMissing: false);
            }

            _accessibilityService?.RegisterCanvas(_canvas);
        }

        private void OnDisable()
        {
            _accessibilityService?.UnregisterCanvas(_canvas);
        }
    }
}
