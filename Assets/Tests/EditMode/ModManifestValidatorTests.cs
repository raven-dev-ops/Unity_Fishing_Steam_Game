using System.Collections.Generic;
using NUnit.Framework;
using RavenDevOps.Fishing.Tools;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class ModManifestValidatorTests
    {
        [Test]
        public void Validate_ReturnsNoErrors_ForValidManifest()
        {
            var manifest = new ModManifestV1
            {
                schemaVersion = "1.0",
                modId = "coastal_pack",
                modVersion = "1.2.0",
                displayName = "Coastal Pack",
                author = "Raven Dev Ops",
                description = "Adds coastal fish set.",
                minGameVersion = "1.0.0",
                maxGameVersion = "1.5.0",
                dataCatalogs = new List<string> { "Data/fish_pack.json" },
                assetOverrides = new List<string> { "Sprites/fish_icon.png", "Audio/splash.ogg" }
            };

            var messages = ModManifestValidator.Validate(manifest);

            Assert.That(ModManifestValidator.CountErrors(messages), Is.EqualTo(0));
        }

        [Test]
        public void Validate_ReturnsErrors_ForInvalidManifestFields()
        {
            var manifest = new ModManifestV1
            {
                schemaVersion = "2.0",
                modId = "BAD ID",
                modVersion = "1.0",
                displayName = string.Empty,
                minGameVersion = "2.0.0",
                maxGameVersion = "1.0.0",
                dataCatalogs = new List<string> { "../escape.txt", "Data/fish_pack.txt", "Data/fish_pack.txt" },
                assetOverrides = new List<string> { "C:/mods/hack.exe" }
            };

            var messages = ModManifestValidator.Validate(manifest);

            Assert.That(messages, Has.Some.Contains("Unsupported schemaVersion"));
            Assert.That(messages, Has.Some.Contains("must match pattern"));
            Assert.That(messages, Has.Some.Contains("modVersion"));
            Assert.That(messages, Has.Some.Contains("displayName is required"));
            Assert.That(messages, Has.Some.Contains("minGameVersion cannot be greater"));
            Assert.That(messages, Has.Some.Contains("must be a safe relative path"));
            Assert.That(messages, Has.Some.Contains("Duplicate dataCatalogs entry"));
            Assert.That(ModManifestValidator.CountErrors(messages), Is.GreaterThan(0));
        }

        [Test]
        public void Validate_ReturnsDuplicateIdError_WhenKnownIdsContainsModId()
        {
            var manifest = new ModManifestV1
            {
                schemaVersion = "1.0",
                modId = "coastal_pack",
                modVersion = "1.0.0",
                displayName = "Coastal Pack",
                dataCatalogs = new List<string> { "Data/fish_pack.json" },
                assetOverrides = new List<string> { "Audio/splash.ogg" }
            };

            var knownIds = new HashSet<string> { "coastal_pack" };
            var messages = ModManifestValidator.Validate(manifest, knownIds);

            Assert.That(messages, Has.Some.Contains("Duplicate modId"));
        }

        [Test]
        public void TryParseJson_ParsesValidManifestJson()
        {
            const string json = "{ \"schemaVersion\": \"1.0\", \"modId\": \"coastal_pack\", \"modVersion\": \"1.0.0\", \"displayName\": \"Coastal Pack\" }";

            var parsed = ModManifestValidator.TryParseJson(json, out var manifest, out var error);

            Assert.That(parsed, Is.True, error);
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.modId, Is.EqualTo("coastal_pack"));
        }
    }
}
