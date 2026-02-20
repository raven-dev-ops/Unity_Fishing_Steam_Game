using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.UI
{
    public sealed class PhotoModeRuntimeService : MonoBehaviour
    {
        [SerializeField] private bool _enableByDefault = true;
        [SerializeField] private bool _autoAttachToMainCamera = true;

        private Camera _lastBoundCamera;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
        }

        private void Update()
        {
            if (!_enableByDefault || !_autoAttachToMainCamera)
            {
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera == null || mainCamera == _lastBoundCamera)
            {
                return;
            }

            if (mainCamera.GetComponent<PhotoModeController>() == null)
            {
                mainCamera.gameObject.AddComponent<PhotoModeController>();
            }

            _lastBoundCamera = mainCamera;
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }
    }
}
