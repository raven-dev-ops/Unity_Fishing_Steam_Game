using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Input
{
    public sealed class InputActionMapController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _uiMapName = "UI";
        [SerializeField] private string _harborMapName = "Harbor";
        [SerializeField] private string _fishingMapName = "Fishing";

        private InputContextRouter _contextRouter;

        public void Initialize(InputContextRouter contextRouter)
        {
            if (_contextRouter != null)
            {
                _contextRouter.ContextChanged -= OnContextChanged;
            }

            _contextRouter = contextRouter;

            if (_contextRouter != null)
            {
                _contextRouter.ContextChanged += OnContextChanged;
                ApplyContext(_contextRouter.ActiveContext);
            }
        }

        private void Awake()
        {
            if (_contextRouter == null)
            {
                _contextRouter = GetComponent<InputContextRouter>();
            }
        }

        private void OnEnable()
        {
            if (_contextRouter != null)
            {
                Initialize(_contextRouter);
            }
        }

        private void OnDisable()
        {
            if (_contextRouter != null)
            {
                _contextRouter.ContextChanged -= OnContextChanged;
            }

            DisableAllMaps();
        }

        public void SetInputActions(InputActionAsset inputActions)
        {
            _inputActions = inputActions;
            ApplyContext(_contextRouter != null ? _contextRouter.ActiveContext : InputContext.None);
        }

        public void ApplyContext(InputContext context)
        {
            DisableAllMaps();

            if (_inputActions == null)
            {
                return;
            }

            var mapName = GetMapName(context);
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return;
            }

            var map = _inputActions.FindActionMap(mapName, throwIfNotFound: false);
            if (map != null)
            {
                map.Enable();
            }
        }

        private void DisableAllMaps()
        {
            if (_inputActions == null)
            {
                return;
            }

            foreach (var map in _inputActions.actionMaps)
            {
                map.Disable();
            }
        }

        private void OnContextChanged(InputContext previous, InputContext next)
        {
            ApplyContext(next);
        }

        private string GetMapName(InputContext context)
        {
            switch (context)
            {
                case InputContext.UI:
                    return _uiMapName;
                case InputContext.Harbor:
                    return _harborMapName;
                case InputContext.Fishing:
                    return _fishingMapName;
                default:
                    return null;
            }
        }
    }
}
