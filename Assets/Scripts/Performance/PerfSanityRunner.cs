using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Performance
{
    public sealed class PerfSanityRunner : MonoBehaviour
    {
        private const float MinimumFrameSeconds = 0.0001f;

        [SerializeField] private Text _fpsLabel;
        [SerializeField] private int _sampleFrames = 300;
        [SerializeField] private bool _emitWarningsOnBudgetFailure = true;
        [SerializeField] private float _targetAverageFps = 60f;
        [SerializeField] private float _targetP95FrameMs = 25f;
        [SerializeField] private float _targetGcDeltaKb = 64f;

        private float[] _windowFrameDurations = System.Array.Empty<float>();
        private float[] _windowFrameDurationsScratch = System.Array.Empty<float>();
        private int _windowSampleCount;
        private int _frameCounter;
        private float _elapsedSeconds;
        private long _gcWindowStartBytes;

        private void OnEnable()
        {
            EnsureSampleBuffers();
            ResetWindow();
        }

        private void OnValidate()
        {
            _sampleFrames = Mathf.Max(1, _sampleFrames);
            EnsureSampleBuffers();
        }

        private void Update()
        {
            EnsureSampleBuffers();

            _frameCounter++;
            var deltaTime = Mathf.Max(MinimumFrameSeconds, Time.unscaledDeltaTime);
            _elapsedSeconds += deltaTime;
            _windowFrameDurations[_windowSampleCount] = deltaTime;
            _windowSampleCount++;

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
            _windowSampleCount = 0;
            _gcWindowStartBytes = System.GC.GetTotalMemory(false);
        }

        private void EmitWindowSample()
        {
            if (_windowSampleCount <= 0)
            {
                return;
            }

            var avgFrameSeconds = _elapsedSeconds / Mathf.Max(1, _windowSampleCount);
            var avgFps = 1f / Mathf.Max(MinimumFrameSeconds, avgFrameSeconds);
            var minFps = float.MaxValue;
            var maxFps = 0f;
            for (var i = 0; i < _windowSampleCount; i++)
            {
                var fps = 1f / Mathf.Max(MinimumFrameSeconds, _windowFrameDurations[i]);
                minFps = Mathf.Min(minFps, fps);
                maxFps = Mathf.Max(maxFps, fps);
            }

            var avgFrameMs = avgFrameSeconds * 1000f;
            var p95FrameMs = ResolvePercentileFrameMsNoAlloc(_windowFrameDurations, _windowFrameDurationsScratch, _windowSampleCount, 0.95f);
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
                _windowSampleCount,
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

        private void EnsureSampleBuffers()
        {
            var requiredSamples = Mathf.Max(1, _sampleFrames);
            var resized = false;
            if (_windowFrameDurations.Length != requiredSamples)
            {
                _windowFrameDurations = new float[requiredSamples];
                resized = true;
            }

            if (_windowFrameDurationsScratch.Length != requiredSamples)
            {
                _windowFrameDurationsScratch = new float[requiredSamples];
                resized = true;
            }

            if (resized)
            {
                _windowSampleCount = 0;
                _frameCounter = 0;
                _elapsedSeconds = 0f;
                _gcWindowStartBytes = System.GC.GetTotalMemory(false);
            }
        }

        public static float ResolvePercentileFrameMsNoAlloc(
            float[] frameDurations,
            float[] scratchBuffer,
            int sampleCount,
            float percentile)
        {
            if (frameDurations == null || scratchBuffer == null || sampleCount <= 0)
            {
                return 0f;
            }

            var availableSamples = System.Math.Min(sampleCount, System.Math.Min(frameDurations.Length, scratchBuffer.Length));
            if (availableSamples <= 0)
            {
                return 0f;
            }

            System.Array.Copy(frameDurations, scratchBuffer, availableSamples);
            System.Array.Sort(scratchBuffer, 0, availableSamples);

            var clampedPercentile = Mathf.Clamp01(percentile);
            var index = Mathf.Clamp(Mathf.CeilToInt((availableSamples - 1) * clampedPercentile), 0, availableSamples - 1);
            return scratchBuffer[index] * 1000f;
        }
    }
}
