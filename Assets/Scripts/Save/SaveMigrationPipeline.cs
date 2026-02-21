using System;
using System.Collections.Generic;
using UnityEngine;

namespace RavenDevOps.Fishing.Save
{
    public interface ISaveMigrator
    {
        int FromVersion { get; }
        int ToVersion { get; }
        bool TryMigrate(string inputJson, out string outputJson, out string error);
    }

    [Serializable]
    internal sealed class SaveVersionEnvelope
    {
        public int saveVersion;
    }

    public sealed class SaveMigrationReport
    {
        public int SourceVersion { get; set; }
        public int FinalVersion { get; set; }
        public bool WasMigrated { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public List<string> AppliedSteps { get; } = new List<string>();
    }

    public static class SaveMigrationPipeline
    {
        public const int LatestVersion = 1;

        private static readonly Dictionary<int, ISaveMigrator> Migrators = new Dictionary<int, ISaveMigrator>
        {
            { 0, new SaveV0ToV1Migrator() }
        };

        public static bool TryPrepareForLoad(string rawJson, out string normalizedJson, out SaveMigrationReport report)
        {
            report = new SaveMigrationReport();
            normalizedJson = rawJson ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedJson))
            {
                report.FailureReason = "save file is empty";
                return false;
            }

            if (!TryReadVersion(normalizedJson, out var sourceVersion, out var versionError))
            {
                report.FailureReason = versionError;
                return false;
            }

            report.SourceVersion = sourceVersion;
            report.FinalVersion = sourceVersion;

            if (sourceVersion > LatestVersion)
            {
                report.FailureReason = $"save version {sourceVersion} is newer than supported version {LatestVersion}";
                return false;
            }

            if (sourceVersion == LatestVersion)
            {
                return true;
            }

            var currentVersion = sourceVersion;
            var currentJson = normalizedJson;

            while (currentVersion < LatestVersion)
            {
                if (!Migrators.TryGetValue(currentVersion, out var migrator) || migrator == null)
                {
                    report.FailureReason = $"no migrator registered for version {currentVersion}";
                    return false;
                }

                if (!migrator.TryMigrate(currentJson, out var nextJson, out var migrateError))
                {
                    report.FailureReason = $"migration {migrator.FromVersion}->{migrator.ToVersion} failed: {migrateError}";
                    return false;
                }

                if (!TryReadVersion(nextJson, out var nextVersion, out var nextVersionError))
                {
                    report.FailureReason = $"migration output version parse failed: {nextVersionError}";
                    return false;
                }

                if (nextVersion != migrator.ToVersion)
                {
                    report.FailureReason = $"migration {migrator.FromVersion}->{migrator.ToVersion} produced unexpected version {nextVersion}";
                    return false;
                }

                report.AppliedSteps.Add($"v{migrator.FromVersion}->v{migrator.ToVersion}");
                report.WasMigrated = true;
                currentVersion = nextVersion;
                currentJson = nextJson;
            }

            normalizedJson = currentJson;
            report.FinalVersion = currentVersion;
            return true;
        }

        private static bool TryReadVersion(string json, out int version, out string error)
        {
            version = 0;
            error = string.Empty;

            try
            {
                var envelope = JsonUtility.FromJson<SaveVersionEnvelope>(json);
                if (envelope == null)
                {
                    error = "version envelope is null";
                    return false;
                }

                version = Mathf.Max(0, envelope.saveVersion);
                return true;
            }
            catch (Exception ex)
            {
                error = $"json parse exception: {ex.Message}";
                return false;
            }
        }
    }

    public sealed class SaveV0ToV1Migrator : ISaveMigrator
    {
        public int FromVersion => 0;
        public int ToVersion => 1;

        public bool TryMigrate(string inputJson, out string outputJson, out string error)
        {
            outputJson = string.Empty;
            error = string.Empty;

            try
            {
                var migrated = JsonUtility.FromJson<SaveDataV1>(inputJson);
                if (migrated == null)
                {
                    error = "legacy payload deserialized to null";
                    return false;
                }

                migrated.saveVersion = 1;
                migrated.ownedShips ??= new List<string>();
                migrated.ownedHooks ??= new List<string>();
                migrated.fishInventory ??= new List<FishInventoryEntry>();
                migrated.catchLog ??= new List<CatchLogEntry>();
                migrated.tutorialFlags ??= new TutorialFlags();
                migrated.stats ??= new SaveStats();
                migrated.progression ??= new ProgressionData();
                migrated.progression.unlockedContentIds ??= new List<string>();
                migrated.objectiveProgress ??= new ObjectiveProgressData();
                migrated.objectiveProgress.entries ??= new List<ObjectiveProgressEntry>();

                outputJson = JsonUtility.ToJson(migrated, true);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
