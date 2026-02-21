using System;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.UI;
using TMPro;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class HudOverlayControllerTests
    {
        [Test]
        public void Refresh_UsesConfiguredSaveDataView()
        {
            var root = new GameObject("hud-root");
            var controller = root.AddComponent<HudOverlayController>();
            var copecsText = new GameObject("copecs-text").AddComponent<TextMeshProUGUI>();
            var dayText = new GameObject("day-text").AddComponent<TextMeshProUGUI>();

            try
            {
                SetPrivateField(controller, "_copecsText", copecsText);
                SetPrivateField(controller, "_dayText", dayText);

                var fakeSave = new FakeSaveDataView(new SaveDataV1
                {
                    copecs = 321,
                    careerStartLocalDate = "2026-02-20"
                });

                controller.ConfigureDependencies(fakeSave);
                controller.Refresh();

                Assert.That(copecsText.text, Is.EqualTo("Copecs: 321"));
                Assert.That(dayText.text, Does.StartWith("Day "));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(copecsText.gameObject);
                UnityEngine.Object.DestroyImmediate(dayText.gameObject);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SaveDataChangedEvent_RefreshesHudWhenUsingConfiguredDependencies()
        {
            var root = new GameObject("hud-root-events");
            var controller = root.AddComponent<HudOverlayController>();
            var copecsText = new GameObject("copecs-text-events").AddComponent<TextMeshProUGUI>();

            try
            {
                SetPrivateField(controller, "_copecsText", copecsText);

                var fakeSave = new FakeSaveDataView(new SaveDataV1
                {
                    copecs = 10,
                    careerStartLocalDate = "2026-02-20"
                });

                controller.ConfigureDependencies(fakeSave);
                controller.enabled = false;
                controller.enabled = true;

                fakeSave.UpdateCopecs(75);
                fakeSave.RaiseChanged();

                Assert.That(copecsText.text, Is.EqualTo("Copecs: 75"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(copecsText.gameObject);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' to exist for test setup.");
            field.SetValue(target, value);
        }

        private sealed class FakeSaveDataView : ISaveDataView
        {
            private readonly SaveDataV1 _saveData;

            public FakeSaveDataView(SaveDataV1 initialData)
            {
                _saveData = initialData ?? new SaveDataV1();
            }

            public SaveDataV1 Current => _saveData;

            public event Action<SaveDataV1> SaveDataChanged;

            public void UpdateCopecs(int copecs)
            {
                _saveData.copecs = Mathf.Max(0, copecs);
            }

            public void RaiseChanged()
            {
                SaveDataChanged?.Invoke(_saveData);
            }
        }
    }
}
