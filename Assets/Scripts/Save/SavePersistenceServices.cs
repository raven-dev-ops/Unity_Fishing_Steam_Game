using System;
using System.IO;
using RavenDevOps.Fishing.Core.Logging;
using UnityEngine;

namespace RavenDevOps.Fishing.Save
{
    public interface ISavePersistenceAdapter
    {
        bool TryPersist(string savePath, SaveDataV1 data, ISaveFileSystem fileSystem, out string failureReason);
    }

    public interface ISaveMigrationLoadCoordinator
    {
        bool TryLoad(string savePath, ISaveFileSystem fileSystem, ITimeProvider timeProvider, out SaveDataV1 loaded, out string failureReason);
    }

    public sealed class AtomicJsonSavePersistenceAdapter : ISavePersistenceAdapter
    {
        private readonly string _tempFileSuffix;
        private readonly string _backupFileSuffix;

        public AtomicJsonSavePersistenceAdapter(string tempFileSuffix = ".tmp", string backupFileSuffix = ".bak")
        {
            _tempFileSuffix = string.IsNullOrWhiteSpace(tempFileSuffix) ? ".tmp" : tempFileSuffix;
            _backupFileSuffix = string.IsNullOrWhiteSpace(backupFileSuffix) ? ".bak" : backupFileSuffix;
        }

        public bool TryPersist(string savePath, SaveDataV1 data, ISaveFileSystem fileSystem, out string failureReason)
        {
            failureReason = string.Empty;
            if (fileSystem == null)
            {
                failureReason = "missing_file_system";
                return false;
            }

            if (data == null)
            {
                failureReason = "missing_save_data";
                return false;
            }

            if (string.IsNullOrWhiteSpace(savePath))
            {
                failureReason = "missing_save_path";
                return false;
            }

            try
            {
                var saveDir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrWhiteSpace(saveDir))
                {
                    fileSystem.EnsureDirectory(saveDir);
                }

                var tempPath = savePath + _tempFileSuffix;
                var backupPath = savePath + _backupFileSuffix;
                var json = JsonUtility.ToJson(data, true);

                fileSystem.WriteAllText(tempPath, json);
                if (fileSystem.FileExists(savePath))
                {
                    AtomicReplace(tempPath, savePath, backupPath, fileSystem);
                }
                else
                {
                    fileSystem.MoveFile(tempPath, savePath);
                }

                if (fileSystem.FileExists(backupPath))
                {
                    fileSystem.DeleteFile(backupPath);
                }

                if (fileSystem.FileExists(tempPath))
                {
                    fileSystem.DeleteFile(tempPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                failureReason = ex.Message;
                return false;
            }
        }

        private static void AtomicReplace(string tempPath, string destinationPath, string backupPath, ISaveFileSystem fileSystem)
        {
            try
            {
                fileSystem.ReplaceFile(tempPath, destinationPath, backupPath);
            }
            catch (PlatformNotSupportedException)
            {
                fileSystem.CopyFile(tempPath, destinationPath, overwrite: true);
                fileSystem.DeleteFile(tempPath);
            }
            catch (IOException)
            {
                fileSystem.CopyFile(tempPath, destinationPath, overwrite: true);
                fileSystem.DeleteFile(tempPath);
            }
        }
    }

    public sealed class SaveMigrationLoadCoordinator : ISaveMigrationLoadCoordinator
    {
        public bool TryLoad(string savePath, ISaveFileSystem fileSystem, ITimeProvider timeProvider, out SaveDataV1 loaded, out string failureReason)
        {
            loaded = null;
            failureReason = string.Empty;
            if (fileSystem == null)
            {
                failureReason = "missing_file_system";
                return false;
            }

            if (timeProvider == null)
            {
                failureReason = "missing_time_provider";
                return false;
            }

            if (string.IsNullOrWhiteSpace(savePath))
            {
                failureReason = "missing_save_path";
                return false;
            }

            if (!fileSystem.FileExists(savePath))
            {
                failureReason = "save_missing";
                return false;
            }

            try
            {
                var rawJson = fileSystem.ReadAllText(savePath);
                if (!SaveMigrationPipeline.TryPrepareForLoad(rawJson, out var normalizedJson, out var migrationReport))
                {
                    var reason = string.IsNullOrWhiteSpace(migrationReport.FailureReason)
                        ? "migration failed"
                        : migrationReport.FailureReason;
                    StructuredLogService.LogWarning(
                        "save-migration",
                        $"status=failed source_version={migrationReport.SourceVersion} final_version={migrationReport.FinalVersion} reason=\"{reason}\" path=\"{savePath}\"");
                    failureReason = reason;
                    BackupCorruptSaveFile(savePath, fileSystem, timeProvider, $"migration failed: {reason}");
                    return false;
                }

                loaded = JsonUtility.FromJson<SaveDataV1>(normalizedJson);
                if (migrationReport.WasMigrated)
                {
                    var steps = migrationReport.AppliedSteps.Count > 0
                        ? string.Join(", ", migrationReport.AppliedSteps)
                        : "unknown";
                    StructuredLogService.LogInfo(
                        "save-migration",
                        $"status=success source_version={migrationReport.SourceVersion} final_version={migrationReport.FinalVersion} steps=\"{steps}\" path=\"{savePath}\"");
                    Debug.Log($"SaveMigrationLoadCoordinator: migrated save v{migrationReport.SourceVersion} -> v{migrationReport.FinalVersion} ({steps}).");
                }
            }
            catch (Exception ex)
            {
                failureReason = $"read/deserialize exception: {ex.Message}";
                BackupCorruptSaveFile(savePath, fileSystem, timeProvider, failureReason);
                return false;
            }

            if (loaded == null)
            {
                failureReason = "deserialize produced null save";
                BackupCorruptSaveFile(savePath, fileSystem, timeProvider, failureReason);
                return false;
            }

            return true;
        }

        private static void BackupCorruptSaveFile(string savePath, ISaveFileSystem fileSystem, ITimeProvider timeProvider, string reason)
        {
            if (string.IsNullOrWhiteSpace(savePath)
                || fileSystem == null
                || timeProvider == null
                || !fileSystem.FileExists(savePath))
            {
                return;
            }

            try
            {
                var timestamp = timeProvider.LocalNow.ToString("yyyyMMdd_HHmmss");
                var corruptPath = savePath + $".corrupt_{timestamp}";
                fileSystem.CopyFile(savePath, corruptPath, overwrite: true);
                Debug.LogWarning($"SaveMigrationLoadCoordinator: detected corrupt save, copied to '{corruptPath}' ({reason}).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveMigrationLoadCoordinator: failed to back up corrupt save ({ex.Message}).");
            }
        }
    }
}
