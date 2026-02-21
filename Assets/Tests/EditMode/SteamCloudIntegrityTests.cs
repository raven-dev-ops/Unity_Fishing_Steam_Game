using NUnit.Framework;
using RavenDevOps.Fishing.Steam;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class SteamCloudIntegrityTests
    {
        [Test]
        public void ComputeSha256_IsDeterministic()
        {
            var hash = SteamCloudIntegrity.ComputeSha256("abc");
            Assert.That(hash, Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        }

        [Test]
        public void TryParseManifest_ValidJson_ReturnsManifest()
        {
            var json = "{\"savedAtUtc\":\"2026-02-21T00:00:00Z\",\"contentSha256\":\"abcd\",\"policy\":\"newest-wins\"}";
            var ok = SteamCloudIntegrity.TryParseManifest(json, out var manifest);

            Assert.That(ok, Is.True);
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.contentSha256, Is.EqualTo("abcd"));
        }

        [Test]
        public void TryParseManifest_InvalidJson_ReturnsFalse()
        {
            var ok = SteamCloudIntegrity.TryParseManifest("{not-json}", out var manifest);

            Assert.That(ok, Is.False);
            Assert.That(manifest, Is.Null);
        }

        [Test]
        public void TryValidatePayload_HashMatch_ReturnsTrue()
        {
            const string payload = "{\"value\":1}";
            var manifest = new CloudSaveManifestData
            {
                contentSha256 = SteamCloudIntegrity.ComputeSha256(payload),
                policy = "newest-wins"
            };

            var ok = SteamCloudIntegrity.TryValidatePayload(payload, manifest, out var failure);

            Assert.That(ok, Is.True);
            Assert.That(failure, Is.EqualTo(CloudIntegrityFailure.None));
        }

        [Test]
        public void TryValidatePayload_HashMismatch_ReturnsFalse()
        {
            const string payload = "{\"value\":1}";
            var manifest = new CloudSaveManifestData
            {
                contentSha256 = SteamCloudIntegrity.ComputeSha256("{\"value\":2}"),
                policy = "newest-wins"
            };

            var ok = SteamCloudIntegrity.TryValidatePayload(payload, manifest, out var failure);

            Assert.That(ok, Is.False);
            Assert.That(failure, Is.EqualTo(CloudIntegrityFailure.HashMismatch));
        }

        [Test]
        public void TryValidatePayload_MissingHash_ReturnsFalse()
        {
            var manifest = new CloudSaveManifestData
            {
                contentSha256 = string.Empty
            };

            var ok = SteamCloudIntegrity.TryValidatePayload("{}", manifest, out var failure);

            Assert.That(ok, Is.False);
            Assert.That(failure, Is.EqualTo(CloudIntegrityFailure.ManifestHashMissing));
        }
    }
}
