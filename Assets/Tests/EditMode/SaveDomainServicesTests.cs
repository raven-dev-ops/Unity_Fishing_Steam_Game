using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class SaveDomainServicesTests
    {
        [Test]
        public void AtomicJsonPersistenceAdapter_ReplacesExistingFile_AndCleansIntermediateArtifacts()
        {
            var fileSystem = new InMemorySaveFileSystem(@"C:\MemorySaveProfile");
            var savePath = Path.Combine(fileSystem.PersistentDataPath, "save_v1.json");
            fileSystem.WriteAllText(savePath, "{\"copecs\":1}");

            var adapter = new AtomicJsonSavePersistenceAdapter();
            var data = new SaveDataV1
            {
                copecs = 42
            };

            var ok = adapter.TryPersist(savePath, data, fileSystem, out var failureReason);

            Assert.That(ok, Is.True, $"Persist should succeed. Reason: {failureReason}");
            Assert.That(fileSystem.FileExists(savePath), Is.True);
            Assert.That(fileSystem.FileExists(savePath + ".tmp"), Is.False);
            Assert.That(fileSystem.FileExists(savePath + ".bak"), Is.False);

            var persisted = JsonUtility.FromJson<SaveDataV1>(fileSystem.ReadAllText(savePath));
            Assert.That(persisted, Is.Not.Null);
            Assert.That(persisted.copecs, Is.EqualTo(42));
        }

        [Test]
        public void SaveWriteThrottle_DelaysFlushUntilIntervalIsSatisfied()
        {
            var throttle = new SaveWriteThrottle(2f);

            Assert.That(throttle.Request(0f, forceImmediate: false), Is.True);
            throttle.MarkPersisted(0f);

            Assert.That(throttle.Request(0.5f, forceImmediate: false), Is.False);
            Assert.That(throttle.HasPendingRequest, Is.True);
            Assert.That(throttle.TryFlush(1.9f), Is.False);
            Assert.That(throttle.TryFlush(2.1f), Is.True);
        }

        [Test]
        public void SaveProgressionService_AppliesXpAndUnlocksAcrossLevelThresholds()
        {
            var service = new SaveProgressionService();
            var thresholds = new List<int>(ProgressionRules.Defaults);
            var unlocks = new List<ProgressionUnlockDefinition>();
            service.NormalizeConfig(thresholds, unlocks);

            var saveData = new SaveDataV1();
            var didLevelUp = service.ApplyProgressionXp(
                saveData,
                xpAmount: 300,
                thresholds,
                unlocks,
                out var previousLevel,
                out var newLevel);

            Assert.That(didLevelUp, Is.True);
            Assert.That(previousLevel, Is.EqualTo(1));
            Assert.That(newLevel, Is.GreaterThanOrEqualTo(3));
            Assert.That(saveData.progression.unlockedContentIds, Contains.Item("hook_lv2"));
            Assert.That(saveData.progression.unlockedContentIds, Contains.Item("ship_lv2"));
        }

        [Test]
        public void SaveDomainMutationService_TutorialFlagsAndProfileResetRemainDeterministic()
        {
            var service = new SaveDomainMutationService();
            var saveData = new SaveDataV1
            {
                copecs = 250
            };
            saveData.stats.totalFishCaught = 4;
            saveData.stats.totalTrips = 3;
            saveData.progression.totalXp = 425;
            saveData.progression.level = 3;
            saveData.progression.unlockedContentIds.Add("hook_lv2");
            saveData.objectiveProgress.entries.Add(new ObjectiveProgressEntry
            {
                id = "obj_test",
                currentCount = 2,
                completed = true
            });

            Assert.That(service.RequestIntroTutorialReplay(saveData), Is.True);
            Assert.That(service.ShouldRunIntroTutorial(saveData), Is.True);
            Assert.That(service.MarkIntroTutorialStarted(saveData), Is.True);
            Assert.That(saveData.tutorialFlags.introTutorialReplayRequested, Is.False);

            Assert.That(service.RequestFishingLoopTutorialReplay(saveData), Is.True);
            Assert.That(service.CompleteFishingLoopTutorial(saveData, skipped: true), Is.True);
            Assert.That(saveData.tutorialFlags.fishingLoopTutorialCompleted, Is.True);
            Assert.That(saveData.tutorialFlags.fishingLoopTutorialSkipped, Is.True);

            var thresholds = new List<int> { 0, 100, 250 };
            Assert.That(service.ResetProfileStats(saveData, thresholds), Is.True);
            Assert.That(saveData.copecs, Is.EqualTo(0));
            Assert.That(saveData.stats.totalFishCaught, Is.EqualTo(0));
            Assert.That(saveData.stats.totalTrips, Is.EqualTo(0));
            Assert.That(saveData.progression.level, Is.EqualTo(1));
            Assert.That(saveData.progression.totalXp, Is.EqualTo(0));
            Assert.That(saveData.progression.unlockedContentIds, Is.Empty);
            Assert.That(saveData.objectiveProgress.entries[0].currentCount, Is.EqualTo(0));
            Assert.That(saveData.objectiveProgress.entries[0].completed, Is.False);
        }

        private sealed class InMemorySaveFileSystem : ISaveFileSystem
        {
            private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

            public InMemorySaveFileSystem(string persistentDataPath)
            {
                PersistentDataPath = persistentDataPath;
            }

            public string PersistentDataPath { get; }

            public bool FileExists(string path) => _files.ContainsKey(Normalize(path));

            public string ReadAllText(string path)
            {
                var key = Normalize(path);
                if (!_files.TryGetValue(key, out var content))
                {
                    throw new FileNotFoundException(path);
                }

                return content;
            }

            public void WriteAllText(string path, string content)
            {
                _files[Normalize(path)] = content ?? string.Empty;
            }

            public void DeleteFile(string path)
            {
                _files.Remove(Normalize(path));
            }

            public void MoveFile(string sourcePath, string destinationPath)
            {
                var sourceKey = Normalize(sourcePath);
                if (!_files.TryGetValue(sourceKey, out var content))
                {
                    throw new FileNotFoundException(sourcePath);
                }

                _files.Remove(sourceKey);
                _files[Normalize(destinationPath)] = content;
            }

            public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
            {
                var sourceKey = Normalize(sourcePath);
                if (!_files.TryGetValue(sourceKey, out var content))
                {
                    throw new FileNotFoundException(sourcePath);
                }

                var destinationKey = Normalize(destinationPath);
                if (!overwrite && _files.ContainsKey(destinationKey))
                {
                    throw new IOException($"Destination exists: {destinationPath}");
                }

                _files[destinationKey] = content;
            }

            public void ReplaceFile(string sourcePath, string destinationPath, string backupPath)
            {
                var sourceKey = Normalize(sourcePath);
                if (!_files.TryGetValue(sourceKey, out var sourceContent))
                {
                    throw new FileNotFoundException(sourcePath);
                }

                var destinationKey = Normalize(destinationPath);
                if (_files.TryGetValue(destinationKey, out var destinationContent) && !string.IsNullOrWhiteSpace(backupPath))
                {
                    _files[Normalize(backupPath)] = destinationContent;
                }

                _files[destinationKey] = sourceContent;
                _files.Remove(sourceKey);
            }

            public void EnsureDirectory(string path)
            {
                // No-op for in-memory implementation.
            }

            private static string Normalize(string path)
            {
                return (path ?? string.Empty).Replace('/', '\\').ToLowerInvariant();
            }
        }
    }
}
