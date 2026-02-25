using System;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Fishing;
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
                InvokePrivateMethod(controller, "OnEnable");

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

        [Test]
        public void SetFishingValues_WhenUnchanged_DoesNotAllocateInSteadyState()
        {
            var root = new GameObject("hud-root-no-alloc");
            var controller = root.AddComponent<HudOverlayController>();
            var depthText = new GameObject("depth-text").AddComponent<TextMeshProUGUI>();
            var tensionText = new GameObject("tension-text").AddComponent<TextMeshProUGUI>();
            var conditionsText = new GameObject("conditions-text").AddComponent<TextMeshProUGUI>();
            var statusText = new GameObject("status-text").AddComponent<TextMeshProUGUI>();
            var failureText = new GameObject("failure-text").AddComponent<TextMeshProUGUI>();
            var gameFlowRoot = new GameObject("hud-flow");
            var gameFlow = gameFlowRoot.AddComponent<GameFlowManager>();

            try
            {
                SetPrivateField(controller, "_depthText", depthText);
                SetPrivateField(controller, "_tensionStateText", tensionText);
                SetPrivateField(controller, "_conditionsText", conditionsText);
                SetPrivateField(controller, "_fishingStatusText", statusText);
                SetPrivateField(controller, "_fishingFailureText", failureText);

                var fakeSave = new FakeSaveDataView(new SaveDataV1
                {
                    copecs = 100,
                    careerStartLocalDate = "2026-02-20"
                });

                gameFlow.SetState(GameFlowState.Fishing);
                controller.ConfigureDependencies(fakeSave, gameFlow);
                controller.SetFishingTelemetry(2, 42.5f);
                controller.SetFishingTension(0.35f, FishingTensionState.Warning);
                controller.SetFishingConditions("Storm | Night");
                controller.SetFishingStatus("Holding depth.");
                controller.SetFishingFailure("Line snapped.");

                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
                var before = System.GC.GetTotalMemory(true);

                for (var i = 0; i < 1024; i++)
                {
                    controller.SetFishingTelemetry(2, 42.5f);
                    controller.SetFishingTension(0.35f, FishingTensionState.Warning);
                    controller.SetFishingConditions("Storm | Night");
                    controller.SetFishingStatus("Holding depth.");
                    controller.SetFishingFailure("Line snapped.");
                }

                System.GC.Collect();
                var after = System.GC.GetTotalMemory(true);
                Assert.That(after - before, Is.LessThanOrEqualTo(1024L));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(depthText.gameObject);
                UnityEngine.Object.DestroyImmediate(tensionText.gameObject);
                UnityEngine.Object.DestroyImmediate(conditionsText.gameObject);
                UnityEngine.Object.DestroyImmediate(statusText.gameObject);
                UnityEngine.Object.DestroyImmediate(failureText.gameObject);
                UnityEngine.Object.DestroyImmediate(gameFlowRoot);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' to exist for test setup.");
            method.Invoke(target, null);
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
