using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Harbor;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.Steam;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
        public IEnumerator PurchaseOwnershipFlow_DeductsCurrencyOnce_AndDefersEquipToShipyard()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_PurchaseEquip");
            yield return null;

            saveManager.AddCopecs(600);
            saveManager.Save(forceImmediate: true);

            var hookShop = CreateComponent<HookShopController>("HookShopController_PurchaseEquip");
            var boatShop = CreateComponent<BoatShopController>("BoatShopController_PurchaseEquip");
            hookShop.ConfigureItems(new List<ShopItem> { new ShopItem { id = "hook_test", price = 120 } });
            boatShop.ConfigureItems(new List<ShopItem> { new ShopItem { id = "ship_test", price = 220 } });
            yield return null;

            var initialCopecs = saveManager.Current.copecs;
            var initialEquippedHookId = saveManager.Current.equippedHookId;
            var initialEquippedShipId = saveManager.Current.equippedShipId;
            var hookPurchased = hookShop.BuyOrEquip("hook_test");
            var shipPurchased = boatShop.BuyOrEquip("ship_test");

            Assert.That(hookPurchased, Is.True);
            Assert.That(shipPurchased, Is.True);
            Assert.That(saveManager.Current.ownedHooks, Contains.Item("hook_test"));
            Assert.That(saveManager.Current.ownedShips, Contains.Item("ship_test"));
            Assert.That(saveManager.Current.equippedHookId, Is.EqualTo(initialEquippedHookId));
            Assert.That(saveManager.Current.equippedShipId, Is.EqualTo(initialEquippedShipId));
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
            hookShop.ConfigureItems(
                new List<ShopItem>
                {
                    new ShopItem { id = "hook_t1", price = 40, valueTier = 1 },
                    new ShopItem { id = "hook_t2", price = 120, valueTier = 2 },
                    new ShopItem { id = "hook_t3", price = 300, valueTier = 3 }
                });
            boatShop.ConfigureItems(
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

        [UnityTest]
        public IEnumerator PhaseTwoFallbackContent_LoadsRequiredAudioAndEnvironmentKeys()
        {
            var root = new GameObject("CatalogService_PhaseTwoFallback");
            _createdRoots.Add(root);
            root.AddComponent<AddressablesPilotCatalogLoader>();
            var catalogService = root.AddComponent<CatalogService>();

            var timeoutAt = Time.realtimeSinceStartup + 5f;
            while ((!catalogService.PhaseTwoAudioLoadCompleted || !catalogService.PhaseTwoEnvironmentLoadCompleted) &&
                   Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(catalogService.PhaseTwoAudioLoadCompleted, Is.True, "Expected phase-two audio load to complete.");
            Assert.That(catalogService.PhaseTwoEnvironmentLoadCompleted, Is.True, "Expected phase-two environment load to complete.");
            Assert.That(
                catalogService.TryGetPhaseTwoAudioClip("menu_music_loop", out var menuMusic) && menuMusic != null,
                Is.True,
                "Expected fallback menu music key to resolve.");
            Assert.That(
                catalogService.TryGetPhaseTwoAudioClip("sfx_cast", out var castSfx) && castSfx != null,
                Is.True,
                "Expected fallback cast SFX key to resolve.");
            Assert.That(
                catalogService.TryGetPhaseTwoEnvironmentMaterial("fishing_skybox", out var skybox) && skybox != null,
                Is.True,
                "Expected fallback fishing skybox key to resolve.");
        }

        [UnityTest]
        public IEnumerator HarborSailFlow_CargoFullBlocksDepartureUntilCargoSold()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_HarborSailGuard");
            yield return null;

            saveManager.Current.fishInventory.Clear();
            saveManager.Current.fishInventory.Add(new FishInventoryEntry
            {
                fishId = "fish_cod",
                distanceTier = 1,
                count = 12
            });
            saveManager.Save(forceImmediate: true);

            var statusText = CreateUiText("HarborStatusText_SailGuard");
            var cargoText = CreateUiText("HarborCargoText_SailGuard");
            var sailButton = CreateUiButton("HarborSailButton_SailGuard");

            var routerRoot = new GameObject("HarborRouter_SailGuard");
            routerRoot.SetActive(false);
            _createdRoots.Add(routerRoot);
            var router = routerRoot.AddComponent<HarborSceneInteractionRouter>();
            router.Configure(
                interactables: new List<WorldInteractable>(),
                hookShop: null,
                boatShop: null,
                fishShop: null,
                statusText: statusText,
                cargoText: cargoText,
                sailButton: sailButton,
                saveManager: saveManager);

            routerRoot.SetActive(true);
            yield return null;

            Assert.That(sailButton.interactable, Is.False, "Expected sailing to be disabled when cargo is full.");

            router.OnSailRequested();
            yield return null;

            Assert.That(statusText.text, Does.Contain("Cargo full"));

            saveManager.ClearFishInventory();
            saveManager.Save(forceImmediate: true);
            yield return null;

            Assert.That(sailButton.interactable, Is.True, "Expected sailing to be re-enabled once cargo is sold/cleared.");

            router.OnSailRequested();
            yield return null;

            Assert.That(statusText.text, Does.Contain("Casting off"));
        }

        [UnityTest]
        public IEnumerator HarborFishMarketSale_ClearsCargoAndCreditsBalance()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_HarborFishSale");
            yield return null;

            saveManager.Current.copecs = 5;
            saveManager.Current.fishInventory.Clear();
            saveManager.Current.fishInventory.Add(new FishInventoryEntry
            {
                fishId = "fish_cod",
                distanceTier = 1,
                count = 2
            });
            saveManager.Current.fishInventory.Add(new FishInventoryEntry
            {
                fishId = "fish_herring",
                distanceTier = 3,
                count = 1
            });
            saveManager.Save(forceImmediate: true);

            var summaryCalculator = CreateComponent<SellSummaryCalculator>("SellSummaryCalculator_HarborFishSale");
            summaryCalculator.SetDistanceTierStep(0.25f);
            var fishShop = CreateComponent<FishShopController>("FishShopController_HarborFishSale");
            yield return null;

            var statusText = CreateUiText("HarborStatusText_FishSale");
            var fishInfoText = CreateUiText("HarborFishInfoText_FishSale");
            var routerRoot = new GameObject("HarborRouter_FishSale");
            routerRoot.SetActive(false);
            _createdRoots.Add(routerRoot);
            var router = routerRoot.AddComponent<HarborSceneInteractionRouter>();
            router.Configure(
                interactables: new List<WorldInteractable>(),
                hookShop: null,
                boatShop: null,
                fishShop: fishShop,
                statusText: statusText,
                fishShopInfoText: fishInfoText,
                saveManager: saveManager);

            routerRoot.SetActive(true);
            yield return null;

            router.OnFishShopSellRequested();
            yield return null;

            Assert.That(saveManager.Current.fishInventory, Is.Empty);
            Assert.That(saveManager.Current.copecs, Is.EqualTo(40));
            Assert.That(statusText.text, Does.Contain("Sold 3 fish for 35 copecs"));
        }

        [UnityTest]
        public IEnumerator HarborPurchaseAndShipyardEquipFlow_UsesRouterTransactions()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_HarborPurchaseEquip");
            yield return null;

            saveManager.Current.copecs = 1000;
            saveManager.Current.ownedHooks = new List<string> { "hook_lv1" };
            saveManager.Current.ownedShips = new List<string> { "ship_lv1" };
            saveManager.Current.equippedHookId = "hook_lv1";
            saveManager.Current.equippedShipId = "ship_lv1";
            saveManager.Save(forceImmediate: true);

            var hookShop = CreateComponent<HookShopController>("HookShopController_HarborPurchaseEquip");
            hookShop.ConfigureItems(
                new List<ShopItem>
                {
                    new ShopItem { id = "hook_lv1", price = 0, valueTier = 1 },
                    new ShopItem { id = "hook_lv2", price = 120, valueTier = 2 }
                });
            hookShop.SetUnlockAllItemsForQa(true);
            var boatShop = CreateComponent<BoatShopController>("BoatShopController_HarborPurchaseEquip");
            boatShop.ConfigureItems(
                new List<ShopItem>
                {
                    new ShopItem { id = "ship_lv1", price = 0, valueTier = 1 },
                    new ShopItem { id = "ship_lv2", price = 180, valueTier = 2 }
                });
            boatShop.SetUnlockAllItemsForQa(true);
            yield return null;

            var statusText = CreateUiText("HarborStatusText_PurchaseEquip");
            var shipyardInfoText = CreateUiText("HarborShipyardInfoText_PurchaseEquip");
            var shipyardCargoText = CreateUiText("HarborShipyardCargoText_PurchaseEquip");

            var routerRoot = new GameObject("HarborRouter_PurchaseEquip");
            routerRoot.SetActive(false);
            _createdRoots.Add(routerRoot);
            var router = routerRoot.AddComponent<HarborSceneInteractionRouter>();
            router.Configure(
                new HarborSceneInteractionRouter.DependencyBundle
                {
                    Runtime = new HarborSceneInteractionRouter.RuntimeDependencyBundle
                    {
                        Interactables = new List<WorldInteractable>(),
                        HookShop = hookShop,
                        BoatShop = boatShop,
                        FishShop = null,
                        InteractionController = null,
                        SaveManager = saveManager
                    },
                    Text = new HarborSceneInteractionRouter.TextDependencyBundle
                    {
                        StatusText = statusText,
                        ShipyardInfoText = shipyardInfoText,
                        ShipyardCargoText = shipyardCargoText
                    }
                });
            router.SetUnlockAllShopItemsForQa(true);
            routerRoot.SetActive(true);
            yield return null;

            var startingCopecs = saveManager.Current.copecs;
            router.OnHookShopItemRequested("hook_lv2");
            router.OnBoatShopItemRequested("ship_lv2");
            yield return null;

            Assert.That(saveManager.Current.ownedHooks, Contains.Item("hook_lv2"));
            Assert.That(saveManager.Current.ownedShips, Contains.Item("ship_lv2"));
            Assert.That(saveManager.Current.copecs, Is.EqualTo(startingCopecs - 300));

            router.OnShipyardHookRequested("hook_lv2");
            router.OnShipyardShipRequested("ship_lv2");
            yield return null;

            Assert.That(saveManager.Current.equippedHookId, Is.EqualTo("hook_lv2"));
            Assert.That(saveManager.Current.equippedShipId, Is.EqualTo("ship_lv2"));
            Assert.That(statusText.text, Does.Contain("Selected Ship Lv2"));
        }

        [UnityTest]
        public IEnumerator HarborFishMarketCharterFlow_AcceptsAndClaimsViaRouter()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_HarborCharterFlow");
            yield return null;

            saveManager.Current.copecs = 0;
            saveManager.Current.fishInventory.Clear();
            saveManager.Save(forceImmediate: true);

            var summaryCalculator = CreateComponent<SellSummaryCalculator>("SellSummaryCalculator_HarborCharterFlow");
            summaryCalculator.SetDistanceTierStep(0.25f);
            var fishShop = CreateComponent<FishShopController>("FishShopController_HarborCharterFlow");
            yield return null;

            var initialSnapshot = fishShop.BuildMarketSnapshot(maxHistoryEntries: 1);
            Assert.That(string.IsNullOrWhiteSpace(initialSnapshot.questFishId), Is.False, "Expected an active charter fish target.");
            var questCount = Mathf.Max(1, initialSnapshot.questRequiredCount);
            var questReward = Mathf.Max(0, initialSnapshot.questRewardCopecs);

            saveManager.Current.fishInventory.Add(new FishInventoryEntry
            {
                fishId = initialSnapshot.questFishId,
                distanceTier = 2,
                count = questCount
            });
            saveManager.Save(forceImmediate: true);

            var statusText = CreateUiText("HarborStatusText_CharterFlow");
            var fishInfoText = CreateUiText("HarborFishInfoText_CharterFlow");
            var acceptButton = CreateUiButton("HarborCharterAcceptButton_CharterFlow");
            var fulfillButton = CreateUiButton("HarborCharterFulfillButton_CharterFlow");

            var routerRoot = new GameObject("HarborRouter_CharterFlow");
            routerRoot.SetActive(false);
            _createdRoots.Add(routerRoot);
            var router = routerRoot.AddComponent<HarborSceneInteractionRouter>();
            router.Configure(
                new HarborSceneInteractionRouter.DependencyBundle
                {
                    Runtime = new HarborSceneInteractionRouter.RuntimeDependencyBundle
                    {
                        Interactables = new List<WorldInteractable>(),
                        HookShop = null,
                        BoatShop = null,
                        FishShop = fishShop,
                        InteractionController = null,
                        SaveManager = saveManager
                    },
                    Text = new HarborSceneInteractionRouter.TextDependencyBundle
                    {
                        StatusText = statusText,
                        FishShopInfoText = fishInfoText
                    },
                    Buttons = new HarborSceneInteractionRouter.ButtonDependencyBundle
                    {
                        FishQuestAcceptButton = acceptButton,
                        FishQuestFulfillButton = fulfillButton
                    }
                });
            routerRoot.SetActive(true);
            yield return null;

            router.OnFishShopQuestAcceptRequested();
            yield return null;

            Assert.That(statusText.text, Does.Contain("accepted").IgnoreCase);

            router.OnFishShopSellRequested();
            yield return null;

            var completedSnapshot = fishShop.BuildMarketSnapshot(maxHistoryEntries: 1);
            Assert.That(completedSnapshot.questCompleted, Is.True, "Expected charter progress to complete after selling target fish.");

            var copecsBeforeClaim = saveManager.Current.copecs;
            router.OnFishShopQuestClaimRequested();
            yield return null;

            var claimedSnapshot = fishShop.BuildMarketSnapshot(maxHistoryEntries: 1);
            Assert.That(claimedSnapshot.questClaimed, Is.True);
            Assert.That(saveManager.Current.copecs, Is.EqualTo(copecsBeforeClaim + questReward));
            Assert.That(fulfillButton.interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator HarborHookShopButtons_RespectUnlockAndAffordabilityRules()
        {
            var saveManager = CreateComponent<SaveManager>("SaveManager_HarborHookButtons");
            yield return null;

            saveManager.Current.copecs = 40;
            saveManager.Current.ownedHooks = new List<string> { "hook_lv1" };
            saveManager.Current.equippedHookId = "hook_lv1";
            saveManager.Current.progression ??= new ProgressionData();
            saveManager.Current.progression.unlockedContentIds ??= new List<string>();
            saveManager.Current.progression.unlockedContentIds.Remove("hook_lv2");
            saveManager.Current.progression.unlockedContentIds.Remove("hook_lv3");
            saveManager.Save(forceImmediate: true);

            var hookShop = CreateComponent<HookShopController>("HookShopController_HarborHookButtons");
            hookShop.ConfigureItems(
                new List<ShopItem>
                {
                    new ShopItem { id = "hook_lv1", price = 0, valueTier = 1 },
                    new ShopItem { id = "hook_lv2", price = 120, valueTier = 2 },
                    new ShopItem { id = "hook_lv3", price = 320, valueTier = 3 }
                });
            yield return null;

            var statusText = CreateUiText("HarborStatusText_HookButtons");
            var hookInfoText = CreateUiText("HarborHookInfoText_HookButtons");
            var hookLv1Button = CreateUiButton("HarborHookLv1Button_HookButtons");
            var hookLv2Button = CreateUiButton("HarborHookLv2Button_HookButtons");
            var hookLv3Button = CreateUiButton("HarborHookLv3Button_HookButtons");
            var hookLv1Icon = CreateUiImage("HarborHookLv1Icon_HookButtons");
            var hookLv2Icon = CreateUiImage("HarborHookLv2Icon_HookButtons");
            var hookLv3Icon = CreateUiImage("HarborHookLv3Icon_HookButtons");

            var routerRoot = new GameObject("HarborRouter_HookButtons");
            routerRoot.SetActive(false);
            _createdRoots.Add(routerRoot);
            var router = routerRoot.AddComponent<HarborSceneInteractionRouter>();
            router.Configure(
                interactables: new List<WorldInteractable>(),
                hookShop: hookShop,
                boatShop: null,
                fishShop: null,
                statusText: statusText,
                hookShopInfoText: hookInfoText,
                hookShopButtons: new List<Button> { hookLv1Button, hookLv2Button, hookLv3Button },
                hookShopIcons: new List<Image> { hookLv1Icon, hookLv2Icon, hookLv3Icon },
                saveManager: saveManager);

            routerRoot.SetActive(true);
            yield return null;

            Assert.That(hookLv1Button.interactable, Is.False, "Owned tier should not be purchasable.");
            Assert.That(hookLv2Button.interactable, Is.False, "Expected Lv2 to remain locked before unlock conditions are met.");
            Assert.That(hookLv3Button.interactable, Is.False, "Expected Lv3 to remain locked before unlock conditions are met.");

            saveManager.Current.copecs = 240;
            if (!saveManager.Current.progression.unlockedContentIds.Contains("hook_lv2"))
            {
                saveManager.Current.progression.unlockedContentIds.Add("hook_lv2");
            }

            saveManager.Save(forceImmediate: true);
            yield return null;

            Assert.That(hookLv2Button.interactable, Is.True, "Expected Lv2 to become available when unlocked and affordable.");
            var hookLv2AffordableColor = hookLv2Icon.color;
            Assert.That(hookLv2AffordableColor.g, Is.GreaterThan(0.9f));

            saveManager.Current.copecs = 10;
            saveManager.Save(forceImmediate: true);
            yield return null;

            Assert.That(hookLv2Button.interactable, Is.False, "Expected Lv2 to disable when copecs drop below price.");
            var hookLv2UnaffordableColor = hookLv2Icon.color;
            Assert.That(hookLv2UnaffordableColor.r, Is.GreaterThan(hookLv2UnaffordableColor.g));
        }

        private T CreateComponent<T>(string rootName) where T : Component
        {
            var root = new GameObject(rootName);
            _createdRoots.Add(root);
            return root.AddComponent<T>();
        }

        private TMP_Text CreateUiText(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            _createdRoots.Add(go);
            var text = go.GetComponent<TMP_Text>();
            text.text = string.Empty;
            return text;
        }

        private Button CreateUiButton(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            _createdRoots.Add(go);
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;

            var labelGo = new GameObject($"{name}_Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, worldPositionStays: false);
            var label = labelGo.GetComponent<TMP_Text>();
            label.text = name;
            return button;
        }

        private Image CreateUiImage(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _createdRoots.Add(go);
            return go.GetComponent<Image>();
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
    }
}
