using System.Collections;
using System.Collections.Generic;
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
            RuntimeServiceRegistry.Register(saveManager);

            var resolver = CreateComponent<CatchResolver>("CatchResolver_Level1Tap");
            resolver.ConfigureReelInputEvaluationForTests(saveManager, levelOneReelPulseDurationSeconds: 0.06f);
            yield return null;

            Assert.That(resolver.IsReelEffortActiveForTests(), Is.False, "Lv1 should not reel without taps.");

            PrimeResolverUpPress(resolver, pressedThisFrame: true);
            Assert.That(resolver.IsReelEffortActiveForTests(), Is.True, "Lv1 should reel on tap pulse.");

            var expired = false;
            var timeoutAt = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                PrimeResolverUpPress(resolver, pressedThisFrame: false);
                if (!resolver.IsReelEffortActiveForTests())
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
            RuntimeServiceRegistry.Register(saveManager);

            var stateMachine = CreateComponent<FishingActionStateMachine>("StateMachine_Level2Hold");
            stateMachine.SetInWater();
            stateMachine.SetHooked();
            stateMachine.AdvanceByAction();

            var castController = CreateComponent<FishingHookCastDropController>("CastController_Level2Hold");
            castController.ConfigureReelInputEvaluationForTests(saveManager, stateMachine, levelTwoReelSpeedMultiplier: 2f);
            yield return null;

            Assert.That(castController.ShouldApplyReelMotionForTests(), Is.False, "Lv2 should not reel without holding Up/W.");
            Assert.That(castController.ResolveHookReelSpeedMultiplierForTests(), Is.EqualTo(2f).Within(0.001f), "Lv2 should apply 2x reel multiplier.");
        }

        [UnityTest]
        public IEnumerator HookLevel3_AutoReel_StartsOnSinglePressAndDisablesInWaterRaiseDoubleTap()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_Level3Auto");
            yield return null;
            saveManager.Current.equippedHookId = "hook_lv3";
            RuntimeServiceRegistry.Register(saveManager);

            var resolver = CreateComponent<CatchResolver>("CatchResolver_Level3Auto");
            resolver.ConfigureReelInputEvaluationForTests(saveManager, levelOneReelPulseDurationSeconds: 0.2f);

            var castController = CreateComponent<FishingHookCastDropController>("CastController_Level3Auto");
            castController.ConfigureReelInputEvaluationForTests(saveManager, stateMachine: null, levelTwoReelSpeedMultiplier: 2f);
            yield return null;

            PrimeResolverUpPress(resolver, pressedThisFrame: true);
            Assert.That(
                resolver.ShouldStartReelFromHookedInputForTests(),
                Is.True,
                "Lv3 first press should start auto reel.");

            Assert.That(
                castController.CanUseInWaterAutoRaiseDoubleTapForTests(),
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
            resolver.PrimeUpPressForTests(pressedThisFrame);
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
