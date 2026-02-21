#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace RavenDevOps.Fishing.EditorTools
{
    public static class SpriteSheetAtlasWorkflow
    {
        private const string ArtManifestPath = "Assets/Art/Source/art_manifest.json";
        private const string SheetOutputRoot = "Assets/Art/Sheets/Icons";
        private const string AtlasOutputRoot = "Assets/Art/Atlases/Icons";
        private const int SheetPaddingPixels = 8;
        private const int SpritePixelsPerUnit = 100;
        private const int AtlasMaxTextureSize = 4096;
        private const string SheetFilePrefix = "icons_";
        private const string SheetFileSuffix = "_sheet_v01";
        private const string SheetFilePattern = "icons_*_sheet_v01.png";
        private const string AtlasFilePattern = "icons_*.spriteatlas";
        private const string LegacyAtlasFilePattern = "icons_*.spriteatlasv2";

        private static MethodInfo _imageConversionLoadImageMethod;
        private static MethodInfo _imageConversionEncodeToPngMethod;
        private static bool _imageConversionLookupCompleted;

        [Serializable]
        private sealed class ArtManifest
        {
            public List<ArtManifestEntry> entries = new List<ArtManifestEntry>();
        }

        [Serializable]
        private sealed class ArtManifestEntry
        {
            public string id = string.Empty;
            public string category = string.Empty;
            public string type = string.Empty;
            public string name = string.Empty;
            public string path = string.Empty;
        }

        private sealed class SourceIcon
        {
            public string spriteName = string.Empty;
            public Texture2D texture;
        }

        private readonly struct SpriteSlice
        {
            public SpriteSlice(string name, Rect rect)
            {
                Name = name;
                Rect = rect;
            }

            public string Name { get; }
            public Rect Rect { get; }
        }

        [MenuItem("Raven/Art/Rebuild Source Icon Sheets + Atlases")]
        public static void RebuildFromMenu()
        {
            if (!RebuildSheetsAndAtlases())
            {
                Debug.LogError("Sprite sheet + atlas rebuild failed. Check console for details.");
            }
        }

        public static void RebuildSheetsAndAtlasesBatchMode()
        {
            if (!RebuildSheetsAndAtlases())
            {
                throw new BuildFailedException("Sprite sheet + atlas rebuild failed.");
            }
        }

        public static bool RebuildSheetsAndAtlases()
        {
            if (!TryLoadManifest(ArtManifestPath, out var manifest))
            {
                return false;
            }

            var iconEntries = manifest.entries
                .Where(entry => entry != null
                    && string.Equals(entry.type, "icon", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(entry.path))
                .OrderBy(entry => entry.category ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (iconEntries.Count == 0)
            {
                Debug.LogWarning("SpriteSheetAtlasWorkflow: no icon entries were found in art manifest.");
                return true;
            }

            Directory.CreateDirectory(ToAbsolutePath(SheetOutputRoot));
            Directory.CreateDirectory(ToAbsolutePath(AtlasOutputRoot));

            var generatedSheetPaths = new List<string>();
            var generatedAtlasPaths = new List<string>();
            var groupedByCategory = iconEntries
                .GroupBy(entry => NormalizeCategory(entry.category), StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var categoryGroup in groupedByCategory)
            {
                if (!TryBuildSheetForCategory(categoryGroup.Key, categoryGroup.ToList(), out var sheetAssetPath))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(sheetAssetPath))
                {
                    generatedSheetPaths.Add(sheetAssetPath);
                    generatedAtlasPaths.Add(BuildAtlasAssetPath(categoryGroup.Key));
                }
            }

            for (var i = 0; i < generatedSheetPaths.Count; i++)
            {
                BuildOrUpdateAtlas(generatedSheetPaths[i]);
            }

            CleanupStaleGeneratedAssets(generatedSheetPaths, generatedAtlasPaths);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log(
                $"SpriteSheetAtlasWorkflow: generated {generatedSheetPaths.Count} sheet(s) and atlas asset(s) under '{SheetOutputRoot}' and '{AtlasOutputRoot}'.");
            return true;
        }

        public static string NormalizeCategory(string rawCategory)
        {
            if (string.IsNullOrWhiteSpace(rawCategory))
            {
                return "uncategorized";
            }

            var compact = rawCategory.Trim().Replace(' ', '_').Replace('-', '_');
            return compact.ToLowerInvariant();
        }

        public static string BuildSheetAssetPath(string normalizedCategory)
        {
            var safeCategory = NormalizeCategory(normalizedCategory);
            return $"{SheetOutputRoot}/icons_{safeCategory}_sheet_v01.png";
        }

        public static string BuildAtlasAssetPath(string normalizedCategory)
        {
            var safeCategory = NormalizeCategory(normalizedCategory);
            return $"{AtlasOutputRoot}/icons_{safeCategory}.spriteatlas";
        }

        public static Vector2Int ResolveGrid(int spriteCount)
        {
            if (spriteCount <= 0)
            {
                return Vector2Int.one;
            }

            var columns = Mathf.CeilToInt(Mathf.Sqrt(spriteCount));
            var rows = Mathf.CeilToInt(spriteCount / Mathf.Max(1f, columns));
            return new Vector2Int(Mathf.Max(1, columns), Mathf.Max(1, rows));
        }

        private static bool TryBuildSheetForCategory(
            string normalizedCategory,
            List<ArtManifestEntry> entries,
            out string sheetAssetPath)
        {
            sheetAssetPath = string.Empty;
            var icons = new List<SourceIcon>(entries.Count);
            var usedSpriteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var absolutePath = ToAbsolutePath(entry.path);
                    if (!File.Exists(absolutePath))
                    {
                        Debug.LogWarning($"SpriteSheetAtlasWorkflow: skipping missing icon asset '{entry.path}'.");
                        continue;
                    }

                    if (!TryDecodeTexture(absolutePath, out var texture))
                    {
                        Debug.LogWarning($"SpriteSheetAtlasWorkflow: failed to decode '{entry.path}'.");
                        continue;
                    }

                    icons.Add(new SourceIcon
                    {
                        spriteName = EnsureUniqueSpriteName(
                            SanitizeSpriteName(entry.id, entry.name),
                            usedSpriteNames),
                        texture = texture
                    });
                }

                if (icons.Count == 0)
                {
                    Debug.LogWarning($"SpriteSheetAtlasWorkflow: no readable icons for category '{normalizedCategory}', skipping.");
                    return true;
                }

                var maxWidth = icons.Max(icon => icon.texture.width);
                var maxHeight = icons.Max(icon => icon.texture.height);
                var grid = ResolveGrid(icons.Count);
                var sheetWidth = grid.x * maxWidth + (grid.x + 1) * SheetPaddingPixels;
                var sheetHeight = grid.y * maxHeight + (grid.y + 1) * SheetPaddingPixels;

                var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGBA32, mipChain: false);
                var clearPixels = new Color32[sheetWidth * sheetHeight];
                for (var i = 0; i < clearPixels.Length; i++)
                {
                    clearPixels[i] = new Color32(0, 0, 0, 0);
                }

                sheet.SetPixels32(clearPixels);

                var slices = new List<SpriteSlice>(icons.Count);
                for (var i = 0; i < icons.Count; i++)
                {
                    var icon = icons[i];
                    var col = i % grid.x;
                    var row = i / grid.x;

                    var x = SheetPaddingPixels + col * (maxWidth + SheetPaddingPixels);
                    var yTop = SheetPaddingPixels + row * (maxHeight + SheetPaddingPixels);
                    var y = sheetHeight - yTop - icon.texture.height;

                    sheet.SetPixels32(x, y, icon.texture.width, icon.texture.height, icon.texture.GetPixels32());
                    slices.Add(new SpriteSlice(icon.spriteName, new Rect(x, y, icon.texture.width, icon.texture.height)));
                }

                sheet.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                if (!TryEncodePng(sheet, out var sheetBytes))
                {
                    Debug.LogError("SpriteSheetAtlasWorkflow: PNG encode failed because ImageConversion module is unavailable.");
                    return false;
                }

                sheetAssetPath = BuildSheetAssetPath(normalizedCategory);
                var absoluteSheetPath = ToAbsolutePath(sheetAssetPath);
                var absoluteSheetDirectory = Path.GetDirectoryName(absoluteSheetPath);
                if (!string.IsNullOrWhiteSpace(absoluteSheetDirectory))
                {
                    Directory.CreateDirectory(absoluteSheetDirectory);
                }

                File.WriteAllBytes(absoluteSheetPath, sheetBytes);
                AssetDatabase.ImportAsset(sheetAssetPath, ImportAssetOptions.ForceUpdate);
                ConfigureSheetImporter(sheetAssetPath, slices);
                return true;
            }
            finally
            {
                for (var i = 0; i < icons.Count; i++)
                {
                    if (icons[i].texture != null)
                    {
                        UnityEngine.Object.DestroyImmediate(icons[i].texture);
                    }
                }
            }
        }

        private static void ConfigureSheetImporter(string sheetAssetPath, List<SpriteSlice> slices)
        {
            var importer = AssetImporter.GetAtPath(sheetAssetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"SpriteSheetAtlasWorkflow: texture importer missing for '{sheetAssetPath}'.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.sRGBTexture = true;
            importer.spritePixelsPerUnit = SpritePixelsPerUnit;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            var maxDimension = Mathf.Max(importer.maxTextureSize, AtlasMaxTextureSize);
            var platform = importer.GetDefaultPlatformTextureSettings();
            platform.maxTextureSize = maxDimension;
            importer.SetPlatformTextureSettings(platform);

            var spriteMetaData = new SpriteMetaData[slices.Count];
            for (var i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                spriteMetaData[i] = new SpriteMetaData
                {
                    name = slice.Name,
                    rect = slice.Rect,
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero
                };
            }

#pragma warning disable CS0618
            importer.spritesheet = spriteMetaData;
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }

        private static void BuildOrUpdateAtlas(string sheetAssetPath)
        {
            var normalizedCategory = ExtractCategoryFromSheetPath(sheetAssetPath);
            var atlasAssetPath = BuildAtlasAssetPath(normalizedCategory);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasAssetPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                var absoluteAtlasPath = ToAbsolutePath(atlasAssetPath);
                var atlasDirectory = Path.GetDirectoryName(absoluteAtlasPath);
                if (!string.IsNullOrWhiteSpace(atlasDirectory))
                {
                    Directory.CreateDirectory(atlasDirectory);
                }

                AssetDatabase.CreateAsset(atlas, atlasAssetPath);
            }

            var packing = atlas.GetPackingSettings();
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            packing.padding = 4;
            atlas.SetPackingSettings(packing);

            var texture = atlas.GetTextureSettings();
            texture.generateMipMaps = false;
            texture.readable = false;
            texture.sRGB = true;
            texture.filterMode = FilterMode.Bilinear;
            atlas.SetTextureSettings(texture);

            var platform = atlas.GetPlatformSettings("Standalone");
            platform.overridden = true;
            platform.maxTextureSize = AtlasMaxTextureSize;
            platform.textureCompression = TextureImporterCompression.Compressed;
            atlas.SetPlatformSettings(platform);

            var existingPackables = atlas.GetPackables();
            if (existingPackables != null && existingPackables.Length > 0)
            {
                atlas.Remove(existingPackables);
            }

            var sheetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetAssetPath);
            if (sheetTexture == null)
            {
                Debug.LogWarning($"SpriteSheetAtlasWorkflow: cannot add missing sheet texture '{sheetAssetPath}' to atlas.");
                return;
            }

            atlas.Add(new UnityEngine.Object[] { sheetTexture });
            EditorUtility.SetDirty(atlas);
        }

        private static string ExtractCategoryFromSheetPath(string sheetAssetPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(sheetAssetPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "uncategorized";
            }

            if (fileName.StartsWith(SheetFilePrefix, StringComparison.OrdinalIgnoreCase) &&
                fileName.EndsWith(SheetFileSuffix, StringComparison.OrdinalIgnoreCase) &&
                fileName.Length > SheetFilePrefix.Length + SheetFileSuffix.Length)
            {
                return fileName.Substring(
                    SheetFilePrefix.Length,
                    fileName.Length - SheetFilePrefix.Length - SheetFileSuffix.Length);
            }

            return fileName;
        }

        private static string SanitizeSpriteName(string preferredName, string fallbackId)
        {
            var candidate = !string.IsNullOrWhiteSpace(preferredName)
                ? preferredName
                : fallbackId;
            if (string.IsNullOrWhiteSpace(candidate))
            {
                candidate = "sprite";
            }

            var sanitized = candidate.Trim().Replace(' ', '_').Replace('-', '_');
            for (var i = 0; i < sanitized.Length; i++)
            {
                var c = sanitized[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    continue;
                }

                sanitized = sanitized.Replace(c, '_');
            }

            return sanitized.ToLowerInvariant();
        }

        private static string EnsureUniqueSpriteName(string candidate, HashSet<string> used)
        {
            if (used == null)
            {
                return candidate;
            }

            var safeCandidate = string.IsNullOrWhiteSpace(candidate) ? "sprite" : candidate;
            if (used.Add(safeCandidate))
            {
                return safeCandidate;
            }

            var suffix = 2;
            while (true)
            {
                var attempt = $"{safeCandidate}_{suffix}";
                if (used.Add(attempt))
                {
                    return attempt;
                }

                suffix++;
            }
        }

        private static bool TryLoadManifest(string assetPath, out ArtManifest manifest)
        {
            manifest = null;
            var absolutePath = ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                Debug.LogError($"SpriteSheetAtlasWorkflow: art manifest not found at '{assetPath}'.");
                return false;
            }

            try
            {
                var json = File.ReadAllText(absolutePath);
                manifest = JsonUtility.FromJson<ArtManifest>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SpriteSheetAtlasWorkflow: failed to parse art manifest ({ex.Message}).");
                return false;
            }

            if (manifest == null || manifest.entries == null)
            {
                Debug.LogError($"SpriteSheetAtlasWorkflow: art manifest '{assetPath}' is invalid.");
                return false;
            }

            return true;
        }

        private static string ToAbsolutePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(relativePath))
            {
                return Path.GetFullPath(relativePath);
            }

            var normalized = relativePath.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Assets", StringComparison.OrdinalIgnoreCase))
            {
                var projectRoot = GetProjectRootPath();
                var relativeToRoot = normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                    ? normalized.Substring("Assets/".Length)
                    : string.Empty;

                return string.IsNullOrWhiteSpace(relativeToRoot)
                    ? Path.GetFullPath(Application.dataPath)
                    : Path.GetFullPath(Path.Combine(projectRoot, "Assets", relativeToRoot));
            }

            return Path.GetFullPath(Path.Combine(GetProjectRootPath(), normalized));
        }

        private static string GetProjectRootPath()
        {
            var dataPath = Application.dataPath;
            if (string.IsNullOrWhiteSpace(dataPath))
            {
                return Directory.GetCurrentDirectory();
            }

            return Path.GetFullPath(Path.Combine(dataPath, ".."));
        }

        private static void CleanupStaleGeneratedAssets(
            List<string> generatedSheetPaths,
            List<string> generatedAtlasPaths)
        {
            CleanupStaleGeneratedAssetsUnderRoot(SheetOutputRoot, SheetFilePattern, generatedSheetPaths);
            CleanupStaleGeneratedAssetsUnderRoot(AtlasOutputRoot, AtlasFilePattern, generatedAtlasPaths);
            CleanupStaleGeneratedAssetsUnderRoot(AtlasOutputRoot, LegacyAtlasFilePattern, keepPaths: null);
        }

        private static void CleanupStaleGeneratedAssetsUnderRoot(
            string root,
            string searchPattern,
            List<string> keepPaths)
        {
            var absoluteRoot = ToAbsolutePath(root);
            if (!Directory.Exists(absoluteRoot))
            {
                return;
            }

            var keepSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (keepPaths != null)
            {
                for (var i = 0; i < keepPaths.Count; i++)
                {
                    var keepPath = keepPaths[i];
                    if (string.IsNullOrWhiteSpace(keepPath))
                    {
                        continue;
                    }

                    keepSet.Add(keepPath.Replace('\\', '/'));
                }
            }

            var absoluteFiles = Directory.GetFiles(
                absoluteRoot,
                searchPattern,
                SearchOption.TopDirectoryOnly);
            for (var i = 0; i < absoluteFiles.Length; i++)
            {
                var absolutePath = absoluteFiles[i];
                var normalizedAbsoluteRoot = absoluteRoot.Replace('\\', '/').TrimEnd('/');
                var normalizedAbsolutePath = absolutePath.Replace('\\', '/');
                var relativeFromRoot = normalizedAbsolutePath.StartsWith(normalizedAbsoluteRoot + "/", StringComparison.OrdinalIgnoreCase)
                    ? normalizedAbsolutePath.Substring(normalizedAbsoluteRoot.Length + 1)
                    : Path.GetFileName(normalizedAbsolutePath);
                var assetPath = string.IsNullOrWhiteSpace(relativeFromRoot)
                    ? root.Replace('\\', '/')
                    : $"{root.Replace('\\', '/')}/{relativeFromRoot}";

                if (keepSet.Contains(assetPath))
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static bool TryDecodeTexture(string absolutePath, out Texture2D texture)
        {
            texture = null;
            if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            {
                return false;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(absolutePath);
            }
            catch
            {
                return false;
            }

            var decodedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            if (!TryLoadImage(decodedTexture, bytes))
            {
                UnityEngine.Object.DestroyImmediate(decodedTexture);
                return false;
            }

            texture = decodedTexture;
            return true;
        }

        private static bool TryLoadImage(Texture2D texture, byte[] bytes)
        {
            if (texture == null || bytes == null || bytes.Length == 0)
            {
                return false;
            }

            EnsureImageConversionMethods();
            if (_imageConversionLoadImageMethod == null)
            {
                return false;
            }

            try
            {
                var result = _imageConversionLoadImageMethod.Invoke(null, new object[] { texture, bytes, false });
                return result is bool loaded && loaded;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryEncodePng(Texture2D texture, out byte[] png)
        {
            png = null;
            if (texture == null)
            {
                return false;
            }

            EnsureImageConversionMethods();
            if (_imageConversionEncodeToPngMethod == null)
            {
                return false;
            }

            try
            {
                png = _imageConversionEncodeToPngMethod.Invoke(null, new object[] { texture }) as byte[];
                return png != null && png.Length > 0;
            }
            catch
            {
                png = null;
                return false;
            }
        }

        private static void EnsureImageConversionMethods()
        {
            if (_imageConversionLookupCompleted)
            {
                return;
            }

            _imageConversionLookupCompleted = true;
            var imageConversionType = Type.GetType(
                "UnityEngine.ImageConversion, UnityEngine.ImageConversionModule",
                throwOnError: false);
            if (imageConversionType == null)
            {
                return;
            }

            _imageConversionLoadImageMethod = imageConversionType.GetMethod(
                "LoadImage",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) },
                modifiers: null);

            _imageConversionEncodeToPngMethod = imageConversionType.GetMethod(
                "EncodeToPNG",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(Texture2D) },
                modifiers: null);
        }
    }
}
#endif
