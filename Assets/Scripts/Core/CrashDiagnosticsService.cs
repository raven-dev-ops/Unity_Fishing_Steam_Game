using System;
using System.IO;
using UnityEngine;

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
            public string privacy;
        }

        [SerializeField] private bool _captureErrorLogs = true;
        [SerializeField] private bool _captureExceptionLogs = true;
        [SerializeField] private bool _verboseLogs = true;
        [SerializeField] private string _artifactFileName = "last_crash_report.json";

        public string ArtifactPath => Path.Combine(Application.persistentDataPath, _artifactFileName);

        private void Awake()
        {
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

                var artifact = new CrashReportArtifact
                {
                    occurredAtUtc = DateTime.UtcNow.ToString("O"),
                    logType = type.ToString(),
                    message = condition ?? string.Empty,
                    stackTrace = stackTrace ?? string.Empty,
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    privacy = "Local-only crash artifact. No automatic telemetry upload."
                };

                File.WriteAllText(ArtifactPath, JsonUtility.ToJson(artifact, true));
                if (_verboseLogs)
                {
                    Debug.Log($"CrashDiagnosticsService: wrote crash artifact to '{ArtifactPath}'.");
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
    }
}
