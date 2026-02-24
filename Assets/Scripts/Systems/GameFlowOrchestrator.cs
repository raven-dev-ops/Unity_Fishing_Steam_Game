using System;
using System.Collections;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RavenDevOps.Fishing.Core
{
    public sealed class GameFlowOrchestrator : MonoBehaviour
    {
        private enum IntroReplayExitRoute
        {
            None = 0,
            MainMenu = 1,
            MainMenuSettings = 2
        }

        private enum FishingTutorialExitRoute
        {
            None = 0,
            Harbor = 1,
            MainMenuProfile = 2
        }

        private static GameFlowOrchestrator _instance;

        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private InputContextRouter _inputContextRouter;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private SaveManager _saveManager;

        private Coroutine _activeLoadRoutine;
        private bool _eventsBound;
        private IntroReplayExitRoute _pendingIntroReplayExitRoute = IntroReplayExitRoute.None;
        private FishingTutorialExitRoute _pendingFishingTutorialExitRoute = FishingTutorialExitRoute.None;
        private bool _openSettingsAfterMainMenuLoad;
        private bool _openProfileAfterMainMenuLoad;

        public static GameFlowOrchestrator Instance => _instance;

        public void Initialize(
            GameFlowManager gameFlowManager,
            SceneLoader sceneLoader,
            InputContextRouter inputContextRouter,
            InputActionMapController inputMapController,
            SaveManager saveManager)
        {
            _gameFlowManager = gameFlowManager;
            _sceneLoader = sceneLoader;
            _inputContextRouter = inputContextRouter;
            _inputMapController = inputMapController;
            _saveManager = saveManager;

            BindEvents();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            RuntimeServiceRegistry.Register(this);
            ResolveDependencies();
            BindEvents();
        }

        private void Start()
        {
            if (_gameFlowManager == null || _gameFlowManager.CurrentState != GameFlowState.None)
            {
                return;
            }

            var activeScenePath = SceneManager.GetActiveScene().path;
            if (ScenePathConstants.TryGetStateForScenePath(activeScenePath, out var sceneBackedState))
            {
                _gameFlowManager.SetState(sceneBackedState);
                return;
            }

            if (string.Equals(activeScenePath, ScenePathConstants.Boot, StringComparison.Ordinal))
            {
                SetInputContext(InputContext.UI);
                return;
            }

            _gameFlowManager.SetState(GameFlowState.MainMenu);
        }

        private void OnDestroy()
        {
            UnbindEvents();

            if (_instance == this)
            {
                _instance = null;
            }

            RuntimeServiceRegistry.Unregister(this);
        }

        private void ResolveDependencies()
        {
            RuntimeServiceRegistry.Resolve(ref _gameFlowManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _sceneLoader, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputContextRouter, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);

            _gameFlowManager ??= GetComponent<GameFlowManager>();
            _sceneLoader ??= GetComponent<SceneLoader>();
            _inputContextRouter ??= GetComponent<InputContextRouter>();
            _inputMapController ??= GetComponent<InputActionMapController>();
            _saveManager ??= GetComponent<SaveManager>();
        }

        private void BindEvents()
        {
            if (_eventsBound || _gameFlowManager == null)
            {
                return;
            }

            _gameFlowManager.StateChanged += OnStateChanged;
            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound || _gameFlowManager == null)
            {
                return;
            }

            _gameFlowManager.StateChanged -= OnStateChanged;
            _eventsBound = false;
        }

        private void OnStateChanged(GameFlowState previous, GameFlowState next)
        {
            if (next == GameFlowState.Pause)
            {
                SetInputContext(InputContext.UI);
                return;
            }

            var context = GetContextForState(next);
            if (!ScenePathConstants.TryGetScenePathForState(next, out var targetScenePath))
            {
                if (ScenePathConstants.IsSceneBackedState(next))
                {
                    Debug.LogError($"GameFlowOrchestrator: Missing scene mapping for state {next}.");
                }
                else
                {
                    Debug.Log($"GameFlowOrchestrator: State {next} does not require scene load.");
                }

                SetInputContext(context);
                return;
            }

            if (SceneManager.GetActiveScene().path == targetScenePath)
            {
                SetInputContext(context);
                return;
            }

            if (_activeLoadRoutine != null)
            {
                StopCoroutine(_activeLoadRoutine);
                _activeLoadRoutine = null;
            }

            _activeLoadRoutine = StartCoroutine(LoadStateScene(targetScenePath, context));
        }

        private IEnumerator LoadStateScene(string scenePath, InputContext postLoadContext)
        {
            SetInputContext(InputContext.None);

            if (_sceneLoader != null)
            {
                yield return _sceneLoader.LoadSceneWithFade(scenePath);
            }
            else
            {
                var op = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
                while (op != null && !op.isDone)
                {
                    yield return null;
                }
            }

            SetInputContext(postLoadContext);
            if (_openSettingsAfterMainMenuLoad && _gameFlowManager != null && _gameFlowManager.CurrentState == GameFlowState.MainMenu)
            {
                yield return null;
                TryOpenMainMenuSettingsPanel();
            }

            if (_openProfileAfterMainMenuLoad && _gameFlowManager != null && _gameFlowManager.CurrentState == GameFlowState.MainMenu)
            {
                yield return null;
                TryOpenMainMenuProfilePanel();
            }

            _activeLoadRoutine = null;
        }

        private void SetInputContext(InputContext context)
        {
            _inputContextRouter?.SetContext(context);
            _inputMapController?.ApplyContext(context);
        }

        private static InputContext GetContextForState(GameFlowState state)
        {
            switch (state)
            {
                case GameFlowState.Harbor:
                    return InputContext.Harbor;
                case GameFlowState.Fishing:
                    return InputContext.Fishing;
                case GameFlowState.Cinematic:
                case GameFlowState.MainMenu:
                case GameFlowState.Pause:
                    return InputContext.UI;
                default:
                    Debug.LogWarning($"GameFlowOrchestrator: No explicit input-context mapping for state {state}.");
                    return InputContext.None;
            }
        }

        public void RequestStartGame()
        {
            _gameFlowManager?.SetState(GameFlowState.Harbor);
        }

        public void RequestOpenMainMenu()
        {
            _gameFlowManager?.SetState(GameFlowState.MainMenu);
        }

        public void RequestOpenCinematic()
        {
            _pendingIntroReplayExitRoute = IntroReplayExitRoute.MainMenu;
            _gameFlowManager?.SetState(GameFlowState.Cinematic);
        }

        public void RequestOpenIntroReplayFromSettings()
        {
            _pendingIntroReplayExitRoute = IntroReplayExitRoute.MainMenuSettings;
            _gameFlowManager?.SetState(GameFlowState.Cinematic);
        }

        public void RequestCompleteIntroFlow()
        {
            var exitRoute = _pendingIntroReplayExitRoute;
            _pendingIntroReplayExitRoute = IntroReplayExitRoute.None;

            switch (exitRoute)
            {
                case IntroReplayExitRoute.MainMenuSettings:
                    _openProfileAfterMainMenuLoad = false;
                    _openSettingsAfterMainMenuLoad = true;
                    _gameFlowManager?.SetState(GameFlowState.MainMenu);
                    return;
                case IntroReplayExitRoute.MainMenu:
                case IntroReplayExitRoute.None:
                default:
                    _gameFlowManager?.SetState(GameFlowState.MainMenu);
                    return;
            }
        }

        public void RequestOpenFishing()
        {
            _gameFlowManager?.SetState(GameFlowState.Fishing);
        }

        public void RequestOpenFishingTutorialReplayFromProfile()
        {
            _pendingFishingTutorialExitRoute = FishingTutorialExitRoute.MainMenuProfile;
            _gameFlowManager?.SetState(GameFlowState.Fishing);
        }

        public void RequestOpenFishingTutorialFromCinematicFirstTime()
        {
            _pendingFishingTutorialExitRoute = FishingTutorialExitRoute.Harbor;
            _gameFlowManager?.SetState(GameFlowState.Fishing);
        }

        public void RequestCompleteFishingTutorialFlow()
        {
            var exitRoute = _pendingFishingTutorialExitRoute;
            _pendingFishingTutorialExitRoute = FishingTutorialExitRoute.None;

            switch (exitRoute)
            {
                case FishingTutorialExitRoute.MainMenuProfile:
                    _openSettingsAfterMainMenuLoad = false;
                    _openProfileAfterMainMenuLoad = true;
                    _gameFlowManager?.SetState(GameFlowState.MainMenu);
                    return;
                case FishingTutorialExitRoute.Harbor:
                    _gameFlowManager?.SetState(GameFlowState.Harbor);
                    return;
                default:
                    return;
            }
        }

        public void RequestReturnToHarbor()
        {
            if (_gameFlowManager != null && _gameFlowManager.CurrentState == GameFlowState.Fishing)
            {
                _saveManager?.MarkTripCompleted();
            }

            _gameFlowManager?.SetState(GameFlowState.Harbor);
        }

        public void RequestReturnToHarborFromPause()
        {
            if (_gameFlowManager == null)
            {
                return;
            }

            if (_gameFlowManager.CurrentState == GameFlowState.Pause)
            {
                if (_gameFlowManager.PreviousPlayableState == GameFlowState.Fishing)
                {
                    _saveManager?.MarkTripCompleted();
                }

                _gameFlowManager.ReturnToHarborFromFishingPause();
                return;
            }

            if (_gameFlowManager.CurrentState == GameFlowState.Fishing)
            {
                _saveManager?.MarkTripCompleted();
            }

            _gameFlowManager.SetState(GameFlowState.Harbor);
        }

        public void RequestExitGame()
        {
            AppExitUtility.QuitGame();
        }

        private void TryOpenMainMenuProfilePanel()
        {
            if (!_openProfileAfterMainMenuLoad)
            {
                return;
            }

            var openedProfile = false;
            var candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                var candidateType = candidate.GetType();
                if (!string.Equals(candidateType.FullName, "RavenDevOps.Fishing.UI.MainMenuController", StringComparison.Ordinal))
                {
                    continue;
                }

                candidate.SendMessage("OpenProfile", SendMessageOptions.DontRequireReceiver);
                openedProfile = true;
                break;
            }

            if (!openedProfile)
            {
                Debug.LogWarning("GameFlowOrchestrator: Unable to auto-open Profile panel after fishing tutorial return.");
            }

            _openProfileAfterMainMenuLoad = false;
        }

        private void TryOpenMainMenuSettingsPanel()
        {
            if (!_openSettingsAfterMainMenuLoad)
            {
                return;
            }

            var openedSettings = false;
            var candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                var candidateType = candidate.GetType();
                if (!string.Equals(candidateType.FullName, "RavenDevOps.Fishing.UI.MainMenuController", StringComparison.Ordinal))
                {
                    continue;
                }

                candidate.SendMessage("OpenSettings", SendMessageOptions.DontRequireReceiver);
                openedSettings = true;
                break;
            }

            if (!openedSettings)
            {
                Debug.LogWarning("GameFlowOrchestrator: Unable to auto-open Settings panel after intro replay return.");
            }

            _openSettingsAfterMainMenuLoad = false;
        }
    }
}
