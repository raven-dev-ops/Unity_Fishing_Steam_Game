using System.Collections;
using System.Reflection;
using NUnit.Framework;
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

            SetPrivateField(resolver, "_stateMachine", stateMachine);
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
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }
    }
}
