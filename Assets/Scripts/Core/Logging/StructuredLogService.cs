using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RavenDevOps.Fishing.Core.Logging
{
    [Serializable]
    public sealed class StructuredLogEntry
    {
        public string timestampUtc;
        public string level;
        public string category;
        public string message;
        public string scene;
        public int frame;
    }

    public sealed class StructuredLogService : MonoBehaviour
    {
        [SerializeField] private int _maxBufferedEntries = 250;
        [SerializeField] private string _logFileName = "raven_runtime.log";
        [SerializeField] private bool _captureUnityLogs = true;

        private static StructuredLogService _instance;
        private readonly object _sync = new object();
        private readonly List<StructuredLogEntry> _entries = new List<StructuredLogEntry>();
        private string _logFilePath;

        public static StructuredLogService Instance => _instance;
        public static string LogFilePath => _instance != null ? _instance._logFilePath : string.Empty;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            RuntimeServiceRegistry.Register(this);
            _logFilePath = Path.Combine(Application.persistentDataPath, _logFileName);

            if (_captureUnityLogs)
            {
                Application.logMessageReceivedThreaded += OnUnityLog;
            }

            LogInternal("INFO", "bootstrap", "Structured logging initialized.");
            LogInternal("INFO", "bootstrap", $"Log path: {_logFilePath}");
        }

        private void OnDestroy()
        {
            if (_captureUnityLogs)
            {
                Application.logMessageReceivedThreaded -= OnUnityLog;
            }

            RuntimeServiceRegistry.Unregister(this);
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static void LogInfo(string category, string message)
        {
            _instance?.LogInternal("INFO", category, message);
        }

        public static void LogWarning(string category, string message)
        {
            _instance?.LogInternal("WARN", category, message);
        }

        public static void LogError(string category, string message)
        {
            _instance?.LogInternal("ERROR", category, message);
        }

        public List<StructuredLogEntry> GetRecentEntriesSnapshot()
        {
            lock (_sync)
            {
                return new List<StructuredLogEntry>(_entries);
            }
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            var level = ToLevel(type);
            if (string.IsNullOrWhiteSpace(level))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(stackTrace) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
            {
                condition = condition + "\n" + stackTrace;
            }

            LogInternal(level, "unity", condition);
        }

        private void LogInternal(string level, string category, string message)
        {
            var entry = new StructuredLogEntry
            {
                timestampUtc = DateTime.UtcNow.ToString("O"),
                level = level,
                category = string.IsNullOrWhiteSpace(category) ? "runtime" : category,
                message = message ?? string.Empty,
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                frame = Time.frameCount
            };

            lock (_sync)
            {
                _entries.Add(entry);
                if (_entries.Count > Mathf.Max(10, _maxBufferedEntries))
                {
                    _entries.RemoveAt(0);
                }

                try
                {
                    var line = JsonUtility.ToJson(entry);
                    File.AppendAllText(_logFilePath, line + Environment.NewLine);
                }
                catch (Exception)
                {
                    // Avoid recursive log-callback loops if file I/O fails.
                }
            }
        }

        private static string ToLevel(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return "WARN";
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return "ERROR";
                case LogType.Log:
                    return "INFO";
                default:
                    return string.Empty;
            }
        }
    }
}
