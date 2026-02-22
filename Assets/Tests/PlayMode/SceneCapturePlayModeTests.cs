using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Fishing;
using RavenDevOps.Fishing.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    public sealed class SceneCapturePlayModeTests
    {
        private static MethodInfo _imageConversionEncodeToPngMethod;
        private static bool _imageConversionLookupCompleted;

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/00_Boot.unity",
            "Assets/Scenes/01_Cinematic.unity",
            "Assets/Scenes/02_MainMenu.unity",
            "Assets/Scenes/03_Harbor.unity",
            "Assets/Scenes/04_Fishing.unity"
        };

        private const string SceneCaptureEnabledEnvVar = "RAVEN_SCENE_CAPTURE_ENABLED";
        private const string FishingScenePath = "Assets/Scenes/04_Fishing.unity";

        [TearDown]
        public void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator CaptureKeyScenes_WritesPngArtifacts()
        {
            if (!IsSceneCaptureEnabled())
            {
                Assert.Ignore($"Scene capture is disabled. Set {SceneCaptureEnabledEnvVar}=1 to enable this test.");
            }

            var outputDirectory = ResolveOutputDirectory();
            Directory.CreateDirectory(outputDirectory);

            var capturedFiles = new List<string>();
            for (var i = 0; i < ScenePaths.Length; i++)
            {
                var scenePath = ScenePaths[i];
                Assert.That(File.Exists(scenePath), Is.True, $"Scene path not found: {scenePath}");

                var loadOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
                Assert.That(loadOperation, Is.Not.Null, $"Failed to start load for scene: {scenePath}");
                yield return loadOperation;
                yield return null;
                yield return new WaitForEndOfFrame();

                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                var outputPath = Path.Combine(outputDirectory, SanitizeFileName(sceneName) + ".png");
                CaptureScenePng(outputPath);
                yield return WaitForFile(outputPath, timeoutSeconds: 5f);

                Assert.That(File.Exists(outputPath), Is.True, $"Expected screenshot file: {outputPath}");
                var fileInfo = new FileInfo(outputPath);
                Assert.That(fileInfo.Length, Is.GreaterThan(0), $"Screenshot file is empty: {outputPath}");
                capturedFiles.Add(outputPath);
            }

            Assert.That(capturedFiles.Count, Is.EqualTo(ScenePaths.Length));
            Debug.Log($"SCENE_CAPTURE_OUTPUT: {outputDirectory}");
        }

        [UnityTest]
        public IEnumerator FishingScene_ComposesRuntimeHudAndControllers()
        {
            yield return LoadScene(FishingScenePath);
            yield return null;

            var runtimeRoot = GameObject.Find("__SceneRuntime");
            Assert.That(runtimeRoot, Is.Not.Null, "Expected scene runtime root.");
            Assert.That(runtimeRoot.GetComponent<FishingActionStateMachine>(), Is.Not.Null, "Expected fishing action state machine.");
            Assert.That(runtimeRoot.GetComponent<CatchResolver>(), Is.Not.Null, "Expected catch resolver.");
            Assert.That(runtimeRoot.GetComponent<FishingAmbientFishSwimController>(), Is.Not.Null, "Expected ambient fish controller.");
            Assert.That(runtimeRoot.GetComponent<SimpleFishingHudOverlay>(), Is.Not.Null, "Expected simple fishing HUD overlay.");
            Assert.That(runtimeRoot.GetComponent<FishingPauseBridge>(), Is.Not.Null, "Expected fishing pause bridge.");
            Assert.That(runtimeRoot.GetComponent<PauseMenuController>(), Is.Not.Null, "Expected pause menu controller.");

            Assert.That(FindSceneObject("FishingInfoPanel"), Is.Not.Null, "Expected fishing info panel.");
            var objectiveTextGo = FindSceneObject("FishingObjectiveText");
            Assert.That(objectiveTextGo, Is.Not.Null, "Expected fishing objective text.");
            var objectiveText = objectiveTextGo.GetComponent<Text>();
            Assert.That(objectiveText, Is.Not.Null, "Expected Unity UI Text component for objective.");
            Assert.That(string.IsNullOrWhiteSpace(objectiveText.text), Is.False, "Expected objective text to be populated.");
            Assert.That(objectiveText.text, Does.StartWith("Objective:"));

            Assert.That(FindSceneObject("FishingMenuButton"), Is.Not.Null, "Expected fishing menu button.");
            Assert.That(FindSceneObject("ResumeButton"), Is.Not.Null, "Expected pause resume button.");
            Assert.That(FindSceneObject("HarborButton"), Is.Not.Null, "Expected pause harbor button.");
            Assert.That(FindSceneObject("PauseSettingsButton"), Is.Not.Null, "Expected pause settings button.");
            Assert.That(FindSceneObject("PauseExitButton"), Is.Not.Null, "Expected pause exit button.");
        }

        [UnityTest]
        public IEnumerator FishingScene_BackdropsCoverViewport()
        {
            yield return LoadScene(FishingScenePath);
            yield return null;
            yield return null;

            var camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
            Assert.That(camera, Is.Not.Null, "Expected active camera.");
            Assert.That(camera.orthographic, Is.True, "Expected orthographic camera for 2D scene.");

            AssertBackdropCoverage("BackdropFar", camera, 0.98f);
            AssertBackdropCoverage("BackdropVeil", camera, 0.98f);
        }

        [UnityTest]
        public IEnumerator FishingScene_AmbientFishController_LimitsVisibleFish()
        {
            yield return LoadScene(FishingScenePath);
            yield return null;

            var ambientController = UnityEngine.Object.FindAnyObjectByType<FishingAmbientFishSwimController>();
            Assert.That(ambientController, Is.Not.Null, "Expected ambient fish swim controller.");

            var deadline = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            var scene = SceneManager.GetActiveScene();
            var renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var totalFish = 0;
            var visibleFish = 0;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer.gameObject.scene != scene)
                {
                    continue;
                }

                if (renderer.gameObject.name.IndexOf("FishingFish", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                totalFish++;
                if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    visibleFish++;
                }
            }

            Assert.That(totalFish, Is.GreaterThanOrEqualTo(5), "Expected scaffolded fish anchors in fishing scene.");
            Assert.That(visibleFish, Is.GreaterThanOrEqualTo(1), "Expected at least one ambient fish visible.");
            Assert.That(visibleFish, Is.LessThanOrEqualTo(3), "Ambient fish visibility exceeded quality cap.");
        }

        private static bool IsSceneCaptureEnabled()
        {
            var raw = Environment.GetEnvironmentVariable(SceneCaptureEnabledEnvVar);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            return string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveOutputDirectory()
        {
            var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
            if (string.IsNullOrWhiteSpace(workspace))
            {
                workspace = Directory.GetCurrentDirectory();
            }

            return Path.Combine(workspace, "artifacts", "scene-captures");
        }

        private static GameObject FindSceneObject(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            var activeScene = SceneManager.GetActiveScene();
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform.gameObject.scene != activeScene)
                {
                    continue;
                }

                if (string.Equals(transform.gameObject.name, objectName, StringComparison.Ordinal))
                {
                    return transform.gameObject;
                }
            }

            return null;
        }

        private static IEnumerator LoadScene(string scenePath)
        {
            Assert.That(File.Exists(scenePath), Is.True, $"Scene path not found: {scenePath}");
            Time.timeScale = 1f;
            var loadOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null, $"Failed to start load for scene: {scenePath}");
            yield return loadOperation;
        }

        private static IEnumerator WaitForFile(string path, float timeoutSeconds)
        {
            var timeout = Mathf.Max(0.1f, timeoutSeconds);
            var deadline = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (File.Exists(path))
                {
                    yield break;
                }

                yield return null;
            }
        }

        private static void AssertBackdropCoverage(string objectName, Camera camera, float minCoverageRatio)
        {
            var backdrop = GameObject.Find(objectName);
            Assert.That(backdrop, Is.Not.Null, $"Expected backdrop object '{objectName}'.");
            Assert.That(backdrop.GetComponent<SceneBackdropFit2D>(), Is.Not.Null, $"Expected SceneBackdropFit2D on '{objectName}'.");

            var renderer = backdrop.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null, $"Expected SpriteRenderer on '{objectName}'.");

            var viewportHeight = camera.orthographicSize * 2f;
            var viewportWidth = viewportHeight * camera.aspect;
            var bounds = renderer.bounds.size;

            Assert.That(bounds.x, Is.GreaterThanOrEqualTo(viewportWidth * minCoverageRatio), $"Backdrop '{objectName}' does not cover viewport width.");
            Assert.That(bounds.y, Is.GreaterThanOrEqualTo(viewportHeight * minCoverageRatio), $"Backdrop '{objectName}' does not cover viewport height.");
        }

        private static void CaptureScenePng(string outputPath)
        {
            var width = Mathf.Max(640, Screen.width);
            var height = Mathf.Max(360, Screen.height);

            var camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
            GameObject tempCameraObject = null;
            if (camera == null)
            {
                tempCameraObject = new GameObject("__SceneCaptureCamera");
                camera = tempCameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
            }

            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;

            var renderTexture = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                texture.Apply();

                if (!TryEncodePng(texture, out var pngBytes))
                {
                    throw new InvalidOperationException("SceneCapturePlayModeTests: PNG encode failed because ImageConversion module is unavailable.");
                }

                File.WriteAllBytes(outputPath, pngBytes);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;

                if (tempCameraObject != null)
                {
                    UnityEngine.Object.Destroy(tempCameraObject);
                }

                UnityEngine.Object.Destroy(renderTexture);
                UnityEngine.Object.Destroy(texture);
            }
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "scene";
            }

            var sanitized = value.Trim();
            var invalidChars = Path.GetInvalidFileNameChars();
            for (var i = 0; i < invalidChars.Length; i++)
            {
                sanitized = sanitized.Replace(invalidChars[i], '_');
            }

            return sanitized;
        }

        private static bool TryEncodePng(Texture2D texture, out byte[] bytes)
        {
            bytes = null;
            if (texture == null)
            {
                return false;
            }

            if (!_imageConversionLookupCompleted)
            {
                _imageConversionLookupCompleted = true;
                var imageConversionType = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule", throwOnError: false);
                if (imageConversionType != null)
                {
                    _imageConversionEncodeToPngMethod = imageConversionType.GetMethod(
                        "EncodeToPNG",
                        BindingFlags.Public | BindingFlags.Static,
                        binder: null,
                        types: new[] { typeof(Texture2D) },
                        modifiers: null);
                }
            }

            if (_imageConversionEncodeToPngMethod == null)
            {
                return false;
            }

            try
            {
                bytes = _imageConversionEncodeToPngMethod.Invoke(null, new object[] { texture }) as byte[];
                return bytes != null && bytes.Length > 0;
            }
            catch
            {
                bytes = null;
                return false;
            }
        }
    }
}
