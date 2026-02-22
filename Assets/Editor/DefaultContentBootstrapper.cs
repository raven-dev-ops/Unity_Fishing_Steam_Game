#if UNITY_EDITOR
using System.Collections.Generic;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Tools;
using UnityEditor;
using UnityEngine;

namespace RavenDevOps.Fishing.EditorTools
{
    public static class DefaultContentBootstrapper
    {
        private const string ConfigFolder = "Assets/Resources/Config";
        private const string FishFolder = ConfigFolder + "/Fish";
        private const string ShipFolder = ConfigFolder + "/Ships";
        private const string HookFolder = ConfigFolder + "/Hooks";
        private const string ConfigAssetPath = ConfigFolder + "/SO_GameConfig.asset";
        private const string TuningAssetPath = ConfigFolder + "/SO_TuningConfig.asset";

        [MenuItem("Raven/Bootstrap Default Content")]
        public static void BootstrapFromMenu()
        {
            BootstrapDefaultContent();
        }

        public static void BootstrapDefaultContent()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(ConfigFolder);
            EnsureFolder(FishFolder);
            EnsureFolder(ShipFolder);
            EnsureFolder(HookFolder);

            var fishCod = UpsertFishDefinition(
                FishFolder + "/SO_Fish_fish_cod.asset",
                "fish_cod",
                "Assets/Art/Sheets/Icons/icons_fish_sheet_v01.png",
                "fish_cod",
                minDistanceTier: 1,
                maxDistanceTier: 2,
                minDepth: 22f,
                maxDepth: 55f,
                rarityWeight: 12,
                baseValue: 18,
                minBiteDelaySeconds: 0.9f,
                maxBiteDelaySeconds: 2.1f,
                fightStamina: 4.5f,
                pullIntensity: 1.0f,
                escapeSeconds: 7.0f,
                minCatchWeightKg: 0.5f,
                maxCatchWeightKg: 2.0f);

            var fishSnapper = UpsertFishDefinition(
                FishFolder + "/SO_Fish_fish_coastal_snapper.asset",
                "fish_coastal_snapper",
                "Assets/Art/Sheets/Icons/icons_fish_sheet_v01.png",
                "fish_coastal_snapper",
                minDistanceTier: 2,
                maxDistanceTier: 3,
                minDepth: 35f,
                maxDepth: 90f,
                rarityWeight: 8,
                baseValue: 30,
                minBiteDelaySeconds: 1.0f,
                maxBiteDelaySeconds: 2.7f,
                fightStamina: 6.2f,
                pullIntensity: 1.3f,
                escapeSeconds: 8.5f,
                minCatchWeightKg: 0.9f,
                maxCatchWeightKg: 3.2f);

            var fishHeavy = UpsertFishDefinition(
                FishFolder + "/SO_Fish_fish_heavy.asset",
                "fish_heavy",
                "Assets/Art/Sheets/Icons/icons_fish_sheet_v01.png",
                "fish_heavy",
                minDistanceTier: 3,
                maxDistanceTier: 4,
                minDepth: 60f,
                maxDepth: 120f,
                rarityWeight: 5,
                baseValue: 52,
                minBiteDelaySeconds: 1.2f,
                maxBiteDelaySeconds: 3.2f,
                fightStamina: 8.5f,
                pullIntensity: 1.7f,
                escapeSeconds: 9.5f,
                minCatchWeightKg: 2.4f,
                maxCatchWeightKg: 6.0f);

            var shipLv1 = UpsertShipDefinition(
                ShipFolder + "/SO_Ship_ship_lv1.asset",
                "ship_lv1",
                "Assets/Art/Sheets/Icons/icons_ships_sheet_v01.png",
                "ship_lv1",
                price: 0,
                maxDistanceTier: 1,
                moveSpeed: 6.0f,
                cargoCapacity: 12);

            var shipLv2 = UpsertShipDefinition(
                ShipFolder + "/SO_Ship_ship_lv2.asset",
                "ship_lv2",
                "Assets/Art/Sheets/Icons/icons_ships_sheet_v01.png",
                "ship_lv2",
                price: 180,
                maxDistanceTier: 2,
                moveSpeed: 6.7f,
                cargoCapacity: 20);

            var shipLv3 = UpsertShipDefinition(
                ShipFolder + "/SO_Ship_ship_lv3.asset",
                "ship_lv3",
                "Assets/Art/Sheets/Icons/icons_ships_sheet_v01.png",
                "ship_lv3",
                price: 420,
                maxDistanceTier: 3,
                moveSpeed: 7.3f,
                cargoCapacity: 32);

            var hookLv1 = UpsertHookDefinition(
                HookFolder + "/SO_Hook_hook_lv1.asset",
                "hook_lv1",
                "Assets/Art/Sheets/Icons/icons_hooks_sheet_v01.png",
                "hook_lv1",
                price: 0,
                maxDepth: 40f);

            var hookLv2 = UpsertHookDefinition(
                HookFolder + "/SO_Hook_hook_lv2.asset",
                "hook_lv2",
                "Assets/Art/Sheets/Icons/icons_hooks_sheet_v01.png",
                "hook_lv2",
                price: 120,
                maxDepth: 75f);

            var hookLv3 = UpsertHookDefinition(
                HookFolder + "/SO_Hook_hook_lv3.asset",
                "hook_lv3",
                "Assets/Art/Sheets/Icons/icons_hooks_sheet_v01.png",
                "hook_lv3",
                price: 320,
                maxDepth: 120f);

            var config = LoadOrCreateAsset<GameConfigSO>(ConfigAssetPath);
            config.fishDefinitions = new[] { fishCod, fishSnapper, fishHeavy };
            config.shipDefinitions = new[] { shipLv1, shipLv2, shipLv3 };
            config.hookDefinitions = new[] { hookLv1, hookLv2, hookLv3 };
            EditorUtility.SetDirty(config);

            var tuningConfig = LoadOrCreateAsset<TuningConfigSO>(TuningAssetPath);
            tuningConfig.waveSpeedA = 0.3f;
            tuningConfig.waveSpeedB = 0.6f;
            tuningConfig.shipSpeedMultiplier = 1f;
            tuningConfig.hookSpeedMultiplier = 1f;
            tuningConfig.spawnRatePerMinute = 6f;
            tuningConfig.distanceTierSellStep = 0.25f;
            EditorUtility.SetDirty(tuningConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Default content bootstrap complete. Config asset: {ConfigAssetPath}");
        }

        private static FishDefinitionSO UpsertFishDefinition(
            string assetPath,
            string id,
            string spriteSheetPath,
            string spriteName,
            int minDistanceTier,
            int maxDistanceTier,
            float minDepth,
            float maxDepth,
            int rarityWeight,
            int baseValue,
            float minBiteDelaySeconds,
            float maxBiteDelaySeconds,
            float fightStamina,
            float pullIntensity,
            float escapeSeconds,
            float minCatchWeightKg,
            float maxCatchWeightKg)
        {
            var fish = LoadOrCreateAsset<FishDefinitionSO>(assetPath);
            fish.id = id;
            fish.icon = LoadSpriteFromSheet(spriteSheetPath, spriteName);
            fish.minDistanceTier = minDistanceTier;
            fish.maxDistanceTier = maxDistanceTier;
            fish.minDepth = minDepth;
            fish.maxDepth = maxDepth;
            fish.rarityWeight = rarityWeight;
            fish.baseValue = baseValue;
            fish.minBiteDelaySeconds = minBiteDelaySeconds;
            fish.maxBiteDelaySeconds = maxBiteDelaySeconds;
            fish.fightStamina = fightStamina;
            fish.pullIntensity = pullIntensity;
            fish.escapeSeconds = escapeSeconds;
            fish.minCatchWeightKg = minCatchWeightKg;
            fish.maxCatchWeightKg = maxCatchWeightKg;
            EditorUtility.SetDirty(fish);
            return fish;
        }

        private static ShipDefinitionSO UpsertShipDefinition(
            string assetPath,
            string id,
            string spriteSheetPath,
            string spriteName,
            int price,
            int maxDistanceTier,
            float moveSpeed,
            int cargoCapacity)
        {
            var ship = LoadOrCreateAsset<ShipDefinitionSO>(assetPath);
            ship.id = id;
            ship.icon = LoadSpriteFromSheet(spriteSheetPath, spriteName);
            ship.price = price;
            ship.maxDistanceTier = maxDistanceTier;
            ship.moveSpeed = moveSpeed;
            ship.cargoCapacity = cargoCapacity;
            EditorUtility.SetDirty(ship);
            return ship;
        }

        private static HookDefinitionSO UpsertHookDefinition(
            string assetPath,
            string id,
            string spriteSheetPath,
            string spriteName,
            int price,
            float maxDepth)
        {
            var hook = LoadOrCreateAsset<HookDefinitionSO>(assetPath);
            hook.id = id;
            hook.icon = LoadSpriteFromSheet(spriteSheetPath, spriteName);
            hook.price = price;
            hook.maxDepth = maxDepth;
            EditorUtility.SetDirty(hook);
            return hook;
        }

        private static Sprite LoadSpriteFromSheet(string sheetPath, string spriteName)
        {
            if (string.IsNullOrWhiteSpace(sheetPath) || string.IsNullOrWhiteSpace(spriteName))
            {
                return null;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            Debug.LogWarning($"DefaultContentBootstrapper: sprite '{spriteName}' not found at '{sheetPath}'.");
            return null;
        }

        private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var split = folderPath.Split('/');
            if (split.Length < 2 || split[0] != "Assets")
            {
                return;
            }

            var current = "Assets";
            for (var i = 1; i < split.Length; i++)
            {
                var next = current + "/" + split[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, split[i]);
                }

                current = next;
            }
        }
    }
}
#endif
