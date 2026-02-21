using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using UnityEngine;
using System.Reflection;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class UserSettingsAccessibilityTests
    {
        [TearDown]
        public void TearDown()
        {
            if (UserSettingsService.Instance != null)
            {
                Object.DestroyImmediate(UserSettingsService.Instance.gameObject);
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            RuntimeServiceRegistry.Clear();
        }

        [Test]
        public void AccessibilitySettings_PersistAcrossServiceRecreate()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            var firstGo = new GameObject("UserSettings_Accessibility_First");
            var first = firstGo.AddComponent<UserSettingsService>();
            InvokePrivateMethod(first, "Awake");
            first.SetReelInputToggle(true);
            first.SetReducedMotion(true);
            first.SetSubtitleScale(1.35f);
            first.SetSubtitleBackgroundOpacity(0.8f);
            first.SetReadabilityBoost(true);
            Object.DestroyImmediate(firstGo);

            var secondGo = new GameObject("UserSettings_Accessibility_Second");
            var second = secondGo.AddComponent<UserSettingsService>();
            InvokePrivateMethod(second, "Awake");

            Assert.That(second.ReelInputToggle, Is.True);
            Assert.That(second.ReducedMotion, Is.True);
            Assert.That(second.SubtitleScale, Is.EqualTo(1.35f).Within(0.001f));
            Assert.That(second.SubtitleBackgroundOpacity, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(second.ReadabilityBoost, Is.True);

            Object.DestroyImmediate(secondGo);
        }

        [Test]
        public void AccessibilitySettings_ClampToGuardrails()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            var go = new GameObject("UserSettings_Accessibility_Clamp");
            var settings = go.AddComponent<UserSettingsService>();
            InvokePrivateMethod(settings, "Awake");
            settings.SetSubtitleScale(9f);
            settings.SetSubtitleBackgroundOpacity(-2f);

            Assert.That(settings.SubtitleScale, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(settings.SubtitleBackgroundOpacity, Is.EqualTo(0f).Within(0.001f));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AccessibilitySettings_DefaultsMatchLaunchReadabilityBaseline()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            var go = new GameObject("UserSettings_Accessibility_Defaults");
            var settings = go.AddComponent<UserSettingsService>();
            InvokePrivateMethod(settings, "Awake");

            Assert.That(settings.SubtitlesEnabled, Is.True);
            Assert.That(settings.SubtitleScale, Is.EqualTo(1f).Within(0.001f));
            Assert.That(settings.SubtitleBackgroundOpacity, Is.EqualTo(0.72f).Within(0.001f));
            Assert.That(settings.ReadabilityBoost, Is.False);
            Assert.That(settings.ReducedMotion, Is.False);
            Assert.That(settings.ReelInputToggle, Is.False);

            Object.DestroyImmediate(go);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' to exist for test setup.");
            method.Invoke(target, null);
        }
    }
}
