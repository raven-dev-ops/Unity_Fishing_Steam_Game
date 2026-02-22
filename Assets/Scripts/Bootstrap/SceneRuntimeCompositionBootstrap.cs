using System.Collections.Generic;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Fishing;
using RavenDevOps.Fishing.Harbor;
using RavenDevOps.Fishing.UI;
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
        private static bool _initialized;
        private static Font _defaultFont;
        private static Sprite _solidSprite;

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
            var selectionAura = CreateSelectionAura(canvas.transform, "BootSelectionAura", new Vector2(248f, 64f));
            var continueButton = CreateButton(canvas.transform, "ContinueButton", "Continue", new Vector2(0f, -114f), new Vector2(220f, 56f));
            continueButton.onClick.AddListener(() => RuntimeServiceRegistry.Get<GameFlowManager>()?.SetState(GameFlowState.Cinematic));
            AttachSelectionAura(canvas.gameObject, selectionAura, new Vector2(16f, 8f), 24f);

            var controller = GetOrAddComponent<BootSceneFlowController>(root);
            controller.Configure(status);
        }

        private static void ComposeCinematic(Scene scene)
        {
            var root = GetOrCreateRuntimeRoot(scene);
            if (root.transform.Find("CinematicCanvas") != null)
            {
                return;
            }

            EnsureEventSystem(scene);
            var canvas = CreateCanvas(root.transform, "CinematicCanvas", 250);
            CreatePanel(canvas.transform, "CinematicPanel", new Vector2(0f, -180f), new Vector2(940f, 230f), new Color(0.04f, 0.07f, 0.16f, 0.68f));
            CreateText(canvas.transform, "CinematicTitle", "Opening Voyage", 34, TextAnchor.MiddleCenter, new Vector2(0f, -142f), new Vector2(840f, 68f));
            var status = CreateText(canvas.transform, "CinematicStatus", "Cinematic: Enter/Esc to skip.", 22, TextAnchor.MiddleCenter, new Vector2(0f, -196f), new Vector2(840f, 56f));
            var selectionAura = CreateSelectionAura(canvas.transform, "CinematicSelectionAura", new Vector2(176f, 56f));
            var skipButton = CreateButton(canvas.transform, "SkipCinematicButton", "Skip", new Vector2(360f, -196f), new Vector2(150f, 48f));
            skipButton.onClick.AddListener(() => RuntimeServiceRegistry.Get<GameFlowManager>()?.SetState(GameFlowState.MainMenu));
            AttachSelectionAura(canvas.gameObject, selectionAura, new Vector2(14f, 6f), 24f);

            var controller = GetOrAddComponent<CinematicSceneFlowController>(root);
            controller.Configure(status);
        }

        private static void ComposeMainMenu(Scene scene)
        {
            var root = GetOrCreateRuntimeRoot(scene);
            if (root.transform.Find("MainMenuCanvas") != null)
            {
                return;
            }

            EnsureEventSystem(scene);
            var canvas = CreateCanvas(root.transform, "MainMenuCanvas", 260);
            CreatePanel(canvas.transform, "MainMenuPanel", new Vector2(0f, 0f), new Vector2(620f, 520f), new Color(0.05f, 0.11f, 0.18f, 0.74f));
            CreateText(canvas.transform, "MainMenuTitle", "Harbor Command", 38, TextAnchor.MiddleCenter, new Vector2(0f, 186f), new Vector2(560f, 88f));
            var selectionAura = CreateSelectionAura(canvas.transform, "MainMenuSelectionAura", new Vector2(336f, 72f));

            var startButton = CreateButton(canvas.transform, "StartButton", "Start Voyage", new Vector2(0f, 88f), new Vector2(300f, 56f));
            var profileButton = CreateButton(canvas.transform, "ProfileButton", "Profile", new Vector2(0f, 20f), new Vector2(300f, 56f));
            var settingsButton = CreateButton(canvas.transform, "SettingsButton", "Settings", new Vector2(0f, -48f), new Vector2(300f, 56f));
            var exitButton = CreateButton(canvas.transform, "ExitButton", "Exit", new Vector2(0f, -116f), new Vector2(300f, 56f));

            var profilePanel = CreatePanel(canvas.transform, "ProfilePanel", new Vector2(0f, -250f), new Vector2(560f, 132f), new Color(0.08f, 0.14f, 0.22f, 0.86f));
            CreateText(profilePanel.transform, "ProfilePanelText", "Profile panel is active.\nUse Enter to start or Esc to close.", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(520f, 94f));
            profilePanel.SetActive(false);

            var settingsPanel = CreatePanel(canvas.transform, "SettingsPanel", new Vector2(0f, -250f), new Vector2(560f, 132f), new Color(0.10f, 0.16f, 0.23f, 0.86f));
            CreateText(settingsPanel.transform, "SettingsPanelText", "Settings panel is active.\nUse Esc to return.", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(520f, 94f));
            settingsPanel.SetActive(false);

            var exitPanel = CreatePanel(canvas.transform, "ExitPanel", new Vector2(0f, -244f), new Vector2(560f, 176f), new Color(0.11f, 0.08f, 0.08f, 0.90f));
            CreateText(exitPanel.transform, "ExitPanelText", "Exit game now?\nYour progress is saved automatically.", 20, TextAnchor.MiddleCenter, new Vector2(0f, 40f), new Vector2(520f, 84f));
            var exitConfirmButton = CreateButton(exitPanel.transform, "ExitConfirmButton", "Exit Game", new Vector2(-112f, -44f), new Vector2(200f, 50f));
            var exitCancelButton = CreateButton(exitPanel.transform, "ExitCancelButton", "Back", new Vector2(112f, -44f), new Vector2(200f, 50f));
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
                exitCancelButton.gameObject);

            startButton.onClick.AddListener(controller.StartGame);
            profileButton.onClick.AddListener(controller.OpenProfile);
            settingsButton.onClick.AddListener(controller.OpenSettings);
            exitButton.onClick.AddListener(controller.OpenExitPanel);
            exitConfirmButton.onClick.AddListener(controller.ConfirmExit);
            exitCancelButton.onClick.AddListener(controller.CancelExit);
            AttachSelectionAura(canvas.gameObject, selectionAura, new Vector2(20f, 10f), 26f);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(startButton.gameObject);
            }
        }

        private static void ComposeHarbor(Scene scene)
        {
            var root = GetOrCreateRuntimeRoot(scene);
            if (root.transform.Find("HarborCanvas") != null)
            {
                return;
            }

            var canvas = CreateCanvas(root.transform, "HarborCanvas", 240);
            var infoPanel = CreateTopRightPanel(canvas.transform, "HarborInfoPanel", new Vector2(20f, 20f), new Vector2(760f, 130f), new Color(0.04f, 0.10f, 0.17f, 0.76f));
            var status = CreateTopLeftText(infoPanel.transform, "HarborStatus", "Harbor ready.", 18, TextAnchor.UpperLeft, new Vector2(18f, 58f), new Vector2(724f, 58f));
            CreateTopLeftText(
                infoPanel.transform,
                "HarborControls",
                "Harbor: Move with arrows/WASD, Enter to interact, Esc to pause.",
                18,
                TextAnchor.UpperLeft,
                new Vector2(18f, 16f),
                new Vector2(724f, 40f));

            var player = FindSceneObject(scene, "HarborShipMain");
            if (player == null)
            {
                player = new GameObject("HarborPlayer");
                SceneManager.MoveGameObjectToScene(player, scene);
                player.transform.position = new Vector3(0f, 0.6f, 0f);
                var playerRenderer = player.AddComponent<SpriteRenderer>();
                playerRenderer.sprite = GetSolidSprite();
                playerRenderer.color = new Color(0.94f, 0.97f, 1f, 0.95f);
                player.transform.localScale = new Vector3(1.4f, 1.0f, 1f);
                playerRenderer.sortingOrder = 40;
            }

            GetOrAddComponent<HarborPlayerController>(player);

            var aura = new GameObject("HarborWorldAura");
            SceneManager.MoveGameObjectToScene(aura, scene);
            var auraRenderer = aura.AddComponent<SpriteRenderer>();
            auraRenderer.sprite = GetSolidSprite();
            auraRenderer.color = new Color(1f, 0.87f, 0.22f, 0.46f);
            auraRenderer.sortingOrder = 60;
            aura.transform.localScale = new Vector3(1.95f, 1.24f, 1f);
            aura.SetActive(false);

            var hookObject = FindSceneObject(scene, "HarborMarketHook1");
            var boatObject = FindSceneObject(scene, "HarborShipSide");
            var fishObject = FindSceneObject(scene, "HarborMarketFish1");
            var sailObject = FindSceneObject(scene, "DockPlank_0");
            if (sailObject == null)
            {
                sailObject = FindSceneObject(scene, "HarborShipMain");
            }

            var interactables = new List<WorldInteractable>
            {
                ConfigureInteractable(hookObject, InteractableType.HookShop, "HookShop_Highlight"),
                ConfigureInteractable(boatObject, InteractableType.BoatShop, "BoatShop_Highlight"),
                ConfigureInteractable(fishObject, InteractableType.FishShop, "FishShop_Highlight"),
                ConfigureInteractable(sailObject, InteractableType.Sail, "Sail_Highlight")
            };
            interactables.RemoveAll(x => x == null);

            var interactionController = GetOrAddComponent<HarborInteractionController>(root);
            interactionController.Configure(player.transform, aura.transform, interactables, null);

            var hookShop = GetOrAddComponent<HookShopController>(root);
            hookShop.ConfigureItems(new List<ShopItem>
            {
                new ShopItem { id = "hook_lv1", price = 0, valueTier = 1 },
                new ShopItem { id = "hook_lv2", price = 120, valueTier = 2 },
                new ShopItem { id = "hook_lv3", price = 320, valueTier = 3 }
            });

            var boatShop = GetOrAddComponent<BoatShopController>(root);
            boatShop.ConfigureItems(new List<ShopItem>
            {
                new ShopItem { id = "ship_lv1", price = 0, valueTier = 1 },
                new ShopItem { id = "ship_lv2", price = 180, valueTier = 2 },
                new ShopItem { id = "ship_lv3", price = 420, valueTier = 3 }
            });

            var fishShop = GetOrAddComponent<FishShopController>(root);
            var router = GetOrAddComponent<HarborSceneInteractionRouter>(root);
            router.Configure(interactables, hookShop, boatShop, fishShop, status);
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
            var infoPanel = CreateTopRightPanel(canvas.transform, "FishingInfoPanel", new Vector2(20f, 20f), new Vector2(880f, 252f), new Color(0.04f, 0.10f, 0.17f, 0.78f));
            var telemetryText = CreateTopLeftText(infoPanel.transform, "FishingTelemetryText", "Distance Tier: 1 | Depth: 0.0", 18, TextAnchor.UpperLeft, new Vector2(18f, 14f), new Vector2(844f, 32f));
            var tensionText = CreateTopLeftText(infoPanel.transform, "FishingTensionText", "Tension: None (0.00)", 18, TextAnchor.UpperLeft, new Vector2(18f, 46f), new Vector2(844f, 32f));
            var conditionText = CreateTopLeftText(infoPanel.transform, "FishingConditionText", string.Empty, 18, TextAnchor.UpperLeft, new Vector2(18f, 78f), new Vector2(844f, 34f));
            var statusText = CreateTopLeftText(infoPanel.transform, "FishingStatusText", "Press Space to cast and drop hook.", 18, TextAnchor.UpperLeft, new Vector2(18f, 112f), new Vector2(844f, 38f));
            var failureText = CreateTopLeftText(infoPanel.transform, "FishingFailureText", string.Empty, 18, TextAnchor.UpperLeft, new Vector2(18f, 150f), new Vector2(844f, 38f));
            CreateTopLeftText(
                infoPanel.transform,
                "FishingControls",
                "Fishing: Left/Right move ship, Space casts and auto-drops hook, Up/Down adjusts depth, Esc pause, H return to harbor from pause.",
                16,
                TextAnchor.UpperLeft,
                new Vector2(18f, 192f),
                new Vector2(844f, 52f));

            var pauseRoot = CreatePanel(canvas.transform, "PausePanel", Vector2.zero, new Vector2(440f, 300f), new Color(0.04f, 0.09f, 0.15f, 0.84f));
            CreateText(pauseRoot.transform, "PauseTitle", "Paused", 30, TextAnchor.MiddleCenter, new Vector2(0f, 108f), new Vector2(320f, 62f));
            var pauseSettingsPanel = CreatePanel(pauseRoot.transform, "PauseSettingsPanel", new Vector2(0f, -102f), new Vector2(360f, 82f), new Color(0.1f, 0.14f, 0.2f, 0.88f));
            CreateText(pauseSettingsPanel.transform, "PauseSettingsText", "Settings preview. Press Esc to resume.", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(320f, 64f));
            pauseSettingsPanel.SetActive(false);
            var pauseSelectionAura = CreateSelectionAura(pauseRoot.transform, "PauseSelectionAura", new Vector2(284f, 56f));

            var resumeButton = CreateButton(pauseRoot.transform, "ResumeButton", "Resume", new Vector2(0f, 40f), new Vector2(250f, 48f));
            var harborButton = CreateButton(pauseRoot.transform, "HarborButton", "Return Harbor", new Vector2(0f, -16f), new Vector2(250f, 48f));
            var settingsButton = CreateButton(pauseRoot.transform, "PauseSettingsButton", "Settings", new Vector2(0f, -72f), new Vector2(250f, 48f));
            var exitButton = CreateButton(pauseRoot.transform, "PauseExitButton", "Exit", new Vector2(0f, -128f), new Vector2(250f, 48f));
            pauseRoot.SetActive(false);

            var shipObject = FindSceneObject(scene, "FishingShip");
            if (shipObject == null)
            {
                shipObject = new GameObject("FishingShip");
                SceneManager.MoveGameObjectToScene(shipObject, scene);
                var renderer = shipObject.AddComponent<SpriteRenderer>();
                renderer.sprite = GetSolidSprite();
                renderer.color = new Color(0.95f, 0.99f, 1f, 0.95f);
                renderer.sortingOrder = 20;
                shipObject.transform.localScale = new Vector3(1.5f, 0.9f, 1f);
                shipObject.transform.position = new Vector3(0f, 2.4f, 0f);
            }

            var hookObject = FindSceneObject(scene, "FishingHook");
            if (hookObject == null)
            {
                hookObject = new GameObject("FishingHook");
                SceneManager.MoveGameObjectToScene(hookObject, scene);
                var renderer = hookObject.AddComponent<SpriteRenderer>();
                renderer.sprite = GetSolidSprite();
                renderer.color = new Color(0.88f, 0.95f, 1f, 0.95f);
                renderer.sortingOrder = 20;
                hookObject.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                hookObject.transform.position = new Vector3(0f, -1f, 0f);
            }

            var shipMovement = GetOrAddComponent<ShipMovementController>(shipObject);
            shipMovement.RefreshShipStats();
            GetOrAddComponent<FishingBoatFloatMotion2D>(shipObject);

            var hookMovement = GetOrAddComponent<HookMovementController>(hookObject);
            hookMovement.ConfigureShipTransform(shipObject.transform);
            hookMovement.RefreshHookStats();

            var dockedHookPosition = hookObject.transform.position;
            dockedHookPosition.y = Mathf.Clamp(
                shipObject.transform.position.y - 0.65f,
                -Mathf.Abs(hookMovement.MaxDepth),
                shipObject.transform.position.y);
            hookObject.transform.position = dockedHookPosition;

            var stateMachine = GetOrAddComponent<FishingActionStateMachine>(root);
            var spawner = GetOrAddComponent<FishSpawner>(root);
            spawner.SetFallbackDefinitions(CreateDefaultFishDefinitions());
            GetOrAddComponent<FishingAmbientFishSwimController>(root);

            var hookDropController = GetOrAddComponent<FishingHookCastDropController>(root);
            hookDropController.Configure(stateMachine, hookMovement, shipObject.transform);

            var hud = GetOrAddComponent<SimpleFishingHudOverlay>(root);
            hud.Configure(telemetryText, tensionText, statusText, failureText, conditionText);

            var resolver = GetOrAddComponent<CatchResolver>(root);
            resolver.Configure(stateMachine, spawner, hookMovement, hud);

            GetOrAddComponent<FishingPauseBridge>(root);

            var pauseMenu = GetOrAddComponent<PauseMenuController>(root);
            pauseMenu.Configure(pauseRoot, pauseSettingsPanel);
            resumeButton.onClick.AddListener(pauseMenu.OnResumePressed);
            harborButton.onClick.AddListener(pauseMenu.OnTownHarborPressed);
            settingsButton.onClick.AddListener(pauseMenu.OnSettingsPressed);
            exitButton.onClick.AddListener(pauseMenu.OnExitGamePressed);
            AttachSelectionAura(canvas.gameObject, pauseSelectionAura, new Vector2(16f, 8f), 24f);
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

        private static RectTransform CreateSelectionAura(Transform parent, string name, Vector2 size)
        {
            var aura = new GameObject(name);
            aura.transform.SetParent(parent, worldPositionStays: false);

            var rect = aura.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var image = aura.AddComponent<Image>();
            image.color = new Color(1f, 0.84f, 0.26f, 0.14f);
            image.raycastTarget = false;

            var outline = aura.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.90f, 0.45f, 0.90f);
            outline.effectDistance = new Vector2(3f, 3f);
            outline.useGraphicAlpha = true;

            aura.SetActive(false);
            return rect;
        }

        private static void AttachSelectionAura(GameObject host, RectTransform auraTransform, Vector2 padding, float followSpeed)
        {
            if (host == null || auraTransform == null)
            {
                return;
            }

            var follower = GetOrAddComponent<SelectionAuraFollower>(host);
            follower.Configure(auraTransform, padding, followSpeed);
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

            var highlight = target.transform.Find(highlightName);
            if (highlight == null)
            {
                var highlightGo = new GameObject(highlightName);
                highlightGo.transform.SetParent(target.transform, worldPositionStays: false);
                highlightGo.transform.localPosition = Vector3.zero;
                highlightGo.transform.localScale = new Vector3(1.40f, 1.40f, 1f);
                var renderer = highlightGo.AddComponent<SpriteRenderer>();
                renderer.sprite = ResolveHighlightSprite(target);
                renderer.color = new Color(1f, 0.88f, 0.26f, 0.70f);
                var targetRenderer = target.GetComponent<SpriteRenderer>();
                renderer.sortingOrder = targetRenderer != null ? targetRenderer.sortingOrder + 1 : 90;
                highlight = highlightGo.transform;
            }

            highlight.gameObject.SetActive(false);
            var interactable = GetOrAddComponent<WorldInteractable>(target);
            interactable.Configure(type, target.transform, highlight.gameObject);
            return interactable;
        }

        private static Sprite ResolveHighlightSprite(GameObject target)
        {
            if (target == null)
            {
                return GetSolidSprite();
            }

            var targetRenderer = target.GetComponent<SpriteRenderer>();
            if (targetRenderer != null && targetRenderer.sprite != null)
            {
                return targetRenderer.sprite;
            }

            return GetSolidSprite();
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
                    minDepth = 0.8f,
                    maxDepth = 7.5f,
                    rarityWeight = 9,
                    baseValue = 18,
                    minBiteDelaySeconds = 1f,
                    maxBiteDelaySeconds = 2.6f,
                    fightStamina = 5.5f,
                    pullIntensity = 1f,
                    escapeSeconds = 5f,
                    minCatchWeightKg = 0.6f,
                    maxCatchWeightKg = 2.3f
                },
                new FishDefinition
                {
                    id = "fish_coastal_snapper",
                    minDistanceTier = 1,
                    maxDistanceTier = 3,
                    minDepth = 1.2f,
                    maxDepth = 9f,
                    rarityWeight = 6,
                    baseValue = 34,
                    minBiteDelaySeconds = 1.4f,
                    maxBiteDelaySeconds = 3.2f,
                    fightStamina = 7f,
                    pullIntensity = 1.2f,
                    escapeSeconds = 5.7f,
                    minCatchWeightKg = 1f,
                    maxCatchWeightKg = 3.4f
                },
                new FishDefinition
                {
                    id = "fish_heavy",
                    minDistanceTier = 2,
                    maxDistanceTier = 3,
                    minDepth = 2.8f,
                    maxDepth = 11.5f,
                    rarityWeight = 3,
                    baseValue = 68,
                    minBiteDelaySeconds = 2f,
                    maxBiteDelaySeconds = 4f,
                    fightStamina = 9f,
                    pullIntensity = 1.45f,
                    escapeSeconds = 6.4f,
                    minCatchWeightKg = 2.8f,
                    maxCatchWeightKg = 6.2f
                }
            };
        }
    }
}
