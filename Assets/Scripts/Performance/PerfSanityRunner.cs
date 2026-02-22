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
        [SerializeField] private bool _emitSampleLogs = true;
        [SerializeField] private int _maxSampleLogsPerScene = 6;
        [SerializeField] private float _targetAverageFps = 60f;
        [SerializeField] private float _targetP95FrameMs = 25f;
        [SerializeField] private float _targetGcDeltaKb = 64f;
        [SerializeField] private string _hardwareTier = "minimum";
        [SerializeField] private float _warningCooldownSeconds = 5f;

        private float[] _windowFrameDurations = System.Array.Empty<float>();
        private float[] _windowFrameDurationsScratch = System.Array.Empty<float>();
        private int _windowSampleCount;
        private int _frameCounter;
        private float _elapsedSeconds;
        private long _gcWindowStartBytes;
        private float _lastBudgetWarningTime = -999f;
        private string _lastBudgetWarningSignature = string.Empty;
        private string _sampleLogScene = string.Empty;
        private int _sampleLogsForScene;

        public void Configure(
            Text fpsLabel,
            int sampleFrames = 300,
            bool emitWarningsOnBudgetFailure = true,
            string hardwareTier = "minimum",
            bool emitSampleLogs = true,
            int maxSampleLogsPerScene = 6)
        {
            _fpsLabel = fpsLabel;
            _sampleFrames = Mathf.Max(1, sampleFrames);
            _emitWarningsOnBudgetFailure = emitWarningsOnBudgetFailure;
            _emitSampleLogs = emitSampleLogs;
            _maxSampleLogsPerScene = Mathf.Max(0, maxSampleLogsPerScene);
            if (!string.IsNullOrWhiteSpace(hardwareTier))
            {
                _hardwareTier = hardwareTier.Trim();
            }

            EnsureSampleBuffers();
            ResetWindow();
        }

        private void OnEnable()
        {
            EnsureSampleBuffers();
            ResetWindow();
        }

        private void OnDisable()
        {
        }

        private void OnValidate()
        {
            _sampleFrames = Mathf.Max(1, _sampleFrames);
            _maxSampleLogsPerScene = Mathf.Max(0, _maxSampleLogsPerScene);
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
            // Budgeting uses average GC allocation per frame so thresholds stay stable across sample window sizes.
            var gcDeltaKb = ResolveGcDeltaKbPerFrame(_windowSampleCount);
            var avgFpsFailed = avgFps < _targetAverageFps;
            var p95Failed = p95FrameMs > _targetP95FrameMs;
            var gcFailed = gcDeltaKb > _targetGcDeltaKb;

            if (_fpsLabel != null)
            {
                _fpsLabel.text = $"FPS {avgFps:0.0} | p95 {p95FrameMs:0.0}ms";
            }

            var sceneName = SceneManager.GetActiveScene().name;
            var sampleLog = string.Format(
                CultureInfo.InvariantCulture,
                "PERF_SANITY scene={0} tier={1} frames={2} avg_fps={3:0.00} min_fps={4:0.00} max_fps={5:0.00} avg_frame_ms={6:0.00} p95_frame_ms={7:0.00} gc_delta_kb={8:0.00}",
                sceneName,
                NormalizeTier(_hardwareTier),
                _windowSampleCount,
                avgFps,
                minFps,
                maxFps,
                avgFrameMs,
                p95FrameMs,
                gcDeltaKb);

            if (!string.Equals(_sampleLogScene, sceneName, System.StringComparison.Ordinal))
            {
                _sampleLogScene = sceneName;
                _sampleLogsForScene = 0;
            }

            var budgetViolation = avgFpsFailed || p95Failed || gcFailed;
            var underSceneLogCap = _sampleLogsForScene < _maxSampleLogsPerScene;
            if (_emitSampleLogs && (underSceneLogCap || budgetViolation))
            {
                Debug.Log(sampleLog);
                _sampleLogsForScene++;
            }

            if (!_emitWarningsOnBudgetFailure)
            {
                return;
            }

            if (budgetViolation)
            {
                var signature = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}|{1}|{2}",
                    avgFpsFailed ? "fps" : "-",
                    p95Failed ? "p95" : "-",
                    gcFailed ? "gc" : "-");
                var now = Time.unscaledTime;
                var warningCooldown = Mathf.Max(0f, _warningCooldownSeconds);
                var shouldLogWarning = !string.Equals(_lastBudgetWarningSignature, signature, System.StringComparison.Ordinal)
                    || now - _lastBudgetWarningTime >= warningCooldown;
                if (!shouldLogWarning)
                {
                    return;
                }

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
                _lastBudgetWarningTime = now;
                _lastBudgetWarningSignature = signature;
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

        private long ResolveGcDeltaBytes()
        {
            var gcDeltaBytes = System.GC.GetTotalMemory(false) - _gcWindowStartBytes;
            return System.Math.Max(0L, gcDeltaBytes);
        }

        private float ResolveGcDeltaKbPerFrame(int sampleCount)
        {
            if (sampleCount <= 0)
            {
                return 0f;
            }

            var gcDeltaBytesPerFrame = (double)ResolveGcDeltaBytes() / sampleCount;
            return (float)(gcDeltaBytesPerFrame / 1024d);
        }

        private static string NormalizeTier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "minimum";
            }

            return value.Trim().ToLowerInvariant();
        }
    }
}
