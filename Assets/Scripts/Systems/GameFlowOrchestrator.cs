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
            MainMenuSettings = 2,
            MainMenuProfile = 3
        }

        private enum FishingTutorialExitRoute
        {
            None = 0,
            Harbor = 1,
            MainMenuProfile = 2,
            MainMenu = 3
        }

        private static GameFlowOrchestrator _instance;

        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private InputContextRouter _inputContextRouter;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private SaveManager _saveManager;
        [NonSerialized] private IMainMenuNavigator _mainMenuNavigator;

        private Coroutine _activeLoadRoutine;
        private bool _eventsBound;
        private IntroReplayExitRoute _pendingIntroReplayExitRoute = IntroReplayExitRoute.None;
        private FishingTutorialExitRoute _pendingFishingTutorialExitRoute = FishingTutorialExitRoute.None;
        private bool _openSettingsAfterMainMenuLoad;
        private bool _openProfileAfterMainMenuLoad;
        private const float HarborDepartureTitleHoldSeconds = 2.15f;

        public static GameFlowOrchestrator Instance => _instance;

        public void Initialize(
            GameFlowManager gameFlowManager,
            SceneLoader sceneLoader,
            InputContextRouter inputContextRouter,
            InputActionMapController inputMapController,
            SaveManager saveManager,
            IMainMenuNavigator mainMenuNavigator = null)
        {
            _gameFlowManager = gameFlowManager;
            _sceneLoader = sceneLoader;
            _inputContextRouter = inputContextRouter;
            _inputMapController = inputMapController;
            _saveManager = saveManager;
            _mainMenuNavigator = mainMenuNavigator;

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
            RuntimeServiceRegistry.ResolveInterface(ref _mainMenuNavigator, this, warnIfMissing: false);

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

        public void RequestOpenIntroReplayFromProfile()
        {
            // Main menu no longer exposes a profile panel; return to the menu root route.
            _pendingIntroReplayExitRoute = IntroReplayExitRoute.MainMenu;
            _gameFlowManager?.SetState(GameFlowState.Cinematic);
        }

        public void RequestCompleteIntroFlow()
        {
            var exitRoute = _pendingIntroReplayExitRoute;
            _pendingIntroReplayExitRoute = IntroReplayExitRoute.None;

            switch (exitRoute)
            {
                case IntroReplayExitRoute.MainMenuSettings:
                    _openSettingsAfterMainMenuLoad = true;
                    _openProfileAfterMainMenuLoad = false;
                    _gameFlowManager?.SetState(GameFlowState.MainMenu);
                    return;
                case IntroReplayExitRoute.MainMenuProfile:
                    _openSettingsAfterMainMenuLoad = false;
                    _openProfileAfterMainMenuLoad = false;
                    _gameFlowManager?.SetState(GameFlowState.MainMenu);
                    return;
                case IntroReplayExitRoute.MainMenu:
                case IntroReplayExitRoute.None:
                default:
                    _openSettingsAfterMainMenuLoad = false;
                    _openProfileAfterMainMenuLoad = false;
                    _gameFlowManager?.SetState(GameFlowState.MainMenu);
                    return;
            }
        }

        public void RequestOpenFishing()
        {
            QueueFishingDepartureTitleCard();
            _gameFlowManager?.SetState(GameFlowState.Fishing);
        }

        public void RequestOpenFishingTutorialReplayFromProfile()
        {
            // Main menu no longer exposes a profile panel; return to the menu root route.
            _pendingFishingTutorialExitRoute = FishingTutorialExitRoute.MainMenu;
            _gameFlowManager?.SetState(GameFlowState.Fishing);
        }

        public void RequestOpenFishingTutorialReplayFromMainMenu()
        {
            _pendingFishingTutorialExitRoute = FishingTutorialExitRoute.MainMenu;
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
                    _openProfileAfterMainMenuLoad = false;
                    _gameFlowManager?.SetState(GameFlowState.MainMenu);
                    return;
                case FishingTutorialExitRoute.MainMenu:
                    _openSettingsAfterMainMenuLoad = false;
                    _openProfileAfterMainMenuLoad = false;
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

        private void QueueFishingDepartureTitleCard()
        {
            if (_sceneLoader == null)
            {
                return;
            }

            var tierCap = ResolveDepartureDistanceTierCap();
            _sceneLoader.QueueTransitionTitleCard($"Distance Tier {Mathf.Max(1, tierCap)}", HarborDepartureTitleHoldSeconds);
        }

        private int ResolveDepartureDistanceTierCap()
        {
            if (_saveManager == null || _saveManager.Current == null)
            {
                return 1;
            }

            var shipId = _saveManager.Current.equippedShipId;
            var parsedFromId = ParseTierFromId(shipId);
            return Mathf.Max(1, parsedFromId);
        }

        private static int ParseTierFromId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            var normalized = itemId.Trim().ToLowerInvariant();
            var lvIndex = normalized.IndexOf("lv", StringComparison.Ordinal);
            if (lvIndex < 0)
            {
                return 0;
            }

            var digitsStart = lvIndex + 2;
            var value = 0;
            var foundDigit = false;
            for (var i = digitsStart; i < normalized.Length; i++)
            {
                var ch = normalized[i];
                if (ch < '0' || ch > '9')
                {
                    if (foundDigit)
                    {
                        break;
                    }

                    continue;
                }

                foundDigit = true;
                value = (value * 10) + (ch - '0');
            }

            return foundDigit ? value : 0;
        }

        private void TryOpenMainMenuProfilePanel()
        {
            if (!_openProfileAfterMainMenuLoad)
            {
                return;
            }

            if (!TryResolveMainMenuNavigator() || !_mainMenuNavigator.TryOpenProfilePanel())
            {
                Debug.LogWarning("GameFlowOrchestrator: Unable to auto-open Profile panel after flow return.");
            }

            _openProfileAfterMainMenuLoad = false;
        }

        private void TryOpenMainMenuSettingsPanel()
        {
            if (!_openSettingsAfterMainMenuLoad)
            {
                return;
            }

            if (!TryResolveMainMenuNavigator() || !_mainMenuNavigator.TryOpenSettingsPanel())
            {
                Debug.LogWarning("GameFlowOrchestrator: Unable to auto-open Settings panel after intro replay return.");
            }

            _openSettingsAfterMainMenuLoad = false;
        }

        private bool TryResolveMainMenuNavigator()
        {
            if (_mainMenuNavigator != null)
            {
                return true;
            }

            return RuntimeServiceRegistry.TryGetInterface(out _mainMenuNavigator) && _mainMenuNavigator != null;
        }
    }
}

