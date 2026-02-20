using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

namespace RavenDevOps.Fishing.Steam
{
    public sealed class SteamCloudSyncService : MonoBehaviour
    {
        [Serializable]
        private sealed class CloudSaveManifest
        {
            public string savedAtUtc = string.Empty;
            public string contentSha256 = string.Empty;
            public string policy = "newest-wins";
        }

        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private bool _autoSyncOnSave = true;
        [SerializeField] private bool _verboseLogs = true;
        [SerializeField] private int _conflictSkewToleranceSeconds = 2;
        [SerializeField] private string _cloudSaveFileName = "save_v1.json";
        [SerializeField] private string _cloudManifestFileName = "save_v1.meta.json";

        private bool _startupSyncCompleted;
        private bool _syncInProgress;

        public string LastConflictDecision { get; private set; } = string.Empty;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Register(this);
        }

        private void OnEnable()
        {
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged += OnSaveDataChanged;
            }
        }

        private void OnDisable()
        {
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged -= OnSaveDataChanged;
            }
        }

        private void Update()
        {
            if (_startupSyncCompleted || !CanUseCloud())
            {
                return;
            }

            TryPerformStartupSync();
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        private void OnSaveDataChanged(SaveDataV1 data)
        {
            if (!_autoSyncOnSave || !_startupSyncCompleted || _syncInProgress || !CanUseCloud())
            {
                return;
            }

            UploadLocalToCloud();
        }

        private void TryPerformStartupSync()
        {
            if (_syncInProgress || _startupSyncCompleted || _saveManager == null)
            {
                return;
            }

            _syncInProgress = true;
            try
            {
                PerformStartupSync();
                _startupSyncCompleted = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SteamCloudSyncService: startup sync failed, local save remains canonical ({ex.Message}).");
                _startupSyncCompleted = true;
            }
            finally
            {
                _syncInProgress = false;
            }
        }

        private void PerformStartupSync()
        {
#if STEAMWORKS_NET
            var localPath = _saveManager.SaveFilePath;
            var localExists = File.Exists(localPath);
            var cloudExists = SteamRemoteStorage.FileExists(_cloudSaveFileName);

            if (!localExists && !cloudExists)
            {
                LastConflictDecision = "no_save_data";
                return;
            }

            if (!localExists && cloudExists)
            {
                if (TryReadCloudText(_cloudSaveFileName, out var cloudJson))
                {
                    var saveDir = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrWhiteSpace(saveDir))
                    {
                        Directory.CreateDirectory(saveDir);
                    }

                    File.WriteAllText(localPath, cloudJson);
                    LastConflictDecision = "cloud_only_downloaded";
                    _saveManager.LoadOrCreate();
                }

                return;
            }

            if (localExists && !cloudExists)
            {
                LastConflictDecision = "local_only_uploaded";
                UploadLocalToCloud();
                return;
            }

            var localJson = File.ReadAllText(localPath);
            if (!TryReadCloudText(_cloudSaveFileName, out var cloudText))
            {
                LastConflictDecision = "cloud_read_failed_keep_local";
                return;
            }

            if (string.Equals(localJson, cloudText, StringComparison.Ordinal))
            {
                LastConflictDecision = "already_in_sync";
                UploadManifest(localPath, localJson);
                return;
            }

            var localTime = File.GetLastWriteTimeUtc(localPath);
            var cloudTime = ResolveCloudTimestampUtc();
            var tolerance = TimeSpan.FromSeconds(Mathf.Max(0, _conflictSkewToleranceSeconds));

            if (cloudTime > localTime.Add(tolerance))
            {
                BackupLocalConflict(localPath, "local");
                var saveDir = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrWhiteSpace(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }

                File.WriteAllText(localPath, cloudText);
                LastConflictDecision = "cloud_newer_downloaded";
                _saveManager.LoadOrCreate();
            }
            else
            {
                BackupCloudConflict(localPath, cloudText);
                LastConflictDecision = "local_newer_uploaded";
                UploadLocalToCloud();
            }

            if (_verboseLogs)
            {
                Debug.Log($"SteamCloudSyncService: startup sync decision '{LastConflictDecision}'.");
            }
#endif
        }

        private void UploadLocalToCloud()
        {
#if STEAMWORKS_NET
            if (_saveManager == null)
            {
                return;
            }

            var localPath = _saveManager.SaveFilePath;
            if (!File.Exists(localPath))
            {
                return;
            }

            var text = File.ReadAllText(localPath);
            var bytes = Encoding.UTF8.GetBytes(text);
            if (!SteamRemoteStorage.FileWrite(_cloudSaveFileName, bytes, bytes.Length))
            {
                Debug.LogWarning($"SteamCloudSyncService: failed to write '{_cloudSaveFileName}' to cloud.");
                return;
            }

            UploadManifest(localPath, text);
#endif
        }

        private void UploadManifest(string localPath, string localJson)
        {
#if STEAMWORKS_NET
            var manifest = new CloudSaveManifest
            {
                savedAtUtc = File.GetLastWriteTimeUtc(localPath).ToString("O"),
                contentSha256 = ComputeSha256(localJson),
                policy = "newest-wins"
            };

            var manifestJson = JsonUtility.ToJson(manifest, true);
            var bytes = Encoding.UTF8.GetBytes(manifestJson);
            SteamRemoteStorage.FileWrite(_cloudManifestFileName, bytes, bytes.Length);
#endif
        }

        private DateTime ResolveCloudTimestampUtc()
        {
#if STEAMWORKS_NET
            if (TryReadCloudText(_cloudManifestFileName, out var manifestJson))
            {
                var manifest = JsonUtility.FromJson<CloudSaveManifest>(manifestJson);
                if (manifest != null && DateTime.TryParse(manifest.savedAtUtc, out var parsed))
                {
                    return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                }
            }

            var timestampUnix = SteamRemoteStorage.GetFileTimestamp(_cloudSaveFileName);
            if (timestampUnix > 0)
            {
                return DateTimeOffset.FromUnixTimeSeconds(timestampUnix).UtcDateTime;
            }
#endif
            return DateTime.MinValue;
        }

        private static string ComputeSha256(string text)
        {
            var raw = Encoding.UTF8.GetBytes(text ?? string.Empty);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(raw);
            var builder = new StringBuilder(hash.Length * 2);
            for (var i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }

        private void BackupLocalConflict(string localPath, string suffix)
        {
            if (!File.Exists(localPath))
            {
                return;
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupPath = localPath + $".conflict_{suffix}_{stamp}";
            File.Copy(localPath, backupPath, overwrite: true);
        }

        private void BackupCloudConflict(string localPath, string cloudText)
        {
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupPath = localPath + $".conflict_cloud_{stamp}";
            File.WriteAllText(backupPath, cloudText ?? string.Empty);
        }

        private bool CanUseCloud()
        {
#if STEAMWORKS_NET
            return SteamBootstrap.IsSteamInitialized;
#else
            return false;
#endif
        }

        private static bool TryReadCloudText(string remoteFileName, out string text)
        {
            text = string.Empty;
#if STEAMWORKS_NET
            if (!SteamRemoteStorage.FileExists(remoteFileName))
            {
                return false;
            }

            var fileSize = SteamRemoteStorage.GetFileSize(remoteFileName);
            if (fileSize <= 0)
            {
                return false;
            }

            var buffer = new byte[fileSize];
            var read = SteamRemoteStorage.FileRead(remoteFileName, buffer, fileSize);
            if (read <= 0)
            {
                return false;
            }

            text = Encoding.UTF8.GetString(buffer, 0, read);
            return true;
#else
            return false;
#endif
        }
    }
}
