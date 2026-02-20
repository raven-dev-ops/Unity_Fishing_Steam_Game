using NUnit.Framework;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class SaveDataSerializationTests
    {
        [Test]
        public void DeserializeFixture_PopulatesExpectedFields()
        {
            const string json = "{\"saveVersion\":1,\"copecs\":250,\"equippedShipId\":\"ship_lv2\",\"equippedHookId\":\"hook_lv2\",\"ownedShips\":[\"ship_lv1\",\"ship_lv2\"],\"ownedHooks\":[\"hook_lv1\",\"hook_lv2\"],\"fishInventory\":[{\"fishId\":\"fish_cod\",\"distanceTier\":2,\"count\":3}],\"catchLog\":[{\"fishId\":\"fish_cod\",\"distanceTier\":2,\"weightKg\":1.3,\"valueCopecs\":12,\"timestampUtc\":\"2026-02-20T10:00:00.0000000Z\",\"sessionId\":\"abc123\",\"landed\":true,\"failReason\":\"\"}],\"tutorialFlags\":{\"tutorialSeen\":true},\"careerStartLocalDate\":\"2026-02-01\",\"lastLoginLocalDate\":\"2026-02-20\",\"stats\":{\"totalFishCaught\":3,\"farthestDistanceTier\":2,\"totalTrips\":1},\"progression\":{\"level\":2,\"totalXp\":140,\"xpIntoLevel\":40,\"xpToNextLevel\":150,\"unlockedContentIds\":[\"hook_lv2\"],\"lastUnlockId\":\"hook_lv2\"}}";

            var save = JsonUtility.FromJson<SaveDataV1>(json);

            Assert.That(save, Is.Not.Null);
            Assert.That(save.copecs, Is.EqualTo(250));
            Assert.That(save.equippedShipId, Is.EqualTo("ship_lv2"));
            Assert.That(save.ownedShips.Count, Is.EqualTo(2));
            Assert.That(save.fishInventory.Count, Is.EqualTo(1));
            Assert.That(save.catchLog.Count, Is.EqualTo(1));
            Assert.That(save.stats.totalFishCaught, Is.EqualTo(3));
            Assert.That(save.progression.level, Is.EqualTo(2));
            Assert.That(save.progression.totalXp, Is.EqualTo(140));
            Assert.That(save.progression.unlockedContentIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void DeserializeLegacyFixture_DoesNotThrowOnMissingFields()
        {
            const string legacyJson = "{\"saveVersion\":1,\"copecs\":42}";

            var save = JsonUtility.FromJson<SaveDataV1>(legacyJson);

            Assert.That(save, Is.Not.Null);
            Assert.That(save.saveVersion, Is.EqualTo(1));
            Assert.That(save.copecs, Is.EqualTo(42));
            Assert.That(save.progression, Is.Not.Null);
            Assert.That(save.tutorialFlags, Is.Not.Null);
        }
    }
}
