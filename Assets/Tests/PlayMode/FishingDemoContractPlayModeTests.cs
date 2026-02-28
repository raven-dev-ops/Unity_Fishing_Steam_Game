using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Fishing;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    public sealed class FishingDemoContractPlayModeTests
    {
        private const string FishingScenePath = "Assets/Scenes/04_Fishing.unity";
        private readonly List<UnityEngine.Object> _cleanup = new List<UnityEngine.Object>(16);
        private static MethodInfo _runtimeServicesEnsureBootstrapMethod;
        private static bool _runtimeServicesBootstrapLookupCompleted;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            for (var i = _cleanup.Count - 1; i >= 0; i--)
            {
                var obj = _cleanup[i];
                if (obj == null)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(obj);
            }

            _cleanup.Clear();
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator DemoContract_SurfaceWeather_IsVisibleInMoveShipScene()
        {
            yield return LoadFishingScene();
            yield return null;

            var saveManager = UnityEngine.Object.FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            Assert.That(saveManager, Is.Not.Null, "Expected SaveManager in fishing scene.");
            saveManager.RequestFishingLoopTutorialReplay();
            yield return null;

            var tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected FishingLoopTutorialController in fishing scene.");
            tutorial.SetAutoplayInBatchModeForTests(true);
            tutorial.BeginTutorialForTests();
            yield return null;

            yield return WaitForDemoSceneStartPhase(tutorial, "MoveShipInfo", timeoutSeconds: 180f);

            var weatherController = UnityEngine.Object.FindAnyObjectByType<FishingSceneWeatherController>(FindObjectsInactive.Include);
            Assert.That(weatherController, Is.Not.Null, "Expected FishingSceneWeatherController in fishing scene.");
            EnableWeatherControllerForBatchTests(weatherController);
            UnityEngine.Random.InitState(41231);
            weatherController.SetWeather(FishingWeatherState.Clouds);
            yield return null;
            yield return null;

            var camera = Camera.main != null
                ? Camera.main
                : UnityEngine.Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            Assert.That(camera, Is.Not.Null, "Expected active camera for weather visibility checks.");

            var visibleClouds = CountVisibleWeatherTracks(weatherController, "_cloudSprites", camera);
            var visibleFog = CountVisibleWeatherTracks(weatherController, "_fogSprites", camera);
            var visibleRain = CountVisibleWeatherTracks(weatherController, "_rainSprites", camera);
            Assert.That(
                visibleClouds + visibleFog + visibleRain,
                Is.GreaterThanOrEqualTo(1),
                "Expected at least one visible weather drift sprite in move-ship scene.");
        }

        [UnityTest]
        public IEnumerator AmbientFish_OffscreenDespawn_RequiresSeveralSecondsOffscreen()
        {
            UnityEngine.Random.InitState(99141);
            var camera = CreateCamera("AmbientOffscreenCamera");
            var ship = CreateTransform("AmbientOffscreenShip", new Vector3(0f, 0f, 0f));
            var hook = CreateTransform("AmbientOffscreenHook", new Vector3(0f, -45f, 0f));

            var fish = new GameObject("AmbientOffscreenSpecimen_0");
            _cleanup.Add(fish);
            var fishRenderer = fish.AddComponent<SpriteRenderer>();
            fishRenderer.sprite = CreateTestSprite();
            fish.transform.position = new Vector3(0f, -2f, 0f);

            var root = new GameObject("AmbientOffscreenControllerRoot");
            _cleanup.Add(root);
            root.SetActive(false);
            var controller = root.AddComponent<FishingAmbientFishSwimController>();
            SetPrivateField(controller, "_fishNameToken", "AmbientOffscreenSpecimen");
            SetPrivateField(controller, "_runtimeCamera", camera);
            SetPrivateField(controller, "_ship", ship);
            SetPrivateField(controller, "_hook", hook);
            SetPrivateField(controller, "_enforceCatchableSpawnRules", false);
            SetPrivateField(controller, "_linkSpawnCadenceToFishSpawner", false);
            SetPrivateField(controller, "_dynamicWaterBand", false);
            SetPrivateField(controller, "_dynamicHorizontalBand", false);
            SetPrivateField(controller, "_offscreenDespawnSeconds", 1.25f);
            SetPrivateField(controller, "_offscreenDespawnDistance", 0.6f);
            SetPrivateField(controller, "_spawnFadeInSeconds", 0f);
            SetPrivateField(controller, "_maxConcurrentFish", 1);
            root.SetActive(true);
            yield return null;
            yield return null;

            var tracks = ReadAmbientTracks(controller);
            Assert.That(tracks.Count, Is.EqualTo(1), "Expected one ambient fish track for off-screen despawn contract test.");
            var track = tracks[0];
            var trackTransform = GetTrackField<Transform>(track, "transform");
            var trackRenderer = GetTrackField<SpriteRenderer>(track, "renderer");
            Assert.That(trackTransform, Is.Not.Null);
            Assert.That(trackRenderer, Is.Not.Null);

            SetTrackField(track, "direction", 0f);
            SetTrackField(track, "speed", 0f);
            SetTrackField(track, "active", true);
            SetTrackField(track, "reserved", false);
            SetTrackField(track, "hooked", false);
            SetTrackField(track, "approaching", false);
            SetTrackField(track, "offscreenSeconds", 0f);
            trackRenderer.enabled = true;
            trackTransform.position = new Vector3(camera.transform.position.x - 12f, -2f, 0f);

            var earlyCheckUntil = Time.realtimeSinceStartup + 0.55f;
            while (Time.realtimeSinceStartup < earlyCheckUntil)
            {
                yield return null;
            }

            Assert.That(GetTrackField<bool>(track, "active"), Is.True, "Fish should remain active before off-screen time budget elapses.");
            Assert.That(trackRenderer.enabled, Is.True, "Fish renderer should remain enabled before off-screen time budget elapses.");

            var despawnCheckUntil = Time.realtimeSinceStartup + 1.1f;
            while (Time.realtimeSinceStartup < despawnCheckUntil)
            {
                yield return null;
            }

            Assert.That(GetTrackField<bool>(track, "active"), Is.False, "Fish should despawn only after remaining off-screen for the configured seconds.");
            Assert.That(trackRenderer.enabled, Is.False, "Fish renderer should disable after despawn.");
        }

        private IEnumerator LoadFishingScene()
        {
            Assert.That(File.Exists(FishingScenePath), Is.True, $"Scene path not found: {FishingScenePath}");
            var previousIgnoreLogs = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                EnsureRuntimeServicesBootstrap();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreLogs;
            }

            Time.timeScale = 1f;
            var loadOperation = SceneManager.LoadSceneAsync(FishingScenePath, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null, $"Failed to start load for scene: {FishingScenePath}");
            yield return loadOperation;
        }

        private static IEnumerator WaitForDemoSceneStartPhase(FishingLoopTutorialController tutorial, string phaseName, float timeoutSeconds)
        {
            var timeoutAt = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                if (tutorial != null
                    && tutorial.IsDemoActiveForTests()
                    && !tutorial.IsDemoSceneTransitionActiveForTests()
                    && string.Equals(tutorial.GetDemoPhaseNameForTests(), phaseName, StringComparison.Ordinal))
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Timed out waiting for demo phase '{phaseName}'.");
        }

        private static void EnableWeatherControllerForBatchTests(FishingSceneWeatherController weatherController)
        {
            if (weatherController == null)
            {
                return;
            }

            SetPrivateField(weatherController, "_allowInBatchMode", true);
            SetPrivateField(weatherController, "_randomizeOnStart", false);
            SetPrivateField(weatherController, "_autoCycleWeather", false);
            weatherController.enabled = true;

            var camera = Camera.main != null
                ? Camera.main
                : UnityEngine.Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            var ship = GameObject.Find("FishingShip")?.transform;
            var condition = UnityEngine.Object.FindAnyObjectByType<FishingConditionController>(FindObjectsInactive.Include);
            if (camera != null)
            {
                weatherController.Configure(camera, conditionController: condition, ship: ship);
            }
        }

        private static int CountVisibleWeatherTracks(FishingSceneWeatherController weatherController, string fieldName, Camera camera)
        {
            if (weatherController == null || camera == null)
            {
                return 0;
            }

            var tracks = ReadTrackList(weatherController, fieldName);
            var visibleCount = 0;
            for (var i = 0; i < tracks.Count; i++)
            {
                var renderer = GetTrackField<SpriteRenderer>(tracks[i], "Renderer");
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                var color = renderer.color;
                if (color.a <= 0.001f)
                {
                    continue;
                }

                var viewportPoint = camera.WorldToViewportPoint(renderer.transform.position);
                if (viewportPoint.z > 0f
                    && viewportPoint.x >= -0.02f
                    && viewportPoint.x <= 1.02f
                    && viewportPoint.y >= -0.02f
                    && viewportPoint.y <= 1.02f)
                {
                    visibleCount++;
                }
            }

            return visibleCount;
        }

        private static List<object> ReadTrackList(object controller, string fieldName)
        {
            Assert.That(controller, Is.Not.Null, "Expected controller for track list read.");
            var field = controller.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Unable to read field '{fieldName}'.");
            var value = field.GetValue(controller) as IEnumerable;
            Assert.That(value, Is.Not.Null, $"Field '{fieldName}' is not enumerable.");

            var tracks = new List<object>(24);
            foreach (var item in value)
            {
                if (item != null)
                {
                    tracks.Add(item);
                }
            }

            return tracks;
        }

        private static List<object> ReadAmbientTracks(FishingAmbientFishSwimController controller)
        {
            return ReadTrackList(controller, "_tracks");
        }

        private Camera CreateCamera(string name)
        {
            var go = new GameObject(name);
            _cleanup.Add(go);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = 16f / 9f;
            go.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private Transform CreateTransform(string name, Vector3 position)
        {
            var go = new GameObject(name);
            _cleanup.Add(go);
            go.transform.position = position;
            return go.transform;
        }

        private Sprite CreateTestSprite()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[]
            {
                Color.white, Color.white,
                Color.white, Color.white
            });
            texture.Apply();
            _cleanup.Add(texture);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                32f);
            _cleanup.Add(sprite);
            return sprite;
        }

        private static void EnsureRuntimeServicesBootstrap()
        {
            if (!_runtimeServicesBootstrapLookupCompleted)
            {
                _runtimeServicesBootstrapLookupCompleted = true;
                _runtimeServicesEnsureBootstrapMethod = typeof(RuntimeServicesBootstrap).GetMethod(
                    "EnsureBootstrap",
                    BindingFlags.NonPublic | BindingFlags.Static);
            }

            Assert.That(_runtimeServicesEnsureBootstrapMethod, Is.Not.Null, "Expected RuntimeServicesBootstrap.EnsureBootstrap method.");
            _runtimeServicesEnsureBootstrapMethod?.Invoke(null, null);
        }

        private static T GetTrackField<T>(object track, string fieldName)
        {
            Assert.That(track, Is.Not.Null, "Expected non-null track.");
            var field = track.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on track.");
            return (T)field.GetValue(track);
        }

        private static void SetTrackField<T>(object track, string fieldName, T value)
        {
            Assert.That(track, Is.Not.Null, "Expected non-null track.");
            var field = track.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on track.");
            field.SetValue(track, value);
        }

        private static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            Assert.That(instance, Is.Not.Null, "Expected instance for private field set.");
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}'.");
            field.SetValue(instance, value);
        }
    }
}
