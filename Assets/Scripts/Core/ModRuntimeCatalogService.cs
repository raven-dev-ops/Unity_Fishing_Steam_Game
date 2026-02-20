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

        public bool ModsEnabled => _lastLoadResult.modsEnabled;
        public bool SafeModeActive => _safeModeActive;
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
            _safeModeActive = ResolveSafeMode();
            var modsRootPath = ResolveModsRootPath();
            var shouldLoad = _enableMods && !_safeModeActive;
            _lastLoadResult = ModRuntimeCatalogLoader.Load(modsRootPath, shouldLoad, Application.version);

            if (_verboseLogging)
            {
                Debug.Log(
                    $"ModRuntimeCatalogService: modsEnabled={_lastLoadResult.modsEnabled}, safeMode={_safeModeActive}, accepted={_lastLoadResult.acceptedMods.Count}, rejected={_lastLoadResult.rejectedMods.Count}, root='{_lastLoadResult.modsRootPath}'.");

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

        private static bool ResolveSafeMode()
        {
            var env = Environment.GetEnvironmentVariable("RAVEN_DISABLE_MODS");
            if (!string.IsNullOrWhiteSpace(env))
            {
                if (string.Equals(env, "1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(env, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(env, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var args = Environment.GetCommandLineArgs();
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
