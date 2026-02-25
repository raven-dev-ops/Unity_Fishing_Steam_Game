using System.Collections;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Fishing;
using UnityEngine;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    public sealed class CatchResolverIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator ReelWithoutHookedFish_RaisesMissedHookEventAndResetsToCast()
        {
            var root = new GameObject("CatchResolverIntegration");
            var stateMachine = root.AddComponent<FishingActionStateMachine>();
            var resolver = root.AddComponent<CatchResolver>();
            var ship = new GameObject("CatchResolverIntegrationShip").transform;
            ship.position = Vector3.zero;
            var hookGo = new GameObject("CatchResolverIntegrationHook");
            hookGo.transform.position = new Vector3(0f, -1f, 0f);
            var hookController = hookGo.AddComponent<HookMovementController>();
            hookController.ConfigureShipTransform(ship);

            resolver.Configure(stateMachine, null, hookController, null);
            resolver.enabled = false;
            resolver.enabled = true;

            var eventRaised = false;
            var eventSuccess = true;
            var eventFailReason = FishingFailReason.None;
            resolver.CatchResolved += (success, failReason, _) =>
            {
                eventRaised = true;
                eventSuccess = success;
                eventFailReason = failReason;
            };

            stateMachine.AdvanceByAction();
            stateMachine.SetHooked();
            stateMachine.AdvanceByAction();

            yield return null;

            Assert.That(eventRaised, Is.True);
            Assert.That(eventSuccess, Is.False);
            Assert.That(eventFailReason, Is.EqualTo(FishingFailReason.MissedHook));
            Assert.That(stateMachine.State, Is.EqualTo(FishingActionState.Cast));

            Object.Destroy(root);
            Object.Destroy(ship.gameObject);
            Object.Destroy(hookGo);
        }

        [UnityTest]
        public IEnumerator ExplicitDependencyBundle_CatchFlowWorksWithoutAutoAttachedSetup()
        {
            var root = new GameObject("CatchResolverExplicitDependencies");
            var stateMachine = root.AddComponent<FishingActionStateMachine>();
            var resolver = root.AddComponent<CatchResolver>();
            var hud = root.AddComponent<TestFishingHudOverlay>();
            var ambient = root.AddComponent<FishingAmbientFishSwimController>();

            var shipGo = new GameObject("CatchResolverExplicitDependenciesShip");
            var ship = shipGo.transform;
            ship.position = Vector3.zero;
            var shipMovement = shipGo.AddComponent<ShipMovementController>();

            var hookGo = new GameObject("CatchResolverExplicitDependenciesHook");
            hookGo.transform.position = new Vector3(0f, -1f, 0f);
            var hookController = hookGo.AddComponent<HookMovementController>();
            hookController.ConfigureShipTransform(ship);

            SetPrivateField(resolver, "_autoAttachFishingCameraController", false);
            SetPrivateField(resolver, "_autoAttachFishingTutorialController", false);
            SetPrivateField(resolver, "_autoAttachEnvironmentSliceController", false);
            SetPrivateField(resolver, "_autoAttachConditionController", false);

            resolver.Configure(stateMachine, null, hookController, hud);
            resolver.ConfigureDependencies(
                new CatchResolver.DependencyBundle
                {
                    HudOverlay = hud,
                    AmbientFishController = ambient,
                    ShipMovement = shipMovement
                });

            var eventRaised = false;
            var eventFailReason = FishingFailReason.None;
            resolver.CatchResolved += (_, failReason, _) =>
            {
                eventRaised = true;
                eventFailReason = failReason;
            };

            stateMachine.AdvanceByAction();
            stateMachine.SetHooked();
            stateMachine.AdvanceByAction();
            yield return null;

            Assert.That(eventRaised, Is.True);
            Assert.That(eventFailReason, Is.EqualTo(FishingFailReason.MissedHook));
            Assert.That(root.GetComponent<FishingLoopTutorialController>(), Is.Null, "Tutorial controller should not be auto-attached.");
            Assert.That(root.GetComponent<FishingEnvironmentSliceController>(), Is.Null, "Environment controller should not be auto-attached.");
            Assert.That(root.GetComponent<FishingConditionController>(), Is.Null, "Condition controller should not be auto-attached.");

            Object.Destroy(root);
            Object.Destroy(shipGo);
            Object.Destroy(hookGo);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private sealed class TestFishingHudOverlay : MonoBehaviour, IFishingHudOverlay
        {
            public void SetFishingTelemetry(int distanceTier, float depth) { }
            public void SetFishingTension(float normalizedTension, FishingTensionState tensionState) { }
            public void SetFishingStatus(string status) { }
            public void SetFishingFailure(string failure) { }
            public void SetFishingConditions(string conditionLabel) { }
        }
    }
}
