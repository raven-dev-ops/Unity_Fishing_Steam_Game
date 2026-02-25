using System;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Fishing;
using RavenDevOps.Fishing.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class InputPromptBindingLabelTests
    {
        [Test]
        public void FishingTutorialController_BuildPromptForStep_UsesReboundBindingLabels()
        {
            var rebindGo = new GameObject("input-rebind-service");
            var mapGo = new GameObject("input-map-controller");
            var tutorialGo = new GameObject("tutorial-controller");
            var rebindService = rebindGo.AddComponent<InputRebindingService>();
            var mapController = mapGo.AddComponent<InputActionMapController>();
            var tutorial = tutorialGo.AddComponent<FishingLoopTutorialController>();
            var actions = LoadInputActionsAsset();

            try
            {
                rebindService.SetInputActions(actions);
                mapController.SetInputActions(actions);

                SetPrivateField(tutorial, "_inputMapController", mapController);
                SetPrivateField(tutorial, "_inputRebindingService", rebindService);

                var moveShip = actions.FindAction("Fishing/MoveShip", false);
                var moveHook = actions.FindAction("Fishing/MoveHook", false);
                Assert.IsNotNull(moveShip);
                Assert.IsNotNull(moveHook);

                var leftIndex = FindCompositePartBindingIndex(moveShip, "negative", "Keyboard");
                var rightIndex = FindCompositePartBindingIndex(moveShip, "positive", "Keyboard");
                var downIndex = FindCompositePartBindingIndex(moveHook, "negative", "Keyboard");
                var upIndex = FindCompositePartBindingIndex(moveHook, "positive", "Keyboard");
                Assert.GreaterOrEqual(leftIndex, 0);
                Assert.GreaterOrEqual(rightIndex, 0);
                Assert.GreaterOrEqual(downIndex, 0);
                Assert.GreaterOrEqual(upIndex, 0);

                moveShip.ApplyBindingOverride(leftIndex, "<Keyboard>/q");
                moveShip.ApplyBindingOverride(rightIndex, "<Keyboard>/e");
                moveHook.ApplyBindingOverride(downIndex, "<Keyboard>/j");
                moveHook.ApplyBindingOverride(upIndex, "<Keyboard>/k");

                var expectedLeft = moveShip.GetBindingDisplayString(leftIndex);
                var expectedRight = moveShip.GetBindingDisplayString(rightIndex);
                var expectedDown = moveHook.GetBindingDisplayString(downIndex);
                var expectedUp = moveHook.GetBindingDisplayString(upIndex);

                var movePrompt = InvokeTutorialPrompt(tutorial, "MoveShip");
                var castPrompt = InvokeTutorialPrompt(tutorial, "Cast");
                var reelPrompt = InvokeTutorialPrompt(tutorial, "Reel");

                Assert.That(movePrompt, Does.Contain(expectedLeft));
                Assert.That(movePrompt, Does.Contain(expectedRight));
                Assert.That(castPrompt, Does.Contain(expectedDown));
                Assert.That(reelPrompt, Does.Contain(expectedUp));
                Assert.That(movePrompt, Does.Not.Contain("A/D"));
                Assert.That(castPrompt, Does.Not.Contain("Down/S"));
                Assert.That(reelPrompt, Does.Not.Contain("Up/W"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actions);
                UnityEngine.Object.DestroyImmediate(rebindGo);
                UnityEngine.Object.DestroyImmediate(mapGo);
                UnityEngine.Object.DestroyImmediate(tutorialGo);
            }
        }

        [Test]
        public void CatchResolver_ControlPrompts_UseReboundBindingLabels()
        {
            var rebindGo = new GameObject("input-rebind-service");
            var resolverGo = new GameObject("catch-resolver");
            var rebindService = rebindGo.AddComponent<InputRebindingService>();
            var resolver = resolverGo.AddComponent<CatchResolver>();
            var actions = LoadInputActionsAsset();

            try
            {
                rebindService.SetInputActions(actions);
                SetPrivateField(resolver, "_inputRebindingService", rebindService);

                var moveHook = actions.FindAction("Fishing/MoveHook", false);
                Assert.IsNotNull(moveHook);

                var downIndex = FindCompositePartBindingIndex(moveHook, "negative", "Keyboard");
                var upIndex = FindCompositePartBindingIndex(moveHook, "positive", "Keyboard");
                Assert.GreaterOrEqual(downIndex, 0);
                Assert.GreaterOrEqual(upIndex, 0);

                moveHook.ApplyBindingOverride(downIndex, "<Keyboard>/j");
                moveHook.ApplyBindingOverride(upIndex, "<Keyboard>/k");

                var expectedDown = moveHook.GetBindingDisplayString(downIndex);
                var expectedUp = moveHook.GetBindingDisplayString(upIndex);

                var castPrompt = InvokePrivateMethod<string>(resolver, "ResolveCastDepthControlPrompt");
                var reelPrompt = InvokePrivateMethod<string>(resolver, "ResolveReelUpControlPrompt");

                Assert.That(castPrompt, Does.Contain(expectedDown));
                Assert.That(reelPrompt, Does.Contain(expectedUp));
                Assert.That(castPrompt, Does.Not.Contain("Down/S"));
                Assert.That(reelPrompt, Does.Not.Contain("Up/W"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actions);
                UnityEngine.Object.DestroyImmediate(rebindGo);
                UnityEngine.Object.DestroyImmediate(resolverGo);
            }
        }

        private static string InvokeTutorialPrompt(FishingLoopTutorialController tutorial, string stepName)
        {
            var stepType = typeof(FishingLoopTutorialController).GetNestedType("TutorialStep", BindingFlags.NonPublic);
            Assert.IsNotNull(stepType, "Expected TutorialStep enum.");

            var stepValue = Enum.Parse(stepType, stepName);
            var buildPrompt = typeof(FishingLoopTutorialController).GetMethod("BuildPromptForStep", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(buildPrompt, "Expected private BuildPromptForStep method.");

            return (string)buildPrompt.Invoke(tutorial, new[] { stepValue });
        }

        private static T InvokePrivateMethod<T>(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected private method '{methodName}'.");
            return (T)method.Invoke(target, args);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static int FindCompositePartBindingIndex(InputAction action, string partName, string deviceLayout)
        {
            for (var i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (!binding.isPartOfComposite
                    || !string.Equals(binding.name, partName, StringComparison.OrdinalIgnoreCase)
                    || !IsBindingForDevice(binding, deviceLayout))
                {
                    continue;
                }

                return i;
            }

            return -1;
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
            return path.StartsWith(token, StringComparison.OrdinalIgnoreCase);
        }

        private static InputActionAsset LoadInputActionsAsset()
        {
            var source = Resources.Load<InputActionAsset>("InputActions_Gameplay");
            if (source != null)
            {
                var instance = UnityEngine.Object.Instantiate(source);
                Assert.IsNotNull(instance, "Expected InputActions_Gameplay asset to be loadable from Resources.");
                return instance;
            }

            var json = Resources.Load<TextAsset>("InputActions_Gameplay");
            Assert.IsNotNull(json, "Expected Resources/InputActions_Gameplay.inputactions to exist.");
            var asset = InputActionAsset.FromJson(json.text);
            Assert.IsNotNull(asset);
            return asset;
        }
    }
}
