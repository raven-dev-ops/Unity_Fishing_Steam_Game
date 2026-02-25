using NUnit.Framework;
using RavenDevOps.Fishing.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class InputActionArchitectureTests
    {
        private const string RebindPrefsKey = "settings.inputBindingOverridesJson";

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(RebindPrefsKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void InputActionMapController_EnablesOnlyActiveContextMap()
        {
            var go = new GameObject("input-map-test");
            var router = go.AddComponent<InputContextRouter>();
            var mapController = go.AddComponent<InputActionMapController>();
            var asset = LoadInputActionsAsset();

            try
            {
                mapController.SetInputActions(asset);
                mapController.Initialize(router);

                router.SetContext(InputContext.Harbor);
                Assert.IsTrue(asset.FindActionMap("Harbor", false).enabled);
                Assert.IsFalse(asset.FindActionMap("UI", false).enabled);
                Assert.IsFalse(asset.FindActionMap("Fishing", false).enabled);

                router.SetContext(InputContext.UI);
                Assert.IsTrue(asset.FindActionMap("UI", false).enabled);
                Assert.IsFalse(asset.FindActionMap("Harbor", false).enabled);
                Assert.IsFalse(asset.FindActionMap("Fishing", false).enabled);
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void InputRebindingService_SavesAndLoadsBindingOverrides()
        {
            var serviceGo = new GameObject("input-rebind-save");
            var saveService = serviceGo.AddComponent<InputRebindingService>();
            var saveAsset = LoadInputActionsAsset();
            var loadGo = new GameObject("input-rebind-load");
            var loadService = loadGo.AddComponent<InputRebindingService>();
            var loadAsset = LoadInputActionsAsset();

            try
            {
                saveService.SetInputActions(saveAsset);

                var action = saveAsset.FindAction("Fishing/Action", false);
                var keyboardBindingIndex = FindKeyboardBindingIndex(action);
                Assert.GreaterOrEqual(keyboardBindingIndex, 0);

                action.ApplyBindingOverride(keyboardBindingIndex, "<Keyboard>/k");
                saveService.SaveBindingOverrides();

                loadService.SetInputActions(loadAsset);

                var loadedAction = loadAsset.FindAction("Fishing/Action", false);
                Assert.AreEqual("<Keyboard>/k", loadedAction.bindings[keyboardBindingIndex].effectivePath);
            }
            finally
            {
                Object.DestroyImmediate(saveAsset);
                Object.DestroyImmediate(loadAsset);
                Object.DestroyImmediate(serviceGo);
                Object.DestroyImmediate(loadGo);
            }
        }

        [Test]
        public void InputRebindingService_ResolvesCompositeAndDeviceBindingDisplays()
        {
            var serviceGo = new GameObject("input-rebind-display");
            var service = serviceGo.AddComponent<InputRebindingService>();
            var asset = LoadInputActionsAsset();

            try
            {
                service.SetInputActions(asset);
                var moveHook = asset.FindAction("Fishing/MoveHook", false);
                Assert.IsNotNull(moveHook);

                var negativeKeyboardIndex = FindCompositePartBindingIndex(moveHook, "negative", "Keyboard");
                var positiveKeyboardIndex = FindCompositePartBindingIndex(moveHook, "positive", "Keyboard");
                Assert.GreaterOrEqual(negativeKeyboardIndex, 0);
                Assert.GreaterOrEqual(positiveKeyboardIndex, 0);

                var expectedDown = moveHook.GetBindingDisplayString(negativeKeyboardIndex);
                var expectedUp = moveHook.GetBindingDisplayString(positiveKeyboardIndex);
                var expectedGamepad = BuildExpectedNonCompositeDisplay(moveHook, "Gamepad", " or ", 2);

                Assert.AreEqual(
                    expectedDown,
                    service.GetDisplayBindingForCompositePart("Fishing/MoveHook", "negative", "Keyboard"));
                Assert.AreEqual(
                    expectedUp,
                    service.GetDisplayBindingForCompositePart("Fishing/MoveHook", "positive", "Keyboard"));
                Assert.AreEqual(
                    expectedGamepad,
                    service.GetDisplayBindingsForAction("Fishing/MoveHook", "Gamepad", " or ", 2));
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(serviceGo);
            }
        }

        [Test]
        public void InputRebindingService_CompositeDisplayResolversReflectOverrides()
        {
            var serviceGo = new GameObject("input-rebind-display-overrides");
            var service = serviceGo.AddComponent<InputRebindingService>();
            var asset = LoadInputActionsAsset();

            try
            {
                service.SetInputActions(asset);
                var moveHook = asset.FindAction("Fishing/MoveHook", false);
                Assert.IsNotNull(moveHook);

                var negativeKeyboardIndex = FindCompositePartBindingIndex(moveHook, "negative", "Keyboard");
                var positiveKeyboardIndex = FindCompositePartBindingIndex(moveHook, "positive", "Keyboard");
                Assert.GreaterOrEqual(negativeKeyboardIndex, 0);
                Assert.GreaterOrEqual(positiveKeyboardIndex, 0);

                moveHook.ApplyBindingOverride(negativeKeyboardIndex, "<Keyboard>/j");
                moveHook.ApplyBindingOverride(positiveKeyboardIndex, "<Keyboard>/k");

                Assert.AreEqual(
                    moveHook.GetBindingDisplayString(negativeKeyboardIndex),
                    service.GetDisplayBindingForCompositePart("Fishing/MoveHook", "negative", "Keyboard"));
                Assert.AreEqual(
                    moveHook.GetBindingDisplayString(positiveKeyboardIndex),
                    service.GetDisplayBindingForCompositePart("Fishing/MoveHook", "positive", "Keyboard"));
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(serviceGo);
            }
        }

        private static InputActionAsset LoadInputActionsAsset()
        {
            var source = Resources.Load<InputActionAsset>("InputActions_Gameplay");
            if (source != null)
            {
                var instance = Object.Instantiate(source);
                Assert.IsNotNull(instance, "Expected InputActions_Gameplay asset to be loadable from Resources.");
                return instance;
            }

            var json = Resources.Load<TextAsset>("InputActions_Gameplay");
            Assert.IsNotNull(json, "Expected Resources/InputActions_Gameplay.inputactions to exist.");
            var asset = InputActionAsset.FromJson(json.text);
            Assert.IsNotNull(asset);
            return asset;
        }

        private static int FindKeyboardBindingIndex(InputAction action)
        {
            for (var i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite || string.IsNullOrWhiteSpace(binding.path))
                {
                    continue;
                }

                if (binding.path.StartsWith("<Keyboard>"))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindCompositePartBindingIndex(InputAction action, string partName, string deviceLayout)
        {
            for (var i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (!binding.isPartOfComposite
                    || !string.Equals(binding.name, partName)
                    || !IsBindingForDevice(binding, deviceLayout))
                {
                    continue;
                }

                return i;
            }

            return -1;
        }

        private static string BuildExpectedNonCompositeDisplay(InputAction action, string deviceLayout, string separator, int maxBindings)
        {
            var result = string.Empty;
            var count = 0;
            for (var i = 0; i < action.bindings.Count && count < maxBindings; i++)
            {
                var binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite || !IsBindingForDevice(binding, deviceLayout))
                {
                    continue;
                }

                var display = action.GetBindingDisplayString(i);
                if (string.IsNullOrWhiteSpace(display))
                {
                    continue;
                }

                if (count == 0)
                {
                    result = display;
                }
                else
                {
                    result += separator + display;
                }

                count++;
            }

            return result;
        }

        private static bool IsBindingForDevice(InputBinding binding, string deviceLayout)
        {
            var path = string.IsNullOrWhiteSpace(binding.effectivePath)
                ? binding.path
                : binding.effectivePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var token = "<" + deviceLayout + ">";
            return path.StartsWith(token);
        }
    }
}
