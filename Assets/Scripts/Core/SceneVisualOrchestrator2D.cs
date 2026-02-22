using System;
using System.Collections;
using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    [DisallowMultipleComponent]
    public sealed class SceneVisualOrchestrator2D : MonoBehaviour
    {
        [Serializable]
        public struct StageDefinition
        {
            public string stageId;
            public Transform root;
            [Min(0f)] public float revealDelaySeconds;
            [Min(0.01f)] public float revealDurationSeconds;
            public bool hideAfterReveal;
            [Min(0f)] public float hideDelaySeconds;

            public StageDefinition(string stageId, Transform root, float revealDelaySeconds, float revealDurationSeconds)
            {
                this.stageId = stageId;
                this.root = root;
                this.revealDelaySeconds = Mathf.Max(0f, revealDelaySeconds);
                this.revealDurationSeconds = Mathf.Max(0.01f, revealDurationSeconds);
                hideAfterReveal = false;
                hideDelaySeconds = 0f;
            }

            public StageDefinition(
                string stageId,
                Transform root,
                float revealDelaySeconds,
                float revealDurationSeconds,
                bool hideAfterReveal,
                float hideDelaySeconds)
            {
                this.stageId = stageId;
                this.root = root;
                this.revealDelaySeconds = Mathf.Max(0f, revealDelaySeconds);
                this.revealDurationSeconds = Mathf.Max(0.01f, revealDurationSeconds);
                this.hideAfterReveal = hideAfterReveal;
                this.hideDelaySeconds = Mathf.Max(0f, hideDelaySeconds);
            }
        }

        private sealed class RuntimeStage
        {
            public StageDefinition definition;
        }

        [SerializeField] private StageDefinition[] _stages = Array.Empty<StageDefinition>();
        [SerializeField, Min(0f)] private float _initialDelaySeconds = 0.05f;
        [SerializeField] private bool _playOnEnable = true;
        [SerializeField] private bool _useUnscaledTime = true;
        [SerializeField] private bool _loopSequence;
        [SerializeField, Min(0f)] private float _loopDelaySeconds;

        private RuntimeStage[] _runtimeStages = Array.Empty<RuntimeStage>();
        private Coroutine _sequenceRoutine;

        public void ConfigureStages(
            StageDefinition[] stages,
            float initialDelaySeconds,
            bool loopSequence = false,
            float loopDelaySeconds = 0f)
        {
            _stages = stages ?? Array.Empty<StageDefinition>();
            _initialDelaySeconds = Mathf.Max(0f, initialDelaySeconds);
            _loopSequence = loopSequence;
            _loopDelaySeconds = Mathf.Max(0f, loopDelaySeconds);
            CacheRuntimeStages();
        }

        public void Play()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_sequenceRoutine != null)
            {
                StopCoroutine(_sequenceRoutine);
            }

            CacheRuntimeStages();
            _sequenceRoutine = StartCoroutine(PlaySequence());
        }

        public void ShowAllImmediate()
        {
            if (_sequenceRoutine != null)
            {
                StopCoroutine(_sequenceRoutine);
                _sequenceRoutine = null;
            }

            CacheRuntimeStages();
            for (var i = 0; i < _runtimeStages.Length; i++)
            {
                SetStageVisible(_runtimeStages[i], true);
            }
        }

        private void Awake()
        {
            CacheRuntimeStages();
        }

        private void OnEnable()
        {
            if (_playOnEnable && Application.isPlaying)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            if (_sequenceRoutine != null)
            {
                StopCoroutine(_sequenceRoutine);
                _sequenceRoutine = null;
            }
        }

        private void CacheRuntimeStages()
        {
            if (_stages == null || _stages.Length == 0)
            {
                _runtimeStages = Array.Empty<RuntimeStage>();
                return;
            }

            var stages = new RuntimeStage[_stages.Length];
            for (var stageIndex = 0; stageIndex < _stages.Length; stageIndex++)
            {
                var definition = _stages[stageIndex];
                var stageRoot = definition.root;
                if (stageRoot != null && !stageRoot.gameObject.activeSelf)
                {
                    stageRoot.gameObject.SetActive(true);
                }

                stages[stageIndex] = new RuntimeStage
                {
                    definition = definition
                };
            }

            _runtimeStages = stages;
        }

        private void ApplyHiddenState()
        {
            for (var i = 0; i < _runtimeStages.Length; i++)
            {
                SetStageVisible(_runtimeStages[i], false);
            }
        }

        private IEnumerator PlaySequence()
        {
            do
            {
                ApplyHiddenState();

                if (_initialDelaySeconds > 0f)
                {
                    if (_useUnscaledTime)
                    {
                        yield return new WaitForSecondsRealtime(_initialDelaySeconds);
                    }
                    else
                    {
                        yield return new WaitForSeconds(_initialDelaySeconds);
                    }
                }

                for (var stageIndex = 0; stageIndex < _runtimeStages.Length; stageIndex++)
                {
                    var stage = _runtimeStages[stageIndex];
                    if (stage == null)
                    {
                        continue;
                    }

                    if (stage.definition.revealDelaySeconds > 0f)
                    {
                        if (_useUnscaledTime)
                        {
                            yield return new WaitForSecondsRealtime(stage.definition.revealDelaySeconds);
                        }
                        else
                        {
                            yield return new WaitForSeconds(stage.definition.revealDelaySeconds);
                        }
                    }

                    SetStageVisible(stage, true);

                    var holdDuration = Mathf.Max(0f, stage.definition.revealDurationSeconds);
                    if (holdDuration > 0f)
                    {
                        if (_useUnscaledTime)
                        {
                            yield return new WaitForSecondsRealtime(holdDuration);
                        }
                        else
                        {
                            yield return new WaitForSeconds(holdDuration);
                        }
                    }

                    if (stage.definition.hideAfterReveal)
                    {
                        if (stage.definition.hideDelaySeconds > 0f)
                        {
                            if (_useUnscaledTime)
                            {
                                yield return new WaitForSecondsRealtime(stage.definition.hideDelaySeconds);
                            }
                            else
                            {
                                yield return new WaitForSeconds(stage.definition.hideDelaySeconds);
                            }
                        }

                        SetStageVisible(stage, false);
                    }
                }

                if (_loopSequence && _loopDelaySeconds > 0f)
                {
                    if (_useUnscaledTime)
                    {
                        yield return new WaitForSecondsRealtime(_loopDelaySeconds);
                    }
                    else
                    {
                        yield return new WaitForSeconds(_loopDelaySeconds);
                    }
                }
            }
            while (_loopSequence && isActiveAndEnabled);

            _sequenceRoutine = null;
        }

        private static void SetStageVisible(RuntimeStage stage, bool visible)
        {
            if (stage == null || stage.definition.root == null)
            {
                return;
            }

            stage.definition.root.gameObject.SetActive(visible);
        }
    }
}
