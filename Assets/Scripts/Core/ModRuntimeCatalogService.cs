using System;
using System.IO;
using RavenDevOps.Fishing.Tools;
using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    public sealed class ModRuntimeCatalogService : MonoBehaviour
    {
        [SerializeField] private bool _enableMods = true;
        [SerializeField] private bool _verboseLogging = true;
        [SerializeField] private string _modsDirectoryName = "Mods";
        [SerializeField] private string _overrideModsRootPath = string.Empty;

        private ModRuntimeCatalogLoadResult _lastLoadResult = new ModRuntimeCatalogLoadResult();
        private bool _safeModeActive;
        private string _safeModeReason = string.Empty;

        public bool ModsEnabled => _lastLoadResult.modsEnabled;
        public bool SafeModeActive => _safeModeActive;
        public string SafeModeReason => _safeModeReason;
        public string ModsRootPath => _lastLoadResult.modsRootPath;
        public ModRuntimeCatalogLoadResult LastLoadResult => _lastLoadResult;

        public event Action CatalogReloaded;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            Reload();
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public void Reload()
        {
            _safeModeActive = ResolveSafeMode(out _safeModeReason);
            var modsRootPath = ResolveModsRootPath();
            var shouldLoad = _enableMods && !_safeModeActive;
            _lastLoadResult = ModRuntimeCatalogLoader.Load(modsRootPath, shouldLoad, Application.version);
            if (_safeModeActive)
            {
                var reason = string.IsNullOrWhiteSpace(_safeModeReason) ? "unknown source" : _safeModeReason;
                _lastLoadResult.messages.Add($"INFO: Mod safe mode is active ({reason}).");
            }

            if (_verboseLogging)
            {
                Debug.Log(
                    $"ModRuntimeCatalogService: modsEnabled={_lastLoadResult.modsEnabled}, safeMode={_safeModeActive}, safeModeReason='{_safeModeReason}', accepted={_lastLoadResult.acceptedMods.Count}, rejected={_lastLoadResult.rejectedMods.Count}, root='{_lastLoadResult.modsRootPath}'.");

                for (var i = 0; i < _lastLoadResult.messages.Count; i++)
                {
                    Debug.Log(_lastLoadResult.messages[i]);
                }
            }

            try
            {
                CatalogReloaded?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogError($"ModRuntimeCatalogService: CatalogReloaded listener failed ({exception.Message}).");
            }
        }

        public void SetModsRootPathForTesting(string path)
        {
            _overrideModsRootPath = path ?? string.Empty;
        }

        public void SetModsEnabledForTesting(bool enabled)
        {
            _enableMods = enabled;
        }

        private string ResolveModsRootPath()
        {
            if (!string.IsNullOrWhiteSpace(_overrideModsRootPath))
            {
                return _overrideModsRootPath;
            }

            var root = string.IsNullOrWhiteSpace(Application.persistentDataPath)
                ? Directory.GetCurrentDirectory()
                : Application.persistentDataPath;
            return Path.Combine(root, _modsDirectoryName);
        }

        private static bool ResolveSafeMode(out string reason)
        {
            var env = Environment.GetEnvironmentVariable("RAVEN_DISABLE_MODS");
            var args = Environment.GetCommandLineArgs();
            var persistedSafeModeEnabled = PlayerPrefs.GetInt(UserSettingsService.ModsSafeModePlayerPrefsKey, 0) == 1;
            return EvaluateSafeModeRequest(persistedSafeModeEnabled, env, args, out reason);
        }

        public static bool EvaluateSafeModeRequest(
            bool persistedSafeModeEnabled,
            string envDisableModsValue,
            string[] commandLineArgs,
            out string reason)
        {
            reason = string.Empty;

            if (IsTruthySafeModeEnv(envDisableModsValue))
            {
                reason = "env:RAVEN_DISABLE_MODS";
                return true;
            }

            if (HasSafeModeArg(commandLineArgs))
            {
                reason = "command-line";
                return true;
            }

            if (persistedSafeModeEnabled)
            {
                reason = $"playerprefs:{UserSettingsService.ModsSafeModePlayerPrefsKey}";
                return true;
            }

            return false;
        }

        private static bool IsTruthySafeModeEnv(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasSafeModeArg(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i] ?? string.Empty;
                if (string.Equals(arg, "-safeMode=true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-safeMode=1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-disableMods=true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-disableMods=1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-mods=false", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
