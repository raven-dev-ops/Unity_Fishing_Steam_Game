using RavenDevOps.Fishing.Audio;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
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
            if (Object.FindObjectOfType<GameFlowOrchestrator>() != null)
            {
                return;
            }

            var servicesGo = new GameObject(ServicesObjectName);
            Object.DontDestroyOnLoad(servicesGo);

            var gameFlow = servicesGo.AddComponent<GameFlowManager>();
            var sceneLoader = servicesGo.AddComponent<SceneLoader>();
            var inputRouter = servicesGo.AddComponent<InputContextRouter>();
            var inputMapController = servicesGo.AddComponent<InputActionMapController>();
            var saveManager = servicesGo.AddComponent<SaveManager>();
            servicesGo.AddComponent<AudioManager>();
            var orchestrator = servicesGo.AddComponent<GameFlowOrchestrator>();
            var inputDriver = servicesGo.AddComponent<KeyboardFlowInputDriver>();

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
    }
}
