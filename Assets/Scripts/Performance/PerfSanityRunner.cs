using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Performance
{
    public sealed class PerfSanityRunner : MonoBehaviour
    {
        [SerializeField] private Text _fpsLabel;
        [SerializeField] private int _sampleFrames = 300;
        [SerializeField] private bool _emitWarningsOnBudgetFailure = true;
        [SerializeField] private float _targetAverageFps = 60f;
        [SerializeField] private float _targetP95FrameMs = 25f;
        [SerializeField] private float _targetGcDeltaKb = 64f;

        private readonly List<float> _windowFrameDurations = new List<float>(512);
        private int _frameCounter;
        private float _elapsedSeconds;
        private long _gcWindowStartBytes;

        private void OnEnable()
        {
            ResetWindow();
        }

        private void Update()
        {
            _frameCounter++;
            var deltaTime = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            _elapsedSeconds += deltaTime;
            _windowFrameDurations.Add(deltaTime);

            if (_frameCounter < _sampleFrames)
            {
                return;
            }

            EmitWindowSample();
            ResetWindow();
        }

        private void ResetWindow()
        {
            _frameCounter = 0;
            _elapsedSeconds = 0f;
            _windowFrameDurations.Clear();
            _gcWindowStartBytes = System.GC.GetTotalMemory(false);
        }

        private void EmitWindowSample()
        {
            if (_windowFrameDurations.Count == 0)
            {
                return;
            }

            var avgFrameSeconds = _elapsedSeconds / Mathf.Max(1, _windowFrameDurations.Count);
            var avgFps = 1f / Mathf.Max(0.0001f, avgFrameSeconds);
            var minFps = float.MaxValue;
            var maxFps = 0f;
            for (var i = 0; i < _windowFrameDurations.Count; i++)
            {
                var fps = 1f / Mathf.Max(0.0001f, _windowFrameDurations[i]);
                minFps = Mathf.Min(minFps, fps);
                maxFps = Mathf.Max(maxFps, fps);
            }

            var avgFrameMs = avgFrameSeconds * 1000f;
            var p95FrameMs = ResolvePercentileFrameMs(_windowFrameDurations, 0.95f);
            var gcDeltaBytes = System.GC.GetTotalMemory(false) - _gcWindowStartBytes;
            var gcDeltaKb = gcDeltaBytes / 1024f;

            if (_fpsLabel != null)
            {
                _fpsLabel.text = $"FPS {avgFps:0.0} | p95 {p95FrameMs:0.0}ms";
            }

            var sceneName = SceneManager.GetActiveScene().name;
            var sampleLog = string.Format(
                CultureInfo.InvariantCulture,
                "PERF_SANITY scene={0} frames={1} avg_fps={2:0.00} min_fps={3:0.00} max_fps={4:0.00} avg_frame_ms={5:0.00} p95_frame_ms={6:0.00} gc_delta_kb={7:0.00}",
                sceneName,
                _frameCounter,
                avgFps,
                minFps,
                maxFps,
                avgFrameMs,
                p95FrameMs,
                gcDeltaKb);

            Debug.Log(sampleLog);

            if (!_emitWarningsOnBudgetFailure)
            {
                return;
            }

            if (avgFps < _targetAverageFps || p95FrameMs > _targetP95FrameMs || gcDeltaKb > _targetGcDeltaKb)
            {
                var budgetWarning = string.Format(
                    CultureInfo.InvariantCulture,
                    "PERF_SANITY_BUDGET_FAIL scene={0} avg_fps={1:0.00}/{2:0.00} p95_frame_ms={3:0.00}/{4:0.00} gc_delta_kb={5:0.00}/{6:0.00}",
                    sceneName,
                    avgFps,
                    _targetAverageFps,
                    p95FrameMs,
                    _targetP95FrameMs,
                    gcDeltaKb,
                    _targetGcDeltaKb);

                Debug.LogWarning(budgetWarning);
            }
        }

        private static float ResolvePercentileFrameMs(List<float> frameDurations, float percentile)
        {
            if (frameDurations == null || frameDurations.Count == 0)
            {
                return 0f;
            }

            var sorted = new List<float>(frameDurations);
            sorted.Sort();

            var clampedPercentile = Mathf.Clamp01(percentile);
            var index = Mathf.Clamp(Mathf.CeilToInt((sorted.Count - 1) * clampedPercentile), 0, sorted.Count - 1);
            return sorted[index] * 1000f;
        }
    }
}
