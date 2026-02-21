using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
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
