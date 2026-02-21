using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    [Serializable]
    internal sealed class SaveMigrationRehearsalManifest
    {
        public List<SaveMigrationRehearsalCase> cases = new List<SaveMigrationRehearsalCase>();
    }

    [Serializable]
    internal sealed class SaveMigrationRehearsalCase
    {
        public string id = string.Empty;
        public string relativePath = string.Empty;
        public bool expectSuccess = true;
        public int expectedSourceVersion = 0;
        public int expectedFinalVersion = 1;
        public string expectFailureContains = string.Empty;
    }

    public sealed class SaveMigrationRehearsalTests
    {
        private const string FixtureDirectory = "Tests/EditMode/Fixtures/Rehearsal";
        private const string ManifestFileName = "save_rehearsal_manifest.json";

        [Test]
        public void RehearsalCorpus_ExpectedOutcomes()
        {
            var manifestPath = ResolveFixturePath(ManifestFileName);
            Assert.That(File.Exists(manifestPath), Is.True, $"Missing rehearsal manifest: {manifestPath}");

            var manifestJson = File.ReadAllText(manifestPath);
            var manifest = JsonUtility.FromJson<SaveMigrationRehearsalManifest>(manifestJson);
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.cases, Is.Not.Null);
            Assert.That(manifest.cases.Count, Is.GreaterThanOrEqualTo(5));

            foreach (var rehearsalCase in manifest.cases)
            {
                Assert.That(rehearsalCase, Is.Not.Null);
                Assert.That(rehearsalCase.id, Is.Not.Empty);
                Assert.That(rehearsalCase.relativePath, Is.Not.Empty);

                var fixturePath = ResolveFixturePath(rehearsalCase.relativePath);
                Assert.That(File.Exists(fixturePath), Is.True, $"Missing rehearsal fixture: {fixturePath}");
                var rawJson = File.ReadAllText(fixturePath);

                var ok = SaveMigrationPipeline.TryPrepareForLoad(rawJson, out var normalized, out var report);
                Assert.That(ok, Is.EqualTo(rehearsalCase.expectSuccess), $"Unexpected migration outcome for case '{rehearsalCase.id}'.");
                Assert.That(report, Is.Not.Null);

                if (rehearsalCase.expectSuccess)
                {
                    Assert.That(report.SourceVersion, Is.EqualTo(rehearsalCase.expectedSourceVersion), $"Unexpected source version for case '{rehearsalCase.id}'.");
                    Assert.That(report.FinalVersion, Is.EqualTo(rehearsalCase.expectedFinalVersion), $"Unexpected final version for case '{rehearsalCase.id}'.");
                    Assert.That(string.IsNullOrWhiteSpace(normalized), Is.False, $"Normalized payload should not be empty for case '{rehearsalCase.id}'.");

                    var idempotentOk = SaveMigrationPipeline.TryPrepareForLoad(normalized, out _, out var idempotentReport);
                    Assert.That(idempotentOk, Is.True, $"Idempotent pass failed for case '{rehearsalCase.id}'.");
                    Assert.That(idempotentReport.WasMigrated, Is.False, $"Idempotent pass should not re-migrate for case '{rehearsalCase.id}'.");
                }
                else
                {
                    Assert.That(report.FailureReason, Is.Not.Empty, $"Failure reason missing for case '{rehearsalCase.id}'.");
                    if (!string.IsNullOrWhiteSpace(rehearsalCase.expectFailureContains))
                    {
                        Assert.That(report.FailureReason, Does.Contain(rehearsalCase.expectFailureContains), $"Failure reason mismatch for case '{rehearsalCase.id}'.");
                    }
                }
            }
        }

        [Test]
        public void RollbackDrill_MigrationFailure_PreservesSourceWithCorruptBackup()
        {
            var manager = (SaveManager)FormatterServices.GetUninitializedObject(typeof(SaveManager));
            var fileSystem = new InMemorySaveFileSystem(@"C:\MemorySaveProfile");
            var now = new DateTime(2026, 2, 21, 4, 0, 0, DateTimeKind.Local);
            var timeProvider = new FixedTimeProvider(now);

            SetPrivateField(manager, "_fileSystem", fileSystem);
            SetPrivateField(manager, "_timeProvider", timeProvider);

            var savePath = Path.Combine(fileSystem.PersistentDataPath, "save_v1.json");
            fileSystem.WriteAllText(savePath, "{bad-json");

            var loaded = default(SaveDataV1);
            var ok = InvokeTryLoadExisting(manager, out loaded);

            Assert.That(ok, Is.False);
            Assert.That(loaded, Is.Null);
            Assert.That(fileSystem.FileExists(savePath), Is.True);
            Assert.That(fileSystem.CopiedTargets.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(fileSystem.CopiedTargets[0], Does.Contain(".corrupt_"));
        }

        private static string ResolveFixturePath(string relativePath)
        {
            return Path.Combine(Application.dataPath, FixtureDirectory, relativePath);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private static bool InvokeTryLoadExisting(SaveManager manager, out SaveDataV1 loaded)
        {
            var method = typeof(SaveManager).GetMethod("TryLoadExisting", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Expected SaveManager.TryLoadExisting private method.");
            var args = new object[] { null };
            var result = method.Invoke(manager, args);
            loaded = args[0] as SaveDataV1;
            return result is bool b && b;
        }

        private sealed class FixedTimeProvider : ITimeProvider
        {
            private readonly DateTime _localNow;

            public FixedTimeProvider(DateTime localNow)
            {
                _localNow = localNow;
            }

            public DateTime LocalNow => _localNow;
            public DateTime UtcNow => _localNow.ToUniversalTime();
            public float RealtimeSinceStartup => 0f;
        }

        private sealed class InMemorySaveFileSystem : ISaveFileSystem
        {
            private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

            public InMemorySaveFileSystem(string persistentDataPath)
            {
                PersistentDataPath = persistentDataPath;
            }

            public string PersistentDataPath { get; }
            public List<string> CopiedTargets { get; } = new List<string>();

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
                CopiedTargets.Add(destinationPath);
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
