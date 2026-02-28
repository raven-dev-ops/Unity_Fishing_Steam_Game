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
using TMPro;
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
        private static MethodInfo _runtimeServicesEnsureBootstrapMethod;
        private static bool _runtimeServicesBootstrapLookupCompleted;

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
            ConfigureTutorialDependencies(tutorial);
            yield return WaitForFishingDemoAnchors(timeoutSeconds: 20f);
            tutorial.SetAutoplayInBatchModeForTests(true);
            tutorial.BeginTutorialForTests();
            yield return null;
            tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected active FishingLoopTutorialController after demo begin.");

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

                var demoActive = tutorial.IsDemoActiveForTests();
                var transitionActive = tutorial.IsDemoSceneTransitionActiveForTests();
                var phaseName = tutorial.GetDemoPhaseNameForTests();

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

            tutorial.SetAutoplayInBatchModeForTests(true);
            tutorial.BeginTutorialForTests();
            yield return null;

            Assert.That(tutorial.AreDependenciesInitializedForTests(), Is.True, "Dependencies should be initialized.");
            Assert.That(tutorial.IsDemoActiveForTests(), Is.True, "Demo autoplay should be active after tutorial start.");
            Assert.That(tutorial.GetDemoPhaseNameForTests(), Is.EqualTo("IntroInfo"), "Demo should start at IntroInfo.");
        }

        [UnityTest]
        public IEnumerator FishingTutorialController_HandsOnProgression_CompletesDeterministically()
        {
            yield return LoadScene(FishingScenePath);
            yield return null;

            var tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected FishingLoopTutorialController in fishing scene.");
            ConfigureTutorialDependencies(tutorial);

            tutorial.BeginTutorialForTests();
            yield return null;

            tutorial.SetDemoActiveForTests(false);
            tutorial.SimulateFishingStateChangedForTests(FishingActionState.Cast, FishingActionState.InWater);
            tutorial.SimulateFishingStateChangedForTests(FishingActionState.InWater, FishingActionState.Hooked);
            tutorial.SimulateFishingStateChangedForTests(FishingActionState.Hooked, FishingActionState.Reel);
            tutorial.SimulateCatchResolvedForTests(true, FishingFailReason.None, "fish_cod");
            yield return null;

            Assert.That(tutorial.IsActiveForTests(), Is.False, "Tutorial should complete and become inactive.");
            Assert.That(tutorial.GetStepNameForTests(), Is.EqualTo("Complete"), "Tutorial step should be Complete after success.");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator FishingTutorialController_DemoTransitions_UseExpectedSceneTitlesInOrder()
        {
            yield return LoadScene(FishingScenePath);
            yield return null;

            var saveManager = UnityEngine.Object.FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            Assert.That(saveManager, Is.Not.Null, "Expected SaveManager in fishing scene.");
            saveManager.RequestFishingLoopTutorialReplay();
            yield return null;

            var tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected FishingLoopTutorialController in fishing scene.");
            ConfigureTutorialDependencies(tutorial);
            yield return WaitForFishingDemoAnchors(timeoutSeconds: 20f);
            tutorial.SetAutoplayInBatchModeForTests(true);
            tutorial.BeginTutorialForTests();
            yield return null;

            var transitionTitleText = GameObject.Find("FishingTutorialTransitionTitleText")?.GetComponent<TMP_Text>();
            var transitionSubtitleText = GameObject.Find("FishingTutorialTransitionSubtitleText")?.GetComponent<TMP_Text>();
            Assert.That(transitionTitleText, Is.Not.Null, "Expected transition title text object.");
            Assert.That(transitionSubtitleText, Is.Not.Null, "Expected transition subtitle text object.");

            var expectedScenes = new (string Phase, string Title, string Subtitle)[]
            {
                ("IntroInfo", "Fishing Demo", "Loading guided tutorial flow"),
                ("MoveShipInfo", "Scene 2: Auto Sail", "Baseline ship motion"),
                ("CastInfo", "Scene 3: Cast", "Hook deployment"),
                ("FishHookInfo", "Scene 4: Hook", "Fish approach and collision"),
                ("ReelInfo", "Scene 5: Reel", "Reel mechanics"),
                ("ShipUpgradeInfo", "Scene 6: Ship Depth Bands", "Depth range progression"),
                ("HookUpgradeInfo", "Scene 7: Hook Light Tiers", "Hook tier abilities"),
                ("Level4DarknessInfo", "Scene 8: Darkness Catch", "Low visibility at 4,500m"),
                ("Level5DeepDarkInfo", "Scene 9: Deep-Dark Catch", "Deep-dark pass at 3,300m"),
                ("FinishInfo", "Scene 10: Your Turn", "Transition to player control")
            };

            var sceneIndex = 0;
            var sawExpectedTransitionTitle = false;
            var timeoutAt = Time.realtimeSinceStartup + 180f;
            while (Time.realtimeSinceStartup < timeoutAt && sceneIndex < expectedScenes.Length)
            {
                if (tutorial == null)
                {
                    break;
                }

                var transitionActive = tutorial.IsDemoSceneTransitionActiveForTests();
                var currentPhase = tutorial.GetDemoPhaseNameForTests();
                var expected = expectedScenes[sceneIndex];
                if (transitionActive)
                {
                    if (string.Equals(transitionTitleText.text, expected.Title, StringComparison.Ordinal)
                        && string.Equals(transitionSubtitleText.text, expected.Subtitle, StringComparison.Ordinal))
                    {
                        sawExpectedTransitionTitle = true;
                    }
                }
                else if (sawExpectedTransitionTitle
                    && string.Equals(currentPhase, expected.Phase, StringComparison.Ordinal))
                {
                    sceneIndex++;
                    sawExpectedTransitionTitle = false;

                    if (sceneIndex < expectedScenes.Length
                        && !string.Equals(currentPhase, "FinishInfo", StringComparison.Ordinal))
                    {
                        tutorial.SkipActiveTutorial();
                    }
                }

                yield return null;
            }

            Assert.That(
                sceneIndex,
                Is.EqualTo(expectedScenes.Length),
                $"Expected full ordered transition/title flow. Completed {sceneIndex}/{expectedScenes.Length} scenes.");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator FishingTutorialController_DepthScenes_BackdropFishLayerStaysActive()
        {
            yield return LoadScene(FishingScenePath);
            yield return null;

            var saveManager = UnityEngine.Object.FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            Assert.That(saveManager, Is.Not.Null, "Expected SaveManager in fishing scene.");
            saveManager.RequestFishingLoopTutorialReplay();
            yield return null;

            var tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected FishingLoopTutorialController in fishing scene.");
            ConfigureTutorialDependencies(tutorial);
            yield return WaitForFishingDemoAnchors(timeoutSeconds: 20f);
            tutorial.SetAutoplayInBatchModeForTests(true);
            tutorial.BeginTutorialForTests();
            yield return null;

            var backdropController = UnityEngine.Object.FindAnyObjectByType<FishingDepthBackdropFishController>(FindObjectsInactive.Include);
            Assert.That(backdropController, Is.Not.Null, "Expected FishingDepthBackdropFishController in fishing scene.");
            EnableBackdropControllerForBatchTests(backdropController);
            yield return null;

            yield return WaitForDemoSceneStartPhase(tutorial, "Level4DarknessInfo", timeoutSeconds: 180f);
            yield return WaitForBackdropFishActivation(backdropController, minimumTrackCount: 3, timeoutSeconds: 8f);

            tutorial.SkipActiveTutorial();
            yield return WaitForDemoSceneStartPhase(tutorial, "Level5DeepDarkInfo", timeoutSeconds: 180f);
            yield return WaitForBackdropFishActivation(backdropController, minimumTrackCount: 3, timeoutSeconds: 8f);
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator FishingTutorialController_SurfaceHookScenes_BackdropFishLayerVisible()
        {
            if (Application.isBatchMode)
            {
                Assert.Ignore("Surface scene backdrop visibility checks are unstable in batch mode scene routing.");
            }

            yield return LoadScene(FishingScenePath);
            yield return null;

            var saveManager = UnityEngine.Object.FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            Assert.That(saveManager, Is.Not.Null, "Expected SaveManager in fishing scene.");
            saveManager.RequestFishingLoopTutorialReplay();
            yield return null;

            var tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected FishingLoopTutorialController in fishing scene.");
            ConfigureTutorialDependencies(tutorial);
            yield return WaitForFishingDemoAnchors(timeoutSeconds: 20f);
            tutorial.SetAutoplayInBatchModeForTests(true);
            tutorial.BeginTutorialForTests();
            yield return null;

            var backdropController = UnityEngine.Object.FindAnyObjectByType<FishingDepthBackdropFishController>(FindObjectsInactive.Include);
            Assert.That(backdropController, Is.Not.Null, "Expected FishingDepthBackdropFishController in fishing scene.");
            EnableBackdropControllerForBatchTests(backdropController);
            yield return null;

            var targetCamera = Camera.main != null
                ? Camera.main
                : UnityEngine.Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            Assert.That(targetCamera, Is.Not.Null, "Expected camera for backdrop fish visibility checks.");

            yield return WaitForDemoSceneStartPhase(tutorial, "IntroInfo", timeoutSeconds: 60f, allowDuringTransition: true);
            tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected active FishingLoopTutorialController before scene skip.");
            tutorial.SkipActiveTutorial();
            yield return WaitForDemoSceneStartPhase(tutorial, "MoveShipInfo", timeoutSeconds: 60f, allowDuringTransition: true);
            tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected active FishingLoopTutorialController before scene skip.");
            tutorial.SkipActiveTutorial();
            yield return WaitForDemoSceneStartPhase(tutorial, "CastInfo", timeoutSeconds: 60f, allowDuringTransition: true);
            tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected active FishingLoopTutorialController before scene skip.");
            tutorial.SkipActiveTutorial();
            yield return WaitForDemoSceneStartPhase(tutorial, "FishHookInfo", timeoutSeconds: 60f, allowDuringTransition: true);
            yield return WaitForBackdropFishVisible(backdropController, targetCamera, minimumVisibleTracks: 1, timeoutSeconds: 10f);

            tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected active FishingLoopTutorialController before scene skip.");
            tutorial.SkipActiveTutorial();
            yield return WaitForDemoSceneStartPhase(tutorial, "ReelInfo", timeoutSeconds: 60f, allowDuringTransition: true);
            yield return WaitForBackdropFishVisible(backdropController, targetCamera, minimumVisibleTracks: 1, timeoutSeconds: 10f);
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator FishingTutorialController_IntroTransition_HoldsTitleBeforeSceneOne()
        {
            if (Application.isBatchMode)
            {
                Assert.Ignore("Intro transition hold verification is unstable in batch mode scene routing.");
            }

            yield return LoadScene(FishingScenePath);
            yield return null;

            var saveManager = UnityEngine.Object.FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            Assert.That(saveManager, Is.Not.Null, "Expected SaveManager in fishing scene.");
            saveManager.RequestFishingLoopTutorialReplay();
            yield return null;

            var tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected FishingLoopTutorialController in fishing scene.");
            ConfigureTutorialDependencies(tutorial);
            yield return WaitForFishingDemoAnchors(timeoutSeconds: 20f);
            tutorial.SetAutoplayInBatchModeForTests(true);
            tutorial.BeginTutorialForTests();
            yield return null;
            tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
            Assert.That(tutorial, Is.Not.Null, "Expected active FishingLoopTutorialController after demo begin.");

            var titleText = GameObject.Find("FishingTutorialTransitionTitleText")?.GetComponent<TMP_Text>();
            Assert.That(titleText, Is.Not.Null, "Expected transition title text object.");

            var timeoutAt = Time.realtimeSinceStartup + 180f;
            var sawIntroTransition = false;
            var introSeenStart = 0f;
            var introSeenEnd = 0f;
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                if (tutorial == null)
                {
                    tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
                    if (tutorial == null)
                    {
                        yield return null;
                        continue;
                    }
                }

                var introTitleVisible = string.Equals(titleText.text, "Fishing Demo", StringComparison.Ordinal)
                    && titleText.color.a > 0.05f;
                if (introTitleVisible)
                {
                    if (!sawIntroTransition)
                    {
                        sawIntroTransition = true;
                        introSeenStart = Time.realtimeSinceStartup;
                    }

                    introSeenEnd = Time.realtimeSinceStartup;
                    yield return null;
                    continue;
                }

                if (sawIntroTransition)
                {
                    break;
                }

                yield return null;
            }

            Assert.That(sawIntroTransition, Is.True, "Expected intro transition/title to appear for demo start.");
            var introVisibleDuration = Mathf.Max(0f, introSeenEnd - introSeenStart);
            Assert.That(
                introVisibleDuration,
                Is.GreaterThanOrEqualTo(1.9f),
                $"Expected intro title hold near 2 seconds before scene 1 fade-in, but observed {introVisibleDuration:0.00}s.");
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
            if (string.Equals(scenePath, FishingScenePath, StringComparison.Ordinal))
            {
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
            }

            Time.timeScale = 1f;
            var loadOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null, $"Failed to start load for scene: {scenePath}");
            yield return loadOperation;
        }

        private static IEnumerator WaitForDemoSceneStartPhase(
            FishingLoopTutorialController tutorial,
            string phaseName,
            float timeoutSeconds,
            bool allowDuringTransition = false)
        {
            var timeoutAt = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                if (tutorial == null)
                {
                    tutorial = UnityEngine.Object.FindAnyObjectByType<FishingLoopTutorialController>(FindObjectsInactive.Include);
                }

                if (tutorial != null
                    && tutorial.IsDemoActiveForTests()
                    && (allowDuringTransition || !tutorial.IsDemoSceneTransitionActiveForTests())
                    && string.Equals(tutorial.GetDemoPhaseNameForTests(), phaseName, StringComparison.Ordinal))
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Timed out waiting for demo phase '{phaseName}' to begin.");
        }

        private static IEnumerator WaitForFishingDemoAnchors(float timeoutSeconds)
        {
            var timeoutAt = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                var ship = GameObject.Find("FishingShip");
                var hook = GameObject.Find("FishingHook");
                if (ship != null && hook != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Timed out waiting for fishing demo anchors (FishingShip/FishingHook).");
        }

        private static IEnumerator WaitForBackdropFishActivation(
            FishingDepthBackdropFishController backdropController,
            int minimumTrackCount,
            float timeoutSeconds)
        {
            var requiredTracks = Mathf.Max(1, minimumTrackCount);
            var timeoutAt = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                var tracks = ReadBackdropTracks(backdropController);
                var populatedTracks = 0;
                var enabledTracks = 0;
                var pendingTracks = 0;
                var layerMask = 0;
                for (var i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    var renderer = GetBackdropTrackField<SpriteRenderer>(track, "Renderer");
                    var layerIndex = Mathf.Clamp(GetBackdropTrackField<int>(track, "LayerIndex"), 0, 2);
                    var pendingSpawn = GetBackdropTrackField<bool>(track, "PendingSpawn");
                    layerMask |= 1 << layerIndex;
                    if (renderer != null)
                    {
                        populatedTracks++;
                        if (renderer.enabled && !pendingSpawn)
                        {
                            enabledTracks++;
                        }
                        else if (pendingSpawn)
                        {
                            pendingTracks++;
                        }
                    }
                }

                var hasAllBackdropLayers = (layerMask & 0b111) == 0b111;
                if (populatedTracks >= requiredTracks
                    && hasAllBackdropLayers
                    && (enabledTracks + pendingTracks) >= requiredTracks)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Timed out waiting for backdrop fish layer readiness (required tracks: {requiredTracks}).");
        }

        private static IEnumerator WaitForBackdropFishVisible(
            FishingDepthBackdropFishController backdropController,
            Camera targetCamera,
            int minimumVisibleTracks,
            float timeoutSeconds)
        {
            Assert.That(targetCamera, Is.Not.Null, "Expected camera for backdrop visibility check.");
            var requiredVisibleTracks = Mathf.Max(1, minimumVisibleTracks);
            var timeoutAt = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                var tracks = ReadBackdropTracks(backdropController);
                var visibleTracks = 0;
                for (var i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    var renderer = GetBackdropTrackField<SpriteRenderer>(track, "Renderer");
                    if (renderer == null || !renderer.enabled)
                    {
                        continue;
                    }

                    if (renderer.color.a <= 0.001f)
                    {
                        continue;
                    }

                    var viewportPoint = targetCamera.WorldToViewportPoint(renderer.transform.position);
                    if (viewportPoint.z > 0f
                        && viewportPoint.x >= -0.02f
                        && viewportPoint.x <= 1.02f
                        && viewportPoint.y >= -0.02f
                        && viewportPoint.y <= 1.02f)
                    {
                        visibleTracks++;
                    }
                }

                if (visibleTracks >= requiredVisibleTracks)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Timed out waiting for visible backdrop fish tracks (required visible tracks: {requiredVisibleTracks}).");
        }

        private static List<object> ReadBackdropTracks(FishingDepthBackdropFishController backdropController)
        {
            Assert.That(backdropController, Is.Not.Null, "Expected backdrop fish controller.");
            var field = typeof(FishingDepthBackdropFishController).GetField("_tracks", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Unable to read depth backdrop tracks.");
            var enumerable = field.GetValue(backdropController) as System.Collections.IEnumerable;
            Assert.That(enumerable, Is.Not.Null, "Backdrop fish track list is unavailable.");

            var tracks = new List<object>(24);
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    tracks.Add(item);
                }
            }

            return tracks;
        }

        private static T GetBackdropTrackField<T>(object track, string fieldName)
        {
            Assert.That(track, Is.Not.Null, "Expected non-null backdrop track.");
            var field = track.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on backdrop track.");
            return (T)field.GetValue(track);
        }

        private static void EnableBackdropControllerForBatchTests(FishingDepthBackdropFishController backdropController)
        {
            if (backdropController == null)
            {
                return;
            }

            SetPrivateField(backdropController, "_allowInBatchMode", true);
            backdropController.enabled = true;

            var targetCamera = Camera.main != null
                ? Camera.main
                : UnityEngine.Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            var ship = GameObject.Find("FishingShip")?.transform;
            var hook = GameObject.Find("FishingHook")?.transform;
            if (targetCamera != null && ship != null && hook != null)
            {
                backdropController.Configure(targetCamera, ship, hook);
            }
        }

        private static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            Assert.That(instance, Is.Not.Null, "Expected instance for private field set.");
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}'.");
            field.SetValue(instance, value);
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
