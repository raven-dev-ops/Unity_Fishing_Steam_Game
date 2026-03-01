using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Fishing;
using UnityEngine;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    public sealed class FishingDepthBackdropFishControllerPlayModeTests
    {
        private readonly List<UnityEngine.Object> _cleanup = new List<UnityEngine.Object>(16);

        [TearDown]
        public void TearDown()
        {
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
        public IEnumerator DepthBackdropFish_InitialSpawn_IsOffscreenAndStaggered()
        {
            UnityEngine.Random.InitState(77191);
            var camera = CreateCamera();
            var ship = CreateTransform("DepthBackdropTestShip", new Vector3(0f, 0f, 0f));
            var hook = CreateTransform("DepthBackdropTestHook", new Vector3(0f, -60f, 0f));
            CreateFishSpriteTemplate();

            var controller = CreateController();
            SetPrivateField(controller, "_totalBackdropFish", 9);
            SetPrivateField(controller, "_initialSpawnDelayRangeSeconds", new Vector2(0.25f, 1.25f));
            SetPrivateField(controller, "_respawnDelayRangeSeconds", new Vector2(0.75f, 1.75f));

            controller.Configure(camera, ship, hook);
            yield return null;
            yield return null;

            var tracks = ReadTracks(controller);
            Assert.That(tracks.Count, Is.EqualTo(9), "Expected the configured non-interactive fish track count.");

            ResolveCameraBounds(camera, out var left, out var right);
            var pendingCount = 0;
            var delays = new List<float>(tracks.Count);
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                var transform = GetTrackField<Transform>(track, "Transform");
                Assert.That(transform, Is.Not.Null);

                var x = transform.position.x;
                Assert.That(
                    x < left || x > right,
                    Is.True,
                    "Non-interactive fish must begin off-screen before floating into view.");

                var pending = GetTrackField<bool>(track, "PendingSpawn");
                if (pending)
                {
                    pendingCount++;
                }

                delays.Add(GetTrackField<float>(track, "SpawnDelaySeconds"));
            }

            Assert.That(pendingCount, Is.GreaterThanOrEqualTo(8), "Spawn delays should keep most fish queued off-screen at start.");
            var minDelay = Mathf.Min(delays.ToArray());
            var maxDelay = Mathf.Max(delays.ToArray());
            Assert.That(minDelay, Is.GreaterThan(0.1f), "Spawn delays should be non-zero for staggered entry.");
            Assert.That(maxDelay - minDelay, Is.GreaterThan(0.35f), "Spawn delays should vary to avoid clumped fish groups.");
        }

        [UnityTest]
        public IEnumerator DepthBackdropFish_VerticalSpread_StaysWideAndDriftsWithShipTravel()
        {
            UnityEngine.Random.InitState(43907);
            var camera = CreateCamera();
            camera.transform.position = new Vector3(0f, 120f, -10f);
            var ship = CreateTransform("DepthBackdropDriftShip", new Vector3(0f, 220f, 0f));
            var hook = CreateTransform("DepthBackdropDriftHook", new Vector3(0f, 120f, 0f));
            CreateFishSpriteTemplate();

            var controller = CreateController();
            SetPrivateField(controller, "_totalBackdropFish", 12);
            SetPrivateField(controller, "_initialSpawnDelayRangeSeconds", Vector2.zero);
            SetPrivateField(controller, "_respawnDelayRangeSeconds", Vector2.zero);
            SetPrivateField(controller, "_verticalRetargetIntervalRangeSeconds", new Vector2(0.1f, 0.25f));
            SetPrivateField(controller, "_verticalDriftLerpSpeedRange", new Vector2(1.8f, 2.8f));
            SetPrivateField(controller, "_shipTravelVerticalOffsetPerMeterX", new Vector2(-0.24f, 0.24f));

            controller.Configure(camera, ship, hook);
            yield return null;
            yield return null;

            var initialTracks = ReadTracks(controller);
            var initialBaseYs = ReadTrackFloatArray(initialTracks, "BaseY");
            var initialSpread = ResolveSpread(initialBaseYs);
            Assert.That(initialSpread, Is.GreaterThan(5.4f), "Non-interactive fish should span a broad vertical area, not a narrow band.");

            var moveUntil = Time.realtimeSinceStartup + 1.3f;
            while (Time.realtimeSinceStartup < moveUntil)
            {
                ship.position += Vector3.left * (2.6f * Time.unscaledDeltaTime);
                yield return null;
            }

            var movedTracks = ReadTracks(controller);
            var movedBaseYs = ReadTrackFloatArray(movedTracks, "BaseY");
            var movedSpread = ResolveSpread(movedBaseYs);
            Assert.That(movedSpread, Is.GreaterThan(3.5f), "Vertical distribution should remain broad while the boat travels.");

            var changedTracks = 0;
            var compared = Mathf.Min(initialBaseYs.Length, movedBaseYs.Length);
            for (var i = 0; i < compared; i++)
            {
                if (Mathf.Abs(movedBaseYs[i] - initialBaseYs[i]) > 0.12f)
                {
                    changedTracks++;
                }
            }

            Assert.That(changedTracks, Is.GreaterThanOrEqualTo(4), "Fish vertical positions should drift/retarget over travel, not stay locked.");
        }

        [UnityTest]
        public IEnumerator DepthBackdropFish_CameraVerticalTravel_DoesNotDragFishBaseY()
        {
            UnityEngine.Random.InitState(15731);
            var camera = CreateCamera();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            var ship = CreateTransform("DepthBackdropCameraTravelShip", new Vector3(0f, 0f, 0f));
            var hook = CreateTransform("DepthBackdropCameraTravelHook", new Vector3(0f, -6f, 0f));
            CreateFishSpriteTemplate();

            var controller = CreateController();
            SetPrivateField(controller, "_totalBackdropFish", 9);
            SetPrivateField(controller, "_initialSpawnDelayRangeSeconds", Vector2.zero);
            SetPrivateField(controller, "_respawnDelayRangeSeconds", Vector2.zero);
            SetPrivateField(controller, "_swimSpeedRange", Vector2.zero);
            SetPrivateField(controller, "_speedVarianceRange", Vector2.one);
            SetPrivateField(controller, "_verticalRetargetIntervalRangeSeconds", new Vector2(60f, 90f));
            SetPrivateField(controller, "_shipTravelVerticalOffsetPerMeterX", Vector2.zero);
            SetPrivateField(controller, "_cameraMotionFreezeThresholdPerFrame", 0.35f);
            SetPrivateField(controller, "_cameraMotionRecoveryPauseSeconds", 1.2f);

            controller.Configure(camera, ship, hook);
            yield return null;
            yield return null;
            yield return null;

            for (var frame = 0; frame < 24; frame++)
            {
                var pendingTracks = 0;
                var tracks = ReadTracks(controller);
                for (var i = 0; i < tracks.Count; i++)
                {
                    if (GetTrackField<bool>(tracks[i], "PendingSpawn"))
                    {
                        pendingTracks++;
                    }
                }

                if (pendingTracks == 0)
                {
                    break;
                }

                yield return null;
            }

            var beforeTracks = ReadTracks(controller);
            var beforeBaseYs = ReadTrackFloatArray(beforeTracks, "BaseY");
            Assert.That(beforeBaseYs.Length, Is.GreaterThanOrEqualTo(6), "Expected backdrop tracks before camera movement.");

            var moveUntil = Time.realtimeSinceStartup + 0.6f;
            while (Time.realtimeSinceStartup < moveUntil)
            {
                camera.transform.position += Vector3.down * (6f * Time.unscaledDeltaTime);
                yield return null;
            }

            var afterTracks = ReadTracks(controller);
            var afterBaseYs = ReadTrackFloatArray(afterTracks, "BaseY");
            var compared = Mathf.Min(beforeBaseYs.Length, afterBaseYs.Length);
            Assert.That(compared, Is.GreaterThanOrEqualTo(6), "Expected backdrop tracks after camera movement.");

            var sumDelta = 0f;
            var maxDelta = 0f;
            for (var i = 0; i < compared; i++)
            {
                var delta = Mathf.Abs(afterBaseYs[i] - beforeBaseYs[i]);
                sumDelta += delta;
                maxDelta = Mathf.Max(maxDelta, delta);
            }

            var averageDelta = sumDelta / Mathf.Max(1, compared);
            Assert.That(averageDelta, Is.LessThan(0.22f), "Camera vertical movement should not drag backdrop fish base Y positions.");
            Assert.That(maxDelta, Is.LessThan(0.6f), "Backdrop fish base Y outliers should remain stable during camera vertical travel.");
        }

        [UnityTest]
        public IEnumerator DepthBackdropFish_LayeredScaleAndOpacity_PreserveDepthCue()
        {
            UnityEngine.Random.InitState(66237);
            var camera = CreateCamera();
            var ship = CreateTransform("DepthBackdropLayerShip", new Vector3(0f, 0f, 0f));
            var hook = CreateTransform("DepthBackdropLayerHook", new Vector3(0f, -60f, 0f));
            CreateFishSpriteTemplate();

            var controller = CreateController();
            SetPrivateField(controller, "_totalBackdropFish", 12);
            SetPrivateField(controller, "_initialSpawnDelayRangeSeconds", Vector2.zero);
            SetPrivateField(controller, "_respawnDelayRangeSeconds", Vector2.zero);

            controller.Configure(camera, ship, hook);
            yield return null;
            yield return null;

            var tracks = ReadTracks(controller);
            Assert.That(tracks.Count, Is.GreaterThanOrEqualTo(9), "Expected layered non-interactive fish tracks.");

            var layerScaleSum = new float[3];
            var layerAlphaSum = new float[3];
            var layerCounts = new int[3];
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                var layer = Mathf.Clamp(GetTrackField<int>(track, "LayerIndex"), 0, 2);
                var transform = GetTrackField<Transform>(track, "Transform");
                var renderer = GetTrackField<SpriteRenderer>(track, "Renderer");
                Assert.That(transform, Is.Not.Null);
                Assert.That(renderer, Is.Not.Null);
                layerScaleSum[layer] += Mathf.Abs(transform.localScale.x);
                layerAlphaSum[layer] += Mathf.Clamp01(renderer.color.a);
                layerCounts[layer]++;
            }

            Assert.That(layerCounts[0], Is.GreaterThan(0), "Expected near-layer fish.");
            Assert.That(layerCounts[1], Is.GreaterThan(0), "Expected mid-layer fish.");
            Assert.That(layerCounts[2], Is.GreaterThan(0), "Expected far-layer fish.");

            var nearScale = layerScaleSum[0] / layerCounts[0];
            var midScale = layerScaleSum[1] / layerCounts[1];
            var farScale = layerScaleSum[2] / layerCounts[2];
            var nearAlpha = layerAlphaSum[0] / layerCounts[0];
            var midAlpha = layerAlphaSum[1] / layerCounts[1];
            var farAlpha = layerAlphaSum[2] / layerCounts[2];

            Assert.That(nearAlpha, Is.GreaterThan(midAlpha + 0.02f), "Near layer should be more opaque than mid layer.");
            Assert.That(midAlpha, Is.GreaterThan(farAlpha + 0.02f), "Mid layer should be more opaque than far layer.");
            Assert.That(nearScale, Is.GreaterThan(midScale + 0.03f), "Near layer fish should render larger than mid layer fish.");
            Assert.That(midScale, Is.GreaterThan(farScale + 0.03f), "Mid layer fish should render larger than far layer fish.");
        }

        [UnityTest]
        public IEnumerator DepthBackdropFish_PendingActivation_DoesNotEnableWhenOnScreen()
        {
            UnityEngine.Random.InitState(91277);
            var camera = CreateCamera();
            var ship = CreateTransform("DepthBackdropActivationShip", new Vector3(0f, 0f, 0f));
            var hook = CreateTransform("DepthBackdropActivationHook", new Vector3(0f, -60f, 0f));
            CreateFishSpriteTemplate();

            var controller = CreateController();
            SetPrivateField(controller, "_totalBackdropFish", 6);
            SetPrivateField(controller, "_initialSpawnDelayRangeSeconds", new Vector2(0.35f, 0.35f));

            controller.Configure(camera, ship, hook);
            yield return null;
            yield return null;

            var tracks = ReadTracks(controller);
            Assert.That(tracks.Count, Is.GreaterThanOrEqualTo(1));
            var track = tracks[0];
            var transform = GetTrackField<Transform>(track, "Transform");
            var renderer = GetTrackField<SpriteRenderer>(track, "Renderer");
            Assert.That(transform, Is.Not.Null);
            Assert.That(renderer, Is.Not.Null);

            // Simulate a delayed spawn that drifts into the camera view before activation.
            transform.position = new Vector3(camera.transform.position.x, transform.position.y, transform.position.z);
            SetTrackField(track, "PendingSpawn", true);
            SetTrackField(track, "SpawnDelaySeconds", 0f);
            renderer.enabled = false;

            yield return null;

            var stillPending = GetTrackField<bool>(track, "PendingSpawn");
            Assert.That(stillPending, Is.True, "Track should remain pending if activation would happen on-screen.");
            Assert.That(renderer.enabled, Is.False, "Renderer should stay disabled until the fish is off-screen.");

            ResolveCameraBounds(camera, out var left, out var right);
            Assert.That(
                transform.position.x < left || transform.position.x > right,
                Is.True,
                "Pending fish should be re-positioned off-screen before it can become visible.");
        }

        [UnityTest]
        public IEnumerator DepthBackdropFish_UsesFallbackSprite_WhenFishTokenSpritesUnavailable()
        {
            UnityEngine.Random.InitState(31021);
            var camera = CreateCamera();
            var ship = CreateTransform("DepthBackdropFallbackShip", new Vector3(0f, 0f, 0f));
            var hook = CreateTransform("DepthBackdropFallbackHook", new Vector3(0f, -60f, 0f));
            var fallbackSprite = CreateTestSprite();

            var controller = CreateController();
            SetPrivateField(controller, "_totalBackdropFish", 8);
            SetPrivateField(controller, "_fishNameToken", "NoMatchingFishToken");
            SetPrivateField(controller, "_allowGenericFishNameFallback", false);
            SetPrivateField(controller, "_fallbackFishSprite", fallbackSprite);
            SetPrivateField(controller, "_initialSpawnDelayRangeSeconds", Vector2.zero);
            SetPrivateField(controller, "_respawnDelayRangeSeconds", Vector2.zero);

            controller.Configure(camera, ship, hook);
            yield return null;
            yield return null;

            var tracks = ReadTracks(controller);
            Assert.That(tracks.Count, Is.EqualTo(8), "Fallback fish sprite should allow all configured backdrop tracks to spawn.");

            for (var i = 0; i < tracks.Count; i++)
            {
                var renderer = GetTrackField<SpriteRenderer>(tracks[i], "Renderer");
                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.sprite, Is.EqualTo(fallbackSprite), "Backdrop fish should use the configured fallback sprite.");
            }
        }

        private FishingDepthBackdropFishController CreateController()
        {
            var go = new GameObject("DepthBackdropFishControllerTest");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _cleanup.Add(go);
            go.SetActive(false);
            var controller = go.AddComponent<FishingDepthBackdropFishController>();
            SetPrivateField(controller, "_allowInBatchMode", true);
            go.SetActive(true);
            return controller;
        }

        private Camera CreateCamera()
        {
            var go = new GameObject("DepthBackdropFishTestCamera");
            UnityEngine.Object.DontDestroyOnLoad(go);
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
            UnityEngine.Object.DontDestroyOnLoad(go);
            _cleanup.Add(go);
            go.transform.position = position;
            return go.transform;
        }

        private void CreateFishSpriteTemplate()
        {
            var template = new GameObject("FishingFishTemplate");
            UnityEngine.Object.DontDestroyOnLoad(template);
            _cleanup.Add(template);
            var renderer = template.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateTestSprite();
            renderer.enabled = false;
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

        private static List<object> ReadTracks(FishingDepthBackdropFishController controller)
        {
            var field = typeof(FishingDepthBackdropFishController).GetField("_tracks", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Unable to read depth backdrop track list.");
            var value = field.GetValue(controller) as IEnumerable;
            Assert.That(value, Is.Not.Null, "Depth backdrop track list is unavailable.");

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

        private static T GetTrackField<T>(object track, string fieldName)
        {
            Assert.That(track, Is.Not.Null, "Expected non-null track.");
            var field = track.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on backdrop track.");
            return (T)field.GetValue(track);
        }

        private static void SetTrackField<T>(object track, string fieldName, T value)
        {
            Assert.That(track, Is.Not.Null, "Expected non-null track.");
            var field = track.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on backdrop track.");
            field.SetValue(track, value);
        }

        private static float[] ReadTrackFloatArray(IReadOnlyList<object> tracks, string fieldName)
        {
            var values = new float[tracks.Count];
            for (var i = 0; i < tracks.Count; i++)
            {
                values[i] = GetTrackField<float>(tracks[i], fieldName);
            }

            return values;
        }

        private static float ResolveSpread(float[] values)
        {
            if (values == null || values.Length == 0)
            {
                return 0f;
            }

            var min = float.PositiveInfinity;
            var max = float.NegativeInfinity;
            for (var i = 0; i < values.Length; i++)
            {
                min = Mathf.Min(min, values[i]);
                max = Mathf.Max(max, values[i]);
            }

            return max - min;
        }

        private static void ResolveCameraBounds(Camera camera, out float left, out float right)
        {
            var halfHeight = Mathf.Max(0.5f, camera.orthographicSize);
            var halfWidth = Mathf.Max(0.5f, halfHeight * Mathf.Max(0.1f, camera.aspect));
            var cameraX = camera.transform.position.x;
            left = cameraX - halfWidth;
            right = cameraX + halfWidth;
        }

        private static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(instance, value);
        }
    }
}
