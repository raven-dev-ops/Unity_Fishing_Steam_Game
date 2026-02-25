using System;
using System.Collections.Generic;
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

        public string GetDisplayBindingForCompositePart(string actionPath, string compositePartName, string deviceLayoutName = null)
        {
            if (!TryFindAction(actionPath, out var action) || string.IsNullOrWhiteSpace(compositePartName))
            {
                return string.Empty;
            }

            for (var i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (!binding.isPartOfComposite
                    || !string.Equals(binding.name, compositePartName, StringComparison.OrdinalIgnoreCase)
                    || !MatchesDeviceLayout(binding, deviceLayoutName))
                {
                    continue;
                }

                var display = action.GetBindingDisplayString(i);
                if (!string.IsNullOrWhiteSpace(display))
                {
                    return display;
                }
            }

            return string.Empty;
        }

        public string GetDisplayBindingsForAction(string actionPath, string deviceLayoutName = null, string separator = " / ", int maxBindings = 2)
        {
            if (!TryFindAction(actionPath, out var action))
            {
                return string.Empty;
            }

            var clampedMaxBindings = Mathf.Max(1, maxBindings);
            if (string.IsNullOrWhiteSpace(separator))
            {
                separator = " / ";
            }

            var displayBindings = new List<string>(clampedMaxBindings);
            for (var i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite || !MatchesDeviceLayout(binding, deviceLayoutName))
                {
                    continue;
                }

                var display = action.GetBindingDisplayString(i);
                if (string.IsNullOrWhiteSpace(display) || ContainsDisplayIgnoreCase(displayBindings, display))
                {
                    continue;
                }

                displayBindings.Add(display);
                if (displayBindings.Count >= clampedMaxBindings)
                {
                    break;
                }
            }

            return displayBindings.Count == 0 ? string.Empty : string.Join(separator, displayBindings);
        }

        private bool TryFindAction(string actionPath, out InputAction action)
        {
            action = null;
            if (_inputActions == null || string.IsNullOrWhiteSpace(actionPath))
            {
                return false;
            }

            action = _inputActions.FindAction(actionPath, throwIfNotFound: false);
            return action != null;
        }

        private static bool MatchesDeviceLayout(InputBinding binding, string deviceLayoutName)
        {
            var requestedLayout = NormalizeDeviceLayoutName(deviceLayoutName);
            if (string.IsNullOrWhiteSpace(requestedLayout))
            {
                return true;
            }

            var path = string.IsNullOrWhiteSpace(binding.effectivePath)
                ? binding.path
                : binding.effectivePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var layoutEnd = path.IndexOf('>');
            if (!path.StartsWith("<", StringComparison.Ordinal) || layoutEnd <= 1)
            {
                return false;
            }

            var actualLayout = path.Substring(1, layoutEnd - 1);
            if (string.Equals(actualLayout, requestedLayout, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return requestedLayout switch
            {
                "Gamepad" => actualLayout.IndexOf("Gamepad", StringComparison.OrdinalIgnoreCase) >= 0,
                "Keyboard" => actualLayout.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0,
                _ => false
            };
        }

        private static string NormalizeDeviceLayoutName(string deviceLayoutName)
        {
            if (string.IsNullOrWhiteSpace(deviceLayoutName))
            {
                return string.Empty;
            }

            return deviceLayoutName.Trim().Trim('<', '>');
        }

        private static bool ContainsDisplayIgnoreCase(List<string> displays, string candidate)
        {
            for (var i = 0; i < displays.Count; i++)
            {
                if (string.Equals(displays[i], candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
