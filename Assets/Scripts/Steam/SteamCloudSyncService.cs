using System;
using System.IO;
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
                    if (!ValidateCloudPayloadIntegrity(cloudJson, hasLocalSave: false, out var integrityReason))
                    {
                        BackupCloudConflict(localPath, cloudJson, integrityReason);
                        LastConflictDecision = $"cloud_only_rejected_{integrityReason}";
                        _saveManager.LoadOrCreate();
                        return;
                    }

                    var saveDir = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrWhiteSpace(saveDir))
                    {
                        Directory.CreateDirectory(saveDir);
                    }

                    File.WriteAllText(localPath, cloudJson);
                    LastConflictDecision = string.IsNullOrWhiteSpace(integrityReason)
                        ? "cloud_only_downloaded"
                        : $"cloud_only_downloaded_{integrityReason}";
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

            if (!ValidateCloudPayloadIntegrity(cloudText, hasLocalSave: true, out var integrityFailureReason))
            {
                BackupCloudConflict(localPath, cloudText, integrityFailureReason);
                LastConflictDecision = $"cloud_integrity_failed_keep_local_{integrityFailureReason}";
                if (_verboseLogs)
                {
                    Debug.LogWarning($"SteamCloudSyncService: cloud payload integrity failed ({integrityFailureReason}), local save kept.");
                }

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
                BackupCloudConflict(localPath, cloudText, "local_newer");
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
            var manifest = new CloudSaveManifestData
            {
                savedAtUtc = File.GetLastWriteTimeUtc(localPath).ToString("O"),
                contentSha256 = SteamCloudIntegrity.ComputeSha256(localJson),
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
                if (SteamCloudIntegrity.TryParseManifest(manifestJson, out var manifest) &&
                    DateTime.TryParse(manifest.savedAtUtc, out var parsed))
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

        private bool ValidateCloudPayloadIntegrity(string cloudPayloadJson, bool hasLocalSave, out string integrityReason)
        {
#if STEAMWORKS_NET
            integrityReason = string.Empty;
            if (!TryReadCloudText(_cloudManifestFileName, out var manifestJson))
            {
                if (hasLocalSave)
                {
                    integrityReason = CloudIntegrityFailure.ManifestMissing.ToString();
                    return false;
                }

                integrityReason = "legacy_no_manifest";
                return true;
            }

            if (!SteamCloudIntegrity.TryParseManifest(manifestJson, out var manifest))
            {
                if (hasLocalSave)
                {
                    integrityReason = CloudIntegrityFailure.ManifestJsonInvalid.ToString();
                    return false;
                }

                integrityReason = "legacy_manifest_parse_failed";
                return true;
            }

            if (!SteamCloudIntegrity.TryValidatePayload(cloudPayloadJson, manifest, out var failure))
            {
                integrityReason = failure.ToString();
                return false;
            }

            return true;
#else
            integrityReason = "steamworks_disabled";
            return false;
#endif
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

        private void BackupCloudConflict(string localPath, string cloudText, string reasonSuffix)
        {
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var safeReason = SanitizeFileToken(reasonSuffix);
            var backupPath = localPath + $".conflict_cloud_{safeReason}_{stamp}";
            File.WriteAllText(backupPath, cloudText ?? string.Empty);
        }

        private static string SanitizeFileToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            var token = value.Trim();
            var invalidChars = Path.GetInvalidFileNameChars();
            for (var i = 0; i < invalidChars.Length; i++)
            {
                token = token.Replace(invalidChars[i], '_');
            }

            return token;
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
