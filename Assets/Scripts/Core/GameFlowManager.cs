using System;
using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    public sealed class GameFlowManager : MonoBehaviour
    {
        private static GameFlowManager _instance;

        [SerializeField] private GameFlowState _currentState = GameFlowState.None;
        [SerializeField] private GameFlowState _previousPlayableState = GameFlowState.None;
        [SerializeField] private bool _isPaused;

        public static GameFlowManager Instance => _instance;
        public GameFlowState CurrentState => _currentState;
        public bool IsPaused => _isPaused;

        public event Action<GameFlowState, GameFlowState> StateChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetState(GameFlowState nextState)
        {
            if (nextState == _currentState)
            {
                return;
            }

            var previous = _currentState;
            _currentState = nextState;

            if (nextState == GameFlowState.Harbor || nextState == GameFlowState.Fishing)
            {
                _previousPlayableState = nextState;
                _isPaused = false;
            }

            StateChanged?.Invoke(previous, nextState);
        }

        public void TogglePause()
        {
            if (_currentState != GameFlowState.Harbor && _currentState != GameFlowState.Fishing && _currentState != GameFlowState.Pause)
            {
                return;
            }

            if (_isPaused)
            {
                ResumeFromPause();
                return;
            }

            _isPaused = true;
            SetState(GameFlowState.Pause);
            Time.timeScale = 0f;
        }

        public void ResumeFromPause()
        {
            if (!_isPaused)
            {
                return;
            }

            _isPaused = false;
            Time.timeScale = 1f;
            SetState(_previousPlayableState == GameFlowState.None ? GameFlowState.Harbor : _previousPlayableState);
        }

        public void ReturnToHarborFromFishingPause()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            SetState(GameFlowState.Harbor);
        }

        private void OnApplicationQuit()
        {
            Time.timeScale = 1f;
        }
    }
}
