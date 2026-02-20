using System;
using RavenDevOps.Fishing.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Input
{
    public sealed class InputRebindingService : MonoBehaviour
    {
        private const string BindingOverridesPrefsKey = "settings.inputBindingOverridesJson";

        [SerializeField] private InputActionAsset _inputActions;

        private InputActionRebindingExtensions.RebindingOperation _activeRebind;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
        }

        private void OnDestroy()
        {
            _activeRebind?.Dispose();
            _activeRebind = null;
            RuntimeServiceRegistry.Unregister(this);
        }

        public void SetInputActions(InputActionAsset inputActions)
        {
            _inputActions = inputActions;
            LoadBindingOverrides();
        }

        public void LoadBindingOverrides()
        {
            if (_inputActions == null)
            {
                return;
            }

            var json = PlayerPrefs.GetString(BindingOverridesPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            _inputActions.LoadBindingOverridesFromJson(json);
        }

        public void SaveBindingOverrides()
        {
            if (_inputActions == null)
            {
                return;
            }

            var json = _inputActions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(BindingOverridesPrefsKey, json);
            PlayerPrefs.Save();
        }

        public void ResetAllOverrides()
        {
            if (_inputActions == null)
            {
                return;
            }

            _activeRebind?.Dispose();
            _activeRebind = null;
            _inputActions.RemoveAllBindingOverrides();
            SaveBindingOverrides();
        }

        public bool StartRebindForAction(string actionPath, Action<string> onCompleted = null)
        {
            if (_inputActions == null || string.IsNullOrWhiteSpace(actionPath))
            {
                return false;
            }

            var action = _inputActions.FindAction(actionPath, throwIfNotFound: false);
            if (action == null)
            {
                return false;
            }

            var bindingIndex = FindPreferredBindingIndex(action);
            if (bindingIndex < 0)
            {
                return false;
            }

            _activeRebind?.Dispose();
            _activeRebind = null;

            action.Disable();
            _activeRebind = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Pointer>/position")
                .OnCancel(op =>
                {
                    action.Enable();
                    op.Dispose();
                    _activeRebind = null;
                })
                .OnComplete(op =>
                {
                    action.Enable();
                    SaveBindingOverrides();
                    var effective = action.GetBindingDisplayString(bindingIndex);
                    op.Dispose();
                    _activeRebind = null;
                    onCompleted?.Invoke(effective);
                });

            _activeRebind.Start();
            return true;
        }

        public string GetDisplayBindingForAction(string actionPath)
        {
            if (_inputActions == null || string.IsNullOrWhiteSpace(actionPath))
            {
                return string.Empty;
            }

            var action = _inputActions.FindAction(actionPath, throwIfNotFound: false);
            if (action == null)
            {
                return string.Empty;
            }

            var bindingIndex = FindPreferredBindingIndex(action);
            return bindingIndex >= 0 ? action.GetBindingDisplayString(bindingIndex) : string.Empty;
        }

        private static int FindPreferredBindingIndex(InputAction action)
        {
            if (action == null)
            {
                return -1;
            }

            for (var i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite || string.IsNullOrWhiteSpace(binding.path))
                {
                    continue;
                }

                if (binding.path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            for (var i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (!binding.isComposite && !binding.isPartOfComposite)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
