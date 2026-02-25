using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Input
{
    public sealed class InputActionMapController : MonoBehaviour
    {
        private const string InputActionsResourcePath = "InputActions_Gameplay";

        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _uiMapName = "UI";
        [SerializeField] private string _harborMapName = "Harbor";
        [SerializeField] private string _fishingMapName = "Fishing";

        private InputContextRouter _contextRouter;
        private bool _missingInputActionsLogged;
        public InputActionAsset InputActions => _inputActions;

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
            if (_inputActions == null)
            {
                LogMissingInputActionsOnce();
            }
            else
            {
                _missingInputActionsLogged = false;
            }

            ApplyContext(_contextRouter != null ? _contextRouter.ActiveContext : InputContext.None);
        }

        public InputAction FindAction(string actionPath)
        {
            return _inputActions != null
                ? _inputActions.FindAction(actionPath, throwIfNotFound: false)
                : null;
        }

        public void ApplyContext(InputContext context)
        {
            DisableAllMaps();

            if (_inputActions == null)
            {
                LogMissingInputActionsOnce();
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

        private void LogMissingInputActionsOnce()
        {
            if (_missingInputActionsLogged)
            {
                return;
            }

            _missingInputActionsLogged = true;
            Debug.LogError(
                $"BOOTSTRAP_ASSET_VALIDATION|status=missing|owner=InputActionMapController|asset=input_actions|path=Resources/{InputActionsResourcePath}|expected=InputActionAsset or TextAsset|details=Input action asset reference was null; context map activation skipped.");
        }
    }
}
