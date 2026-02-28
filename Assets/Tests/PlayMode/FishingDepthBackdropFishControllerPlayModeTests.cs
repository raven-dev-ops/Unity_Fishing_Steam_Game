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

        private FishingDepthBackdropFishController CreateController()
        {
            var go = new GameObject("DepthBackdropFishControllerTest");
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

        private void CreateFishSpriteTemplate()
        {
            var template = new GameObject("FishingFishTemplate");
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
