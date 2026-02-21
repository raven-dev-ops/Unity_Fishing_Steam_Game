using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RavenDevOps.Fishing.EditorTools
{
    public enum AssetImportAudioCategory
    {
        Unknown = 0,
        Music = 1,
        Sfx = 2,
        Dialogue = 3
    }

    public sealed class AssetImportAuditResult
    {
        public int TextureAssetsChecked;
        public int AudioAssetsChecked;
        public string ReportPath;
        public readonly List<string> Warnings = new List<string>();

        public int WarningCount => Warnings.Count;
    }

    public static class AssetImportComplianceAuditor
    {
        public const string ReportRelativeDirectory = "Artifacts/AssetImportAudit";
        public const string ReportFileName = "asset_import_audit_report.txt";

        private const float MusicQualityMin = 0.45f;
        private const float MusicQualityMax = 0.60f;
        private const float DialogueQualityMin = 0.55f;
        private const float DialogueQualityMax = 0.70f;

        public static AssetImportAuditResult Run()
        {
            return Run(BuildDefaultReportPath());
        }

        public static AssetImportAuditResult Run(string reportPath)
        {
            var result = new AssetImportAuditResult();
            AuditTextures(result);
            AuditAudio(result);
            WriteReport(reportPath, result);
            return result;
        }

        public static string BuildDefaultReportPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), ReportRelativeDirectory, ReportFileName);
        }

        public static AssetImportAudioCategory ClassifyAudioCategoryPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return AssetImportAudioCategory.Unknown;
            }

            var normalized = assetPath.Replace('\\', '/').ToLowerInvariant();
            if (normalized.Contains("/music/") || normalized.Contains("/bgm/"))
            {
                return AssetImportAudioCategory.Music;
            }

            if (normalized.Contains("/sfx/") || normalized.Contains("/fx/"))
            {
                return AssetImportAudioCategory.Sfx;
            }

            if (normalized.Contains("/vo/") || normalized.Contains("/dialogue/") || normalized.Contains("/voice/"))
            {
                return AssetImportAudioCategory.Dialogue;
            }

            return AssetImportAudioCategory.Unknown;
        }

        public static void ValidateTextureSettings(
            string assetPath,
            TextureImporterType textureType,
            bool isReadable,
            bool mipmapEnabled,
            bool sRgbTexture,
            int maxTextureSize,
            List<string> warnings)
        {
            if (warnings == null)
            {
                return;
            }

            if (isReadable)
            {
                warnings.Add($"WARN: Texture '{assetPath}' has Read/Write enabled.");
            }

            if (textureType == TextureImporterType.Sprite)
            {
                if (mipmapEnabled)
                {
                    warnings.Add($"WARN: Sprite texture '{assetPath}' has mipmaps enabled.");
                }

                if (maxTextureSize > 1024)
                {
                    warnings.Add($"WARN: Sprite texture '{assetPath}' max size {maxTextureSize} exceeds recommended 1024.");
                }

                return;
            }

            if (textureType == TextureImporterType.NormalMap)
            {
                if (sRgbTexture)
                {
                    warnings.Add($"WARN: Normal map '{assetPath}' has sRGB enabled.");
                }

                if (!mipmapEnabled)
                {
                    warnings.Add($"WARN: Normal map '{assetPath}' has mipmaps disabled.");
                }
            }
        }

        public static void ValidateAudioSettings(
            string assetPath,
            AssetImportAudioCategory category,
            AudioClipLoadType loadType,
            AudioCompressionFormat compressionFormat,
            float quality,
            bool preloadAudioData,
            float clipLengthSeconds,
            List<string> warnings)
        {
            if (warnings == null)
            {
                return;
            }

            switch (category)
            {
                case AssetImportAudioCategory.Music:
                    if (loadType != AudioClipLoadType.Streaming)
                    {
                        warnings.Add($"WARN: Music clip '{assetPath}' should use Streaming load type.");
                    }

                    if (compressionFormat != AudioCompressionFormat.Vorbis)
                    {
                        warnings.Add($"WARN: Music clip '{assetPath}' should use Vorbis compression.");
                    }

                    if (quality < MusicQualityMin || quality > MusicQualityMax)
                    {
                        warnings.Add(
                            $"WARN: Music clip '{assetPath}' quality {quality:0.00} is outside recommended range {MusicQualityMin:0.00}-{MusicQualityMax:0.00}.");
                    }

                    if (!preloadAudioData)
                    {
                        warnings.Add($"WARN: Music clip '{assetPath}' should preload audio data.");
                    }

                    break;

                case AssetImportAudioCategory.Sfx:
                    if (clipLengthSeconds >= 0f)
                    {
                        if (clipLengthSeconds < 4f)
                        {
                            if (loadType != AudioClipLoadType.DecompressOnLoad)
                            {
                                warnings.Add($"WARN: Short SFX clip '{assetPath}' should use DecompressOnLoad.");
                            }

                            if (compressionFormat != AudioCompressionFormat.ADPCM)
                            {
                                warnings.Add($"WARN: Short SFX clip '{assetPath}' should use ADPCM compression.");
                            }
                        }
                        else
                        {
                            if (loadType != AudioClipLoadType.CompressedInMemory)
                            {
                                warnings.Add($"WARN: Medium/long SFX clip '{assetPath}' should use CompressedInMemory.");
                            }

                            if (compressionFormat != AudioCompressionFormat.Vorbis)
                            {
                                warnings.Add($"WARN: Medium/long SFX clip '{assetPath}' should use Vorbis compression.");
                            }
                        }
                    }
                    else
                    {
                        warnings.Add($"WARN: SFX clip '{assetPath}' length could not be read for policy checks.");
                    }

                    break;

                case AssetImportAudioCategory.Dialogue:
                    if (loadType != AudioClipLoadType.CompressedInMemory)
                    {
                        warnings.Add($"WARN: Dialogue clip '{assetPath}' should use CompressedInMemory.");
                    }

                    if (compressionFormat != AudioCompressionFormat.Vorbis)
                    {
                        warnings.Add($"WARN: Dialogue clip '{assetPath}' should use Vorbis compression.");
                    }

                    if (quality < DialogueQualityMin || quality > DialogueQualityMax)
                    {
                        warnings.Add(
                            $"WARN: Dialogue clip '{assetPath}' quality {quality:0.00} is outside recommended range {DialogueQualityMin:0.00}-{DialogueQualityMax:0.00}.");
                    }

                    break;

                case AssetImportAudioCategory.Unknown:
                default:
                    warnings.Add($"WARN: Audio clip '{assetPath}' category is unknown. Use path folder naming (Music/SFX/VO/Dialogue).");
                    break;
            }
        }

        private static void AuditTextures(AssetImportAuditResult result)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                result.TextureAssetsChecked++;
                var platformSettings = importer.GetDefaultPlatformTextureSettings();
                ValidateTextureSettings(
                    path,
                    importer.textureType,
                    importer.isReadable,
                    importer.mipmapEnabled,
                    importer.sRGBTexture,
                    platformSettings.maxTextureSize,
                    result.Warnings);
            }
        }

        private static void AuditAudio(AssetImportAuditResult result)
        {
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    continue;
                }

                result.AudioAssetsChecked++;
                var settings = importer.defaultSampleSettings;
                var category = ClassifyAudioCategoryPath(path);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                var clipLengthSeconds = clip != null ? clip.length : -1f;
#if UNITY_6000_0_OR_NEWER
                var preloadAudioData = settings.preloadAudioData;
#else
                var preloadAudioData = importer.preloadAudioData;
#endif
                ValidateAudioSettings(
                    path,
                    category,
                    settings.loadType,
                    settings.compressionFormat,
                    settings.quality,
                    preloadAudioData,
                    clipLengthSeconds,
                    result.Warnings);
            }
        }

        private static void WriteReport(string reportPath, AssetImportAuditResult result)
        {
            var safePath = string.IsNullOrWhiteSpace(reportPath) ? BuildDefaultReportPath() : reportPath;
            var directory = Path.GetDirectoryName(safePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var builder = new StringBuilder();
            builder.AppendLine("Asset Import Compliance Audit");
            builder.AppendLine($"GeneratedUTC: {DateTime.UtcNow:O}");
            builder.AppendLine($"TextureAssetsChecked: {result.TextureAssetsChecked}");
            builder.AppendLine($"AudioAssetsChecked: {result.AudioAssetsChecked}");
            builder.AppendLine($"WarningCount: {result.WarningCount}");
            builder.AppendLine();

            if (result.WarningCount == 0)
            {
                builder.AppendLine("No warnings found.");
            }
            else
            {
                for (var i = 0; i < result.Warnings.Count; i++)
                {
                    builder.AppendLine($"- {result.Warnings[i]}");
                }
            }

            File.WriteAllText(safePath, builder.ToString());
            result.ReportPath = safePath;
        }
    }
}
