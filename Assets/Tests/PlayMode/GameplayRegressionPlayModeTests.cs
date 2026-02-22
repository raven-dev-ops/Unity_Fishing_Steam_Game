using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.Steam;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RavenDevOps.Fishing.Tests.PlayMode
{
    public sealed class GameplayRegressionPlayModeTests
    {
        private readonly List<GameObject> _createdRoots = new List<GameObject>();
        private readonly Dictionary<string, string> _saveBackups = new Dictionary<string, string>();
        private string _backupDirectory = string.Empty;
        private string _savePath = string.Empty;

        [SetUp]
        public void SetUp()
        {
            BackupAndClearSaveFiles();
            RuntimeServiceRegistry.Clear();
            CleanupSingletons();
            ClearTrackedSaveFiles();
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _createdRoots.Count; i++)
            {
                var root = _createdRoots[i];
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            _createdRoots.Clear();
            CleanupSingletons();
            RuntimeServiceRegistry.Clear();
            RestoreSaveFiles();
        }

        [UnityTest]
        public IEnumerator CatchInventorySellLoop_UpdatesEconomyAndStats()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_CatchSell");
            yield return null;

            saveManager.RecordCatch("fish_cod", 1, weightKg: 1.2f, valueCopecs: 10);
            saveManager.RecordCatch("fish_cod", 1, weightKg: 1.3f, valueCopecs: 10);
            saveManager.RecordCatch("fish_cod", 2, weightKg: 1.8f, valueCopecs: 15);

            Assert.That(saveManager.Current.stats.totalFishCaught, Is.EqualTo(3));
            Assert.That(saveManager.Current.fishInventory.Count, Is.EqualTo(2));
            Assert.That(saveManager.Current.fishInventory[0].count + saveManager.Current.fishInventory[1].count, Is.EqualTo(3));

            var summaryCalculator = CreateComponent<SellSummaryCalculator>("SellSummaryCalculator_CatchSell");
            summaryCalculator.SetDistanceTierStep(0.5f);
            var fishShop = CreateComponent<FishShopController>("FishShopController_CatchSell");
            yield return null;

            var earned = fishShop.SellAll();

            Assert.That(earned, Is.EqualTo(35));
            Assert.That(saveManager.Current.copecs, Is.EqualTo(35));
            Assert.That(saveManager.Current.fishInventory, Is.Empty);
        }

        [UnityTest]
        public IEnumerator PurchaseOwnershipEquipFlow_DeductsCurrencyOnceAndKeepsEquippedState()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_PurchaseEquip");
            yield return null;

            saveManager.AddCopecs(600);
            saveManager.Save(forceImmediate: true);

            var hookShop = CreateComponent<HookShopController>("HookShopController_PurchaseEquip");
            var boatShop = CreateComponent<BoatShopController>("BoatShopController_PurchaseEquip");
            SetPrivateField(hookShop, "_items", new List<ShopItem> { new ShopItem { id = "hook_test", price = 120 } });
            SetPrivateField(boatShop, "_items", new List<ShopItem> { new ShopItem { id = "ship_test", price = 220 } });
            yield return null;

            var initialCopecs = saveManager.Current.copecs;
            var hookPurchased = hookShop.BuyOrEquip("hook_test");
            var shipPurchased = boatShop.BuyOrEquip("ship_test");

            Assert.That(hookPurchased, Is.True);
            Assert.That(shipPurchased, Is.True);
            Assert.That(saveManager.Current.ownedHooks, Contains.Item("hook_test"));
            Assert.That(saveManager.Current.ownedShips, Contains.Item("ship_test"));
            Assert.That(saveManager.Current.equippedHookId, Is.EqualTo("hook_test"));
            Assert.That(saveManager.Current.equippedShipId, Is.EqualTo("ship_test"));
            Assert.That(saveManager.Current.copecs, Is.EqualTo(initialCopecs - 340));
            Assert.That(saveManager.Current.stats.totalPurchases, Is.EqualTo(2));

            var copecsAfterPurchase = saveManager.Current.copecs;
            Assert.That(hookShop.BuyOrEquip("hook_test"), Is.True);
            Assert.That(boatShop.BuyOrEquip("ship_test"), Is.True);
            Assert.That(saveManager.Current.copecs, Is.EqualTo(copecsAfterPurchase));
            Assert.That(saveManager.Current.stats.totalPurchases, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ShopUpgradeFlow_RequiresPriorTierAndSufficientCopecs()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_UpgradeRules");
            yield return null;

            saveManager.AddCopecs(400);
            saveManager.Save(forceImmediate: true);

            var hookShop = CreateComponent<HookShopController>("HookShopController_UpgradeRules");
            var boatShop = CreateComponent<BoatShopController>("BoatShopController_UpgradeRules");
            SetPrivateField(
                hookShop,
                "_items",
                new List<ShopItem>
                {
                    new ShopItem { id = "hook_t1", price = 40, valueTier = 1 },
                    new ShopItem { id = "hook_t2", price = 120, valueTier = 2 },
                    new ShopItem { id = "hook_t3", price = 300, valueTier = 3 }
                });
            SetPrivateField(
                boatShop,
                "_items",
                new List<ShopItem>
                {
                    new ShopItem { id = "boat_t1", price = 60, valueTier = 1 },
                    new ShopItem { id = "boat_t2", price = 180, valueTier = 2 },
                    new ShopItem { id = "boat_t3", price = 360, valueTier = 3 }
                });
            yield return null;

            Assert.That(hookShop.BuyOrEquip("hook_t3"), Is.False);
            Assert.That(hookShop.BuyOrEquip("hook_t2"), Is.False);
            Assert.That(hookShop.BuyOrEquip("hook_t1"), Is.True);
            Assert.That(hookShop.BuyOrEquip("hook_t2"), Is.True);
            Assert.That(hookShop.BuyOrEquip("hook_t3"), Is.False);

            Assert.That(boatShop.BuyOrEquip("boat_t3"), Is.False);
            Assert.That(boatShop.BuyOrEquip("boat_t2"), Is.False);
            Assert.That(boatShop.BuyOrEquip("boat_t1"), Is.True);
            Assert.That(boatShop.BuyOrEquip("boat_t2"), Is.True);
            Assert.That(boatShop.BuyOrEquip("boat_t3"), Is.False);

            saveManager.AddCopecs(700);
            yield return null;

            Assert.That(hookShop.BuyOrEquip("hook_t3"), Is.True);
            Assert.That(boatShop.BuyOrEquip("boat_t3"), Is.True);
            Assert.That(saveManager.Current.ownedHooks, Contains.Item("hook_t3"));
            Assert.That(saveManager.Current.ownedShips, Contains.Item("boat_t3"));
        }

        [UnityTest]
        public IEnumerator SaveLoadRoundtrip_PersistsAcrossSceneTransitions()
        {
            var originalActiveScene = SceneManager.GetActiveScene();
            var saveManager = CreateComponent<SaveManager>("SaveManager_Roundtrip_Source");
            yield return null;

            saveManager.AddCopecs(250);
            saveManager.RecordCatch("fish_roundtrip", 3, weightKg: 2.2f, valueCopecs: 25);
            saveManager.RecordPurchase("hook_roundtrip", 80);
            if (!saveManager.Current.ownedHooks.Contains("hook_roundtrip"))
            {
                saveManager.Current.ownedHooks.Add("hook_roundtrip");
            }

            saveManager.Current.equippedHookId = "hook_roundtrip";
            saveManager.Save(forceImmediate: true);
            Assert.That(File.Exists(saveManager.SaveFilePath), Is.True);

            var sceneA = SceneManager.CreateScene("PlayModeRoundtrip_A");
            Assert.That(SceneManager.SetActiveScene(sceneA), Is.True);
            yield return null;

            var sceneB = SceneManager.CreateScene("PlayModeRoundtrip_B");
            Assert.That(SceneManager.SetActiveScene(sceneB), Is.True);
            yield return null;

            if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(originalActiveScene);
            }

            var unloadA = SceneManager.UnloadSceneAsync(sceneA);
            Assert.That(unloadA, Is.Not.Null);
            yield return unloadA;

            UnityEngine.Object.Destroy(saveManager.gameObject);
            yield return null;

            var reloadedSaveManager = CreateComponent<SaveManager>("SaveManager_Roundtrip_Reloaded");
            yield return null;

            Assert.That(reloadedSaveManager.Current.copecs, Is.EqualTo(250));
            Assert.That(reloadedSaveManager.Current.stats.totalFishCaught, Is.EqualTo(1));
            Assert.That(reloadedSaveManager.Current.stats.totalPurchases, Is.EqualTo(1));
            Assert.That(reloadedSaveManager.Current.fishInventory.Exists(x => x != null && x.fishId == "fish_roundtrip"), Is.True);
            Assert.That(reloadedSaveManager.Current.ownedHooks, Contains.Item("hook_roundtrip"));
            Assert.That(reloadedSaveManager.Current.equippedHookId, Is.EqualTo("hook_roundtrip"));

            var unloadB = SceneManager.UnloadSceneAsync(sceneB);
            if (unloadB != null)
            {
                yield return unloadB;
            }
        }

        [UnityTest]
        public IEnumerator NonSteamFallback_GuardsSteamServicesWithoutErrors()
        {
#if STEAMWORKS_NET
            Assert.Ignore("Non-Steam fallback assertions apply only when STEAMWORKS_NET is not defined.");
#else
            var saveManager = CreateComponent<SaveManager>("SaveManager_SteamFallback");
            var gameFlowManager = CreateComponent<GameFlowManager>("GameFlowManager_SteamFallback");
            CreateComponent<UserSettingsService>("UserSettingsService_SteamFallback");
            var steamBootstrap = CreateComponent<SteamBootstrap>("SteamBootstrap_SteamFallback");
            var cloudSync = CreateComponent<SteamCloudSyncService>("SteamCloudSyncService_SteamFallback");
            CreateComponent<SteamRichPresenceService>("SteamRichPresenceService_SteamFallback");
            CreateComponent<SteamStatsService>("SteamStatsService_SteamFallback");

            yield return null;

            gameFlowManager.SetState(GameFlowState.Harbor);
            saveManager.RecordCatch("fish_non_steam", 1, weightKg: 0.5f, valueCopecs: 5);
            yield return null;

            Assert.That(steamBootstrap, Is.Not.Null);
            Assert.That(SteamBootstrap.IsSteamInitialized, Is.False);
            Assert.That(SteamBootstrap.LastFallbackReason, Does.Contain("STEAMWORKS_NET"));
            Assert.That(cloudSync.LastConflictDecision, Is.EqualTo(string.Empty));
#endif
        }

        private T CreateComponent<T>(string rootName) where T : Component
        {
            var root = new GameObject(rootName);
            _createdRoots.Add(root);
            return root.AddComponent<T>();
        }

        private void BackupAndClearSaveFiles()
        {
            _saveBackups.Clear();
            _backupDirectory = Path.Combine(Application.temporaryCachePath, "playmode_save_backups", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_backupDirectory);

            _savePath = Path.Combine(Application.persistentDataPath, "save_v1.json");
            var trackedFiles = new[]
            {
                _savePath,
                _savePath + ".bak",
                _savePath + ".tmp"
            };

            for (var i = 0; i < trackedFiles.Length; i++)
            {
                var path = trackedFiles[i];
                if (!File.Exists(path))
                {
                    continue;
                }

                var backupPath = Path.Combine(_backupDirectory, Path.GetFileName(path));
                File.Copy(path, backupPath, overwrite: true);
                _saveBackups[path] = backupPath;
                File.Delete(path);
            }
        }

        private void ClearTrackedSaveFiles()
        {
            if (string.IsNullOrWhiteSpace(_savePath))
            {
                return;
            }

            var trackedFiles = new[]
            {
                _savePath,
                _savePath + ".bak",
                _savePath + ".tmp"
            };

            for (var i = 0; i < trackedFiles.Length; i++)
            {
                var path = trackedFiles[i];
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private void RestoreSaveFiles()
        {
            var trackedFiles = new[]
            {
                _savePath,
                _savePath + ".bak",
                _savePath + ".tmp"
            };

            for (var i = 0; i < trackedFiles.Length; i++)
            {
                var path = trackedFiles[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            foreach (var pair in _saveBackups)
            {
                if (!File.Exists(pair.Value))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(pair.Key);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.Copy(pair.Value, pair.Key, overwrite: true);
            }

            if (Directory.Exists(_backupDirectory))
            {
                Directory.Delete(_backupDirectory, recursive: true);
            }

            _saveBackups.Clear();
            _backupDirectory = string.Empty;
            _savePath = string.Empty;
        }

        private static void CleanupSingletons()
        {
            if (SaveManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(SaveManager.Instance.gameObject);
            }

            if (GameFlowManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(GameFlowManager.Instance.gameObject);
            }

            if (UserSettingsService.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(UserSettingsService.Instance.gameObject);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null || string.IsNullOrWhiteSpace(fieldName))
            {
                return;
            }

            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            field.SetValue(target, value);
        }
    }
}
