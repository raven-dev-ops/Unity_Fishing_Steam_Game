using System.Collections.Generic;
using NUnit.Framework;
using RavenDevOps.Fishing.EditorTools;
using UnityEditor;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class AssetImportComplianceAuditorTests
    {
        [Test]
        public void ClassifyAudioCategoryPath_ReturnsExpectedCategories()
        {
            Assert.That(
                AssetImportComplianceAuditor.ClassifyAudioCategoryPath("Assets/Audio/Music/theme.ogg"),
                Is.EqualTo(AssetImportAudioCategory.Music));

            Assert.That(
                AssetImportComplianceAuditor.ClassifyAudioCategoryPath("Assets/Audio/SFX/cast.wav"),
                Is.EqualTo(AssetImportAudioCategory.Sfx));

            Assert.That(
                AssetImportComplianceAuditor.ClassifyAudioCategoryPath("Assets/Audio/VO/intro.wav"),
                Is.EqualTo(AssetImportAudioCategory.Dialogue));

            Assert.That(
                AssetImportComplianceAuditor.ClassifyAudioCategoryPath("Assets/Audio/Misc/loop.wav"),
                Is.EqualTo(AssetImportAudioCategory.Unknown));
        }

        [Test]
        public void ValidateTextureSettings_FlagsReadableAndSpriteMipmaps()
        {
            var warnings = new List<string>();

            AssetImportComplianceAuditor.ValidateTextureSettings(
                "Assets/Art/UI/icon_test.png",
                TextureImporterType.Sprite,
                isReadable: true,
                mipmapEnabled: true,
                sRgbTexture: true,
                maxTextureSize: 2048,
                warnings: warnings);

            Assert.That(warnings, Has.Some.Contains("Read/Write enabled"));
            Assert.That(warnings, Has.Some.Contains("mipmaps enabled"));
            Assert.That(warnings, Has.Some.Contains("exceeds recommended 1024"));
        }

        [Test]
        public void ValidateAudioSettings_FlagsInvalidMusicConfiguration()
        {
            var warnings = new List<string>();

            AssetImportComplianceAuditor.ValidateAudioSettings(
                "Assets/Audio/Music/theme.ogg",
                AssetImportAudioCategory.Music,
                AudioClipLoadType.DecompressOnLoad,
                AudioCompressionFormat.ADPCM,
                quality: 0.20f,
                preloadAudioData: false,
                clipLengthSeconds: 60f,
                warnings: warnings);

            Assert.That(warnings, Has.Some.Contains("should use Streaming"));
            Assert.That(warnings, Has.Some.Contains("should use Vorbis"));
            Assert.That(warnings, Has.Some.Contains("outside recommended range"));
            Assert.That(warnings, Has.Some.Contains("should preload audio data"));
        }
    }
}
