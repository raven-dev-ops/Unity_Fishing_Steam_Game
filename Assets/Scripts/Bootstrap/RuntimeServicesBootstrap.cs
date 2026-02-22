using RavenDevOps.Fishing.Audio;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.Steam;
using RavenDevOps.Fishing.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Core
{
    public static class RuntimeServicesBootstrap
    {
        private const string ServicesObjectName = "__GameServices";
        private const string FadeCanvasObjectName = "__GlobalFadeCanvas";
        private const string InputActionsResourcePath = "InputActions_Gameplay";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBootstrap()
        {
            var servicesGo = GameObject.Find(ServicesObjectName);
            if (servicesGo == null && GameFlowOrchestrator.Instance != null)
            {
                servicesGo = GameFlowOrchestrator.Instance.gameObject;
                servicesGo.name = ServicesObjectName;
            }

            if (servicesGo == null)
            {
                servicesGo = new GameObject(ServicesObjectName);
            }

            Object.DontDestroyOnLoad(servicesGo);

            var gameFlow = GetOrAddService<GameFlowManager>(servicesGo);
            var sceneLoader = GetOrAddService<SceneLoader>(servicesGo);
            var inputRouter = GetOrAddService<InputContextRouter>(servicesGo);
            var inputMapController = GetOrAddService<InputActionMapController>(servicesGo);
            var inputRebindingService = GetOrAddService<InputRebindingService>(servicesGo);
            GetOrAddService<UserSettingsService>(servicesGo);
            GetOrAddService<FallbackCameraService>(servicesGo);
            var saveManager = GetOrAddService<SaveManager>(servicesGo);
            GetOrAddService<AddressablesPilotCatalogLoader>(servicesGo);
            GetOrAddService<CatalogService>(servicesGo);
            GetOrAddService<MetaLoopRuntimeService>(servicesGo);
            GetOrAddService<SellSummaryCalculator>(servicesGo);
            GetOrAddService<ObjectivesService>(servicesGo);
            GetOrAddService<GlobalUiAccessibilityService>(servicesGo);
            GetOrAddService<PhotoModeRuntimeService>(servicesGo);
            GetOrAddService<AudioManager>(servicesGo);
            GetOrAddService<SceneMusicController>(servicesGo);
            GetOrAddService<SfxTriggerRouter>(servicesGo);
            GetOrAddService<CrashDiagnosticsService>(servicesGo);
            GetOrAddService<SteamBootstrap>(servicesGo);
            GetOrAddService<SteamStatsService>(servicesGo);
            GetOrAddService<SteamCloudSyncService>(servicesGo);
            GetOrAddService<SteamRichPresenceService>(servicesGo);
            var orchestrator = GetOrAddService<GameFlowOrchestrator>(servicesGo);
            var inputDriver = GetOrAddComponent<KeyboardFlowInputDriver>(servicesGo);
            GetOrAddService<Logging.StructuredLogService>(servicesGo);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GetOrAddComponent<Logging.DevelopmentLogConsole>(servicesGo);
#endif

            var inputActions = Resources.Load<InputActionAsset>(InputActionsResourcePath);
            if (inputActions == null)
            {
                var inputJson = Resources.Load<TextAsset>(InputActionsResourcePath);
                if (inputJson != null)
                {
                    inputActions = InputActionAsset.FromJson(inputJson.text);
                }
            }

            inputMapController.SetInputActions(inputActions);
            inputMapController.Initialize(inputRouter);
            inputRebindingService.SetInputActions(inputActions);

            var fadeCanvas = CreateGlobalFadeCanvas();
            sceneLoader.SetFadeCanvas(fadeCanvas);

            orchestrator.Initialize(gameFlow, sceneLoader, inputRouter, inputMapController, saveManager);
            inputDriver.Initialize(gameFlow, orchestrator);
        }

        private static CanvasGroup CreateGlobalFadeCanvas()
        {
            var existingGo = GameObject.Find(FadeCanvasObjectName);
            if (existingGo != null && existingGo.TryGetComponent<CanvasGroup>(out var existing))
            {
                return existing;
            }

            var canvasGo = new GameObject(FadeCanvasObjectName);
            Object.DontDestroyOnLoad(canvasGo);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var image = canvasGo.AddComponent<Image>();
            image.color = Color.black;

            var fade = canvasGo.AddComponent<CanvasGroup>();
            fade.alpha = 0f;
            fade.blocksRaycasts = false;
            fade.interactable = false;

            var rect = canvas.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return fade;
        }

        private static T GetOrAddService<T>(GameObject root) where T : Component
        {
            var component = GetOrAddComponent<T>(root);
            RuntimeServiceRegistry.Register(component);
            return component;
        }

        private static T GetOrAddComponent<T>(GameObject root) where T : Component
        {
            if (root == null)
            {
                return null;
            }

            var component = root.GetComponent<T>();
            if (component == null)
            {
                component = root.AddComponent<T>();
            }

            return component;
        }
    }
}
