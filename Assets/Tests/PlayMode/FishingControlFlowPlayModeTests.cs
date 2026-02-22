using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Fishing;
using UnityEngine;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    public sealed class FishingControlFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator AutoCast_DropsHookToConfiguredDepth()
        {
            var root = new GameObject("FishingControlFlow_AutoCast");
            var stateMachine = root.AddComponent<FishingActionStateMachine>();
            var castController = root.AddComponent<FishingHookCastDropController>();

            var ship = new GameObject("FishingControlFlow_Ship").transform;
            ship.position = Vector3.zero;

            var hookGo = new GameObject("FishingControlFlow_Hook");
            hookGo.transform.position = new Vector3(0f, -0.8f, 0f);
            var hookController = hookGo.AddComponent<HookMovementController>();
            hookController.ConfigureShipTransform(ship);

            SetPrivateField(castController, "_autoDropSpeed", 80f);
            SetPrivateField(castController, "_initialAutoCastDepth", 25f);
            castController.Configure(stateMachine, hookController, ship);

            stateMachine.AdvanceByAction();

            var timeoutAt = Time.realtimeSinceStartup + 2f;
            while (hookController.CurrentDepth < 24.5f && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(stateMachine.State, Is.EqualTo(FishingActionState.InWater));
            Assert.That(hookController.CurrentDepth, Is.GreaterThanOrEqualTo(24.5f));
            Assert.That(hookController.CurrentDepth, Is.LessThanOrEqualTo(25.5f));

            Object.Destroy(root);
            Object.Destroy(ship.gameObject);
            Object.Destroy(hookGo);
        }

        [UnityTest]
        public IEnumerator ShipSteeringLock_TracksFishingCastStates()
        {
            var root = new GameObject("FishingControlFlow_SteeringLock");
            var stateMachine = root.AddComponent<FishingActionStateMachine>();
            var shipMovement = root.AddComponent<ShipMovementController>();
            shipMovement.ConfigureFishingStateMachine(stateMachine);

            yield return null;

            Assert.That(shipMovement.SteeringLockedForFishingState, Is.False, "Cast (idle) should allow steering.");

            stateMachine.AdvanceByAction();
            yield return null;
            Assert.That(shipMovement.SteeringLockedForFishingState, Is.True, "InWater should lock steering while hook is cast.");

            stateMachine.SetHooked();
            yield return null;
            Assert.That(shipMovement.SteeringLockedForFishingState, Is.True, "Hooked should lock steering.");

            yield return null;
            stateMachine.AdvanceByAction();
            yield return null;
            Assert.That(shipMovement.SteeringLockedForFishingState, Is.True, "Reel should keep steering locked.");

            stateMachine.ResetToCast();
            yield return null;
            Assert.That(shipMovement.SteeringLockedForFishingState, Is.False, "Returning to Cast should unlock steering.");

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ReelFight_WithoutReelEffort_AllowsFishEscape()
        {
            var root = new GameObject("FishingControlFlow_ReelEscape");
            var stateMachine = root.AddComponent<FishingActionStateMachine>();
            var resolver = root.AddComponent<CatchResolver>();

            var ship = new GameObject("FishingControlFlow_ReelEscape_Ship").transform;
            ship.position = Vector3.zero;

            var hookGo = new GameObject("FishingControlFlow_ReelEscape_Hook");
            hookGo.transform.position = new Vector3(0f, -30f, 0f);
            var hookController = hookGo.AddComponent<HookMovementController>();
            hookController.ConfigureShipTransform(ship);

            SetPrivateField(resolver, "_minimumReelEscapeWindowSeconds", 0.1f);
            SetPrivateField(resolver, "_reelEscapeDrainWhileIdle", 8f);
            SetPrivateField(resolver, "_reelEscapeDrainWhileReeling", 0.5f);
            resolver.Configure(stateMachine, null, hookController, null);

            var raised = false;
            var success = true;
            var failReason = FishingFailReason.None;
            resolver.CatchResolved += (didCatch, reason, _) =>
            {
                raised = true;
                success = didCatch;
                failReason = reason;
            };

            stateMachine.AdvanceByAction();
            yield return null;

            var fastEscapeFish = new FishDefinition
            {
                id = "fish_escape_test",
                minDistanceTier = 1,
                maxDistanceTier = 1,
                minDepth = 20f,
                maxDepth = 60f,
                rarityWeight = 1,
                baseValue = 10,
                minBiteDelaySeconds = 0.1f,
                maxBiteDelaySeconds = 0.1f,
                fightStamina = 4f,
                pullIntensity = 1f,
                escapeSeconds = 0.2f,
                minCatchWeightKg = 0.5f,
                maxCatchWeightKg = 1f
            };

            SetPrivateField(resolver, "_targetFish", fastEscapeFish);
            stateMachine.SetHooked();
            yield return null;

            stateMachine.AdvanceByAction();

            var timeoutAt = Time.realtimeSinceStartup + 2f;
            while (!raised && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(raised, Is.True, "Expected CatchResolved to fire.");
            Assert.That(success, Is.False, "Expected failed catch when reel effort is not applied.");
            Assert.That(failReason, Is.EqualTo(FishingFailReason.FishEscaped), "Expected fish to escape during reel struggle.");
            Assert.That(stateMachine.State, Is.EqualTo(FishingActionState.Cast));

            Object.Destroy(root);
            Object.Destroy(ship.gameObject);
            Object.Destroy(hookGo);
        }

        [UnityTest]
        public IEnumerator BiteAttraction_RequiresHookToRemainStationary()
        {
            var root = new GameObject("FishingControlFlow_StationaryAttraction");
            var stateMachine = root.AddComponent<FishingActionStateMachine>();
            var resolver = root.AddComponent<CatchResolver>();

            var ship = new GameObject("FishingControlFlow_StationaryAttraction_Ship").transform;
            ship.position = Vector3.zero;

            var hookGo = new GameObject("FishingControlFlow_StationaryAttraction_Hook");
            hookGo.transform.position = new Vector3(0f, -30f, 0f);
            var hookController = hookGo.AddComponent<HookMovementController>();
            hookController.ConfigureShipTransform(ship);

            resolver.Configure(stateMachine, null, hookController, null);

            stateMachine.AdvanceByAction();
            yield return null;
            Assert.That(stateMachine.State, Is.EqualTo(FishingActionState.InWater), "Expected in-water state after cast.");

            var testFish = new FishDefinition
            {
                id = "fish_stationary_delay_test",
                minDistanceTier = 1,
                maxDistanceTier = 1,
                minDepth = 20f,
                maxDepth = 60f,
                rarityWeight = 1,
                baseValue = 10,
                minBiteDelaySeconds = 0.1f,
                maxBiteDelaySeconds = 0.1f,
                fightStamina = 2f,
                pullIntensity = 1f,
                escapeSeconds = 1f,
                minCatchWeightKg = 0.5f,
                maxCatchWeightKg = 1f
            };

            SetPrivateField(resolver, "_targetFish", testFish);
            SetPrivateField(resolver, "_biteSelectionResolvedForCurrentDrop", true);
            SetPrivateField(resolver, "_biteTimerSeconds", 0f);
            SetPrivateField(resolver, "_hookStationaryAttractionDelayRangeSeconds", new Vector2(0.25f, 0.25f));
            SetPrivateField(resolver, "_hookStationaryDelaySeconds", 0.25f);
            SetPrivateField(resolver, "_hookStationaryElapsedSeconds", 0f);
            SetPrivateField(resolver, "_hasRecordedHookPositionForStationaryCheck", false);

            var initialWaitEndsAt = Time.realtimeSinceStartup + 0.12f;
            while (Time.realtimeSinceStartup < initialWaitEndsAt)
            {
                yield return null;
            }

            Assert.That(stateMachine.State, Is.EqualTo(FishingActionState.InWater), "Fish should not hook before stationary delay has elapsed.");

            var movedPosition = hookGo.transform.position;
            movedPosition.y -= 1f;
            hookGo.transform.position = movedPosition;
            yield return null;

            var postMoveWaitEndsAt = Time.realtimeSinceStartup + 0.18f;
            while (Time.realtimeSinceStartup < postMoveWaitEndsAt)
            {
                yield return null;
            }

            Assert.That(stateMachine.State, Is.EqualTo(FishingActionState.InWater), "Hook movement should reset the attraction delay.");

            var timeoutAt = Time.realtimeSinceStartup + 1.2f;
            while (stateMachine.State != FishingActionState.Hooked && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(stateMachine.State, Is.EqualTo(FishingActionState.Hooked), "Fish should hook after the hook remains still for the configured delay.");

            Object.Destroy(root);
            Object.Destroy(ship.gameObject);
            Object.Destroy(hookGo);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }
    }
}
