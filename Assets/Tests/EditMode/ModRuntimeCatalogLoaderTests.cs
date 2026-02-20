using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RavenDevOps.Fishing.Tools;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class ModRuntimeCatalogLoaderTests
    {
        [Test]
        public void Load_AppliesDeterministicOverrideOrder_ByModId()
        {
            var root = CreateTempDirectory();
            try
            {
                CreatePack(
                    root,
                    "mods_a",
                    manifest: new ModManifestV1
                    {
                        schemaVersion = "1.0",
                        modId = "beta_pack",
                        modVersion = "1.0.0",
                        displayName = "Beta Pack",
                        dataCatalogs = new List<string> { "Data/catalog.json" },
                        assetOverrides = new List<string>()
                    },
                    catalog: new ModCatalogDataV1
                    {
                        fishDefinitions = new List<ModFishDefinitionData>
                        {
                            new ModFishDefinitionData
                            {
                                id = "fish_modded",
                                minDistanceTier = 0,
                                maxDistanceTier = 2,
                                minDepth = 0f,
                                maxDepth = 10f,
                                rarityWeight = 1,
                                baseValue = 25
                            }
                        }
                    });

                CreatePack(
                    root,
                    "mods_z",
                    manifest: new ModManifestV1
                    {
                        schemaVersion = "1.0",
                        modId = "alpha_pack",
                        modVersion = "1.0.0",
                        displayName = "Alpha Pack",
                        dataCatalogs = new List<string> { "Data/catalog.json" },
                        assetOverrides = new List<string>()
                    },
                    catalog: new ModCatalogDataV1
                    {
                        fishDefinitions = new List<ModFishDefinitionData>
                        {
                            new ModFishDefinitionData
                            {
                                id = "fish_modded",
                                minDistanceTier = 0,
                                maxDistanceTier = 2,
                                minDepth = 0f,
                                maxDepth = 10f,
                                rarityWeight = 1,
                                baseValue = 10
                            }
                        }
                    });

                var result = ModRuntimeCatalogLoader.Load(root, modsEnabled: true, currentGameVersion: "1.0.0");

                Assert.That(result.acceptedMods.Count, Is.EqualTo(2));
                Assert.That(result.fishById.ContainsKey("fish_modded"), Is.True);
                Assert.That(result.fishById["fish_modded"].baseValue, Is.EqualTo(25));
                Assert.That(result.messages, Has.Some.Contains("overrides fish id 'fish_modded'"));
            }
            finally
            {
                SafeDeleteDirectory(root);
            }
        }

        [Test]
        public void Load_RejectsPack_WhenManifestMissing()
        {
            var root = CreateTempDirectory();
            try
            {
                Directory.CreateDirectory(Path.Combine(root, "broken_pack"));
                var result = ModRuntimeCatalogLoader.Load(root, modsEnabled: true, currentGameVersion: "1.0.0");

                Assert.That(result.acceptedMods.Count, Is.EqualTo(0));
                Assert.That(result.rejectedMods.Count, Is.EqualTo(1));
                Assert.That(result.rejectedMods[0].reason, Does.Contain("Missing manifest.json"));
            }
            finally
            {
                SafeDeleteDirectory(root);
            }
        }

        [Test]
        public void Load_ReturnsDisabledResult_WhenModsDisabled()
        {
            var root = CreateTempDirectory();
            try
            {
                var result = ModRuntimeCatalogLoader.Load(root, modsEnabled: false, currentGameVersion: "1.0.0");

                Assert.That(result.modsEnabled, Is.False);
                Assert.That(result.acceptedMods.Count, Is.EqualTo(0));
                Assert.That(result.messages, Has.Some.Contains("loading disabled"));
            }
            finally
            {
                SafeDeleteDirectory(root);
            }
        }

        private static void CreatePack(string root, string folderName, ModManifestV1 manifest, ModCatalogDataV1 catalog)
        {
            var packRoot = Path.Combine(root, folderName);
            Directory.CreateDirectory(packRoot);
            Directory.CreateDirectory(Path.Combine(packRoot, "Data"));

            var manifestJson = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(Path.Combine(packRoot, ModRuntimeCatalogLoader.ManifestFileName), manifestJson);

            var catalogJson = JsonUtility.ToJson(catalog, true);
            File.WriteAllText(Path.Combine(packRoot, "Data", "catalog.json"), catalogJson);
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "raven_mod_loader_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void SafeDeleteDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return;
            }

            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch
            {
            }
        }
    }
}
