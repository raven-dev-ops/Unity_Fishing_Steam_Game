using System.IO;
using NUnit.Framework;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class SaveMigrationPipelineTests
    {
        [Test]
        public void TryPrepareForLoad_CurrentVersion_NoMigration()
        {
            const string json = "{\"saveVersion\":1,\"copecs\":42}";

            var ok = SaveMigrationPipeline.TryPrepareForLoad(json, out var normalized, out var report);

            Assert.That(ok, Is.True);
            Assert.That(normalized, Is.EqualTo(json));
            Assert.That(report.SourceVersion, Is.EqualTo(1));
            Assert.That(report.FinalVersion, Is.EqualTo(1));
            Assert.That(report.WasMigrated, Is.False);
            Assert.That(report.AppliedSteps.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryPrepareForLoad_LegacyPayload_MigratesToV1()
        {
            const string legacyJson = "{\"copecs\":120,\"ownedShips\":[\"ship_lv1\"],\"fishInventory\":[{\"fishId\":\"fish_cod\",\"distanceTier\":1,\"count\":2}]}";

            var ok = SaveMigrationPipeline.TryPrepareForLoad(legacyJson, out var normalized, out var report);

            Assert.That(ok, Is.True);
            Assert.That(report.SourceVersion, Is.EqualTo(0));
            Assert.That(report.FinalVersion, Is.EqualTo(1));
            Assert.That(report.WasMigrated, Is.True);
            Assert.That(report.AppliedSteps, Has.One.EqualTo("v0->v1"));

            var migrated = JsonUtility.FromJson<SaveDataV1>(normalized);
            Assert.That(migrated, Is.Not.Null);
            Assert.That(migrated.saveVersion, Is.EqualTo(1));
            Assert.That(migrated.copecs, Is.EqualTo(120));
            Assert.That(migrated.progression, Is.Not.Null);
            Assert.That(migrated.objectiveProgress, Is.Not.Null);
            Assert.That(migrated.tutorialFlags, Is.Not.Null);
            Assert.That(migrated.stats, Is.Not.Null);
        }

        [Test]
        public void TryPrepareForLoad_InvalidJson_FailsWithReason()
        {
            var ok = SaveMigrationPipeline.TryPrepareForLoad("{bad-json", out _, out var report);

            Assert.That(ok, Is.False);
            Assert.That(report.FailureReason, Is.Not.Empty);
        }

        [Test]
        public void TryPrepareForLoad_FutureVersion_FailsSafely()
        {
            const string json = "{\"saveVersion\":99,\"copecs\":10}";

            var ok = SaveMigrationPipeline.TryPrepareForLoad(json, out _, out var report);

            Assert.That(ok, Is.False);
            Assert.That(report.FailureReason, Does.Contain("newer than supported"));
        }

        [Test]
        public void TryPrepareForLoad_MigratedPayload_IsIdempotentOnSecondPass()
        {
            const string legacyJson = "{\"copecs\":88}";
            var first = SaveMigrationPipeline.TryPrepareForLoad(legacyJson, out var normalized, out var reportFirst);
            Assert.That(first, Is.True);
            Assert.That(reportFirst.WasMigrated, Is.True);

            var second = SaveMigrationPipeline.TryPrepareForLoad(normalized, out _, out var reportSecond);
            Assert.That(second, Is.True);
            Assert.That(reportSecond.WasMigrated, Is.False);
            Assert.That(reportSecond.SourceVersion, Is.EqualTo(1));
            Assert.That(reportSecond.FinalVersion, Is.EqualTo(1));
        }

        [Test]
        public void TryPrepareForLoad_FixtureLegacyPayload_MatchesExpectedSnapshot()
        {
            var legacyPath = Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/save_v0_legacy.json");
            var expectedPath = Path.Combine(Application.dataPath, "Tests/EditMode/Fixtures/save_v1_migrated_snapshot.json");
            var legacyJson = File.ReadAllText(legacyPath);
            var expectedJson = File.ReadAllText(expectedPath);

            var ok = SaveMigrationPipeline.TryPrepareForLoad(legacyJson, out var normalized, out var report);

            Assert.That(ok, Is.True);
            Assert.That(report.WasMigrated, Is.True);

            var migrated = JsonUtility.FromJson<SaveDataV1>(normalized);
            var expected = JsonUtility.FromJson<SaveDataV1>(expectedJson);
            Assert.That(migrated, Is.Not.Null);
            Assert.That(expected, Is.Not.Null);
            Assert.That(migrated.saveVersion, Is.EqualTo(expected.saveVersion));
            Assert.That(migrated.copecs, Is.EqualTo(expected.copecs));
            Assert.That(migrated.ownedShips.Count, Is.EqualTo(expected.ownedShips.Count));
            Assert.That(migrated.ownedHooks.Count, Is.EqualTo(expected.ownedHooks.Count));
            Assert.That(migrated.fishInventory.Count, Is.EqualTo(expected.fishInventory.Count));
            Assert.That(migrated.progression, Is.Not.Null);
            Assert.That(migrated.objectiveProgress, Is.Not.Null);
            Assert.That(migrated.tutorialFlags, Is.Not.Null);
            Assert.That(migrated.stats, Is.Not.Null);
        }
    }
}
