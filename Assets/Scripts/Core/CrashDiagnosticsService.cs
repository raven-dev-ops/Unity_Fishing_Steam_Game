using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RavenDevOps.Fishing.Core
{
    public sealed class CrashDiagnosticsService : MonoBehaviour
    {
        [Serializable]
        private sealed class CrashReportArtifact
        {
            public string occurredAtUtc;
            public string logType;
            public string message;
            public string stackTrace;
            public string unityVersion;
            public string platform;
            public string eventCategory;
            public string sessionId;
            public string activeScene;
            public int frameCount;
            public float timeScale;
            public string privacy;
        }

        [SerializeField] private bool _captureErrorLogs = true;
        [SerializeField] private bool _captureExceptionLogs = true;
        [SerializeField] private bool _verboseLogs = true;
        [SerializeField] private string _artifactFilePrefix = "crash_report";
        [SerializeField] private int _maxArtifactHistory = 10;
        [SerializeField] private string _artifactFileName = "last_crash_report.json";

        private string _sessionId;

        public string ArtifactDirectory => Application.persistentDataPath;
        public string ArtifactPath => Path.Combine(ArtifactDirectory, _artifactFileName);

        private void Awake()
        {
            _sessionId = Guid.NewGuid().ToString("N");
            RuntimeServiceRegistry.Register(this);
        }

        private void OnEnable()
        {
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!ShouldCapture(type))
            {
                return;
            }

            try
            {
                var dir = Path.GetDirectoryName(ArtifactPath);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var activeScene = SceneManager.GetActiveScene();
                var artifact = new CrashReportArtifact
                {
                    occurredAtUtc = DateTime.UtcNow.ToString("O"),
                    logType = type.ToString(),
                    message = condition ?? string.Empty,
                    stackTrace = stackTrace ?? string.Empty,
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    eventCategory = ResolveEventCategory(type),
                    sessionId = _sessionId,
                    activeScene = activeScene.IsValid() ? activeScene.path : string.Empty,
                    frameCount = Time.frameCount,
                    timeScale = Time.timeScale,
                    privacy = "Local-only crash artifact. No automatic telemetry upload."
                };

                var artifactJson = JsonUtility.ToJson(artifact, true);
                var historyPath = Path.Combine(
                    ArtifactDirectory,
                    $"{_artifactFilePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.json");

                File.WriteAllText(historyPath, artifactJson);
                File.WriteAllText(ArtifactPath, artifactJson);
                PruneArtifactHistory();
                if (_verboseLogs)
                {
                    Debug.Log($"CrashDiagnosticsService: wrote crash artifact '{historyPath}' and updated '{ArtifactPath}'.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CrashDiagnosticsService: failed to write crash artifact ({ex.Message}).");
            }
        }

        private bool ShouldCapture(LogType type)
        {
            if (_captureExceptionLogs && type == LogType.Exception)
            {
                return true;
            }

            if (_captureErrorLogs && (type == LogType.Error || type == LogType.Assert))
            {
                return true;
            }

            return false;
        }

        private string ResolveEventCategory(LogType type)
        {
            if (type == LogType.Exception)
            {
                return "exception";
            }

            if (type == LogType.Assert)
            {
                return "assert";
            }

            if (type == LogType.Error)
            {
                return "error";
            }

            return "other";
        }

        private void PruneArtifactHistory()
        {
            try
            {
                var historyCap = Mathf.Max(1, _maxArtifactHistory);
                var pattern = $"{_artifactFilePrefix}_*.json";
                var files = Directory.GetFiles(ArtifactDirectory, pattern);
                if (files.Length <= historyCap)
                {
                    return;
                }

                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                var removeCount = files.Length - historyCap;
                for (var i = 0; i < removeCount; i++)
                {
                    File.Delete(files[i]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CrashDiagnosticsService: failed to prune crash artifact history ({ex.Message}).");
            }
        }
    }
}
