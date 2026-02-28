using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Fishing;
using UnityEngine;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    public sealed class FishingSceneWeatherControllerPlayModeTests
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
        public IEnumerator WeatherSkyVisibility_FadesOutWhenCameraDescends()
        {
            UnityEngine.Random.InitState(55781);
            var camera = CreateCamera();
            var ship = CreateTransform("WeatherFadeShip", new Vector3(0f, 0f, 0f));

            var root = new GameObject("WeatherFadeController");
            _cleanup.Add(root);
            root.SetActive(false);
            var controller = root.AddComponent<FishingSceneWeatherController>();
            SetPrivateField(controller, "_allowInBatchMode", true);
            SetPrivateField(controller, "_randomizeOnStart", false);
            SetPrivateField(controller, "_autoCycleWeather", false);
            SetPrivateField(controller, "_cloudSpriteCount", 4);
            SetPrivateField(controller, "_rainSpriteCount", 12);
            SetPrivateField(controller, "_fogBandCount", 2);
            SetPrivateField(controller, "_skyVisibilityFadeDistance", 3f);
            root.SetActive(true);

            controller.Configure(camera, conditionController: null, ship: ship);
            controller.SetWeather(FishingWeatherState.Rain);
            yield return null;
            yield return null;

            var cloudRenderer = ResolveFirstWeatherEnabledCloudRenderer(controller);
            Assert.That(cloudRenderer, Is.Not.Null, "Expected at least one weather-enabled cloud renderer.");

            var skyVisibilitySamples = new List<float>(28);
            skyVisibilitySamples.Add(GetPrivateField<float>(controller, "_skyVisibilityFactor"));
            var alphaSamples = new List<float>(28)
            {
                Mathf.Clamp01(cloudRenderer.color.a)
            };

            for (var i = 0; i < 25; i++)
            {
                camera.transform.position += Vector3.down * 0.35f;
                yield return null;
                skyVisibilitySamples.Add(GetPrivateField<float>(controller, "_skyVisibilityFactor"));
                alphaSamples.Add(Mathf.Clamp01(cloudRenderer.color.a));
            }

            var startVisibility = skyVisibilitySamples[0];
            var endVisibility = skyVisibilitySamples[skyVisibilitySamples.Count - 1];
            Assert.That(startVisibility, Is.GreaterThan(0.95f), "Sky visibility should start near fully visible.");
            Assert.That(endVisibility, Is.LessThan(0.02f), "Sky visibility should fade out near fully hidden underwater.");

            var sawIntermediateVisibility = false;
            for (var i = 0; i < skyVisibilitySamples.Count; i++)
            {
                var value = skyVisibilitySamples[i];
                if (value > 0.05f && value < 0.95f)
                {
                    sawIntermediateVisibility = true;
                    break;
                }
            }

            Assert.That(sawIntermediateVisibility, Is.True, "Weather visibility should include intermediate fade values.");

            var maxSingleFrameDrop = 0f;
            for (var i = 1; i < skyVisibilitySamples.Count; i++)
            {
                var drop = Mathf.Max(0f, skyVisibilitySamples[i - 1] - skyVisibilitySamples[i]);
                maxSingleFrameDrop = Mathf.Max(maxSingleFrameDrop, drop);
            }

            Assert.That(maxSingleFrameDrop, Is.LessThan(0.55f), "Weather visibility should not hard-cut in a single frame.");

            var startAlpha = alphaSamples[0];
            var endAlpha = alphaSamples[alphaSamples.Count - 1];
            Assert.That(startAlpha, Is.GreaterThan(0.05f), "Cloud alpha should start visible.");
            Assert.That(endAlpha, Is.LessThan(0.02f), "Cloud alpha should fade near zero as sky visibility fades out.");
        }

        [UnityTest]
        public IEnumerator WeatherClouds_SeedVisibleAtStart()
        {
            UnityEngine.Random.InitState(11903);
            var camera = CreateCamera();
            var ship = CreateTransform("WeatherCloudSeedShip", new Vector3(0f, 0f, 0f));

            var root = new GameObject("WeatherCloudSeedController");
            _cleanup.Add(root);
            root.SetActive(false);
            var controller = root.AddComponent<FishingSceneWeatherController>();
            SetPrivateField(controller, "_allowInBatchMode", true);
            SetPrivateField(controller, "_randomizeOnStart", false);
            SetPrivateField(controller, "_autoCycleWeather", false);
            SetPrivateField(controller, "_cloudSpriteCount", 6);
            root.SetActive(true);

            controller.Configure(camera, conditionController: null, ship: ship);
            controller.SetWeather(FishingWeatherState.Clouds);
            yield return null;
            yield return null;

            ResolveCameraBounds(camera, out var left, out var right, out var bottom, out var top);
            var visibleClouds = 0;
            var tracks = ResolveWeatherTracks(controller, "_cloudSprites");
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                var renderer = GetTrackField<SpriteRenderer>(track, "Renderer");
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                var p = renderer.transform.position;
                if (p.x >= left && p.x <= right && p.y >= bottom && p.y <= top)
                {
                    visibleClouds++;
                }
            }

            Assert.That(visibleClouds, Is.GreaterThanOrEqualTo(1), "Expected at least one visible cloud after weather setup.");
        }

        private Camera CreateCamera()
        {
            var go = new GameObject("WeatherFadeTestCamera");
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

        private static SpriteRenderer ResolveFirstWeatherEnabledCloudRenderer(FishingSceneWeatherController controller)
        {
            Assert.That(controller, Is.Not.Null, "Expected weather controller.");
            var tracks = ResolveWeatherTracks(controller, "_cloudSprites");

            for (var i = 0; i < tracks.Count; i++)
            {
                var item = tracks[i];
                if (item == null)
                {
                    continue;
                }

                var renderer = GetTrackField<SpriteRenderer>(item, "Renderer");
                var weatherEnabled = GetTrackField<bool>(item, "WeatherEnabled");
                if (renderer != null && weatherEnabled)
                {
                    return renderer;
                }
            }

            return null;
        }

        private static List<object> ResolveWeatherTracks(FishingSceneWeatherController controller, string fieldName)
        {
            var field = typeof(FishingSceneWeatherController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Unable to read weather track field '{fieldName}'.");
            var value = field.GetValue(controller) as IEnumerable;
            Assert.That(value, Is.Not.Null, $"Weather track collection '{fieldName}' was unavailable.");

            var tracks = new List<object>(16);
            foreach (var item in value)
            {
                if (item != null)
                {
                    tracks.Add(item);
                }
            }

            return tracks;
        }

        private static void ResolveCameraBounds(Camera camera, out float left, out float right, out float bottom, out float top)
        {
            var halfHeight = Mathf.Max(0.5f, camera.orthographicSize);
            var halfWidth = Mathf.Max(0.5f, halfHeight * Mathf.Max(0.1f, camera.aspect));
            var cameraPosition = camera.transform.position;
            left = cameraPosition.x - halfWidth;
            right = cameraPosition.x + halfWidth;
            bottom = cameraPosition.y - halfHeight;
            top = cameraPosition.y + halfHeight;
        }

        private static T GetPrivateField<T>(object instance, string fieldName)
        {
            Assert.That(instance, Is.Not.Null, "Expected instance for field read.");
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            return (T)field.GetValue(instance);
        }

        private static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            Assert.That(instance, Is.Not.Null, "Expected instance for field write.");
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            field.SetValue(instance, value);
        }

        private static T GetTrackField<T>(object track, string fieldName)
        {
            Assert.That(track, Is.Not.Null, "Expected non-null weather track.");
            var field = track.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on weather track.");
            return (T)field.GetValue(track);
        }
    }
}
