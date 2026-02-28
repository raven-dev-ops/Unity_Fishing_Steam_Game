using System;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Fishing;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class FishingLoopTutorialDemoConfigTests
    {
        [Test]
        public void DemoSceneTitle_IntroInfo_UsesHowToPlayDemoTitle()
        {
            var phase = ResolveDemoPhaseValue("IntroInfo");
            var title = InvokePrivateStatic<string>("BuildDemoSceneTitle", phase);
            Assert.That(title, Is.EqualTo("How To Play: Demo"));
        }

        [Test]
        public void DemoSceneSubtitle_IntroInfo_UsesLoadingGuidedFlowSubtitle()
        {
            var phase = ResolveDemoPhaseValue("IntroInfo");
            var subtitle = InvokePrivateStatic<string>("BuildDemoSceneSubtitle", phase);
            Assert.That(subtitle, Is.EqualTo("Loading guided tutorial flow"));
        }

        [Test]
        public void Scene8AndScene9LightRadii_ApplyConfiguredBoost()
        {
            var go = new GameObject("demo-config-tests");
            var controller = go.AddComponent<FishingLoopTutorialController>();

            try
            {
                SetPrivateField(controller, "_demoScene89LightRadiusBoostMeters", 10f);
                SetPrivateField(controller, "_demoLevel4LightRadiiMeters", new Vector2(16f, 8f));
                SetPrivateField(controller, "_demoLevel5LightRadiiMeters", new Vector2(30f, 15f));

                var scene8 = InvokePrivateInstance<Vector2>(controller, "ResolveScene8LightRadiiMeters");
                var scene9 = InvokePrivateInstance<Vector2>(controller, "ResolveScene9LightRadiiMeters");

                Assert.That(scene8.x, Is.EqualTo(40f).Within(0.001f));
                Assert.That(scene8.y, Is.EqualTo(25f).Within(0.001f));
                Assert.That(scene9.x, Is.EqualTo(26f).Within(0.001f));
                Assert.That(scene9.y, Is.EqualTo(18f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static object ResolveDemoPhaseValue(string phaseName)
        {
            var enumType = typeof(FishingLoopTutorialController).GetNestedType("DemoAutoplayPhase", BindingFlags.NonPublic);
            Assert.That(enumType, Is.Not.Null, "Expected DemoAutoplayPhase enum.");
            return Enum.Parse(enumType, phaseName);
        }

        private static T InvokePrivateStatic<T>(string methodName, params object[] args)
        {
            var method = typeof(FishingLoopTutorialController).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Expected static private method '{methodName}'.");
            return (T)method.Invoke(null, args);
        }

        private static T InvokePrivateInstance<T>(object instance, string methodName, params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"Expected private method '{methodName}'.");
            return (T)method.Invoke(instance, args);
        }

        private static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}'.");
            field.SetValue(instance, value);
        }
    }
}
