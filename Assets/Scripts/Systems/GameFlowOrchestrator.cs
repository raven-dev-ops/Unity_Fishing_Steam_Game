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
        private static GameFlowOrchestrator _instance;

        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private InputContextRouter _inputContextRouter;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private SaveManager _saveManager;

        private Coroutine _activeLoadRoutine;
        private bool _eventsBound;

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
            _gameFlowManager?.SetState(GameFlowState.Cinematic);
        }

        public void RequestOpenFishing()
        {
            _gameFlowManager?.SetState(GameFlowState.Fishing);
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
            Application.Quit();
        }
    }
}
