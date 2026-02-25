using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Fishing;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    public sealed class FishingTutorialSceneCapturePlayModeTests
    {
        private const string FishingScenePath = "Assets/Scenes/04_Fishing.unity";

        private static readonly string[] TutorialSceneStartPhases =
        {
            "IntroInfo",
            "MoveShipInfo",
            "CastInfo",
            "FishHookInfo",
            "ReelInfo",
            "ShipUpgradeInfo",
            "HookUpgradeInfo",
            "Level4DarknessInfo",
            "Level5DeepDarkInfo",
            "FinishInfo"
        };

        private static MethodInfo _imageConversionEncodeToPngMethod;
        private static bool _imageConversionLookupCompleted;

        [TearDown]
        public void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator CaptureFishingTutorialScenes_WritesPngArtifacts()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Tutorial scene capture requires a graphics device. Run without -nographics.");
            }

            yield return LoadScene(FishingScenePath);
            yield return null;

            var saveManager = UnityEngine.Object.FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            Assert.That(saveManager, Is.Not.Null, "Expected SaveManager in fishing scene.");
            saveManager.RequestFishingLoopTutorialReplay();
            yield return null;

            var tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected FishingLoopTutorialController in fishing scene.");
            var beginTutorialMethod = typeof(FishingLoopTutorialController).GetMethod("BeginTutorial", BindingFlags.Instance | BindingFlags.NonPublic);
            var allowAutoplayInBatchModeField = typeof(FishingLoopTutorialController).GetField("_allowAutoplayInBatchMode", BindingFlags.Instance | BindingFlags.NonPublic);

            var phaseField = typeof(FishingLoopTutorialController).GetField("_demoPhase", BindingFlags.Instance | BindingFlags.NonPublic);
            var transitionActiveField = typeof(FishingLoopTutorialController).GetField("_demoSceneTransitionActive", BindingFlags.Instance | BindingFlags.NonPublic);
            var demoActiveField = typeof(FishingLoopTutorialController).GetField("_demoActive", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(beginTutorialMethod, Is.Not.Null, "Expected private method BeginTutorial.");
            Assert.That(allowAutoplayInBatchModeField, Is.Not.Null, "Expected private field _allowAutoplayInBatchMode.");
            Assert.That(phaseField, Is.Not.Null, "Expected private field _demoPhase.");
            Assert.That(transitionActiveField, Is.Not.Null, "Expected private field _demoSceneTransitionActive.");
            Assert.That(demoActiveField, Is.Not.Null, "Expected private field _demoActive.");
            allowAutoplayInBatchModeField.SetValue(tutorial, true);
            beginTutorialMethod.Invoke(tutorial, null);
            yield return null;

            var outputDirectory = ResolveOutputDirectory();
            Directory.CreateDirectory(outputDirectory);

            var capturedPhases = new HashSet<string>(StringComparer.Ordinal);
            var waitTimeoutAt = Time.realtimeSinceStartup + 180f;
            while (Time.realtimeSinceStartup < waitTimeoutAt && capturedPhases.Count < TutorialSceneStartPhases.Length)
            {
                if (tutorial == null)
                {
                    break;
                }

                var demoActive = demoActiveField.GetValue(tutorial) is bool active && active;
                var transitionActive = transitionActiveField.GetValue(tutorial) is bool transitioning && transitioning;
                var phaseValue = phaseField.GetValue(tutorial);
                var phaseName = phaseValue != null ? phaseValue.ToString() : string.Empty;

                if (demoActive
                    && !transitionActive
                    && !string.IsNullOrWhiteSpace(phaseName)
                    && Array.IndexOf(TutorialSceneStartPhases, phaseName) >= 0
                    && !capturedPhases.Contains(phaseName))
                {
                    var ordinal = Array.IndexOf(TutorialSceneStartPhases, phaseName) + 1;
                    var outputPath = Path.Combine(outputDirectory, $"{ordinal:00}_{phaseName}.png");
                    CaptureScenePng(outputPath);
                    Assert.That(File.Exists(outputPath), Is.True, $"Expected tutorial scene capture: {outputPath}");
                    Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0), $"Expected non-empty tutorial scene capture: {outputPath}");
                    capturedPhases.Add(phaseName);

                    if (!string.Equals(phaseName, "FinishInfo", StringComparison.Ordinal))
                    {
                        tutorial.SkipActiveTutorial();
                    }
                }

                yield return null;
            }

            Assert.That(
                capturedPhases.Count,
                Is.EqualTo(TutorialSceneStartPhases.Length),
                $"Expected captures for all tutorial scene start phases. Captured: {string.Join(", ", capturedPhases)}");

            Debug.Log($"TUTORIAL_SCENE_CAPTURE_OUTPUT: {outputDirectory}");
        }

        [UnityTest]
        public IEnumerator FishingTutorialController_ExplicitDependencies_InitializeAndStartDemo()
        {
            yield return LoadScene(FishingScenePath);
            yield return null;

            var tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected FishingLoopTutorialController in fishing scene.");
            ConfigureTutorialDependencies(tutorial);

            var initializedField = typeof(FishingLoopTutorialController).GetField("_dependenciesInitialized", BindingFlags.Instance | BindingFlags.NonPublic);
            var beginTutorialMethod = typeof(FishingLoopTutorialController).GetMethod("BeginTutorial", BindingFlags.Instance | BindingFlags.NonPublic);
            var allowAutoplayInBatchModeField = typeof(FishingLoopTutorialController).GetField("_allowAutoplayInBatchMode", BindingFlags.Instance | BindingFlags.NonPublic);
            var demoActiveField = typeof(FishingLoopTutorialController).GetField("_demoActive", BindingFlags.Instance | BindingFlags.NonPublic);
            var phaseField = typeof(FishingLoopTutorialController).GetField("_demoPhase", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(initializedField, Is.Not.Null, "Expected _dependenciesInitialized field.");
            Assert.That(beginTutorialMethod, Is.Not.Null, "Expected BeginTutorial method.");
            Assert.That(allowAutoplayInBatchModeField, Is.Not.Null, "Expected _allowAutoplayInBatchMode field.");
            Assert.That(demoActiveField, Is.Not.Null, "Expected _demoActive field.");
            Assert.That(phaseField, Is.Not.Null, "Expected _demoPhase field.");

            allowAutoplayInBatchModeField.SetValue(tutorial, true);
            beginTutorialMethod.Invoke(tutorial, null);
            yield return null;

            Assert.That(initializedField.GetValue(tutorial) is bool initialized && initialized, Is.True, "Dependencies should be initialized.");
            Assert.That(demoActiveField.GetValue(tutorial) is bool demoActive && demoActive, Is.True, "Demo autoplay should be active after tutorial start.");
            Assert.That(phaseField.GetValue(tutorial)?.ToString(), Is.EqualTo("IntroInfo"), "Demo should start at IntroInfo.");
        }

        [UnityTest]
        public IEnumerator FishingTutorialController_HandsOnProgression_CompletesDeterministically()
        {
            yield return LoadScene(FishingScenePath);
            yield return null;

            var tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected FishingLoopTutorialController in fishing scene.");
            ConfigureTutorialDependencies(tutorial);

            var beginTutorialMethod = typeof(FishingLoopTutorialController).GetMethod("BeginTutorial", BindingFlags.Instance | BindingFlags.NonPublic);
            var onStateChangedMethod = typeof(FishingLoopTutorialController).GetMethod("OnFishingStateChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            var onCatchResolvedMethod = typeof(FishingLoopTutorialController).GetMethod("OnCatchResolved", BindingFlags.Instance | BindingFlags.NonPublic);
            var demoActiveField = typeof(FishingLoopTutorialController).GetField("_demoActive", BindingFlags.Instance | BindingFlags.NonPublic);
            var isActiveField = typeof(FishingLoopTutorialController).GetField("_isActive", BindingFlags.Instance | BindingFlags.NonPublic);
            var stepField = typeof(FishingLoopTutorialController).GetField("_step", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(beginTutorialMethod, Is.Not.Null, "Expected BeginTutorial method.");
            Assert.That(onStateChangedMethod, Is.Not.Null, "Expected OnFishingStateChanged method.");
            Assert.That(onCatchResolvedMethod, Is.Not.Null, "Expected OnCatchResolved method.");
            Assert.That(demoActiveField, Is.Not.Null, "Expected _demoActive field.");
            Assert.That(isActiveField, Is.Not.Null, "Expected _isActive field.");
            Assert.That(stepField, Is.Not.Null, "Expected _step field.");

            beginTutorialMethod.Invoke(tutorial, null);
            yield return null;

            demoActiveField.SetValue(tutorial, false);
            onStateChangedMethod.Invoke(tutorial, new object[] { FishingActionState.Cast, FishingActionState.InWater });
            onStateChangedMethod.Invoke(tutorial, new object[] { FishingActionState.InWater, FishingActionState.Hooked });
            onStateChangedMethod.Invoke(tutorial, new object[] { FishingActionState.Hooked, FishingActionState.Reel });
            onCatchResolvedMethod.Invoke(tutorial, new object[] { true, FishingFailReason.None, "fish_cod" });
            yield return null;

            Assert.That(isActiveField.GetValue(tutorial) is bool isActive && !isActive, Is.True, "Tutorial should complete and become inactive.");
            Assert.That(stepField.GetValue(tutorial)?.ToString(), Is.EqualTo("Complete"), "Tutorial step should be Complete after success.");
        }

        private static string ResolveOutputDirectory()
        {
            var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
            if (string.IsNullOrWhiteSpace(workspace))
            {
                workspace = Directory.GetCurrentDirectory();
            }

            return Path.Combine(workspace, "artifacts", "tutorial-scene-captures");
        }

        private static IEnumerator LoadScene(string scenePath)
        {
            Assert.That(File.Exists(scenePath), Is.True, $"Scene path not found: {scenePath}");
            Time.timeScale = 1f;
            var loadOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null, $"Failed to start load for scene: {scenePath}");
            yield return loadOperation;
        }

        private static void CaptureScenePng(string outputPath)
        {
            var width = Mathf.Max(640, Screen.width);
            var height = Mathf.Max(360, Screen.height);

            var camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
            GameObject tempCameraObject = null;
            if (camera == null)
            {
                tempCameraObject = new GameObject("__TutorialSceneCaptureCamera");
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
                    throw new InvalidOperationException("FishingTutorialSceneCapturePlayModeTests: PNG encode failed because ImageConversion module is unavailable.");
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

            bytes = _imageConversionEncodeToPngMethod.Invoke(null, new object[] { texture }) as byte[];
            return bytes != null && bytes.Length > 0;
        }

        private static void ConfigureTutorialDependencies(FishingLoopTutorialController tutorial)
        {
            Assert.That(tutorial, Is.Not.Null, "Expected tutorial controller.");

            var saveManager = UnityEngine.Object.FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            var orchestrator = UnityEngine.Object.FindAnyObjectByType<GameFlowOrchestrator>(FindObjectsInactive.Include);
            var stateMachine = UnityEngine.Object.FindAnyObjectByType<FishingActionStateMachine>(FindObjectsInactive.Include);
            var catchResolver = UnityEngine.Object.FindAnyObjectByType<CatchResolver>(FindObjectsInactive.Include);
            var shipMovement = UnityEngine.Object.FindAnyObjectByType<ShipMovementController>(FindObjectsInactive.Include);
            var hookMovement = UnityEngine.Object.FindAnyObjectByType<HookMovementController>(FindObjectsInactive.Include);
            var hookCastDropController = UnityEngine.Object.FindAnyObjectByType<FishingHookCastDropController>(FindObjectsInactive.Include);
            var depthDarknessController = UnityEngine.Object.FindAnyObjectByType<FishingDepthDarknessController>(FindObjectsInactive.Include);
            var ambientFishController = UnityEngine.Object.FindAnyObjectByType<FishingAmbientFishSwimController>(FindObjectsInactive.Include);
            var inputMapController = UnityEngine.Object.FindAnyObjectByType<InputActionMapController>(FindObjectsInactive.Include);
            var inputRebindingService = UnityEngine.Object.FindAnyObjectByType<InputRebindingService>(FindObjectsInactive.Include);
            var hudOverlay = UnityEngine.Object.FindAnyObjectByType<HudOverlayController>(FindObjectsInactive.Include);
            var fishingCameraController = Camera.main != null
                ? Camera.main.GetComponent<FishingCameraController>()
                : UnityEngine.Object.FindAnyObjectByType<FishingCameraController>(FindObjectsInactive.Include);

            Assert.That(saveManager, Is.Not.Null, "Expected SaveManager dependency.");
            Assert.That(orchestrator, Is.Not.Null, "Expected GameFlowOrchestrator dependency.");
            Assert.That(stateMachine, Is.Not.Null, "Expected FishingActionStateMachine dependency.");
            Assert.That(catchResolver, Is.Not.Null, "Expected CatchResolver dependency.");
            Assert.That(shipMovement, Is.Not.Null, "Expected ShipMovementController dependency.");
            Assert.That(hookMovement, Is.Not.Null, "Expected HookMovementController dependency.");
            Assert.That(hookCastDropController, Is.Not.Null, "Expected FishingHookCastDropController dependency.");
            Assert.That(depthDarknessController, Is.Not.Null, "Expected FishingDepthDarknessController dependency.");
            Assert.That(ambientFishController, Is.Not.Null, "Expected FishingAmbientFishSwimController dependency.");
            Assert.That(inputMapController, Is.Not.Null, "Expected InputActionMapController dependency.");
            Assert.That(inputRebindingService, Is.Not.Null, "Expected InputRebindingService dependency.");
            Assert.That(hudOverlay, Is.Not.Null, "Expected HudOverlayController dependency.");

            tutorial.ConfigureDependencies(
                new FishingLoopTutorialController.DependencyBundle
                {
                    SaveManager = saveManager,
                    Orchestrator = orchestrator,
                    StateMachine = stateMachine,
                    CatchResolver = catchResolver,
                    ShipMovement = shipMovement,
                    HookMovement = hookMovement,
                    HookCastDropController = hookCastDropController,
                    DepthDarknessController = depthDarknessController,
                    AmbientFishController = ambientFishController,
                    FishingCameraController = fishingCameraController,
                    InputMapController = inputMapController,
                    InputRebindingService = inputRebindingService,
                    HudOverlay = hudOverlay
                });
        }
    }
}
