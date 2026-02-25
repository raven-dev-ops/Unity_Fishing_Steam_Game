using System;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.Steam;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class SteamCloudSyncServiceTests
    {
        private GameObject _flowRoot;
        private GameObject _serviceRoot;
        private GameFlowManager _flowManager;
        private SteamCloudSyncService _service;

        [SetUp]
        public void SetUp()
        {
            _flowRoot = new GameObject("SteamCloudSyncServiceTests_Flow");
            _flowManager = _flowRoot.AddComponent<GameFlowManager>();

            _serviceRoot = new GameObject("SteamCloudSyncServiceTests_Service");
            _service = _serviceRoot.AddComponent<SteamCloudSyncService>();

            SetPrivateField(_service, "_gameFlowManager", _flowManager);
            SetPrivateField(_service, "_restrictBlockingSyncToSafeStates", true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(_serviceRoot);
            }

            if (_flowRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(_flowRoot);
            }
        }

        [Test]
        public void IsBlockingSyncWindowSafe_RejectsFishing_AllowsMainMenu()
        {
            _flowManager.SetState(GameFlowState.Fishing);
            Assert.That(InvokePrivateMethod<bool>(_service, "IsBlockingSyncWindowSafe"), Is.False);

            _flowManager.SetState(GameFlowState.MainMenu);
            Assert.That(InvokePrivateMethod<bool>(_service, "IsBlockingSyncWindowSafe"), Is.True);
        }

        [Test]
        public void OnSaveDataChanged_QueuesDeferredUpload_WhenStartupCompleteAndUnsafeState()
        {
            _flowManager.SetState(GameFlowState.Fishing);
            SetPrivateField(_service, "_startupSyncCompleted", true);
            SetPrivateField(_service, "_autoSyncOnSave", true);
            SetPrivateField(_service, "_syncInProgress", false);
            SetPrivateField(_service, "_deferredUploadPending", false);

            InvokePrivateMethod<object>(_service, "OnSaveDataChanged", new SaveDataV1());

            Assert.That(GetPrivateField<bool>(_service, "_deferredUploadPending"), Is.True);
        }

        [Test]
        public void OnSaveDataChanged_DoesNotQueueDeferredUpload_WhenAutoSyncDisabled()
        {
            _flowManager.SetState(GameFlowState.Fishing);
            SetPrivateField(_service, "_startupSyncCompleted", true);
            SetPrivateField(_service, "_autoSyncOnSave", false);
            SetPrivateField(_service, "_syncInProgress", false);
            SetPrivateField(_service, "_deferredUploadPending", false);

            InvokePrivateMethod<object>(_service, "OnSaveDataChanged", new SaveDataV1());

            Assert.That(GetPrivateField<bool>(_service, "_deferredUploadPending"), Is.False);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            Assert.That(target, Is.Not.Null);
            Assert.That(string.IsNullOrWhiteSpace(fieldName), Is.False);

            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            Assert.That(target, Is.Not.Null);
            Assert.That(string.IsNullOrWhiteSpace(fieldName), Is.False);

            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            return (T)field.GetValue(target);
        }

        private static T InvokePrivateMethod<T>(object target, string methodName, params object[] args)
        {
            Assert.That(target, Is.Not.Null);
            Assert.That(string.IsNullOrWhiteSpace(methodName), Is.False);

            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            var result = method.Invoke(target, args);
            if (typeof(T) == typeof(object))
            {
                return default;
            }

            if (result == null)
            {
                return default;
            }

            return (T)result;
        }
    }
}
