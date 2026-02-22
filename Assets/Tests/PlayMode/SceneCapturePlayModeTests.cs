using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Fishing;
using RavenDevOps.Fishing.Harbor;
using RavenDevOps.Fishing.Performance;
using RavenDevOps.Fishing.Tools;
using RavenDevOps.Fishing.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
        private const string MainMenuScenePath = "Assets/Scenes/02_MainMenu.unity";
        private const string HarborScenePath = "Assets/Scenes/03_Harbor.unity";
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
                Assert.Ignore($"Scene capture is disabled. Clear or set {SceneCaptureEnabledEnvVar}=1 to enable.");
            }

            if (!IsGraphicsCaptureSupported())
            {
                Assert.Ignore("Scene capture requires a graphics device. Run without -nographics for visual capture.");
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
        public IEnumerator MainMenuScene_ComposesProfileAndSettingsPanels()
        {
            yield return LoadScene(MainMenuScenePath);
            yield return null;

            var runtimeRoot = GameObject.Find("__SceneRuntime");
            Assert.That(runtimeRoot, Is.Not.Null, "Expected main menu runtime root.");
            Assert.That(runtimeRoot.GetComponent<MainMenuController>(), Is.Not.Null, "Expected main menu controller.");
            Assert.That(runtimeRoot.GetComponent<ProfileMenuController>(), Is.Not.Null, "Expected profile menu controller.");
            Assert.That(runtimeRoot.GetComponent<SettingsMenuController>(), Is.Not.Null, "Expected settings menu controller.");
            Assert.That(runtimeRoot.GetComponent<TutorialControlPanel>(), Is.Not.Null, "Expected tutorial control panel.");

            var mainMenuCanvas = FindSceneObject("MainMenuCanvas");
            Assert.That(mainMenuCanvas, Is.Not.Null, "Expected main menu canvas.");
            Assert.That(mainMenuCanvas.GetComponent<UiAccessibilityCanvasRegistrant>(), Is.Not.Null, "Expected accessibility registrant on main menu canvas.");

            Assert.That(FindSceneObject("ProfilePanel"), Is.Not.Null, "Expected profile panel.");
            Assert.That(FindSceneObject("ProfileDayText"), Is.Not.Null, "Expected profile day text.");
            Assert.That(FindSceneObject("ProfileCopecsText"), Is.Not.Null, "Expected profile copecs text.");
            Assert.That(FindSceneObject("ProfileObjectiveText"), Is.Not.Null, "Expected profile objective text.");
            Assert.That(FindSceneObject("ProfileCatchLogText"), Is.Not.Null, "Expected profile catch-log text.");
            Assert.That(FindSceneObject("ProfileTutorialPanel"), Is.Not.Null, "Expected profile tutorial panel.");
            Assert.That(FindSceneObject("ProfileTutorialStatusText"), Is.Not.Null, "Expected profile tutorial status text.");
            Assert.That(FindSceneObject("ProfileSkipIntroTutorialButton"), Is.Not.Null, "Expected skip intro tutorial button.");
            Assert.That(FindSceneObject("ProfileReplayIntroTutorialButton"), Is.Not.Null, "Expected replay intro tutorial button.");
            Assert.That(FindSceneObject("ProfileSkipFishingTutorialButton"), Is.Not.Null, "Expected skip fishing tutorial button.");
            Assert.That(FindSceneObject("ProfileReplayFishingTutorialButton"), Is.Not.Null, "Expected replay fishing tutorial button.");
            Assert.That(FindSceneObject("ProfileResetButton"), Is.Not.Null, "Expected profile reset button.");
            Assert.That(FindSceneObject("ProfileResetObjectivesButton"), Is.Not.Null, "Expected profile reset-objectives button.");
            Assert.That(FindSceneObject("ProfileBackButton"), Is.Not.Null, "Expected profile back button.");

            Assert.That(FindSceneObject("SettingsPanel"), Is.Not.Null, "Expected settings panel.");
            Assert.That(FindSceneObject("SettingMasterSlider"), Is.Not.Null, "Expected master volume slider.");
            Assert.That(FindSceneObject("SettingMusicSlider"), Is.Not.Null, "Expected music slider.");
            Assert.That(FindSceneObject("SettingSfxSlider"), Is.Not.Null, "Expected sfx slider.");
            Assert.That(FindSceneObject("SettingVoSlider"), Is.Not.Null, "Expected vo slider.");
            Assert.That(FindSceneObject("SettingInputSensitivitySlider"), Is.Not.Null, "Expected input-sensitivity slider.");
            Assert.That(FindSceneObject("SettingUiScaleSlider"), Is.Not.Null, "Expected UI-scale slider.");
            Assert.That(FindSceneObject("SettingSubtitleScaleSlider"), Is.Not.Null, "Expected subtitle-scale slider.");
            Assert.That(FindSceneObject("SettingSubtitleBackgroundOpacitySlider"), Is.Not.Null, "Expected subtitle-background slider.");
            Assert.That(FindSceneObject("SettingFullscreenToggle"), Is.Not.Null, "Expected fullscreen toggle.");
            Assert.That(FindSceneObject("SettingSubtitlesToggle"), Is.Not.Null, "Expected subtitles toggle.");
            Assert.That(FindSceneObject("SettingHighContrastFishingCuesToggle"), Is.Not.Null, "Expected high-contrast toggle.");
            Assert.That(FindSceneObject("SettingReelInputToggle"), Is.Not.Null, "Expected reel-input toggle.");
            Assert.That(FindSceneObject("SettingReducedMotionToggle"), Is.Not.Null, "Expected reduced-motion toggle.");
            Assert.That(FindSceneObject("SettingReadabilityBoostToggle"), Is.Not.Null, "Expected readability-boost toggle.");
            Assert.That(FindSceneObject("SettingSteamRichPresenceToggle"), Is.Not.Null, "Expected Steam Rich Presence toggle.");
            Assert.That(FindSceneObject("SettingResolutionPrevButton"), Is.Not.Null, "Expected previous-resolution button.");
            Assert.That(FindSceneObject("SettingResolutionNextButton"), Is.Not.Null, "Expected next-resolution button.");
            Assert.That(FindSceneObject("SettingRebindFishingActionButton"), Is.Not.Null, "Expected rebind fishing-action button.");
            Assert.That(FindSceneObject("SettingRebindHarborInteractButton"), Is.Not.Null, "Expected rebind harbor-interact button.");
            Assert.That(FindSceneObject("SettingRebindMenuCancelButton"), Is.Not.Null, "Expected rebind menu-cancel button.");
            Assert.That(FindSceneObject("SettingRebindReturnHarborButton"), Is.Not.Null, "Expected rebind return-harbor button.");
            Assert.That(FindSceneObject("SettingResetRebindsButton"), Is.Not.Null, "Expected reset rebinds button.");
            Assert.That(FindSceneObject("SettingsBackButton"), Is.Not.Null, "Expected settings back button.");
        }

        [UnityTest]
        public IEnumerator HarborScene_ComposesRuntimeHudAndInteractionFlow()
        {
            yield return LoadScene(HarborScenePath);
            yield return null;

            var runtimeRoot = GameObject.Find("__SceneRuntime");
            Assert.That(runtimeRoot, Is.Not.Null, "Expected harbor runtime root.");
            Assert.That(runtimeRoot.GetComponent<HarborInteractionController>(), Is.Not.Null, "Expected harbor interaction controller.");
            Assert.That(runtimeRoot.GetComponent<HarborSceneInteractionRouter>(), Is.Not.Null, "Expected harbor interaction router.");
            Assert.That(runtimeRoot.GetComponent<HarborPauseMenuController>(), Is.Not.Null, "Expected harbor pause menu controller.");
            Assert.That(runtimeRoot.GetComponent<MermaidTutorialController>(), Is.Not.Null, "Expected harbor tutorial controller.");
            Assert.That(runtimeRoot.GetComponent<DialogueBubbleController>(), Is.Not.Null, "Expected harbor dialogue controller.");
            Assert.That(runtimeRoot.GetComponent<PerfSanityRunner>(), Is.Not.Null, "Expected harbor performance sanity runner.");

            var harborCanvas = FindSceneObject("HarborCanvas");
            Assert.That(harborCanvas, Is.Not.Null, "Expected harbor canvas.");
            Assert.That(harborCanvas.GetComponent<UiAccessibilityCanvasRegistrant>(), Is.Not.Null, "Expected accessibility registrant on harbor canvas.");

            Assert.That(FindSceneObject("HarborHudRoot"), Is.Not.Null, "Expected harbor HUD root.");
            Assert.That(FindSceneObject("HarborActionPanel"), Is.Not.Null, "Expected harbor action panel.");
            Assert.That(FindSceneObject("HarborHookShopButton"), Is.Not.Null, "Expected harbor hook shop button.");
            Assert.That(FindSceneObject("HarborBoatShopButton"), Is.Not.Null, "Expected harbor boat shop button.");
            Assert.That(FindSceneObject("HarborFishShopButton"), Is.Not.Null, "Expected harbor fish shop button.");
            Assert.That(FindSceneObject("HarborSailButton"), Is.Not.Null, "Expected harbor sail button.");
            Assert.That(FindSceneObject("HarborPauseButton"), Is.Not.Null, "Expected harbor pause button.");
            Assert.That(FindSceneObject("HarborHookShopPanel"), Is.Not.Null, "Expected harbor hook-shop submenu panel.");
            Assert.That(FindSceneObject("HarborHookShopInfo"), Is.Not.Null, "Expected harbor hook-shop info text.");
            Assert.That(FindSceneObject("HarborHookLv1Button"), Is.Not.Null, "Expected harbor hook lv1 button.");
            Assert.That(FindSceneObject("HarborHookLv2Button"), Is.Not.Null, "Expected harbor hook lv2 button.");
            Assert.That(FindSceneObject("HarborHookLv3Button"), Is.Not.Null, "Expected harbor hook lv3 button.");
            Assert.That(FindSceneObject("HarborHookLv1Icon"), Is.Not.Null, "Expected harbor hook lv1 icon.");
            Assert.That(FindSceneObject("HarborHookLv2Icon"), Is.Not.Null, "Expected harbor hook lv2 icon.");
            Assert.That(FindSceneObject("HarborHookLv3Icon"), Is.Not.Null, "Expected harbor hook lv3 icon.");
            Assert.That(FindSceneObject("HarborHookShopBackButton"), Is.Not.Null, "Expected harbor hook-shop back button.");
            Assert.That(FindSceneObject("HarborBoatShopPanel"), Is.Not.Null, "Expected harbor boat-shop submenu panel.");
            Assert.That(FindSceneObject("HarborBoatShopInfo"), Is.Not.Null, "Expected harbor boat-shop info text.");
            Assert.That(FindSceneObject("HarborBoatLv1Button"), Is.Not.Null, "Expected harbor boat lv1 button.");
            Assert.That(FindSceneObject("HarborBoatLv2Button"), Is.Not.Null, "Expected harbor boat lv2 button.");
            Assert.That(FindSceneObject("HarborBoatLv3Button"), Is.Not.Null, "Expected harbor boat lv3 button.");
            Assert.That(FindSceneObject("HarborBoatLv1Icon"), Is.Not.Null, "Expected harbor boat lv1 icon.");
            Assert.That(FindSceneObject("HarborBoatLv2Icon"), Is.Not.Null, "Expected harbor boat lv2 icon.");
            Assert.That(FindSceneObject("HarborBoatLv3Icon"), Is.Not.Null, "Expected harbor boat lv3 icon.");
            Assert.That(FindSceneObject("HarborBoatShopBackButton"), Is.Not.Null, "Expected harbor boat-shop back button.");
            Assert.That(FindSceneObject("HarborFishShopPanel"), Is.Not.Null, "Expected harbor fish-shop submenu panel.");
            Assert.That(FindSceneObject("HarborFishShopInfo"), Is.Not.Null, "Expected harbor fish-shop info text.");
            Assert.That(FindSceneObject("HarborFishShopSellButton"), Is.Not.Null, "Expected harbor fish-shop sell button.");
            Assert.That(FindSceneObject("HarborFishShopBackButton"), Is.Not.Null, "Expected harbor fish-shop back button.");
            Assert.That(FindSceneObject("HarborTutorialDialoguePanel"), Is.Not.Null, "Expected harbor tutorial dialogue panel.");
            Assert.That(FindSceneObject("HarborTutorialDialogueText"), Is.Not.Null, "Expected harbor tutorial dialogue text.");

            Assert.That(FindSceneObject("HarborInfoPanel"), Is.Not.Null, "Expected harbor info panel.");
            var statusTextGo = FindSceneObject("HarborStatus");
            Assert.That(statusTextGo, Is.Not.Null, "Expected harbor status text object.");
            var statusText = statusTextGo.GetComponent<Text>();
            Assert.That(statusText, Is.Not.Null, "Expected Text on harbor status object.");
            Assert.That(string.IsNullOrWhiteSpace(statusText.text), Is.False, "Expected harbor status text to be populated.");

            var selectionTextGo = FindSceneObject("HarborSelection");
            Assert.That(selectionTextGo, Is.Not.Null, "Expected harbor selection text object.");
            var selectionText = selectionTextGo.GetComponent<Text>();
            Assert.That(selectionText, Is.Not.Null, "Expected Text on harbor selection object.");
            Assert.That(selectionText.text, Does.StartWith("Nearby"), "Expected harbor selection hint text.");

            Assert.That(FindSceneObject("HarborEconomy"), Is.Not.Null, "Expected harbor economy text.");
            Assert.That(FindSceneObject("HarborEquipment"), Is.Not.Null, "Expected harbor equipment text.");
            Assert.That(FindSceneObject("HarborCargo"), Is.Not.Null, "Expected harbor cargo text.");
            Assert.That(FindSceneObject("HarborActivityLog"), Is.Not.Null, "Expected harbor activity log text.");

            Assert.That(FindSceneObject("HarborPausePanel"), Is.Not.Null, "Expected harbor pause panel.");
            Assert.That(FindSceneObject("HarborPauseResumeButton"), Is.Not.Null, "Expected harbor pause resume button.");
            Assert.That(FindSceneObject("HarborPauseMainMenuButton"), Is.Not.Null, "Expected harbor pause main-menu button.");
            Assert.That(FindSceneObject("HarborPauseExitButton"), Is.Not.Null, "Expected harbor pause exit button.");
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
            Assert.That(runtimeRoot.GetComponent<HudOverlayController>(), Is.Not.Null, "Expected fishing HUD overlay controller.");
            Assert.That(runtimeRoot.GetComponent<FishingPauseBridge>(), Is.Not.Null, "Expected fishing pause bridge.");
            Assert.That(runtimeRoot.GetComponent<PauseMenuController>(), Is.Not.Null, "Expected pause menu controller.");
            Assert.That(runtimeRoot.GetComponent<WaveAnimator>(), Is.Not.Null, "Expected wave animator.");
            Assert.That(runtimeRoot.GetComponent<TuningConfigApplier>(), Is.Not.Null, "Expected tuning config applier.");
            Assert.That(runtimeRoot.GetComponent<PerfSanityRunner>(), Is.Not.Null, "Expected fishing performance sanity runner.");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Assert.That(runtimeRoot.GetComponent<DebugPanelController>(), Is.Not.Null, "Expected debug panel controller in editor/development.");
#endif

            var fishingCanvas = FindSceneObject("FishingCanvas");
            Assert.That(fishingCanvas, Is.Not.Null, "Expected fishing canvas.");
            Assert.That(fishingCanvas.GetComponent<UiAccessibilityCanvasRegistrant>(), Is.Not.Null, "Expected accessibility registrant on fishing canvas.");

            Assert.That(FindSceneObject("FishingInfoPanel"), Is.Not.Null, "Expected fishing info panel.");
            Assert.That(FindSceneObject("FishingDistanceTierText"), Is.Not.Null, "Expected fishing distance-tier text.");
            Assert.That(FindSceneObject("FishingDepthText"), Is.Not.Null, "Expected fishing depth text.");
            Assert.That(FindSceneObject("FishingCopecsText"), Is.Not.Null, "Expected fishing copecs text.");
            Assert.That(FindSceneObject("FishingDayText"), Is.Not.Null, "Expected fishing day text.");
            var objectiveTextGo = FindSceneObject("FishingObjectiveText");
            Assert.That(objectiveTextGo, Is.Not.Null, "Expected fishing objective text.");
            var objectiveText = objectiveTextGo.GetComponent<TMP_Text>();
            Assert.That(objectiveText, Is.Not.Null, "Expected TMP_Text component for objective.");
            Assert.That(string.IsNullOrWhiteSpace(objectiveText.text), Is.False, "Expected objective text to be populated.");

            Assert.That(FindSceneObject("FishingMenuButton"), Is.Not.Null, "Expected fishing menu button.");
            Assert.That(FindSceneObject("ResumeButton"), Is.Not.Null, "Expected pause resume button.");
            Assert.That(FindSceneObject("HarborButton"), Is.Not.Null, "Expected pause harbor button.");
            Assert.That(FindSceneObject("PauseSettingsButton"), Is.Not.Null, "Expected pause settings button.");
            Assert.That(FindSceneObject("PauseExitButton"), Is.Not.Null, "Expected pause exit button.");
            Assert.That(FindSceneObject("PauseSettingReelInputButton"), Is.Not.Null, "Expected pause reel input setting button.");
            Assert.That(FindSceneObject("PauseSettingReducedMotionButton"), Is.Not.Null, "Expected pause reduced motion setting button.");
            Assert.That(FindSceneObject("PauseSettingHighContrastButton"), Is.Not.Null, "Expected pause high contrast setting button.");
            Assert.That(FindSceneObject("PauseSettingUiScaleDownButton"), Is.Not.Null, "Expected pause UI scale down setting button.");
            Assert.That(FindSceneObject("PauseSettingUiScaleUpButton"), Is.Not.Null, "Expected pause UI scale up setting button.");
            Assert.That(FindSceneObject("PauseSettingsBackButton"), Is.Not.Null, "Expected pause settings back button.");

            var dynamicLine = FindSceneObject("FishingDynamicLine");
            Assert.That(dynamicLine, Is.Not.Null, "Expected fishing dynamic line object.");
            Assert.That(dynamicLine.GetComponent<LineRenderer>(), Is.Not.Null, "Expected LineRenderer on fishing dynamic line.");
            Assert.That(dynamicLine.GetComponent<FishingLineBridge2D>(), Is.Not.Null, "Expected FishingLineBridge2D on fishing dynamic line.");
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

            var resolveDeadline = Time.realtimeSinceStartup + 2f;
            FishingAmbientFishSwimController ambientController = null;
            FishingActionStateMachine stateMachine = null;
            while (Time.realtimeSinceStartup < resolveDeadline)
            {
                ambientController = UnityEngine.Object.FindAnyObjectByType<FishingAmbientFishSwimController>();
                stateMachine = UnityEngine.Object.FindAnyObjectByType<FishingActionStateMachine>();
                if (ambientController != null && stateMachine != null)
                {
                    break;
                }

                yield return null;
            }

            Assert.That(ambientController, Is.Not.Null, "Expected ambient fish swim controller.");
            Assert.That(stateMachine, Is.Not.Null, "Expected fishing state machine.");

            var shipObject = FindSceneObject("FishingShip");
            Assert.That(shipObject, Is.Not.Null, "Expected fishing ship object.");
            var hookObject = FindSceneObject("FishingHook");
            Assert.That(hookObject, Is.Not.Null, "Expected fishing hook object.");

            SetPrivateFieldValue(ambientController, "_ship", shipObject.transform);
            SetPrivateFieldValue(ambientController, "_hook", hookObject.transform);

            var deadline = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            var totalFish = CountSceneFish(out var visibleFish);
            Assert.That(totalFish, Is.GreaterThanOrEqualTo(5), "Expected scaffolded fish anchors in fishing scene.");
            Assert.That(visibleFish, Is.EqualTo(0), "Fish should remain hidden until hook reaches depth 20+.");

            if (stateMachine.State != FishingActionState.Cast)
            {
                stateMachine.ResetToCast();
                yield return null;
            }

            stateMachine.AdvanceByAction();

            var forceDepthDeadline = Time.realtimeSinceStartup + 0.75f;
            while (Time.realtimeSinceStartup < forceDepthDeadline)
            {
                var hookPosition = hookObject.transform.position;
                hookPosition.y = shipObject.transform.position.y - 25f;
                hookObject.transform.position = hookPosition;
                yield return null;
            }

            var spawnDeadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < spawnDeadline)
            {
                yield return null;
            }

            totalFish = CountSceneFish(out visibleFish);
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.That(visibleFish, Is.LessThanOrEqualTo(3), "Ambient fish visibility exceeded quality cap.");
                yield break;
            }

            Assert.That(visibleFish, Is.GreaterThanOrEqualTo(1), "Expected at least one ambient fish visible.");
            Assert.That(visibleFish, Is.LessThanOrEqualTo(3), "Ambient fish visibility exceeded quality cap.");
        }

        private static int CountSceneFish(out int visibleFish)
        {
            var scene = SceneManager.GetActiveScene();
            var renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var totalFish = 0;
            visibleFish = 0;

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

            return totalFish;
        }

        private static void SetPrivateFieldValue(object target, string fieldName, object value)
        {
            if (target == null || string.IsNullOrWhiteSpace(fieldName))
            {
                return;
            }

            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return;
            }

            field.SetValue(target, value);
        }

        private static bool IsSceneCaptureEnabled()
        {
            var raw = Environment.GetEnvironmentVariable(SceneCaptureEnabledEnvVar);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }

            if (string.Equals(raw, "0", StringComparison.OrdinalIgnoreCase)
                || string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase)
                || string.Equals(raw, "no", StringComparison.OrdinalIgnoreCase)
                || string.Equals(raw, "off", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static bool IsGraphicsCaptureSupported()
        {
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
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
