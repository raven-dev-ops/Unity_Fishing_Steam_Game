using System.Collections;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RavenDevOps.Fishing.Core
{
    public sealed class GameFlowOrchestrator : MonoBehaviour
    {
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private InputContextRouter _inputContextRouter;
        [SerializeField] private InputActionMapController _inputMapController;
        [SerializeField] private SaveManager _saveManager;

        private Coroutine _activeLoadRoutine;
        private bool _eventsBound;

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
            ResolveDependencies();
            BindEvents();
        }

        private void Start()
        {
            if (_saveManager != null)
            {
                _saveManager.LoadOrCreate();
            }

            if (_gameFlowManager != null && _gameFlowManager.CurrentState == GameFlowState.None)
            {
                _gameFlowManager.SetState(GameFlowState.MainMenu);
            }
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void ResolveDependencies()
        {
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
            var targetScenePath = ScenePathConstants.GetScenePathForState(next);

            if (string.IsNullOrWhiteSpace(targetScenePath))
            {
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

        public void RequestOpenFishing()
        {
            _gameFlowManager?.SetState(GameFlowState.Fishing);
        }

        public void RequestReturnToHarbor()
        {
            _gameFlowManager?.SetState(GameFlowState.Harbor);
        }

        public void RequestExitGame()
        {
            Application.Quit();
        }
    }
}
