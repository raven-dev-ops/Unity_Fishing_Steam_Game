using System.Collections.Generic;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Fishing;
using RavenDevOps.Fishing.Harbor;
using RavenDevOps.Fishing.Performance;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.Tools;
using RavenDevOps.Fishing.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Core
{
    public static class SceneRuntimeCompositionBootstrap
    {
        private const string RuntimeRootName = "__SceneRuntime";
        private const string TutorialSpriteLibraryResourcePath = "Pilot/Tutorial/SO_TutorialSpriteLibrary";
        private static readonly string[] NonCinematicCanvasNames =
        {
            "BootCanvas",
            "MainMenuCanvas",
            "HarborCanvas",
            "FishingCanvas"
        };
        private static readonly string[] NonCinematicUiMarkerNames =
        {
            "HarborHudRoot",
            "HarborActionPanel",
            "HarborInfoPanel",
            "HarborHookShopPanel",
            "HarborBoatShopPanel",
            "HarborFishShopPanel",
            "HarborPausePanel",
            "HarborTutorialDialoguePanel",
            "MainMenuPanel",
            "ProfilePanel",
            "SettingsPanel",
            "FishingInfoPanel",
            "PausePanel"
        };
        private static bool _initialized;
        private static Font _defaultFont;
        private static TMP_FontAsset _defaultTmpFontAsset;
        private static Material _lineMaterial;
        private static Sprite _solidSprite;
        private static TutorialSpriteLibrary _tutorialSpriteLibrary;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ComposeScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ComposeScene(scene);
        }

        private static void ComposeScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(scene.path))
            {
                return;
            }

            switch (scene.path)
            {
                case ScenePathConstants.Boot:
                    ComposeBoot(scene);
                    break;
                case ScenePathConstants.Cinematic:
                    ComposeCinematic(scene);
                    break;
                case ScenePathConstants.MainMenu:
                    ComposeMainMenu(scene);
                    break;
                case ScenePathConstants.Harbor:
                    ComposeHarbor(scene);
                    break;
                case ScenePathConstants.Fishing:
                    ComposeFishing(scene);
                    break;
            }
        }

        private static void ComposeBoot(Scene scene)
        {
            var root = GetOrCreateRuntimeRoot(scene);
            if (root.transform.Find("BootCanvas") != null)
            {
                return;
            }

            EnsureEventSystem(scene);
            var canvas = CreateCanvas(root.transform, "BootCanvas", 250);
            CreatePanel(canvas.transform, "BootPanel", new Vector2(0f, 0f), new Vector2(860f, 360f), new Color(0.05f, 0.08f, 0.14f, 0.72f));
            CreateText(canvas.transform, "BootTitle", "Raven DevOps Fishing", 42, TextAnchor.MiddleCenter, new Vector2(0f, 82f), new Vector2(780f, 92f));
            var status = CreateText(canvas.transform, "BootStatus", "Boot: Press Enter to continue.", 24, TextAnchor.MiddleCenter, new Vector2(0f, -18f), new Vector2(760f, 72f));
            var continueButton = CreateButton(canvas.transform, "ContinueButton", "Continue", new Vector2(0f, -114f), new Vector2(220f, 56f));
            continueButton.onClick.AddListener(() => RuntimeServiceRegistry.Get<GameFlowManager>()?.SetState(GameFlowState.Cinematic));

            var controller = GetOrAddComponent<BootSceneFlowController>(root);
            controller.Configure(status);
            EnsurePerfSanityRunner(root, canvas.transform, "BootPerfLabel");
        }

        private static void ComposeCinematic(Scene scene)
        {
            var root = GetOrCreateRuntimeRoot(scene);
            if (root.transform.Find("CinematicCanvas") != null)
            {
                return;
            }

            EnsureEventSystem(scene);
            SuppressLeakedNonCinematicUi();
            HideHarborVisualArtifactsFromCinematic(scene);
            NormalizeCinematicBackdrop(scene);
            var canvas = CreateCanvas(root.transform, "CinematicCanvas", 250);
            var skipButton = CreateBottomRightButton(
                canvas.transform,
                "SkipCinematicButton",
                "Skip Intro",
                new Vector2(24f, 24f),
                new Vector2(188f, 44f));

            var titleOverlay = new GameObject("CinematicTitleOverlay");
            titleOverlay.transform.SetParent(canvas.transform, worldPositionStays: false);
            var titleOverlayRect = titleOverlay.AddComponent<RectTransform>();
            titleOverlayRect.anchorMin = Vector2.zero;
            titleOverlayRect.anchorMax = Vector2.one;
            titleOverlayRect.offsetMin = Vector2.zero;
            titleOverlayRect.offsetMax = Vector2.zero;
            var titleOverlayImage = titleOverlay.AddComponent<Image>();
            titleOverlayImage.color = Color.black;
            var titleOverlayGroup = titleOverlay.AddComponent<CanvasGroup>();
            titleOverlayGroup.alpha = 0f;
            titleOverlayGroup.blocksRaycasts = false;
            titleOverlayGroup.interactable = false;
            titleOverlay.SetActive(false);

            var titleText = CreateText(
                titleOverlay.transform,
                "CinematicGameTitle",
                string.Empty,
                64,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(1560f, 180f));
            titleText.color = new Color(0.96f, 0.98f, 1f, 1f);

            var controller = GetOrAddComponent<CinematicSceneFlowController>(root);
            controller.Configure(skipButton, titleOverlayGroup, titleText);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(skipButton.gameObject);
            }

            EnsurePerfSanityRunner(root, canvas.transform, "CinematicPerfLabel");
        }

        private static void SuppressLeakedNonCinematicUi()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null || canvas.gameObject == null)
                {
                    continue;
                }

                if (!IsNonCinematicCanvasName(canvas.gameObject.name)
                    && !ContainsSuppressedUiMarker(canvas.transform))
                {
                    continue;
                }

                if (canvas.gameObject.activeSelf)
                {
                    canvas.gameObject.SetActive(false);
                }
            }
        }

        private static bool IsNonCinematicCanvasName(string canvasName)
        {
            if (string.IsNullOrWhiteSpace(canvasName))
            {
                return false;
            }

            for (var i = 0; i < NonCinematicCanvasNames.Length; i++)
            {
                if (string.Equals(canvasName, NonCinematicCanvasNames[i], System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSuppressedUiMarker(Transform root)
        {
            if (root == null)
            {
                return false;
            }

            if (IsSuppressedUiMarkerName(root.name))
            {
                return true;
            }

            var childCount = root.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var child = root.GetChild(i);
                if (ContainsSuppressedUiMarker(child))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSuppressedUiMarkerName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            for (var i = 0; i < NonCinematicUiMarkerNames.Length; i++)
            {
                if (string.Equals(objectName, NonCinematicUiMarkerNames[i], System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void HideHarborVisualArtifactsFromCinematic(Scene scene)
        {
            HideSceneObjects(
                scene,
                "TopBadge",
                "HarborShipMain",
                "HarborShipSide",
                "HarborMarketHook1",
                "HarborMarketHook2",
                "HarborMarketFish1",
                "HarborMarketFish2",
                "HarborMarketFish3",
                "DockPlank_-5",
                "DockPlank_-4",
                "DockPlank_-3",
                "DockPlank_-2",
                "DockPlank_-1",
                "DockPlank_0",
                "DockPlank_1",
                "DockPlank_2",
                "DockPlank_3",
                "DockPlank_4",
                "DockPlank_5");
        }

        private static void NormalizeCinematicBackdrop(Scene scene)
        {
            var backdropLayerA = FindSceneObject(scene, "BackdropFar");
            var backdropLayerB = FindSceneObject(scene, "BackdropVeil");
            if (backdropLayerA == null && backdropLayerB == null)
            {
                return;
            }

            var backdropRendererA = backdropLayerA != null ? backdropLayerA.GetComponent<SpriteRenderer>() : null;
            var backdropRendererB = backdropLayerB != null ? backdropLayerB.GetComponent<SpriteRenderer>() : null;
            if (backdropRendererB != null && backdropRendererB.sprite != null)
            {
                SetSpriteRendererAlpha(backdropRendererB, 1f);
                if (backdropRendererA != null)
                {
                    SetSpriteRendererAlpha(backdropRendererA, 0f);
                    backdropRendererB.sortingLayerID = backdropRendererA.sortingLayerID;
                    backdropRendererB.sortingOrder = Mathf.Max(backdropRendererB.sortingOrder, backdropRendererA.sortingOrder + 1);
                }

                return;
            }

            if (backdropRendererA != null)
            {
                SetSpriteRendererAlpha(backdropRendererA, 1f);
            }
        }

        private static void SetSpriteRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
            {
                return;
            }

            var color = renderer.color;
            color.a = Mathf.Clamp01(alpha);
            renderer.color = color;
        }

        private static void ComposeMainMenu(Scene scene)
        {
            var root = GetOrCreateRuntimeRoot(scene);
            if (root.transform.Find("MainMenuCanvas") != null)
            {
                return;
            }

            EnsureEventSystem(scene);
            HideSceneObjects(
                scene,
                "TopBadge",
                "MenuHeroShip",
                "MenuHookL1",
                "MenuHookL2",
                "MenuFishR1",
                "MenuFishR2");
            var canvas = CreateCanvas(root.transform, "MainMenuCanvas", 260);
            CreatePanel(canvas.transform, "MainMenuPanel", new Vector2(0f, 0f), new Vector2(620f, 520f), new Color(0.05f, 0.11f, 0.18f, 0.74f));
            CreateText(canvas.transform, "MainMenuTitle", "Harbor Command", 38, TextAnchor.MiddleCenter, new Vector2(0f, 186f), new Vector2(560f, 88f));

            var startButton = CreateButton(canvas.transform, "StartButton", "Start Voyage", new Vector2(0f, 88f), new Vector2(300f, 56f));
            var profileButton = CreateButton(canvas.transform, "ProfileButton", "Tutorial", new Vector2(0f, 20f), new Vector2(300f, 56f));
            var settingsButton = CreateButton(canvas.transform, "SettingsButton", "Settings", new Vector2(0f, -48f), new Vector2(300f, 56f));
            var exitButton = CreateButton(canvas.transform, "ExitButton", "Exit", new Vector2(0f, -116f), new Vector2(300f, 56f));

            var profilePanel = CreatePanel(canvas.transform, "ProfilePanel", new Vector2(0f, -8f), new Vector2(1320f, 720f), new Color(0.08f, 0.14f, 0.22f, 0.92f));
            CreateTopLeftTmpText(profilePanel.transform, "ProfileTitleText", "Tutorial Center", 32, TextAlignmentOptions.TopLeft, new Vector2(24f, 20f), new Vector2(600f, 48f));
            CreateTopLeftTmpText(
                profilePanel.transform,
                "ProfileHintText",
                "Replay or skip tutorial flows. Use Esc or Back to return.",
                16,
                TextAlignmentOptions.TopLeft,
                new Vector2(24f, 62f),
                new Vector2(820f, 38f));

            var profileStatsPanel = CreateTopLeftPanel(profilePanel.transform, "ProfileStatsPanel", new Vector2(22f, 104f), new Vector2(620f, 440f), new Color(0.09f, 0.16f, 0.25f, 0.92f));
            var profileDayText = CreateTopLeftTmpText(profileStatsPanel.transform, "ProfileDayText", "Day -", 20, TextAlignmentOptions.TopLeft, new Vector2(20f, 18f), new Vector2(576f, 32f));
            var profileCopecsText = CreateTopLeftTmpText(profileStatsPanel.transform, "ProfileCopecsText", "Copecs: -", 20, TextAlignmentOptions.TopLeft, new Vector2(20f, 56f), new Vector2(576f, 32f));
            var profileTotalFishText = CreateTopLeftTmpText(profileStatsPanel.transform, "ProfileTotalFishText", "Total Fish Caught: -", 20, TextAlignmentOptions.TopLeft, new Vector2(20f, 94f), new Vector2(576f, 32f));
            var profileFarthestDistanceText = CreateTopLeftTmpText(profileStatsPanel.transform, "ProfileFarthestDistanceText", "Farthest Distance Tier: -", 20, TextAlignmentOptions.TopLeft, new Vector2(20f, 132f), new Vector2(576f, 32f));
            var profileLevelText = CreateTopLeftTmpText(profileStatsPanel.transform, "ProfileLevelText", "Level: -", 20, TextAlignmentOptions.TopLeft, new Vector2(20f, 170f), new Vector2(576f, 32f));
            var profileXpProgressText = CreateTopLeftTmpText(profileStatsPanel.transform, "ProfileXpProgressText", "XP: -", 18, TextAlignmentOptions.TopLeft, new Vector2(20f, 208f), new Vector2(576f, 32f));
            var profileNextUnlockText = CreateTopLeftTmpText(profileStatsPanel.transform, "ProfileNextUnlockText", "Next Unlock: -", 18, TextAlignmentOptions.TopLeft, new Vector2(20f, 246f), new Vector2(576f, 32f));
            var profileObjectiveText = CreateTopLeftTmpText(profileStatsPanel.transform, "ProfileObjectiveText", "Objective: -", 17, TextAlignmentOptions.TopLeft, new Vector2(20f, 286f), new Vector2(576f, 150f));

            var profileCatchLogPanel = CreateTopLeftPanel(profilePanel.transform, "ProfileCatchLogPanel", new Vector2(662f, 104f), new Vector2(634f, 440f), new Color(0.10f, 0.17f, 0.25f, 0.92f));
            var profileCatchLogText = CreateTopLeftTmpText(profileCatchLogPanel.transform, "ProfileCatchLogText", "Catch Log: -", 17, TextAlignmentOptions.TopLeft, new Vector2(20f, 18f), new Vector2(592f, 400f));

            var tutorialPanel = CreateTopLeftPanel(profilePanel.transform, "ProfileTutorialPanel", new Vector2(22f, 104f), new Vector2(1274f, 520f), new Color(0.10f, 0.19f, 0.29f, 0.94f));
            var tutorialStatusText = CreateTopLeftTmpText(
                tutorialPanel.transform,
                "ProfileTutorialStatusText",
                "Tutorial flags: initializing...",
                17,
                TextAlignmentOptions.TopLeft,
                new Vector2(18f, 16f),
                new Vector2(1238f, 76f));
            var replayIntroTutorialButton = CreateTopLeftButton(tutorialPanel.transform, "ProfileReplayIntroTutorialButton", "Replay Intro Tutorial", new Vector2(18f, 108f), new Vector2(300f, 42f));
            var replayFishingTutorialButton = CreateTopLeftButton(tutorialPanel.transform, "ProfileReplayFishingTutorialButton", "Replay Fishing Tutorial", new Vector2(332f, 108f), new Vector2(320f, 42f));
            var skipIntroTutorialButton = CreateTopLeftButton(tutorialPanel.transform, "ProfileSkipIntroTutorialButton", "Skip Intro Tutorial", new Vector2(18f, 160f), new Vector2(300f, 42f));
            var skipFishingTutorialButton = CreateTopLeftButton(tutorialPanel.transform, "ProfileSkipFishingTutorialButton", "Skip Fishing Tutorial", new Vector2(332f, 160f), new Vector2(320f, 42f));

            var profileResetButton = CreateButton(profilePanel.transform, "ProfileResetButton", "Reset Profile", new Vector2(-280f, -320f), new Vector2(240f, 52f));
            var profileResetObjectivesButton = CreateButton(profilePanel.transform, "ProfileResetObjectivesButton", "Reset Objectives", new Vector2(0f, -320f), new Vector2(240f, 52f));
            var profileBackButton = CreateButton(profilePanel.transform, "ProfileBackButton", "Back", new Vector2(280f, -320f), new Vector2(240f, 52f));
            profileStatsPanel.SetActive(false);
            profileCatchLogPanel.SetActive(false);
            profileResetButton.gameObject.SetActive(false);
            profileResetObjectivesButton.gameObject.SetActive(false);
            profilePanel.SetActive(false);

            var settingsPanel = CreatePanel(canvas.transform, "SettingsPanel", new Vector2(0f, -8f), new Vector2(1480f, 840f), new Color(0.10f, 0.16f, 0.23f, 0.94f));
            CreateTopLeftTmpText(settingsPanel.transform, "SettingsTitleText", "Settings", 32, TextAlignmentOptions.TopLeft, new Vector2(24f, 20f), new Vector2(520f, 48f));
            CreateTopLeftTmpText(
                settingsPanel.transform,
                "SettingsHintText",
                "Adjust runtime settings, display controls, accessibility, and keyboard bindings.",
                16,
                TextAlignmentOptions.TopLeft,
                new Vector2(24f, 62f),
                new Vector2(980f, 38f));

            var settingsAudioPanel = CreateTopLeftPanel(settingsPanel.transform, "SettingsAudioPanel", new Vector2(22f, 104f), new Vector2(710f, 300f), new Color(0.12f, 0.20f, 0.30f, 0.92f));
            CreateTopLeftTmpText(settingsAudioPanel.transform, "SettingsAudioTitle", "Audio", 24, TextAlignmentOptions.TopLeft, new Vector2(18f, 14f), new Vector2(300f, 36f));
            CreateTopLeftTmpText(settingsAudioPanel.transform, "SettingMasterLabel", "Master Volume", 16, TextAlignmentOptions.TopLeft, new Vector2(18f, 62f), new Vector2(190f, 26f));
            var masterSlider = CreateTopLeftSlider(settingsAudioPanel.transform, "SettingMasterSlider", new Vector2(220f, 64f), new Vector2(360f, 28f), 0f, 1f);
            var masterMuteToggle = CreateTopLeftToggle(settingsAudioPanel.transform, "SettingMasterMuteToggle", new Vector2(592f, 62f), new Vector2(24f, 24f));
            CreateTopLeftTmpText(settingsAudioPanel.transform, "SettingMasterMuteLabel", "Mute", 14, TextAlignmentOptions.TopLeft, new Vector2(626f, 62f), new Vector2(64f, 24f));
            CreateTopLeftTmpText(settingsAudioPanel.transform, "SettingMusicLabel", "Music Volume", 16, TextAlignmentOptions.TopLeft, new Vector2(18f, 110f), new Vector2(190f, 26f));
            var musicSlider = CreateTopLeftSlider(settingsAudioPanel.transform, "SettingMusicSlider", new Vector2(220f, 112f), new Vector2(360f, 28f), 0f, 1f);
            var musicMuteToggle = CreateTopLeftToggle(settingsAudioPanel.transform, "SettingMusicMuteToggle", new Vector2(592f, 110f), new Vector2(24f, 24f));
            CreateTopLeftTmpText(settingsAudioPanel.transform, "SettingMusicMuteLabel", "Mute", 14, TextAlignmentOptions.TopLeft, new Vector2(626f, 110f), new Vector2(64f, 24f));
            CreateTopLeftTmpText(settingsAudioPanel.transform, "SettingSfxLabel", "SFX Volume", 16, TextAlignmentOptions.TopLeft, new Vector2(18f, 158f), new Vector2(190f, 26f));
            var sfxSlider = CreateTopLeftSlider(settingsAudioPanel.transform, "SettingSfxSlider", new Vector2(220f, 160f), new Vector2(360f, 28f), 0f, 1f);
            var sfxMuteToggle = CreateTopLeftToggle(settingsAudioPanel.transform, "SettingSfxMuteToggle", new Vector2(592f, 158f), new Vector2(24f, 24f));
            CreateTopLeftTmpText(settingsAudioPanel.transform, "SettingSfxMuteLabel", "Mute", 14, TextAlignmentOptions.TopLeft, new Vector2(626f, 158f), new Vector2(64f, 24f));
            CreateTopLeftTmpText(settingsAudioPanel.transform, "SettingVoLabel", "Voice Volume", 16, TextAlignmentOptions.TopLeft, new Vector2(18f, 206f), new Vector2(190f, 26f));
            var voSlider = CreateTopLeftSlider(settingsAudioPanel.transform, "SettingVoSlider", new Vector2(220f, 208f), new Vector2(360f, 28f), 0f, 1f);
            var voMuteToggle = CreateTopLeftToggle(settingsAudioPanel.transform, "SettingVoMuteToggle", new Vector2(592f, 206f), new Vector2(24f, 24f));
            CreateTopLeftTmpText(settingsAudioPanel.transform, "SettingVoMuteLabel", "Mute", 14, TextAlignmentOptions.TopLeft, new Vector2(626f, 206f), new Vector2(64f, 24f));

            var settingsGameplayPanel = CreateTopLeftPanel(settingsPanel.transform, "SettingsGameplayPanel", new Vector2(22f, 422f), new Vector2(710f, 322f), new Color(0.12f, 0.20f, 0.30f, 0.92f));
            CreateTopLeftTmpText(settingsGameplayPanel.transform, "SettingsGameplayTitle", "Gameplay and Accessibility", 24, TextAlignmentOptions.TopLeft, new Vector2(18f, 14f), new Vector2(400f, 36f));
            CreateTopLeftTmpText(settingsGameplayPanel.transform, "SettingInputSensitivityLabel", "Input Sensitivity", 16, TextAlignmentOptions.TopLeft, new Vector2(18f, 62f), new Vector2(190f, 26f));
            var inputSensitivitySlider = CreateTopLeftSlider(settingsGameplayPanel.transform, "SettingInputSensitivitySlider", new Vector2(220f, 64f), new Vector2(330f, 28f), 0.5f, 2f);
            var inputSensitivityValueText = CreateTopLeftTmpText(settingsGameplayPanel.transform, "SettingInputSensitivityValueText", "Input Sensitivity: 1.00x", 15, TextAlignmentOptions.TopLeft, new Vector2(560f, 62f), new Vector2(134f, 26f));

            CreateTopLeftTmpText(settingsGameplayPanel.transform, "SettingUiScaleLabel", "UI Scale", 16, TextAlignmentOptions.TopLeft, new Vector2(18f, 108f), new Vector2(190f, 26f));
            var uiScaleSlider = CreateTopLeftSlider(settingsGameplayPanel.transform, "SettingUiScaleSlider", new Vector2(220f, 110f), new Vector2(330f, 28f), 0.8f, 1.5f);
            var uiScaleValueText = CreateTopLeftTmpText(settingsGameplayPanel.transform, "SettingUiScaleValueText", "UI Scale: 1.00x", 15, TextAlignmentOptions.TopLeft, new Vector2(560f, 108f), new Vector2(134f, 26f));

            CreateTopLeftTmpText(settingsGameplayPanel.transform, "SettingSubtitleScaleLabel", "Subtitle Scale", 16, TextAlignmentOptions.TopLeft, new Vector2(18f, 154f), new Vector2(190f, 26f));
            var subtitleScaleSlider = CreateTopLeftSlider(settingsGameplayPanel.transform, "SettingSubtitleScaleSlider", new Vector2(220f, 156f), new Vector2(330f, 28f), 0.8f, 1.5f);
            var subtitleScaleValueText = CreateTopLeftTmpText(settingsGameplayPanel.transform, "SettingSubtitleScaleValueText", "Subtitle Scale: 1.00x", 15, TextAlignmentOptions.TopLeft, new Vector2(560f, 154f), new Vector2(134f, 26f));

            CreateTopLeftTmpText(settingsGameplayPanel.transform, "SettingSubtitleBackgroundLabel", "Subtitle Background", 16, TextAlignmentOptions.TopLeft, new Vector2(18f, 200f), new Vector2(190f, 26f));
            var subtitleBackgroundOpacitySlider = CreateTopLeftSlider(settingsGameplayPanel.transform, "SettingSubtitleBackgroundOpacitySlider", new Vector2(220f, 202f), new Vector2(330f, 28f), 0f, 1f);
            var subtitleBackgroundOpacityValueText = CreateTopLeftTmpText(settingsGameplayPanel.transform, "SettingSubtitleBackgroundOpacityValueText", "Subtitle Background: 72%", 15, TextAlignmentOptions.TopLeft, new Vector2(560f, 200f), new Vector2(134f, 26f));

            var subtitlesToggle = CreateTopLeftToggle(settingsGameplayPanel.transform, "SettingSubtitlesToggle", new Vector2(18f, 248f), new Vector2(32f, 32f));
            CreateTopLeftTmpText(settingsGameplayPanel.transform, "SettingSubtitlesToggleLabel", "Subtitles", 16, TextAlignmentOptions.TopLeft, new Vector2(56f, 250f), new Vector2(220f, 26f));
            var highContrastFishingCuesToggle = CreateTopLeftToggle(settingsGameplayPanel.transform, "SettingHighContrastFishingCuesToggle", new Vector2(320f, 248f), new Vector2(32f, 32f));
            CreateTopLeftTmpText(settingsGameplayPanel.transform, "SettingHighContrastFishingCuesToggleLabel", "High Contrast Cues", 16, TextAlignmentOptions.TopLeft, new Vector2(358f, 250f), new Vector2(300f, 26f));

            var settingsSystemPanel = CreateTopLeftPanel(settingsPanel.transform, "SettingsSystemPanel", new Vector2(748f, 104f), new Vector2(710f, 640f), new Color(0.12f, 0.20f, 0.30f, 0.92f));
            CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingsDisplayTitle", "Display and Input", 24, TextAlignmentOptions.TopLeft, new Vector2(18f, 14f), new Vector2(320f, 36f));
            var fullscreenToggle = CreateTopLeftToggle(settingsSystemPanel.transform, "SettingFullscreenToggle", new Vector2(18f, 64f), new Vector2(32f, 32f));
            CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingFullscreenToggleLabel", "Fullscreen Window", 16, TextAlignmentOptions.TopLeft, new Vector2(56f, 66f), new Vector2(220f, 26f));
            var displayModeText = CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingDisplayModeText", "Display: Fullscreen Window", 15, TextAlignmentOptions.TopLeft, new Vector2(286f, 66f), new Vector2(404f, 26f));
            var resolutionText = CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingResolutionText", "Resolution: --", 15, TextAlignmentOptions.TopLeft, new Vector2(18f, 104f), new Vector2(672f, 26f));
            var resolutionPrevButton = CreateTopLeftButton(settingsSystemPanel.transform, "SettingResolutionPrevButton", "Prev", new Vector2(18f, 136f), new Vector2(150f, 40f));
            var resolutionNextButton = CreateTopLeftButton(settingsSystemPanel.transform, "SettingResolutionNextButton", "Next", new Vector2(540f, 136f), new Vector2(150f, 40f));

            var reelInputToggle = CreateTopLeftToggle(settingsSystemPanel.transform, "SettingReelInputToggle", new Vector2(18f, 194f), new Vector2(32f, 32f));
            CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingReelInputToggleLabel", "Reel Input Toggle", 16, TextAlignmentOptions.TopLeft, new Vector2(56f, 196f), new Vector2(220f, 26f));
            var reducedMotionToggle = CreateTopLeftToggle(settingsSystemPanel.transform, "SettingReducedMotionToggle", new Vector2(18f, 232f), new Vector2(32f, 32f));
            CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingReducedMotionToggleLabel", "Reduced Motion", 16, TextAlignmentOptions.TopLeft, new Vector2(56f, 234f), new Vector2(220f, 26f));
            var readabilityBoostToggle = CreateTopLeftToggle(settingsSystemPanel.transform, "SettingReadabilityBoostToggle", new Vector2(18f, 270f), new Vector2(32f, 32f));
            CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingReadabilityBoostToggleLabel", "Readability Boost", 16, TextAlignmentOptions.TopLeft, new Vector2(56f, 272f), new Vector2(220f, 26f));
            var steamRichPresenceToggle = CreateTopLeftToggle(settingsSystemPanel.transform, "SettingSteamRichPresenceToggle", new Vector2(18f, 308f), new Vector2(32f, 32f));
            CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingSteamRichPresenceToggleLabel", "Steam Rich Presence", 16, TextAlignmentOptions.TopLeft, new Vector2(56f, 310f), new Vector2(260f, 26f));

            CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingsBindingsTitle", "Keyboard Rebinds", 22, TextAlignmentOptions.TopLeft, new Vector2(18f, 358f), new Vector2(260f, 34f));
            var fishingActionBindingText = CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingFishingActionBindingText", "Action: --", 15, TextAlignmentOptions.TopLeft, new Vector2(18f, 398f), new Vector2(470f, 26f));
            var rebindFishingActionButton = CreateTopLeftButton(settingsSystemPanel.transform, "SettingRebindFishingActionButton", "Rebind", new Vector2(534f, 394f), new Vector2(156f, 34f));
            var harborInteractBindingText = CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingHarborInteractBindingText", "Interact: --", 15, TextAlignmentOptions.TopLeft, new Vector2(18f, 438f), new Vector2(470f, 26f));
            var rebindHarborInteractButton = CreateTopLeftButton(settingsSystemPanel.transform, "SettingRebindHarborInteractButton", "Rebind", new Vector2(534f, 434f), new Vector2(156f, 34f));
            var menuCancelBindingText = CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingMenuCancelBindingText", "Cancel: --", 15, TextAlignmentOptions.TopLeft, new Vector2(18f, 478f), new Vector2(470f, 26f));
            var rebindMenuCancelButton = CreateTopLeftButton(settingsSystemPanel.transform, "SettingRebindMenuCancelButton", "Rebind", new Vector2(534f, 474f), new Vector2(156f, 34f));
            var returnHarborBindingText = CreateTopLeftTmpText(settingsSystemPanel.transform, "SettingReturnHarborBindingText", "Return Harbor: --", 15, TextAlignmentOptions.TopLeft, new Vector2(18f, 518f), new Vector2(470f, 26f));
            var rebindReturnHarborButton = CreateTopLeftButton(settingsSystemPanel.transform, "SettingRebindReturnHarborButton", "Rebind", new Vector2(534f, 514f), new Vector2(156f, 34f));
            var resetRebindsButton = CreateTopLeftButton(settingsSystemPanel.transform, "SettingResetRebindsButton", "Reset Rebinds", new Vector2(18f, 566f), new Vector2(672f, 40f));

            var settingsBackButton = CreateButton(settingsPanel.transform, "SettingsBackButton", "Back", new Vector2(0f, -370f), new Vector2(240f, 52f));
            settingsPanel.SetActive(false);

            var exitPanel = CreatePanel(canvas.transform, "ExitPanel", new Vector2(0f, -244f), new Vector2(560f, 176f), new Color(0.11f, 0.08f, 0.08f, 0.90f));
            CreateText(exitPanel.transform, "ExitPanelText", "Close the game?", 20, TextAnchor.MiddleCenter, new Vector2(0f, 40f), new Vector2(520f, 84f));
            var exitConfirmButton = CreateButton(exitPanel.transform, "ExitConfirmButton", "Exit", new Vector2(-112f, -44f), new Vector2(200f, 50f));
            var exitCancelButton = CreateButton(exitPanel.transform, "ExitCancelButton", "Cancel", new Vector2(112f, -44f), new Vector2(200f, 50f));
            exitPanel.SetActive(false);

            var controller = GetOrAddComponent<MainMenuController>(root);
            controller.Configure(
                startButton.gameObject,
                profileButton.gameObject,
                settingsButton.gameObject,
                exitButton.gameObject,
                profilePanel,
                settingsPanel,
                exitPanel,
                exitConfirmButton.gameObject,
                exitCancelButton.gameObject,
                profileBackButton.gameObject,
                settingsBackButton.gameObject);

            var profileController = GetOrAddComponent<ProfileMenuController>(root);
            profileController.Configure(
                profileDayText,
                profileCopecsText,
                profileTotalFishText,
                profileFarthestDistanceText,
                profileLevelText,
                profileXpProgressText,
                profileNextUnlockText,
                profileObjectiveText,
                profileCatchLogText,
                10);

            var tutorialControlPanel = GetOrAddComponent<TutorialControlPanel>(root);
            tutorialControlPanel.Configure(tutorialStatusText);

            var settingsController = GetOrAddComponent<SettingsMenuController>(root);
            settingsController.Configure(
                masterSlider,
                musicSlider,
                sfxSlider,
                voSlider,
                masterMuteToggle,
                musicMuteToggle,
                sfxMuteToggle,
                voMuteToggle,
                inputSensitivitySlider,
                uiScaleSlider,
                subtitleScaleSlider,
                subtitleBackgroundOpacitySlider,
                fullscreenToggle,
                subtitlesToggle,
                highContrastFishingCuesToggle,
                reelInputToggle,
                reducedMotionToggle,
                readabilityBoostToggle,
                steamRichPresenceToggle,
                displayModeText,
                resolutionText,
                inputSensitivityValueText,
                uiScaleValueText,
                subtitleScaleValueText,
                subtitleBackgroundOpacityValueText,
                fishingActionBindingText,
                harborInteractBindingText,
                menuCancelBindingText,
                returnHarborBindingText);

            startButton.onClick.AddListener(controller.StartGame);
            profileButton.onClick.AddListener(controller.StartTutorialFromMenu);
            settingsButton.onClick.AddListener(controller.OpenSettings);
            exitButton.onClick.AddListener(controller.OpenExitPanel);
            exitConfirmButton.onClick.AddListener(controller.ConfirmExit);
            exitCancelButton.onClick.AddListener(controller.CancelExit);
            profileResetButton.onClick.AddListener(profileController.ResetProfile);
            profileResetObjectivesButton.onClick.AddListener(profileController.ResetObjectivesForQa);
            skipIntroTutorialButton.onClick.AddListener(tutorialControlPanel.SkipTutorial);
            skipFishingTutorialButton.onClick.AddListener(tutorialControlPanel.SkipFishingTutorial);
            replayIntroTutorialButton.onClick.AddListener(tutorialControlPanel.ReplayTutorial);
            replayFishingTutorialButton.onClick.AddListener(tutorialControlPanel.ReplayFishingTutorial);
            profileBackButton.onClick.AddListener(controller.CloseProfilePanel);

            masterSlider.onValueChanged.AddListener(settingsController.OnMasterVolumeChanged);
            musicSlider.onValueChanged.AddListener(settingsController.OnMusicVolumeChanged);
            sfxSlider.onValueChanged.AddListener(settingsController.OnSfxVolumeChanged);
            voSlider.onValueChanged.AddListener(settingsController.OnVoVolumeChanged);
            masterMuteToggle.onValueChanged.AddListener(settingsController.OnMasterMuteChanged);
            musicMuteToggle.onValueChanged.AddListener(settingsController.OnMusicMuteChanged);
            sfxMuteToggle.onValueChanged.AddListener(settingsController.OnSfxMuteChanged);
            voMuteToggle.onValueChanged.AddListener(settingsController.OnVoMuteChanged);
            inputSensitivitySlider.onValueChanged.AddListener(settingsController.OnInputSensitivityChanged);
            uiScaleSlider.onValueChanged.AddListener(settingsController.OnUiScaleChanged);
            subtitleScaleSlider.onValueChanged.AddListener(settingsController.OnSubtitleScaleChanged);
            subtitleBackgroundOpacitySlider.onValueChanged.AddListener(settingsController.OnSubtitleBackgroundOpacityChanged);
            fullscreenToggle.onValueChanged.AddListener(settingsController.OnFullscreenChanged);
            subtitlesToggle.onValueChanged.AddListener(settingsController.OnSubtitlesChanged);
            highContrastFishingCuesToggle.onValueChanged.AddListener(settingsController.OnHighContrastFishingCuesChanged);
            reelInputToggle.onValueChanged.AddListener(settingsController.OnReelInputToggleChanged);
            reducedMotionToggle.onValueChanged.AddListener(settingsController.OnReducedMotionChanged);
            readabilityBoostToggle.onValueChanged.AddListener(settingsController.OnReadabilityBoostChanged);
            steamRichPresenceToggle.onValueChanged.AddListener(settingsController.OnSteamRichPresenceChanged);
            resolutionPrevButton.onClick.AddListener(settingsController.OnPreviousResolutionPressed);
            resolutionNextButton.onClick.AddListener(settingsController.OnNextResolutionPressed);
            rebindFishingActionButton.onClick.AddListener(settingsController.OnRebindFishingActionPressed);
            rebindHarborInteractButton.onClick.AddListener(settingsController.OnRebindHarborInteractPressed);
            rebindMenuCancelButton.onClick.AddListener(settingsController.OnRebindMenuCancelPressed);
            rebindReturnHarborButton.onClick.AddListener(settingsController.OnRebindReturnHarborPressed);
            resetRebindsButton.onClick.AddListener(settingsController.OnResetRebindsPressed);
            settingsBackButton.onClick.AddListener(controller.CloseSettingsPanel);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(startButton.gameObject);
            }

            EnsurePerfSanityRunner(root, canvas.transform, "MainMenuPerfLabel");
        }

        private static void ComposeHarbor(Scene scene)
        {
            var root = GetOrCreateRuntimeRoot(scene);
            if (root.transform.Find("HarborCanvas") != null)
            {
                return;
            }

            EnsureEventSystem(scene);
            HideSceneObjects(
                scene,
                "TopBadge",
                "HarborMarketHook1",
                "HarborMarketHook2",
                "HarborMarketFish1",
                "HarborMarketFish2",
                "HarborMarketFish3",
                "HarborShipSide");
            var canvas = CreateCanvas(root.transform, "HarborCanvas", 240);
            var hudRoot = new GameObject("HarborHudRoot");
            hudRoot.transform.SetParent(canvas.transform, worldPositionStays: false);
            var hudRootRect = hudRoot.AddComponent<RectTransform>();
            hudRootRect.anchorMin = Vector2.zero;
            hudRootRect.anchorMax = Vector2.one;
            hudRootRect.pivot = new Vector2(0.5f, 0.5f);
            hudRootRect.anchoredPosition = Vector2.zero;
            hudRootRect.sizeDelta = Vector2.zero;

            var actionPanel = CreatePanel(
                hudRoot.transform,
                "HarborActionPanel",
                new Vector2(0f, -8f),
                new Vector2(430f, 520f),
                new Color(0.05f, 0.10f, 0.18f, 0.82f));
            CreateText(actionPanel.transform, "HarborActionTitle", "Harbor Operations", 30, TextAnchor.MiddleCenter, new Vector2(0f, 212f), new Vector2(376f, 64f));
            CreateText(actionPanel.transform, "HarborActionHint", "Select an action from this menu. Profile, shops, and shipyard are managed from these buttons.", 16, TextAnchor.MiddleCenter, new Vector2(0f, 162f), new Vector2(376f, 48f));
            var profileButton = CreateButton(actionPanel.transform, "HarborProfileButton", "Profile", new Vector2(0f, 108f), new Vector2(320f, 52f));
            var hookButton = CreateButton(actionPanel.transform, "HarborHookShopButton", "Hook Shop", new Vector2(0f, 46f), new Vector2(320f, 52f));
            var boatButton = CreateButton(actionPanel.transform, "HarborBoatShopButton", "Boat Shop", new Vector2(0f, -16f), new Vector2(320f, 52f));
            var fishButton = CreateButton(actionPanel.transform, "HarborFishShopButton", "Fish Market", new Vector2(0f, -78f), new Vector2(320f, 52f));
            var sailButton = CreateButton(actionPanel.transform, "HarborSailButton", "Shipyard", new Vector2(0f, -140f), new Vector2(320f, 52f));
            var exitButton = CreateButton(actionPanel.transform, "HarborExitButton", "Main Menu", new Vector2(0f, -202f), new Vector2(320f, 50f));

            var infoPanel = CreateTopRightPanel(
                hudRoot.transform,
                "HarborInfoPanel",
                new Vector2(20f, 20f),
                new Vector2(760f, 304f),
                new Color(0.04f, 0.10f, 0.17f, 0.78f));
            var status = CreateTopLeftText(infoPanel.transform, "HarborStatus", "Harbor ready.", 20, TextAnchor.UpperLeft, new Vector2(18f, 18f), new Vector2(724f, 42f));
            var selection = CreateTopLeftText(infoPanel.transform, "HarborSelection", "Nearby target: none.", 18, TextAnchor.UpperLeft, new Vector2(18f, 56f), new Vector2(724f, 42f));
            var economy = CreateTopLeftText(infoPanel.transform, "HarborEconomy", "Copecs: 0", 18, TextAnchor.UpperLeft, new Vector2(18f, 94f), new Vector2(724f, 42f));
            var equipment = CreateTopLeftText(infoPanel.transform, "HarborEquipment", "Equipped Ship: Ship Lv1 | Hook: Hook Lv1", 18, TextAnchor.UpperLeft, new Vector2(18f, 132f), new Vector2(724f, 42f));
            var cargo = CreateTopLeftText(infoPanel.transform, "HarborCargo", "Cargo: 0 fish | Trips: 0 | Level: 1", 18, TextAnchor.UpperLeft, new Vector2(18f, 170f), new Vector2(724f, 42f));
            var activityLog = CreateTopLeftText(infoPanel.transform, "HarborActivityLog", "Recent Activity:\n- Harbor systems online.", 16, TextAnchor.UpperLeft, new Vector2(18f, 206f), new Vector2(724f, 94f));
            CreateTopLeftText(
                infoPanel.transform,
                "HarborControls",
                "Harbor: Navigate menu with arrows/WASD, Enter to confirm, Esc to pause. Use center menu and submenus.",
                16,
                TextAnchor.UpperLeft,
                new Vector2(18f, 272f),
                new Vector2(724f, 30f));

            var hookShopPanel = CreatePanel(
                hudRoot.transform,
                "HarborHookShopPanel",
                new Vector2(0f, -8f),
                new Vector2(520f, 486f),
                new Color(0.06f, 0.12f, 0.20f, 0.88f));
            CreateText(hookShopPanel.transform, "HarborHookShopTitle", "Hook Shop", 30, TextAnchor.MiddleCenter, new Vector2(0f, 202f), new Vector2(468f, 56f));
            CreateText(hookShopPanel.transform, "HarborHookShopHint", "Purchase hooks to send them to shipyard inventory.", 16, TextAnchor.MiddleCenter, new Vector2(0f, 164f), new Vector2(468f, 38f));
            var hookShopInfo = CreateTopLeftText(
                hookShopPanel.transform,
                "HarborHookShopInfo",
                "Loading hook inventory...",
                16,
                TextAnchor.UpperLeft,
                new Vector2(28f, 122f),
                new Vector2(464f, 116f));
            var hookLv1Button = CreateButton(hookShopPanel.transform, "HarborHookLv1Button", "Hook Lv1", new Vector2(0f, 28f), new Vector2(332f, 44f));
            var hookLv2Button = CreateButton(hookShopPanel.transform, "HarborHookLv2Button", "Hook Lv2", new Vector2(0f, -24f), new Vector2(332f, 44f));
            var hookLv3Button = CreateButton(hookShopPanel.transform, "HarborHookLv3Button", "Hook Lv3", new Vector2(0f, -76f), new Vector2(332f, 44f));
            var hookLv4Button = CreateButton(hookShopPanel.transform, "HarborHookLv4Button", "Hook Lv4", new Vector2(0f, -128f), new Vector2(332f, 44f));
            var hookLv5Button = CreateButton(hookShopPanel.transform, "HarborHookLv5Button", "Hook Lv5", new Vector2(0f, -180f), new Vector2(332f, 44f));
            var hookLv1Icon = CreateImageElement(hookShopPanel.transform, "HarborHookLv1Icon", new Vector2(-204f, 28f), new Vector2(40f, 40f), new Color(1f, 1f, 1f, 0.98f));
            var hookLv2Icon = CreateImageElement(hookShopPanel.transform, "HarborHookLv2Icon", new Vector2(-204f, -24f), new Vector2(40f, 40f), new Color(1f, 1f, 1f, 0.98f));
            var hookLv3Icon = CreateImageElement(hookShopPanel.transform, "HarborHookLv3Icon", new Vector2(-204f, -76f), new Vector2(40f, 40f), new Color(1f, 1f, 1f, 0.98f));
            var hookLv4Icon = CreateImageElement(hookShopPanel.transform, "HarborHookLv4Icon", new Vector2(-204f, -128f), new Vector2(40f, 40f), new Color(1f, 1f, 1f, 0.98f));
            var hookLv5Icon = CreateImageElement(hookShopPanel.transform, "HarborHookLv5Icon", new Vector2(-204f, -180f), new Vector2(40f, 40f), new Color(1f, 1f, 1f, 0.98f));
            var hookBackButton = CreateButton(hookShopPanel.transform, "HarborHookShopBackButton", "Back", new Vector2(0f, -226f), new Vector2(260f, 42f));
            hookShopPanel.SetActive(false);

            var boatShopPanel = CreatePanel(
                hudRoot.transform,
                "HarborBoatShopPanel",
                new Vector2(0f, -8f),
                new Vector2(520f, 486f),
                new Color(0.06f, 0.12f, 0.20f, 0.88f));
            CreateText(boatShopPanel.transform, "HarborBoatShopTitle", "Boat Shop", 30, TextAnchor.MiddleCenter, new Vector2(0f, 202f), new Vector2(468f, 56f));
            CreateText(boatShopPanel.transform, "HarborBoatShopHint", "Purchase ships to send them to shipyard inventory.", 16, TextAnchor.MiddleCenter, new Vector2(0f, 164f), new Vector2(468f, 38f));
            var boatShopInfo = CreateTopLeftText(
                boatShopPanel.transform,
                "HarborBoatShopInfo",
                "Loading ship inventory...",
                16,
                TextAnchor.UpperLeft,
                new Vector2(28f, 122f),
                new Vector2(464f, 116f));
            var boatLv1Button = CreateButton(boatShopPanel.transform, "HarborBoatLv1Button", "Ship Lv1", new Vector2(0f, 28f), new Vector2(332f, 44f));
            var boatLv2Button = CreateButton(boatShopPanel.transform, "HarborBoatLv2Button", "Ship Lv2", new Vector2(0f, -24f), new Vector2(332f, 44f));
            var boatLv3Button = CreateButton(boatShopPanel.transform, "HarborBoatLv3Button", "Ship Lv3", new Vector2(0f, -76f), new Vector2(332f, 44f));
            var boatLv4Button = CreateButton(boatShopPanel.transform, "HarborBoatLv4Button", "Ship Lv4", new Vector2(0f, -128f), new Vector2(332f, 44f));
            var boatLv5Button = CreateButton(boatShopPanel.transform, "HarborBoatLv5Button", "Ship Lv5", new Vector2(0f, -180f), new Vector2(332f, 44f));
            var boatLv1Icon = CreateImageElement(boatShopPanel.transform, "HarborBoatLv1Icon", new Vector2(-204f, 28f), new Vector2(40f, 40f), new Color(1f, 1f, 1f, 0.98f));
            var boatLv2Icon = CreateImageElement(boatShopPanel.transform, "HarborBoatLv2Icon", new Vector2(-204f, -24f), new Vector2(40f, 40f), new Color(1f, 1f, 1f, 0.98f));
            var boatLv3Icon = CreateImageElement(boatShopPanel.transform, "HarborBoatLv3Icon", new Vector2(-204f, -76f), new Vector2(40f, 40f), new Color(1f, 1f, 1f, 0.98f));
            var boatLv4Icon = CreateImageElement(boatShopPanel.transform, "HarborBoatLv4Icon", new Vector2(-204f, -128f), new Vector2(40f, 40f), new Color(1f, 1f, 1f, 0.98f));
            var boatLv5Icon = CreateImageElement(boatShopPanel.transform, "HarborBoatLv5Icon", new Vector2(-204f, -180f), new Vector2(40f, 40f), new Color(1f, 1f, 1f, 0.98f));
            var boatBackButton = CreateButton(boatShopPanel.transform, "HarborBoatShopBackButton", "Back", new Vector2(0f, -226f), new Vector2(260f, 42f));
            boatShopPanel.SetActive(false);

            var fishShopPanel = CreatePanel(
                hudRoot.transform,
                "HarborFishShopPanel",
                new Vector2(0f, -8f),
                new Vector2(500f, 420f),
                new Color(0.06f, 0.12f, 0.20f, 0.88f));
            CreateText(fishShopPanel.transform, "HarborFishShopTitle", "Fish Market", 30, TextAnchor.MiddleCenter, new Vector2(0f, 156f), new Vector2(448f, 56f));
            CreateText(fishShopPanel.transform, "HarborFishShopHint", "Review current cargo and sell your catch.", 16, TextAnchor.MiddleCenter, new Vector2(0f, 118f), new Vector2(448f, 34f));
            var fishShopInfo = CreateTopLeftText(
                fishShopPanel.transform,
                "HarborFishShopInfo",
                "Loading fish market summary...",
                17,
                TextAnchor.UpperLeft,
                new Vector2(26f, 98f),
                new Vector2(448f, 126f));
            var fishSellButton = CreateButton(fishShopPanel.transform, "HarborFishShopSellButton", "Sell Cargo", new Vector2(0f, -62f), new Vector2(318f, 52f));
            var fishBackButton = CreateButton(fishShopPanel.transform, "HarborFishShopBackButton", "Back", new Vector2(0f, -128f), new Vector2(240f, 46f));
            fishShopPanel.SetActive(false);

            var shipyardPanel = CreatePanel(
                hudRoot.transform,
                "HarborShipyardPanel",
                new Vector2(0f, -8f),
                new Vector2(820f, 620f),
                new Color(0.06f, 0.12f, 0.20f, 0.90f));
            CreateText(shipyardPanel.transform, "HarborShipyardTitle", "Shipyard", 32, TextAnchor.MiddleCenter, new Vector2(0f, 258f), new Vector2(760f, 56f));
            CreateText(shipyardPanel.transform, "HarborShipyardHint", "Equip owned hooks, select ship, and review cargo hold before departure.", 16, TextAnchor.MiddleCenter, new Vector2(0f, 218f), new Vector2(760f, 34f));
            var shipyardInfoText = CreateTopLeftText(shipyardPanel.transform, "HarborShipyardInfoText", "Loading shipyard inventory...", 17, TextAnchor.UpperLeft, new Vector2(26f, 96f), new Vector2(768f, 98f));
            var shipyardCargoText = CreateTopLeftText(shipyardPanel.transform, "HarborShipyardCargoText", "Cargo hold loading...", 16, TextAnchor.UpperLeft, new Vector2(26f, 198f), new Vector2(768f, 146f));
            CreateTopLeftText(shipyardPanel.transform, "HarborShipyardShipLabel", "Ships", 20, TextAnchor.UpperLeft, new Vector2(26f, 360f), new Vector2(140f, 30f));
            var shipyardShipLv1Button = CreateTopLeftButton(shipyardPanel.transform, "HarborShipyardShipLv1Button", "Ship Lv1", new Vector2(26f, 396f), new Vector2(360f, 38f));
            var shipyardShipLv2Button = CreateTopLeftButton(shipyardPanel.transform, "HarborShipyardShipLv2Button", "Ship Lv2", new Vector2(26f, 438f), new Vector2(360f, 38f));
            var shipyardShipLv3Button = CreateTopLeftButton(shipyardPanel.transform, "HarborShipyardShipLv3Button", "Ship Lv3", new Vector2(26f, 480f), new Vector2(360f, 38f));
            var shipyardShipLv4Button = CreateTopLeftButton(shipyardPanel.transform, "HarborShipyardShipLv4Button", "Ship Lv4", new Vector2(26f, 522f), new Vector2(360f, 38f));
            var shipyardShipLv5Button = CreateTopLeftButton(shipyardPanel.transform, "HarborShipyardShipLv5Button", "Ship Lv5", new Vector2(26f, 564f), new Vector2(360f, 38f));
            CreateTopLeftText(shipyardPanel.transform, "HarborShipyardHookLabel", "Hooks", 20, TextAnchor.UpperLeft, new Vector2(434f, 360f), new Vector2(140f, 30f));
            var shipyardHookLv1Button = CreateTopLeftButton(shipyardPanel.transform, "HarborShipyardHookLv1Button", "Hook Lv1", new Vector2(434f, 396f), new Vector2(360f, 38f));
            var shipyardHookLv2Button = CreateTopLeftButton(shipyardPanel.transform, "HarborShipyardHookLv2Button", "Hook Lv2", new Vector2(434f, 438f), new Vector2(360f, 38f));
            var shipyardHookLv3Button = CreateTopLeftButton(shipyardPanel.transform, "HarborShipyardHookLv3Button", "Hook Lv3", new Vector2(434f, 480f), new Vector2(360f, 38f));
            var shipyardHookLv4Button = CreateTopLeftButton(shipyardPanel.transform, "HarborShipyardHookLv4Button", "Hook Lv4", new Vector2(434f, 522f), new Vector2(360f, 38f));
            var shipyardHookLv5Button = CreateTopLeftButton(shipyardPanel.transform, "HarborShipyardHookLv5Button", "Hook Lv5", new Vector2(434f, 564f), new Vector2(360f, 38f));
            var setSailButton = CreateButton(shipyardPanel.transform, "HarborSetSailButton", "Set Sail", new Vector2(-126f, -278f), new Vector2(250f, 46f));
            var shipyardBackButton = CreateButton(shipyardPanel.transform, "HarborShipyardBackButton", "Back", new Vector2(126f, -278f), new Vector2(250f, 46f));
            shipyardPanel.SetActive(false);

            var harborProfilePanel = CreatePanel(hudRoot.transform, "HarborProfilePanel", new Vector2(0f, -8f), new Vector2(1320f, 720f), new Color(0.08f, 0.14f, 0.22f, 0.92f));
            CreateTopLeftTmpText(harborProfilePanel.transform, "HarborProfileTitleText", "Captain Profile", 32, TextAlignmentOptions.TopLeft, new Vector2(24f, 20f), new Vector2(600f, 48f));
            CreateTopLeftTmpText(
                harborProfilePanel.transform,
                "HarborProfileHintText",
                "View progression, objective status, and recent catches. Use Esc or Back to return.",
                16,
                TextAlignmentOptions.TopLeft,
                new Vector2(24f, 62f),
                new Vector2(820f, 38f));
            var harborProfileStatsPanel = CreateTopLeftPanel(harborProfilePanel.transform, "HarborProfileStatsPanel", new Vector2(22f, 104f), new Vector2(620f, 440f), new Color(0.09f, 0.16f, 0.25f, 0.92f));
            var harborProfileDayText = CreateTopLeftTmpText(harborProfileStatsPanel.transform, "HarborProfileDayText", "Day -", 20, TextAlignmentOptions.TopLeft, new Vector2(20f, 18f), new Vector2(576f, 32f));
            var harborProfileCopecsText = CreateTopLeftTmpText(harborProfileStatsPanel.transform, "HarborProfileCopecsText", "Copecs: -", 20, TextAlignmentOptions.TopLeft, new Vector2(20f, 56f), new Vector2(576f, 32f));
            var harborProfileTotalFishText = CreateTopLeftTmpText(harborProfileStatsPanel.transform, "HarborProfileTotalFishText", "Total Fish Caught: -", 20, TextAlignmentOptions.TopLeft, new Vector2(20f, 94f), new Vector2(576f, 32f));
            var harborProfileFarthestDistanceText = CreateTopLeftTmpText(harborProfileStatsPanel.transform, "HarborProfileFarthestDistanceText", "Farthest Distance Tier: -", 20, TextAlignmentOptions.TopLeft, new Vector2(20f, 132f), new Vector2(576f, 32f));
            var harborProfileLevelText = CreateTopLeftTmpText(harborProfileStatsPanel.transform, "HarborProfileLevelText", "Level: -", 20, TextAlignmentOptions.TopLeft, new Vector2(20f, 170f), new Vector2(576f, 32f));
            var harborProfileXpProgressText = CreateTopLeftTmpText(harborProfileStatsPanel.transform, "HarborProfileXpProgressText", "XP: -", 18, TextAlignmentOptions.TopLeft, new Vector2(20f, 208f), new Vector2(576f, 32f));
            var harborProfileNextUnlockText = CreateTopLeftTmpText(harborProfileStatsPanel.transform, "HarborProfileNextUnlockText", "Next Unlock: -", 18, TextAlignmentOptions.TopLeft, new Vector2(20f, 246f), new Vector2(576f, 32f));
            var harborProfileObjectiveText = CreateTopLeftTmpText(harborProfileStatsPanel.transform, "HarborProfileObjectiveText", "Objective: -", 17, TextAlignmentOptions.TopLeft, new Vector2(20f, 286f), new Vector2(576f, 150f));
            var harborProfileCatchLogPanel = CreateTopLeftPanel(harborProfilePanel.transform, "HarborProfileCatchLogPanel", new Vector2(662f, 104f), new Vector2(634f, 440f), new Color(0.10f, 0.17f, 0.25f, 0.92f));
            var harborProfileCatchLogText = CreateTopLeftTmpText(harborProfileCatchLogPanel.transform, "HarborProfileCatchLogText", "Catch Log: -", 17, TextAlignmentOptions.TopLeft, new Vector2(20f, 18f), new Vector2(592f, 400f));
            var harborProfileBackButton = CreateButton(harborProfilePanel.transform, "HarborProfileBackButton", "Back", new Vector2(280f, -320f), new Vector2(240f, 52f));
            harborProfilePanel.SetActive(false);

            var harborMainMenuConfirmPanel = CreatePanel(hudRoot.transform, "HarborMainMenuConfirmPanel", new Vector2(0f, -8f), new Vector2(560f, 220f), new Color(0.09f, 0.10f, 0.16f, 0.94f));
            CreateText(harborMainMenuConfirmPanel.transform, "HarborMainMenuConfirmTitle", "Return to Main Menu?", 28, TextAnchor.MiddleCenter, new Vector2(0f, 64f), new Vector2(500f, 56f));
            CreateText(harborMainMenuConfirmPanel.transform, "HarborMainMenuConfirmHint", "Are you sure you want to leave harbor operations?", 16, TextAnchor.MiddleCenter, new Vector2(0f, 20f), new Vector2(500f, 38f));
            var harborMainMenuConfirmYesButton = CreateButton(harborMainMenuConfirmPanel.transform, "HarborMainMenuConfirmYesButton", "Yes", new Vector2(-112f, -62f), new Vector2(200f, 46f));
            var harborMainMenuConfirmNoButton = CreateButton(harborMainMenuConfirmPanel.transform, "HarborMainMenuConfirmNoButton", "No", new Vector2(112f, -62f), new Vector2(200f, 46f));
            harborMainMenuConfirmPanel.SetActive(false);

            var tutorialDialoguePanel = CreatePanel(
                canvas.transform,
                "HarborTutorialDialoguePanel",
                new Vector2(0f, -336f),
                new Vector2(1210f, 166f),
                new Color(0.08f, 0.12f, 0.18f, 0.88f));
            var tutorialDialogueBackground = tutorialDialoguePanel.AddComponent<CanvasGroup>();
            tutorialDialogueBackground.alpha = 0.72f;
            tutorialDialogueBackground.blocksRaycasts = false;
            tutorialDialogueBackground.interactable = false;
            var tutorialDialogueText = CreateTopLeftTmpText(
                tutorialDialoguePanel.transform,
                "HarborTutorialDialogueText",
                string.Empty,
                20,
                TextAlignmentOptions.TopLeft,
                new Vector2(22f, 18f),
                new Vector2(1168f, 132f));
            tutorialDialoguePanel.SetActive(false);
            var tutorialSkipButton = CreateBottomRightButton(
                canvas.transform,
                "HarborTutorialSkipButton",
                "Skip Intro",
                new Vector2(24f, 24f),
                new Vector2(188f, 36f));
            tutorialSkipButton.gameObject.SetActive(false);

            var pauseRoot = CreatePanel(canvas.transform, "HarborPausePanel", Vector2.zero, new Vector2(440f, 292f), new Color(0.04f, 0.09f, 0.15f, 0.86f));
            CreateText(pauseRoot.transform, "HarborPauseTitle", "Harbor Paused", 30, TextAnchor.MiddleCenter, new Vector2(0f, 96f), new Vector2(320f, 62f));
            var pauseResumeButton = CreateButton(pauseRoot.transform, "HarborPauseResumeButton", "Resume", new Vector2(0f, 34f), new Vector2(250f, 48f));
            var pauseMainMenuButton = CreateButton(pauseRoot.transform, "HarborPauseMainMenuButton", "Main Menu", new Vector2(0f, -24f), new Vector2(250f, 48f));
            var pauseExitButton = CreateButton(pauseRoot.transform, "HarborPauseExitButton", "Exit", new Vector2(0f, -82f), new Vector2(250f, 48f));
            pauseRoot.SetActive(false);

            var tutorialHarborShipSprite = ResolveTutorialHarborShipSprite();
            var player = FindSceneObject(scene, "HarborShipMain");
            SpriteRenderer playerRenderer = null;
            if (player == null)
            {
                player = new GameObject("HarborPlayer");
                SceneManager.MoveGameObjectToScene(player, scene);
                player.transform.position = new Vector3(0f, 0.6f, 0f);
                playerRenderer = player.AddComponent<SpriteRenderer>();
                playerRenderer.sprite = tutorialHarborShipSprite != null ? tutorialHarborShipSprite : GetSolidSprite();
                playerRenderer.color = new Color(0.94f, 0.97f, 1f, 0.95f);
                player.transform.localScale = new Vector3(1.4f, 1.0f, 1f);
                playerRenderer.sortingOrder = 40;
            }
            else
            {
                playerRenderer = player.GetComponent<SpriteRenderer>();
            }

            if (playerRenderer != null && tutorialHarborShipSprite != null)
            {
                playerRenderer.sprite = tutorialHarborShipSprite;
            }

            GetOrAddComponent<HarborPlayerController>(player);

            var sailObject = FindSceneObject(scene, "DockPlank_0");
            if (sailObject == null)
            {
                sailObject = FindSceneObject(scene, "HarborShipMain");
            }

            var interactables = new List<WorldInteractable>
            {
                ConfigureInteractable(sailObject, InteractableType.Sail, "Sail_Highlight")
            };
            interactables.RemoveAll(x => x == null);

            var dialogueController = GetOrAddComponent<DialogueBubbleController>(root);
            dialogueController.Configure(
                tutorialDialoguePanel,
                tutorialDialogueText,
                CreateHarborTutorialDialogueLines(),
                tutorialDialogueBackground);
            var tutorialController = GetOrAddComponent<MermaidTutorialController>(root);
            tutorialController.Configure(dialogueController, tutorialSkipButton, hudRoot);

            var interactionController = GetOrAddComponent<HarborInteractionController>(root);
            interactionController.Configure(player.transform, null, interactables, tutorialController);

            var hookShop = GetOrAddComponent<HookShopController>(root);
            hookShop.ConfigureItems(new List<ShopItem>
            {
                new ShopItem { id = "hook_lv1", price = 0, valueTier = 1 },
                new ShopItem { id = "hook_lv2", price = 120, valueTier = 2 },
                new ShopItem { id = "hook_lv3", price = 320, valueTier = 3 },
                new ShopItem { id = "hook_lv4", price = 680, valueTier = 4 },
                new ShopItem { id = "hook_lv5", price = 1200, valueTier = 5 }
            });
            hookShop.SetUnlockAllItemsForQa(true);

            var boatShop = GetOrAddComponent<BoatShopController>(root);
            boatShop.ConfigureItems(new List<ShopItem>
            {
                new ShopItem { id = "ship_lv1", price = 0, valueTier = 1 },
                new ShopItem { id = "ship_lv2", price = 180, valueTier = 2 },
                new ShopItem { id = "ship_lv3", price = 420, valueTier = 3 },
                new ShopItem { id = "ship_lv4", price = 860, valueTier = 4 },
                new ShopItem { id = "ship_lv5", price = 1500, valueTier = 5 }
            });
            boatShop.SetUnlockAllItemsForQa(true);

            var fishShop = GetOrAddComponent<FishShopController>(root);
            var harborProfileController = GetOrAddComponent<ProfileMenuController>(root);
            harborProfileController.Configure(
                harborProfileDayText,
                harborProfileCopecsText,
                harborProfileTotalFishText,
                harborProfileFarthestDistanceText,
                harborProfileLevelText,
                harborProfileXpProgressText,
                harborProfileNextUnlockText,
                harborProfileObjectiveText,
                harborProfileCatchLogText,
                10);
            var router = GetOrAddComponent<HarborSceneInteractionRouter>(root);
            router.Configure(
                interactables,
                hookShop,
                boatShop,
                fishShop,
                status,
                selection,
                economy,
                equipment,
                cargo,
                activityLog,
                interactionController,
                actionPanel,
                hookShopPanel,
                boatShopPanel,
                fishShopPanel,
                harborProfilePanel,
                shipyardPanel,
                harborMainMenuConfirmPanel,
                hookShopInfo,
                boatShopInfo,
                fishShopInfo,
                shipyardInfoText,
                shipyardCargoText,
                profileButton.gameObject,
                hookLv1Button.gameObject,
                boatLv1Button.gameObject,
                fishSellButton.gameObject,
                harborProfileBackButton.gameObject,
                shipyardShipLv1Button.gameObject,
                harborMainMenuConfirmNoButton.gameObject,
                setSailButton,
                new List<Button> { hookLv1Button, hookLv2Button, hookLv3Button, hookLv4Button, hookLv5Button },
                new List<Button> { boatLv1Button, boatLv2Button, boatLv3Button, boatLv4Button, boatLv5Button },
                new List<Button> { shipyardHookLv1Button, shipyardHookLv2Button, shipyardHookLv3Button, shipyardHookLv4Button, shipyardHookLv5Button },
                new List<Button> { shipyardShipLv1Button, shipyardShipLv2Button, shipyardShipLv3Button, shipyardShipLv4Button, shipyardShipLv5Button },
                new List<Image> { hookLv1Icon, hookLv2Icon, hookLv3Icon, hookLv4Icon, hookLv5Icon },
                new List<Image> { boatLv1Icon, boatLv2Icon, boatLv3Icon, boatLv4Icon, boatLv5Icon });
            router.SetUnlockAllShopItemsForQa(true);

            hookButton.onClick.AddListener(router.OnHookShopRequested);
            boatButton.onClick.AddListener(router.OnBoatShopRequested);
            fishButton.onClick.AddListener(router.OnFishShopRequested);
            profileButton.onClick.AddListener(router.OnProfileRequested);
            sailButton.onClick.AddListener(router.OnShipyardRequested);
            exitButton.onClick.AddListener(router.OnMainMenuRequested);
            hookLv1Button.onClick.AddListener(() => router.OnHookShopItemRequested("hook_lv1"));
            hookLv2Button.onClick.AddListener(() => router.OnHookShopItemRequested("hook_lv2"));
            hookLv3Button.onClick.AddListener(() => router.OnHookShopItemRequested("hook_lv3"));
            hookLv4Button.onClick.AddListener(() => router.OnHookShopItemRequested("hook_lv4"));
            hookLv5Button.onClick.AddListener(() => router.OnHookShopItemRequested("hook_lv5"));
            hookBackButton.onClick.AddListener(router.OnShopBackRequested);
            boatLv1Button.onClick.AddListener(() => router.OnBoatShopItemRequested("ship_lv1"));
            boatLv2Button.onClick.AddListener(() => router.OnBoatShopItemRequested("ship_lv2"));
            boatLv3Button.onClick.AddListener(() => router.OnBoatShopItemRequested("ship_lv3"));
            boatLv4Button.onClick.AddListener(() => router.OnBoatShopItemRequested("ship_lv4"));
            boatLv5Button.onClick.AddListener(() => router.OnBoatShopItemRequested("ship_lv5"));
            boatBackButton.onClick.AddListener(router.OnShopBackRequested);
            fishSellButton.onClick.AddListener(router.OnFishShopSellRequested);
            fishBackButton.onClick.AddListener(router.OnShopBackRequested);
            harborProfileBackButton.onClick.AddListener(router.OnShopBackRequested);
            shipyardShipLv1Button.onClick.AddListener(() => router.OnShipyardShipRequested("ship_lv1"));
            shipyardShipLv2Button.onClick.AddListener(() => router.OnShipyardShipRequested("ship_lv2"));
            shipyardShipLv3Button.onClick.AddListener(() => router.OnShipyardShipRequested("ship_lv3"));
            shipyardShipLv4Button.onClick.AddListener(() => router.OnShipyardShipRequested("ship_lv4"));
            shipyardShipLv5Button.onClick.AddListener(() => router.OnShipyardShipRequested("ship_lv5"));
            shipyardHookLv1Button.onClick.AddListener(() => router.OnShipyardHookRequested("hook_lv1"));
            shipyardHookLv2Button.onClick.AddListener(() => router.OnShipyardHookRequested("hook_lv2"));
            shipyardHookLv3Button.onClick.AddListener(() => router.OnShipyardHookRequested("hook_lv3"));
            shipyardHookLv4Button.onClick.AddListener(() => router.OnShipyardHookRequested("hook_lv4"));
            shipyardHookLv5Button.onClick.AddListener(() => router.OnShipyardHookRequested("hook_lv5"));
            setSailButton.onClick.AddListener(router.OnSailRequested);
            shipyardBackButton.onClick.AddListener(router.OnShopBackRequested);
            harborMainMenuConfirmYesButton.onClick.AddListener(router.OnMainMenuConfirmAccepted);
            harborMainMenuConfirmNoButton.onClick.AddListener(router.OnMainMenuConfirmDeclined);

            var pauseController = GetOrAddComponent<HarborPauseMenuController>(root);
            pauseController.Configure(
                pauseRoot,
                hudRoot,
                pauseResumeButton.gameObject);
            pauseResumeButton.onClick.AddListener(pauseController.OnResumePressed);
            pauseMainMenuButton.onClick.AddListener(pauseController.OnMainMenuPressed);
            pauseExitButton.onClick.AddListener(pauseController.OnExitGamePressed);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(profileButton.gameObject);
            }

            EnsurePerfSanityRunner(root, canvas.transform, "HarborPerfLabel");
        }

        private static void ComposeFishing(Scene scene)
        {
            var root = GetOrCreateRuntimeRoot(scene);
            if (root.transform.Find("FishingCanvas") != null)
            {
                return;
            }

            EnsureEventSystem(scene);
            var canvas = CreateCanvas(root.transform, "FishingCanvas", 245);
            var infoPanel = CreateTopRightPanel(canvas.transform, "FishingInfoPanel", new Vector2(20f, 20f), new Vector2(880f, 292f), new Color(0.04f, 0.10f, 0.17f, 0.78f));
            var distanceTierText = CreateTopLeftTmpText(infoPanel.transform, "FishingDistanceTierText", "Distance Tier: 1", 18, TextAlignmentOptions.TopLeft, new Vector2(18f, 14f), new Vector2(428f, 32f));
            var depthText = CreateTopLeftTmpText(infoPanel.transform, "FishingDepthText", "Depth: 0.0 m", 18, TextAlignmentOptions.TopLeft, new Vector2(18f, 46f), new Vector2(428f, 32f));
            var copecsText = CreateTopLeftTmpText(infoPanel.transform, "FishingCopecsText", "Copecs: 0", 16, TextAlignmentOptions.TopLeft, new Vector2(454f, 14f), new Vector2(190f, 28f));
            var dayText = CreateTopLeftTmpText(infoPanel.transform, "FishingDayText", "Day 1", 16, TextAlignmentOptions.TopLeft, new Vector2(454f, 42f), new Vector2(190f, 28f));
            var tensionText = CreateTopLeftTmpText(infoPanel.transform, "FishingTensionText", "Tension: None (0.00)", 18, TextAlignmentOptions.TopLeft, new Vector2(18f, 78f), new Vector2(844f, 32f));
            var conditionText = CreateTopLeftTmpText(infoPanel.transform, "FishingConditionText", string.Empty, 18, TextAlignmentOptions.TopLeft, new Vector2(18f, 110f), new Vector2(844f, 32f));
            var objectiveText = CreateTopLeftTmpText(infoPanel.transform, "FishingObjectiveText", "Objective: Follow current task goals.", 18, TextAlignmentOptions.TopLeft, new Vector2(18f, 142f), new Vector2(844f, 32f));
            var statusText = CreateTopLeftTmpText(infoPanel.transform, "FishingStatusText", "Press Down Arrow or S to cast. Use Up Arrow or W to reel.", 18, TextAlignmentOptions.TopLeft, new Vector2(18f, 174f), new Vector2(844f, 32f));
            var failureText = CreateTopLeftTmpText(infoPanel.transform, "FishingFailureText", string.Empty, 18, TextAlignmentOptions.TopLeft, new Vector2(18f, 206f), new Vector2(844f, 32f));
            CreateTopLeftTmpText(
                infoPanel.transform,
                "FishingControls",
                "Fishing: Left/Right steers the ship at all times, even with hook down. Hook fish by colliding the hook with fish. Lv3 hook bait can attract fish. Down/S casts and lowers; Up/W reels. Esc pause, H return harbor.",
                16,
                TextAlignmentOptions.TopLeft,
                new Vector2(18f, 238f),
                new Vector2(844f, 46f));
            var menuButton = CreateTopLeftButton(canvas.transform, "FishingMenuButton", "Menu", new Vector2(20f, 20f), new Vector2(140f, 44f));
            menuButton.onClick.AddListener(() => RuntimeServiceRegistry.Get<GameFlowManager>()?.TogglePause());
            var fishingTutorialSkipAllButton = CreateTopLeftButton(infoPanel.transform, "FishingTutorialSkipAllButton", "Skip All", new Vector2(532f, 12f), new Vector2(156f, 28f));
            fishingTutorialSkipAllButton.gameObject.SetActive(false);
            var fishingTutorialSkipButton = CreateTopLeftButton(infoPanel.transform, "FishingTutorialSkipButton", "Skip Tutorial", new Vector2(694f, 12f), new Vector2(160f, 28f));
            fishingTutorialSkipButton.gameObject.SetActive(false);
            var fishingTutorialMessagePanel = CreatePanel(
                canvas.transform,
                "FishingTutorialMessagePanel",
                new Vector2(0f, -248f),
                new Vector2(1160f, 152f),
                new Color(0.02f, 0.07f, 0.12f, 0.86f));
            var fishingTutorialMessagePanelImage = fishingTutorialMessagePanel.GetComponent<Image>();
            if (fishingTutorialMessagePanelImage != null)
            {
                fishingTutorialMessagePanelImage.raycastTarget = false;
            }

            var fishingTutorialMessageText = CreateTopLeftText(
                fishingTutorialMessagePanel.transform,
                "FishingTutorialMessageText",
                string.Empty,
                24,
                TextAnchor.UpperLeft,
                new Vector2(28f, 18f),
                new Vector2(1104f, 116f));
            fishingTutorialMessageText.raycastTarget = false;
            fishingTutorialMessagePanel.SetActive(false);
            var fishingTutorialTransitionPanel = new GameObject("FishingTutorialTransitionPanel");
            fishingTutorialTransitionPanel.transform.SetParent(canvas.transform, worldPositionStays: false);
            var fishingTutorialTransitionRect = fishingTutorialTransitionPanel.AddComponent<RectTransform>();
            fishingTutorialTransitionRect.anchorMin = Vector2.zero;
            fishingTutorialTransitionRect.anchorMax = Vector2.one;
            fishingTutorialTransitionRect.pivot = new Vector2(0.5f, 0.5f);
            fishingTutorialTransitionRect.offsetMin = Vector2.zero;
            fishingTutorialTransitionRect.offsetMax = Vector2.zero;
            var fishingTutorialTransitionFadeImage = fishingTutorialTransitionPanel.AddComponent<Image>();
            fishingTutorialTransitionFadeImage.color = new Color(0f, 0f, 0f, 0f);
            fishingTutorialTransitionFadeImage.raycastTarget = false;
            var fishingTutorialTransitionTitleText = CreateText(
                fishingTutorialTransitionPanel.transform,
                "FishingTutorialTransitionTitleText",
                string.Empty,
                52,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 38f),
                new Vector2(1520f, 170f));
            fishingTutorialTransitionTitleText.color = new Color(0.95f, 0.98f, 1f, 0f);
            fishingTutorialTransitionTitleText.raycastTarget = false;
            var fishingTutorialTransitionSubtitleText = CreateText(
                fishingTutorialTransitionPanel.transform,
                "FishingTutorialTransitionSubtitleText",
                string.Empty,
                30,
                TextAnchor.MiddleCenter,
                new Vector2(0f, -34f),
                new Vector2(1440f, 84f));
            fishingTutorialTransitionSubtitleText.color = new Color(0.78f, 0.88f, 0.96f, 0f);
            fishingTutorialTransitionSubtitleText.raycastTarget = false;
            fishingTutorialTransitionPanel.SetActive(false);

            var pauseRoot = CreatePanel(canvas.transform, "PausePanel", Vector2.zero, new Vector2(440f, 300f), new Color(0.04f, 0.09f, 0.15f, 0.84f));
            CreateText(pauseRoot.transform, "PauseTitle", "Paused", 30, TextAnchor.MiddleCenter, new Vector2(0f, 108f), new Vector2(320f, 62f));
            var pauseSettingsPanel = CreatePanel(pauseRoot.transform, "PauseSettingsPanel", new Vector2(0f, -38f), new Vector2(392f, 214f), new Color(0.1f, 0.14f, 0.2f, 0.92f));
            CreateText(pauseSettingsPanel.transform, "PauseSettingsTitle", "Quick Settings", 22, TextAnchor.MiddleCenter, new Vector2(0f, 84f), new Vector2(320f, 36f));
            var reelInputButton = CreateButton(pauseSettingsPanel.transform, "PauseSettingReelInputButton", "Reel Input: Hold", new Vector2(0f, 44f), new Vector2(320f, 38f));
            var reducedMotionButton = CreateButton(pauseSettingsPanel.transform, "PauseSettingReducedMotionButton", "Reduced Motion: Off", new Vector2(0f, 2f), new Vector2(320f, 38f));
            var highContrastButton = CreateButton(pauseSettingsPanel.transform, "PauseSettingHighContrastButton", "High Contrast Cues: Off", new Vector2(0f, -40f), new Vector2(320f, 38f));
            var uiScaleDownButton = CreateButton(pauseSettingsPanel.transform, "PauseSettingUiScaleDownButton", "-", new Vector2(-110f, -82f), new Vector2(56f, 34f));
            var uiScaleUpButton = CreateButton(pauseSettingsPanel.transform, "PauseSettingUiScaleUpButton", "+", new Vector2(110f, -82f), new Vector2(56f, 34f));
            var uiScaleValueText = CreateText(pauseSettingsPanel.transform, "PauseSettingUiScaleValueText", "UI Scale: 1.00x", 17, TextAnchor.MiddleCenter, new Vector2(0f, -82f), new Vector2(164f, 34f));
            var backSettingsButton = CreateButton(pauseSettingsPanel.transform, "PauseSettingsBackButton", "Back", new Vector2(0f, -124f), new Vector2(220f, 36f));
            pauseSettingsPanel.SetActive(false);

            var resumeButton = CreateButton(pauseRoot.transform, "ResumeButton", "Resume", new Vector2(0f, 40f), new Vector2(250f, 48f));
            var harborButton = CreateButton(pauseRoot.transform, "HarborButton", "Return Harbor", new Vector2(0f, -16f), new Vector2(250f, 48f));
            var settingsButton = CreateButton(pauseRoot.transform, "PauseSettingsButton", "Settings", new Vector2(0f, -72f), new Vector2(250f, 48f));
            var exitButton = CreateButton(pauseRoot.transform, "PauseExitButton", "Exit", new Vector2(0f, -128f), new Vector2(250f, 48f));
            pauseRoot.SetActive(false);

            var tutorialFishingShipSprite = ResolveTutorialFishingShipSprite();
            var shipObject = FindSceneObject(scene, "FishingShip");
            if (shipObject == null)
            {
                shipObject = new GameObject("FishingShip");
                SceneManager.MoveGameObjectToScene(shipObject, scene);
                var renderer = shipObject.AddComponent<SpriteRenderer>();
                renderer.sprite = tutorialFishingShipSprite != null ? tutorialFishingShipSprite : GetSolidSprite();
                renderer.color = new Color(0.95f, 0.99f, 1f, 0.95f);
                renderer.sortingOrder = 20;
                shipObject.transform.localScale = new Vector3(1.15f, 0.72f, 1f);
                shipObject.transform.position = new Vector3(0f, 2.4f, 0f);
            }

            var tutorialHookSprite = ResolveTutorialHookSprite();
            var hookObject = FindSceneObject(scene, "FishingHook");
            if (hookObject == null)
            {
                hookObject = new GameObject("FishingHook");
                SceneManager.MoveGameObjectToScene(hookObject, scene);
                var renderer = hookObject.AddComponent<SpriteRenderer>();
                renderer.sprite = tutorialHookSprite != null ? tutorialHookSprite : GetSolidSprite();
                renderer.color = new Color(0.88f, 0.95f, 1f, 0.95f);
                renderer.sortingOrder = 20;
                hookObject.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                hookObject.transform.position = new Vector3(0f, -1f, 0f);
            }

            var legacyLineObject = FindSceneObject(scene, "FishingLine");
            if (legacyLineObject != null)
            {
                legacyLineObject.SetActive(false);
            }

            var dynamicLineObject = FindSceneObject(scene, "FishingDynamicLine");
            if (dynamicLineObject == null)
            {
                dynamicLineObject = new GameObject("FishingDynamicLine");
                SceneManager.MoveGameObjectToScene(dynamicLineObject, scene);
            }

            var shipSpriteRenderer = shipObject.GetComponent<SpriteRenderer>();
            if (shipSpriteRenderer != null && tutorialFishingShipSprite != null)
            {
                shipSpriteRenderer.sprite = tutorialFishingShipSprite;
            }

            var hookSpriteRenderer = hookObject.GetComponent<SpriteRenderer>();
            if (hookSpriteRenderer != null && tutorialHookSprite != null)
            {
                hookSpriteRenderer.sprite = tutorialHookSprite;
            }

            ApplyTutorialFishSprites(scene, ResolveTutorialFishSprite());

            var legacyDynamicLineRenderer = dynamicLineObject.GetComponent<SpriteRenderer>();
            if (legacyDynamicLineRenderer != null)
            {
                legacyDynamicLineRenderer.enabled = false;
            }

            var dynamicLineRenderer = dynamicLineObject.GetComponent<LineRenderer>();
            if (dynamicLineRenderer == null)
            {
                dynamicLineRenderer = dynamicLineObject.AddComponent<LineRenderer>();
            }

            dynamicLineRenderer.useWorldSpace = true;
            dynamicLineRenderer.alignment = LineAlignment.View;
            dynamicLineRenderer.textureMode = LineTextureMode.Stretch;
            dynamicLineRenderer.numCapVertices = 2;
            dynamicLineRenderer.numCornerVertices = 2;
            dynamicLineRenderer.positionCount = 2;
            dynamicLineRenderer.sharedMaterial = GetLineMaterial();
            dynamicLineRenderer.startColor = new Color(0.92f, 0.98f, 1f, 0.92f);
            dynamicLineRenderer.endColor = new Color(0.92f, 0.98f, 1f, 0.92f);
            if (shipSpriteRenderer != null)
            {
                dynamicLineRenderer.sortingLayerID = shipSpriteRenderer.sortingLayerID;
                dynamicLineRenderer.sortingOrder = shipSpriteRenderer.sortingOrder - 1;
            }
            else
            {
                dynamicLineRenderer.sortingOrder = 19;
            }

            var lineBridge = GetOrAddComponent<FishingLineBridge2D>(dynamicLineObject);
            lineBridge.Configure(shipObject.transform, hookObject.transform, 0.06f, -0.36f);

            var shipMovement = GetOrAddComponent<ShipMovementController>(shipObject);
            shipMovement.RefreshShipStats();
            GetOrAddComponent<FishingBoatFloatMotion2D>(shipObject);

            var hookMovement = GetOrAddComponent<HookMovementController>(hookObject);
            hookMovement.ConfigureShipTransform(shipObject.transform);
            hookMovement.RefreshHookStats();
            GetOrAddComponent<FishingDepthDarknessController>(root);
            var hookSway = hookObject.GetComponent<SpriteSwayMotion2D>();
            if (hookSway != null)
            {
                hookSway.enabled = false;
            }

            var dockedHookPosition = hookObject.transform.position;
            dockedHookPosition.y = hookMovement.GetDockedY(0.65f);
            hookObject.transform.position = dockedHookPosition;

            var backdropLayerA = FindSceneObject(scene, "BackdropFar");
            var backdropLayerB = FindSceneObject(scene, "BackdropVeil");
            if (backdropLayerA != null && backdropLayerB != null)
            {
                var backdropRendererA = backdropLayerA.GetComponent<SpriteRenderer>();
                var backdropRendererB = backdropLayerB.GetComponent<SpriteRenderer>();
                if (backdropRendererA != null
                    && backdropRendererB != null
                    && backdropRendererA.sprite != null
                    && backdropRendererA.sprite == backdropRendererB.sprite)
                {
                    // Keep the veil object for scene/layout checks, but remove duplicate visual stacking.
                    var veilColor = backdropRendererB.color;
                    veilColor.a = 0f;
                    backdropRendererB.color = veilColor;
                }
            }

            var waveAnimator = GetOrAddComponent<WaveAnimator>(root);
            waveAnimator.ConfigureLayers(
                backdropLayerA != null ? backdropLayerA.transform : null,
                backdropLayerB != null ? backdropLayerB.transform : null);

            var stateMachine = GetOrAddComponent<FishingActionStateMachine>(root);
            var spawner = GetOrAddComponent<FishSpawner>(root);
            spawner.SetFallbackDefinitions(CreateDefaultFishDefinitions());
            GetOrAddComponent<FishingAmbientFishSwimController>(root);

            var hookDropController = GetOrAddComponent<FishingHookCastDropController>(root);
            hookDropController.Configure(stateMachine, hookMovement, shipObject.transform);

            var hud = GetOrAddComponent<HudOverlayController>(root);
            hud.Configure(
                copecsText,
                dayText,
                distanceTierText,
                depthText,
                tensionText,
                conditionText,
                objectiveText,
                statusText,
                failureText,
                RuntimeServiceRegistry.Get<ObjectivesService>());
            hud.ConfigureDependencies(
                RuntimeServiceRegistry.Get<SaveManager>(),
                RuntimeServiceRegistry.Get<GameFlowManager>(),
                RuntimeServiceRegistry.Get<UserSettingsService>());

            var resolver = GetOrAddComponent<CatchResolver>(root);
            resolver.Configure(stateMachine, spawner, hookMovement, hud);
            var fishingTutorialController = GetOrAddComponent<FishingLoopTutorialController>(root);
            fishingTutorialTransitionPanel.transform.SetAsLastSibling();
            fishingTutorialController.ConfigureSkipButton(fishingTutorialSkipButton);
            fishingTutorialController.ConfigureSkipAllButton(fishingTutorialSkipAllButton);
            fishingTutorialController.ConfigureTutorialMessageBox(fishingTutorialMessagePanel, fishingTutorialMessageText);
            fishingTutorialController.ConfigureTutorialTransitionOverlay(
                fishingTutorialTransitionPanel,
                fishingTutorialTransitionFadeImage,
                fishingTutorialTransitionTitleText,
                fishingTutorialTransitionSubtitleText);

            var tuningConfig = Resources.Load<TuningConfigSO>("Config/SO_TuningConfig");
            var tuningConfigApplier = GetOrAddComponent<TuningConfigApplier>(root);
            tuningConfigApplier.Configure(
                tuningConfig,
                waveAnimator,
                shipMovement,
                hookMovement,
                spawner,
                RuntimeServiceRegistry.Get<SellSummaryCalculator>());
            tuningConfigApplier.ApplyNow();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var debugPanelController = GetOrAddComponent<DebugPanelController>(root);
            debugPanelController.Configure(waveAnimator, spawner, RuntimeServiceRegistry.Get<SaveManager>());
#endif

            GetOrAddComponent<FishingPauseBridge>(root);

            var pauseMenu = GetOrAddComponent<PauseMenuController>(root);
            pauseMenu.Configure(pauseRoot, pauseSettingsPanel);
            var pauseSettingsController = GetOrAddComponent<PauseSettingsPanelController>(root);
            pauseSettingsController.Configure(
                pauseMenu,
                reelInputButton,
                reducedMotionButton,
                highContrastButton,
                uiScaleDownButton,
                uiScaleUpButton,
                uiScaleValueText,
                backSettingsButton);
            resumeButton.onClick.AddListener(pauseMenu.OnResumePressed);
            harborButton.onClick.AddListener(pauseMenu.OnTownHarborPressed);
            settingsButton.onClick.AddListener(pauseMenu.OnSettingsPressed);
            exitButton.onClick.AddListener(pauseMenu.OnExitGamePressed);
            EnsurePerfSanityRunner(root, canvas.transform, "FishingPerfLabel");
        }

        private static GameObject GetOrCreateRuntimeRoot(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root != null && root.name == RuntimeRootName)
                {
                    return root;
                }
            }

            var runtimeRoot = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(runtimeRoot, scene);
            return runtimeRoot;
        }

        private static void EnsureEventSystem(Scene scene)
        {
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < eventSystems.Length; i++)
            {
                var eventSystem = eventSystems[i];
                if (eventSystem != null && eventSystem.gameObject.scene == scene)
                {
                    if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                    {
                        eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                    }

                    return;
                }
            }

            var eventSystemGo = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystemGo, scene);
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }

        private static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            GetOrAddComponent<UiAccessibilityCanvasRegistrant>(go);
            return canvas;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, worldPositionStays: false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            var image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            var text = go.AddComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = Mathf.Max(10, fontSize);
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(0.96f, 0.98f, 1f, 1f);
            text.text = value ?? string.Empty;
            return text;
        }

        private static GameObject CreateTopRightPanel(Transform parent, string name, Vector2 marginFromTopRight, Vector2 size, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, worldPositionStays: false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(-Mathf.Abs(marginFromTopRight.x), -Mathf.Abs(marginFromTopRight.y));
            var image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static GameObject CreateTopLeftPanel(Transform parent, string name, Vector2 marginFromTopLeft, Vector2 size, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, worldPositionStays: false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(Mathf.Abs(marginFromTopLeft.x), -Mathf.Abs(marginFromTopLeft.y));
            var image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Text CreateTopLeftText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 marginFromTopLeft,
            Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(Mathf.Abs(marginFromTopLeft.x), -Mathf.Abs(marginFromTopLeft.y));
            var text = go.AddComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = Mathf.Max(10, fontSize);
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(0.96f, 0.98f, 1f, 1f);
            text.text = value ?? string.Empty;
            return text;
        }

        private static TMP_Text CreateTopLeftTmpText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAlignmentOptions alignment,
            Vector2 marginFromTopLeft,
            Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(Mathf.Abs(marginFromTopLeft.x), -Mathf.Abs(marginFromTopLeft.y));
            var text = go.AddComponent<TextMeshProUGUI>();
            var fontAsset = GetDefaultTmpFontAsset();
            if (fontAsset != null)
            {
                text.font = fontAsset;
            }

            text.fontSize = Mathf.Max(10, fontSize);
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = new Color(0.96f, 0.98f, 1f, 1f);
            text.text = value ?? string.Empty;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.33f, 0.51f, 0.94f);
            var button = go.AddComponent<Button>();
            var colorBlock = button.colors;
            colorBlock.normalColor = new Color(0.18f, 0.33f, 0.51f, 0.94f);
            colorBlock.highlightedColor = new Color(0.26f, 0.45f, 0.66f, 0.98f);
            colorBlock.pressedColor = new Color(0.14f, 0.27f, 0.42f, 0.98f);
            colorBlock.selectedColor = new Color(0.24f, 0.44f, 0.66f, 0.98f);
            colorBlock.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.5f);
            button.colors = colorBlock;

            var labelText = CreateText(go.transform, "Label", label, 22, TextAnchor.MiddleCenter, Vector2.zero, size - new Vector2(12f, 8f));
            labelText.color = Color.white;
            labelText.raycastTarget = false;

            return button;
        }

        private static Image CreateImageElement(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateTopLeftButton(
            Transform parent,
            string name,
            string label,
            Vector2 marginFromTopLeft,
            Vector2 size)
        {
            var button = CreateButton(parent, name, label, Vector2.zero, size);
            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(Mathf.Abs(marginFromTopLeft.x), -Mathf.Abs(marginFromTopLeft.y));
            }

            return button;
        }

        private static Button CreateBottomRightButton(
            Transform parent,
            string name,
            string label,
            Vector2 marginFromBottomRight,
            Vector2 size)
        {
            var button = CreateButton(parent, name, label, Vector2.zero, size);
            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(
                    -Mathf.Abs(marginFromBottomRight.x),
                    Mathf.Abs(marginFromBottomRight.y));
            }

            return button;
        }

        private static Slider CreateTopLeftSlider(
            Transform parent,
            string name,
            Vector2 marginFromTopLeft,
            Vector2 size,
            float minValue,
            float maxValue,
            bool wholeNumbers = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(Mathf.Abs(marginFromTopLeft.x), -Mathf.Abs(marginFromTopLeft.y));

            var slider = go.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.wholeNumbers = wholeNumbers;
            slider.direction = Slider.Direction.LeftToRight;

            var background = new GameObject("Background");
            background.transform.SetParent(go.transform, worldPositionStays: false);
            var backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(0f, 12f);
            backgroundRect.anchoredPosition = Vector2.zero;
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.11f, 0.16f, 0.22f, 0.95f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, worldPositionStays: false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(10f, 6f);
            fillAreaRect.offsetMax = new Vector2(-10f, -6f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, worldPositionStays: false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.26f, 0.54f, 0.80f, 0.98f);

            var handleSlideArea = new GameObject("Handle Slide Area");
            handleSlideArea.transform.SetParent(go.transform, worldPositionStays: false);
            var handleSlideRect = handleSlideArea.AddComponent<RectTransform>();
            handleSlideRect.anchorMin = new Vector2(0f, 0f);
            handleSlideRect.anchorMax = new Vector2(1f, 1f);
            handleSlideRect.offsetMin = new Vector2(10f, 0f);
            handleSlideRect.offsetMax = new Vector2(-10f, 0f);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleSlideArea.transform, worldPositionStays: false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(22f, 22f);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.93f, 0.97f, 1f, 0.98f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            return slider;
        }

        private static Toggle CreateTopLeftToggle(
            Transform parent,
            string name,
            Vector2 marginFromTopLeft,
            Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(Mathf.Abs(marginFromTopLeft.x), -Mathf.Abs(marginFromTopLeft.y));

            var toggle = go.AddComponent<Toggle>();

            var background = new GameObject("Background");
            background.transform.SetParent(go.transform, worldPositionStays: false);
            var backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(24f, 24f);
            backgroundRect.anchoredPosition = Vector2.zero;
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.11f, 0.16f, 0.22f, 0.95f);

            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(background.transform, worldPositionStays: false);
            var checkmarkRect = checkmark.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(14f, 14f);
            checkmarkRect.anchoredPosition = Vector2.zero;
            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = new Color(0.26f, 0.56f, 0.84f, 0.98f);

            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;

            return toggle;
        }

        private static Font GetDefaultFont()
        {
            if (_defaultFont != null)
            {
                return _defaultFont;
            }

            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_defaultFont == null)
            {
                _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (_defaultFont == null)
            {
                _defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
            }

            return _defaultFont;
        }

        private static TMP_FontAsset GetDefaultTmpFontAsset()
        {
            if (_defaultTmpFontAsset != null)
            {
                return _defaultTmpFontAsset;
            }

            var configuredFont = TMP_Settings.GetFontAsset();
            if (configuredFont != null)
            {
                _defaultTmpFontAsset = configuredFont;
                return _defaultTmpFontAsset;
            }

            var fallbackFont = GetDefaultFont();
            if (fallbackFont == null)
            {
                return null;
            }

            _defaultTmpFontAsset = TMP_FontAsset.CreateFontAsset(fallbackFont);
            if (_defaultTmpFontAsset != null)
            {
                _defaultTmpFontAsset.hideFlags = HideFlags.HideAndDontSave;
            }

            return _defaultTmpFontAsset;
        }

        private static Material GetLineMaterial()
        {
            if (_lineMaterial != null)
            {
                return _lineMaterial;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                return null;
            }

            _lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return _lineMaterial;
        }

        private static TutorialSpriteLibrary GetTutorialSpriteLibrary()
        {
            if (_tutorialSpriteLibrary != null)
            {
                return _tutorialSpriteLibrary;
            }

            _tutorialSpriteLibrary = Resources.Load<TutorialSpriteLibrary>(TutorialSpriteLibraryResourcePath);
            return _tutorialSpriteLibrary;
        }

        private static Sprite ResolveTutorialHarborShipSprite()
        {
            var library = GetTutorialSpriteLibrary();
            if (library == null)
            {
                return null;
            }

            return library.HarborShipSprite != null
                ? library.HarborShipSprite
                : library.FishingShipSprite;
        }

        private static Sprite ResolveTutorialFishingShipSprite()
        {
            var library = GetTutorialSpriteLibrary();
            if (library == null)
            {
                return null;
            }

            return library.FishingShipSprite != null
                ? library.FishingShipSprite
                : library.HarborShipSprite;
        }

        private static Sprite ResolveTutorialHookSprite()
        {
            var library = GetTutorialSpriteLibrary();
            return library != null ? library.HookSprite : null;
        }

        private static Sprite ResolveTutorialFishSprite()
        {
            var library = GetTutorialSpriteLibrary();
            return library != null ? library.FishSprite : null;
        }

        private static void ApplyTutorialFishSprites(Scene scene, Sprite tutorialFishSprite)
        {
            if (!scene.IsValid() || tutorialFishSprite == null)
            {
                return;
            }

            var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer.gameObject.scene != scene)
                {
                    continue;
                }

                var objectName = renderer.gameObject.name;
                if (string.IsNullOrWhiteSpace(objectName)
                    || objectName.IndexOf("FishingFish", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                renderer.sprite = tutorialFishSprite;
            }
        }

        private static Sprite GetSolidSprite()
        {
            if (_solidSprite != null)
            {
                return _solidSprite;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            texture.SetPixels(new[]
            {
                Color.white, Color.white,
                Color.white, Color.white
            });
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            _solidSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit: 1f);
            return _solidSprite;
        }

        private static WorldInteractable ConfigureInteractable(GameObject target, InteractableType type, string highlightName)
        {
            if (target == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(highlightName))
            {
                var existingHighlight = target.transform.Find(highlightName);
                if (existingHighlight != null)
                {
                    existingHighlight.gameObject.SetActive(false);
                }
            }

            var interactable = GetOrAddComponent<WorldInteractable>(target);
            interactable.Configure(type, target.transform, null);
            return interactable;
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName) || !scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                if (root.name == objectName)
                {
                    return root;
                }

                var child = FindChildRecursive(root.transform, objectName);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static void HideSceneObjects(Scene scene, params string[] objectNames)
        {
            if (!scene.IsValid() || objectNames == null || objectNames.Length == 0)
            {
                return;
            }

            for (var i = 0; i < objectNames.Length; i++)
            {
                var objectName = objectNames[i];
                if (string.IsNullOrWhiteSpace(objectName))
                {
                    continue;
                }

                var target = FindSceneObject(scene, objectName);
                if (target != null && target.activeSelf)
                {
                    target.SetActive(false);
                }
            }
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (child.name == targetName)
                {
                    return child;
                }

                var nested = FindChildRecursive(child, targetName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void EnsurePerfSanityRunner(GameObject root, Transform uiParent, string labelName)
        {
            if (root == null || uiParent == null || string.IsNullOrWhiteSpace(labelName))
            {
                return;
            }

            Text label = null;
            var existing = FindChildRecursive(uiParent, labelName);
            if (existing != null)
            {
                label = existing.GetComponent<Text>();
            }

            if (label == null)
            {
                label = CreateTopLeftText(
                    uiParent,
                    labelName,
                    string.Empty,
                    13,
                    TextAnchor.UpperLeft,
                    new Vector2(16f, 14f),
                    new Vector2(360f, 24f));
                if (label != null)
                {
                    label.raycastTarget = false;
                }
            }

            var perfRunner = GetOrAddComponent<PerfSanityRunner>(root);
            perfRunner.Configure(
                label,
                sampleFrames: 240,
                emitWarningsOnBudgetFailure: true,
                hardwareTier: "minimum",
                emitSampleLogs: Application.isBatchMode,
                maxSampleLogsPerScene: 6);
        }

        private static List<DialogueLine> CreateHarborTutorialDialogueLines()
        {
            return new List<DialogueLine>
            {
                new DialogueLine
                {
                    text = "Welcome to harbor command. Use shops to buy gear, then open Shipyard to equip and set sail."
                },
                new DialogueLine
                {
                    text = "Hook and ship upgrades unlock depth and cargo capacity. Purchases go to Shipyard inventory."
                },
                new DialogueLine
                {
                    text = "Sell fish at market to refill copecs. Press interact to continue or pause/cancel to skip."
                }
            };
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            if (go == null)
            {
                return null;
            }

            var component = go.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return go.AddComponent<T>();
        }

        private static List<FishDefinition> CreateDefaultFishDefinitions()
        {
            return new List<FishDefinition>
            {
                new FishDefinition
                {
                    id = "fish_cod",
                    minDistanceTier = 1,
                    maxDistanceTier = 2,
                    minDepth = 22f,
                    maxDepth = 55f,
                    rarityWeight = 12,
                    baseValue = 18,
                    minBiteDelaySeconds = 0.9f,
                    maxBiteDelaySeconds = 2.1f,
                    fightStamina = 4.5f,
                    pullIntensity = 1f,
                    escapeSeconds = 7f,
                    minCatchWeightKg = 0.6f,
                    maxCatchWeightKg = 2.3f
                },
                new FishDefinition
                {
                    id = "fish_coastal_snapper",
                    minDistanceTier = 2,
                    maxDistanceTier = 3,
                    minDepth = 35f,
                    maxDepth = 90f,
                    rarityWeight = 8,
                    baseValue = 30,
                    minBiteDelaySeconds = 1f,
                    maxBiteDelaySeconds = 2.7f,
                    fightStamina = 6.2f,
                    pullIntensity = 1.3f,
                    escapeSeconds = 8.5f,
                    minCatchWeightKg = 1f,
                    maxCatchWeightKg = 3.4f
                },
                new FishDefinition
                {
                    id = "fish_heavy",
                    minDistanceTier = 3,
                    maxDistanceTier = 4,
                    minDepth = 60f,
                    maxDepth = 120f,
                    rarityWeight = 5,
                    baseValue = 52,
                    minBiteDelaySeconds = 1.2f,
                    maxBiteDelaySeconds = 3.2f,
                    fightStamina = 8.5f,
                    pullIntensity = 1.7f,
                    escapeSeconds = 9.5f,
                    minCatchWeightKg = 2.8f,
                    maxCatchWeightKg = 6.2f
                }
            };
        }
    }
}
