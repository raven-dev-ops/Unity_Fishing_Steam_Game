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
    }
}
