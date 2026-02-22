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

            public StageDefinition(string stageId, Transform root, float revealDelaySeconds, float revealDurationSeconds)
            {
                this.stageId = stageId;
                this.root = root;
                this.revealDelaySeconds = Mathf.Max(0f, revealDelaySeconds);
                this.revealDurationSeconds = Mathf.Max(0.01f, revealDurationSeconds);
            }
        }

        private sealed class RuntimeStage
        {
            public StageDefinition definition;
            public SpriteRenderer[] renderers;
            public Color[] baseColors;
        }

        [SerializeField] private StageDefinition[] _stages = Array.Empty<StageDefinition>();
        [SerializeField, Min(0f)] private float _initialDelaySeconds = 0.05f;
        [SerializeField] private bool _playOnEnable = true;

        private RuntimeStage[] _runtimeStages = Array.Empty<RuntimeStage>();
        private Coroutine _sequenceRoutine;

        public void ConfigureStages(StageDefinition[] stages, float initialDelaySeconds)
        {
            _stages = stages ?? Array.Empty<StageDefinition>();
            _initialDelaySeconds = Mathf.Max(0f, initialDelaySeconds);
            CacheRuntimeStages();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ApplyHiddenState();
            }
#endif
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
                SetStageAlpha(_runtimeStages[i], 1f);
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

                var renderers = stageRoot != null
                    ? stageRoot.GetComponentsInChildren<SpriteRenderer>(true)
                    : Array.Empty<SpriteRenderer>();
                var baseColors = new Color[renderers.Length];
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var renderer = renderers[rendererIndex];
                    baseColors[rendererIndex] = renderer != null ? renderer.color : Color.white;
                }

                stages[stageIndex] = new RuntimeStage
                {
                    definition = definition,
                    renderers = renderers,
                    baseColors = baseColors
                };
            }

            _runtimeStages = stages;
        }

        private void ApplyHiddenState()
        {
            for (var i = 0; i < _runtimeStages.Length; i++)
            {
                SetStageAlpha(_runtimeStages[i], 0f);
            }
        }

        private IEnumerator PlaySequence()
        {
            ApplyHiddenState();

            if (_initialDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(_initialDelaySeconds);
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
                    yield return new WaitForSeconds(stage.definition.revealDelaySeconds);
                }

                var duration = Mathf.Max(0.01f, stage.definition.revealDurationSeconds);
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    SetStageAlpha(stage, Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }

                SetStageAlpha(stage, 1f);
            }

            _sequenceRoutine = null;
        }

        private static void SetStageAlpha(RuntimeStage stage, float alpha)
        {
            if (stage == null || stage.renderers == null || stage.baseColors == null)
            {
                return;
            }

            var clampedAlpha = Mathf.Clamp01(alpha);
            var rendererCount = Mathf.Min(stage.renderers.Length, stage.baseColors.Length);
            for (var i = 0; i < rendererCount; i++)
            {
                var renderer = stage.renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var baseColor = stage.baseColors[i];
                renderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * clampedAlpha);
            }
        }
    }
}
