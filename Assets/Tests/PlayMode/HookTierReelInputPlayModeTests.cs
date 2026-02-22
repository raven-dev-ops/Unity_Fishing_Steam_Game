using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Fishing;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    public sealed class HookTierReelInputPlayModeTests
    {
        private readonly List<GameObject> _roots = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            CleanupSingletons();
            RuntimeServiceRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupSingletons();
            RuntimeServiceRegistry.Clear();

            for (var i = _roots.Count - 1; i >= 0; i--)
            {
                if (_roots[i] != null)
                {
                    Object.DestroyImmediate(_roots[i]);
                }
            }

            _roots.Clear();
        }

        [UnityTest]
        public IEnumerator HookLevel1_ReelEffort_UsesTapPulse()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_Level1Tap");
            yield return null;
            saveManager.Current.equippedHookId = "hook_lv1";

            var resolver = CreateComponent<CatchResolver>("CatchResolver_Level1Tap");
            SetPrivateField(resolver, "_saveManager", saveManager);
            SetPrivateField(resolver, "_levelOneReelPulseDurationSeconds", 0.06f);
            yield return null;

            Assert.That(InvokePrivate<bool>(resolver, "IsReelEffortActive"), Is.False, "Lv1 should not reel without taps.");

            PrimeResolverUpPress(resolver, pressedThisFrame: true);
            Assert.That(InvokePrivate<bool>(resolver, "IsReelEffortActive"), Is.True, "Lv1 should reel on tap pulse.");

            var expired = false;
            var timeoutAt = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                PrimeResolverUpPress(resolver, pressedThisFrame: false);
                if (!InvokePrivate<bool>(resolver, "IsReelEffortActive"))
                {
                    expired = true;
                    break;
                }

                yield return null;
            }

            Assert.That(expired, Is.True, "Lv1 pulse should expire without further taps.");
        }

        [UnityTest]
        public IEnumerator HookLevel2_ReelMotion_RequiresHoldAndUsesDoubleSpeed()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_Level2Hold");
            yield return null;
            saveManager.Current.equippedHookId = "hook_lv2";

            var stateMachine = CreateComponent<FishingActionStateMachine>("StateMachine_Level2Hold");
            SetPrivateField(stateMachine, "_state", FishingActionState.Reel);

            var castController = CreateComponent<FishingHookCastDropController>("CastController_Level2Hold");
            SetPrivateField(castController, "_saveManager", saveManager);
            SetPrivateField(castController, "_stateMachine", stateMachine);
            SetPrivateField(castController, "_levelTwoReelSpeedMultiplier", 2f);
            yield return null;

            Assert.That(InvokePrivate<bool>(castController, "ShouldApplyReelMotion"), Is.False, "Lv2 should not reel without holding Up/W.");
            Assert.That(InvokePrivate<float>(castController, "ResolveHookReelSpeedMultiplier"), Is.EqualTo(2f).Within(0.001f), "Lv2 should apply 2x reel multiplier.");
        }

        [UnityTest]
        public IEnumerator HookLevel3_AutoReel_RequiresDoubleTapAndDisablesInWaterRaiseDoubleTap()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_Level3Auto");
            yield return null;
            saveManager.Current.equippedHookId = "hook_lv3";

            var resolver = CreateComponent<CatchResolver>("CatchResolver_Level3Auto");
            SetPrivateField(resolver, "_saveManager", saveManager);
            SetPrivateField(resolver, "_hookedDoubleTapWindowSeconds", 0.35f);

            var castController = CreateComponent<FishingHookCastDropController>("CastController_Level3Auto");
            SetPrivateField(castController, "_saveManager", saveManager);
            yield return null;

            PrimeResolverUpPress(resolver, pressedThisFrame: true);
            Assert.That(
                InvokePrivate<bool>(resolver, "ShouldStartReelFromHookedInput"),
                Is.False,
                "Lv3 first tap should not start auto reel.");

            yield return null;
            SetPrivateField(resolver, "_lastHookedUpPressTime", Time.unscaledTime - 0.1f);
            PrimeResolverUpPress(resolver, pressedThisFrame: true);
            Assert.That(
                InvokePrivate<bool>(resolver, "ShouldStartReelFromHookedInput"),
                Is.True,
                "Lv3 second tap inside window should start auto reel.");

            Assert.That(
                InvokePrivate<bool>(castController, "CanUseInWaterAutoRaiseDoubleTap"),
                Is.False,
                "Lv3 should not allow in-water Up/W double-tap raise before bite.");
        }

        private T CreateComponent<T>(string rootName) where T : Component
        {
            var root = new GameObject(rootName);
            _roots.Add(root);
            return root.AddComponent<T>();
        }

        private static void PrimeResolverUpPress(CatchResolver resolver, bool pressedThisFrame)
        {
            SetPrivateField(resolver, "_cachedUpPressFrame", Time.frameCount);
            SetPrivateField(resolver, "_cachedUpPressResult", pressedThisFrame);
        }

        private static T InvokePrivate<T>(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected private method '{methodName}' to exist.");
            return (T)method.Invoke(target, args);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private static void CleanupSingletons()
        {
            if (SaveManager.Instance != null)
            {
                Object.DestroyImmediate(SaveManager.Instance.gameObject);
            }
        }
    }
}
