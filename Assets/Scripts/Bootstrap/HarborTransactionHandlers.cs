using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Audio;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    internal sealed class HarborTransactionResult
    {
        public string StatusMessage { get; set; } = string.Empty;
        public string ActivityMessage { get; set; } = string.Empty;
        public SfxEvent? SoundEffect { get; set; }
        public bool RefreshSaveSnapshot { get; set; }
        public bool RefreshShopMenuDetails { get; set; }
        public bool RefreshShipyardDetails { get; set; }
        public bool RequestOpenFishing { get; set; }
    }

    internal sealed class HarborShopTransactionHandler
    {
        public sealed class DependencyBundle
        {
            public SaveManager SaveManager { get; set; }
            public HookShopController HookShop { get; set; }
            public BoatShopController BoatShop { get; set; }
        }

        private SaveManager _saveManager;
        private HookShopController _hookShop;
        private BoatShopController _boatShop;
        private bool _unlockAllShopItemsForQa;

        public void ConfigureDependencies(DependencyBundle dependencies)
        {
            if (dependencies == null)
            {
                return;
            }

            _saveManager = dependencies.SaveManager ?? _saveManager;
            _hookShop = dependencies.HookShop ?? _hookShop;
            _boatShop = dependencies.BoatShop ?? _boatShop;
        }

        public void SetUnlockAllShopItemsForQa(bool enabled)
        {
            _unlockAllShopItemsForQa = enabled;
        }

        public HarborTransactionResult HandleHookPurchase(string hookId, string[] hookOrder)
        {
            var result = new HarborTransactionResult();
            if (_hookShop == null)
            {
                result.StatusMessage = "Warehouse is unavailable.";
                result.ActivityMessage = "Warehouse unavailable.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(hookId))
            {
                result.StatusMessage = "No hook selected.";
                return result;
            }

            if (_saveManager == null || _saveManager.Current == null)
            {
                result.StatusMessage = "Cannot process hook purchase without save data.";
                result.ActivityMessage = "Warehouse unavailable: save not ready.";
                return result;
            }

            var save = _saveManager.Current;
            save.ownedHooks ??= new List<string>();
            var wasOwned = save.ownedHooks.Contains(hookId);
            var price = Mathf.Max(0, _hookShop.GetPrice(hookId));
            var hasRequiredTier = HasRequiredPreviousTierForShop(save.ownedHooks, hookId, hookOrder, out var requiredTierId);

            if (wasOwned)
            {
                result.StatusMessage = $"{HarborTextFormatting.ToDisplayLabel(hookId)} already in inventory. Equip it in Shipyard.";
                result.ActivityMessage = $"Warehouse: {HarborTextFormatting.ToDisplayLabel(hookId)} already owned.";
                result.RefreshShopMenuDetails = true;
                return result;
            }

            if (!IsShopItemUnlocked(hookId))
            {
                var unlockLevel = _saveManager.GetUnlockLevel(hookId);
                result.StatusMessage = $"{HarborTextFormatting.ToDisplayLabel(hookId)} unlocks at level {unlockLevel}.";
                result.ActivityMessage = $"Warehouse: {HarborTextFormatting.ToDisplayLabel(hookId)} is locked.";
                result.RefreshShopMenuDetails = true;
                return result;
            }

            if (!hasRequiredTier)
            {
                var requiredLabel = HarborTextFormatting.ToDisplayLabel(requiredTierId);
                result.StatusMessage = $"Need {requiredLabel} before buying {HarborTextFormatting.ToDisplayLabel(hookId)}.";
                result.ActivityMessage = $"Warehouse: {HarborTextFormatting.ToDisplayLabel(hookId)} requires {requiredLabel}.";
                result.RefreshShopMenuDetails = true;
                return result;
            }

            if (save.copecs < price)
            {
                result.StatusMessage = $"Need {price} copecs for {HarborTextFormatting.ToDisplayLabel(hookId)}. Balance: {save.copecs}.";
                result.ActivityMessage = $"Warehouse: insufficient copecs for {HarborTextFormatting.ToDisplayLabel(hookId)}.";
                result.RefreshShopMenuDetails = true;
                return result;
            }

            if (!_hookShop.BuyOrEquip(hookId))
            {
                result.StatusMessage = $"Could not purchase {HarborTextFormatting.ToDisplayLabel(hookId)}.";
                result.ActivityMessage = $"Warehouse: action failed for {HarborTextFormatting.ToDisplayLabel(hookId)}.";
                result.RefreshShopMenuDetails = true;
                return result;
            }

            result.StatusMessage = $"Purchased {HarborTextFormatting.ToDisplayLabel(hookId)} for {price} copecs. Equip it in Shipyard. Balance: {CurrentCopecs()} copecs.";
            result.ActivityMessage = $"Warehouse: purchased {HarborTextFormatting.ToDisplayLabel(hookId)}.";
            result.SoundEffect = SfxEvent.Purchase;
            result.RefreshSaveSnapshot = true;
            result.RefreshShopMenuDetails = true;
            return result;
        }

        public HarborTransactionResult HandleBoatPurchase(string shipId, string[] shipOrder)
        {
            var result = new HarborTransactionResult();
            if (_boatShop == null)
            {
                result.StatusMessage = "Dockyard is unavailable.";
                result.ActivityMessage = "Dockyard unavailable.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(shipId))
            {
                result.StatusMessage = "No ship selected.";
                return result;
            }

            if (_saveManager == null || _saveManager.Current == null)
            {
                result.StatusMessage = "Cannot process boat purchase without save data.";
                result.ActivityMessage = "Dockyard unavailable: save not ready.";
                return result;
            }

            var save = _saveManager.Current;
            save.ownedShips ??= new List<string>();
            var wasOwned = save.ownedShips.Contains(shipId);
            var price = Mathf.Max(0, _boatShop.GetPrice(shipId));
            var hasRequiredTier = HasRequiredPreviousTierForShop(save.ownedShips, shipId, shipOrder, out var requiredTierId);

            if (wasOwned)
            {
                result.StatusMessage = $"{HarborTextFormatting.ToDisplayLabel(shipId)} already in inventory. Select it in Shipyard.";
                result.ActivityMessage = $"Dockyard: {HarborTextFormatting.ToDisplayLabel(shipId)} already owned.";
                result.RefreshShopMenuDetails = true;
                return result;
            }

            if (!IsShopItemUnlocked(shipId))
            {
                var unlockLevel = _saveManager.GetUnlockLevel(shipId);
                result.StatusMessage = $"{HarborTextFormatting.ToDisplayLabel(shipId)} unlocks at level {unlockLevel}.";
                result.ActivityMessage = $"Dockyard: {HarborTextFormatting.ToDisplayLabel(shipId)} is locked.";
                result.RefreshShopMenuDetails = true;
                return result;
            }

            if (!hasRequiredTier)
            {
                var requiredLabel = HarborTextFormatting.ToDisplayLabel(requiredTierId);
                result.StatusMessage = $"Need {requiredLabel} before buying {HarborTextFormatting.ToDisplayLabel(shipId)}.";
                result.ActivityMessage = $"Dockyard: {HarborTextFormatting.ToDisplayLabel(shipId)} requires {requiredLabel}.";
                result.RefreshShopMenuDetails = true;
                return result;
            }

            if (save.copecs < price)
            {
                result.StatusMessage = $"Need {price} copecs for {HarborTextFormatting.ToDisplayLabel(shipId)}. Balance: {save.copecs}.";
                result.ActivityMessage = $"Dockyard: insufficient copecs for {HarborTextFormatting.ToDisplayLabel(shipId)}.";
                result.RefreshShopMenuDetails = true;
                return result;
            }

            if (!_boatShop.BuyOrEquip(shipId))
            {
                result.StatusMessage = $"Could not purchase {HarborTextFormatting.ToDisplayLabel(shipId)}.";
                result.ActivityMessage = $"Dockyard: action failed for {HarborTextFormatting.ToDisplayLabel(shipId)}.";
                result.RefreshShopMenuDetails = true;
                return result;
            }

            result.StatusMessage = $"Purchased {HarborTextFormatting.ToDisplayLabel(shipId)} for {price} copecs. Select it in Shipyard. Balance: {CurrentCopecs()} copecs.";
            result.ActivityMessage = $"Dockyard: purchased {HarborTextFormatting.ToDisplayLabel(shipId)}.";
            result.SoundEffect = SfxEvent.Purchase;
            result.RefreshSaveSnapshot = true;
            result.RefreshShopMenuDetails = true;
            return result;
        }

        private bool IsShopItemUnlocked(string itemId)
        {
            if (_unlockAllShopItemsForQa)
            {
                return true;
            }

            return _saveManager != null && _saveManager.IsContentUnlocked(itemId);
        }

        private bool HasRequiredPreviousTierForShop(List<string> ownedIds, string itemId, string[] orderedIds, out string requiredTierId)
        {
            if (_unlockAllShopItemsForQa)
            {
                requiredTierId = string.Empty;
                return true;
            }

            return HasRequiredPreviousTier(ownedIds, itemId, orderedIds, out requiredTierId);
        }

        private static bool HasRequiredPreviousTier(List<string> ownedIds, string itemId, string[] orderedIds, out string requiredTierId)
        {
            requiredTierId = string.Empty;
            if (string.IsNullOrWhiteSpace(itemId) || orderedIds == null || orderedIds.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < orderedIds.Length; i++)
            {
                if (!string.Equals(orderedIds[i], itemId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (i <= 0)
                {
                    return true;
                }

                requiredTierId = orderedIds[i - 1];
                return ownedIds != null && ownedIds.Contains(requiredTierId);
            }

            return true;
        }

        private int CurrentCopecs()
        {
            return _saveManager != null && _saveManager.Current != null
                ? _saveManager.Current.copecs
                : 0;
        }
    }

    internal sealed class HarborFisheryTransactionHandler
    {
        public sealed class DependencyBundle
        {
            public SaveManager SaveManager { get; set; }
            public FishShopController FishShop { get; set; }
        }

        private SaveManager _saveManager;
        private FishShopController _fishShop;

        public void ConfigureDependencies(DependencyBundle dependencies)
        {
            if (dependencies == null)
            {
                return;
            }

            _saveManager = dependencies.SaveManager ?? _saveManager;
            _fishShop = dependencies.FishShop ?? _fishShop;
        }

        public HarborTransactionResult HandleSellAll()
        {
            var result = new HarborTransactionResult();
            if (_fishShop == null)
            {
                result.StatusMessage = "Fishery is unavailable.";
                result.ActivityMessage = "Fishery unavailable.";
                return result;
            }

            var summary = _fishShop.PreviewSellAll();
            var pendingCount = Mathf.Max(0, summary.itemCount);
            if (pendingCount <= 0)
            {
                result.StatusMessage = "No fish in cargo to sell.";
                result.ActivityMessage = "Fishery: cargo already empty.";
                result.RefreshShopMenuDetails = true;
                return result;
            }

            var saleResult = _fishShop.SellAllDetailed();
            var earned = Mathf.Max(0, saleResult.totalEarnedCopecs);
            var dailyBonus = Mathf.Max(0, saleResult.dailyBonusEarnedCopecs);
            if (dailyBonus > 0)
            {
                result.StatusMessage = $"Sold {pendingCount} fish for {saleResult.baseEarnedCopecs} copecs + daily bonus {dailyBonus}c. Balance: {CurrentCopecs()} copecs.";
                result.ActivityMessage = $"Fishery: sold {pendingCount} fish for {earned} copecs (daily bonus +{dailyBonus}c).";
            }
            else
            {
                result.StatusMessage = $"Sold {pendingCount} fish for {earned} copecs. Balance: {CurrentCopecs()} copecs.";
                result.ActivityMessage = $"Fishery: sold {pendingCount} fish for {earned} copecs.";
            }

            result.SoundEffect = SfxEvent.Sell;
            result.RefreshSaveSnapshot = true;
            result.RefreshShopMenuDetails = true;
            return result;
        }

        public HarborTransactionResult HandleQuestAccept()
        {
            var result = new HarborTransactionResult();
            if (_fishShop == null)
            {
                result.StatusMessage = "Fishery is unavailable.";
                result.ActivityMessage = "Fishery unavailable.";
                return result;
            }

            if (_fishShop.AcceptQuest(out var message))
            {
                result.StatusMessage = message;
                result.ActivityMessage = "Fishing Charter accepted.";
                result.SoundEffect = SfxEvent.UiSelect;
            }
            else
            {
                result.StatusMessage = message;
                result.ActivityMessage = "Fishing Charter not accepted.";
                result.SoundEffect = SfxEvent.UiCancel;
            }

            result.RefreshSaveSnapshot = true;
            result.RefreshShopMenuDetails = true;
            return result;
        }

        public HarborTransactionResult HandleQuestClaim()
        {
            var result = new HarborTransactionResult();
            if (_fishShop == null)
            {
                result.StatusMessage = "Fishery is unavailable.";
                result.ActivityMessage = "Fishery unavailable.";
                return result;
            }

            if (_fishShop.ClaimQuestReward(out var rewardCopecs, out var message))
            {
                result.StatusMessage = message;
                result.ActivityMessage = $"Fishing Charter fulfilled (+{rewardCopecs}c).";
                result.SoundEffect = SfxEvent.Purchase;
            }
            else
            {
                result.StatusMessage = message;
                result.ActivityMessage = "Fishing Charter not ready.";
                result.SoundEffect = SfxEvent.UiCancel;
            }

            result.RefreshSaveSnapshot = true;
            result.RefreshShopMenuDetails = true;
            return result;
        }

        private int CurrentCopecs()
        {
            return _saveManager != null && _saveManager.Current != null
                ? _saveManager.Current.copecs
                : 0;
        }
    }

    internal sealed class HarborShipyardTransactionHandler
    {
        public sealed class DependencyBundle
        {
            public SaveManager SaveManager { get; set; }
        }

        private SaveManager _saveManager;

        public void ConfigureDependencies(DependencyBundle dependencies)
        {
            if (dependencies == null)
            {
                return;
            }

            _saveManager = dependencies.SaveManager ?? _saveManager;
        }

        public HarborTransactionResult HandleEquipHook(string hookId)
        {
            var result = new HarborTransactionResult();
            if (_saveManager == null || _saveManager.Current == null || string.IsNullOrWhiteSpace(hookId))
            {
                return result;
            }

            var save = _saveManager.Current;
            save.ownedHooks ??= new List<string>();
            if (!save.ownedHooks.Contains(hookId))
            {
                result.StatusMessage = $"{HarborTextFormatting.ToDisplayLabel(hookId)} is not in inventory.";
                result.ActivityMessage = $"Shipyard: missing {HarborTextFormatting.ToDisplayLabel(hookId)}.";
                result.SoundEffect = SfxEvent.UiCancel;
                result.RefreshShipyardDetails = true;
                return result;
            }

            if (string.Equals(save.equippedHookId, hookId, StringComparison.Ordinal))
            {
                result.StatusMessage = $"{HarborTextFormatting.ToDisplayLabel(hookId)} is already equipped.";
                result.ActivityMessage = $"Shipyard: {HarborTextFormatting.ToDisplayLabel(hookId)} already equipped.";
                result.RefreshShipyardDetails = true;
                return result;
            }

            save.equippedHookId = hookId;
            _saveManager.Save();
            result.StatusMessage = $"Equipped {HarborTextFormatting.ToDisplayLabel(hookId)} in shipyard.";
            result.ActivityMessage = $"Shipyard: equipped {HarborTextFormatting.ToDisplayLabel(hookId)}.";
            result.SoundEffect = SfxEvent.UiSelect;
            result.RefreshSaveSnapshot = true;
            result.RefreshShopMenuDetails = true;
            return result;
        }

        public HarborTransactionResult HandleEquipShip(string shipId)
        {
            var result = new HarborTransactionResult();
            if (_saveManager == null || _saveManager.Current == null || string.IsNullOrWhiteSpace(shipId))
            {
                return result;
            }

            var save = _saveManager.Current;
            save.ownedShips ??= new List<string>();
            if (!save.ownedShips.Contains(shipId))
            {
                result.StatusMessage = $"{HarborTextFormatting.ToDisplayLabel(shipId)} is not in inventory.";
                result.ActivityMessage = $"Shipyard: missing {HarborTextFormatting.ToDisplayLabel(shipId)}.";
                result.SoundEffect = SfxEvent.UiCancel;
                result.RefreshShipyardDetails = true;
                return result;
            }

            if (string.Equals(save.equippedShipId, shipId, StringComparison.Ordinal))
            {
                result.StatusMessage = $"{HarborTextFormatting.ToDisplayLabel(shipId)} is already selected.";
                result.ActivityMessage = $"Shipyard: {HarborTextFormatting.ToDisplayLabel(shipId)} already selected.";
                result.RefreshShipyardDetails = true;
                return result;
            }

            save.equippedShipId = shipId;
            _saveManager.Save();
            result.StatusMessage = $"Selected {HarborTextFormatting.ToDisplayLabel(shipId)} in shipyard.";
            result.ActivityMessage = $"Shipyard: selected {HarborTextFormatting.ToDisplayLabel(shipId)}.";
            result.SoundEffect = SfxEvent.UiSelect;
            result.RefreshSaveSnapshot = true;
            result.RefreshShopMenuDetails = true;
            return result;
        }

        public HarborTransactionResult HandleSail(Func<string, int> cargoCapacityResolver)
        {
            var result = new HarborTransactionResult();
            if (_saveManager != null && _saveManager.Current != null && IsCargoFull(_saveManager.Current, cargoCapacityResolver, out var fishCount, out var cargoCapacity))
            {
                result.StatusMessage = $"Cargo full ({fishCount}/{cargoCapacity}). Sell fish at the Fishery before sailing.";
                result.ActivityMessage = "Departure blocked: cargo is full.";
                result.SoundEffect = SfxEvent.UiCancel;
                result.RefreshSaveSnapshot = true;
                return result;
            }

            result.StatusMessage = "Casting off for fishing grounds...";
            result.ActivityMessage = "Departure confirmed. Sailing to fishing waters.";
            result.SoundEffect = SfxEvent.Depart;
            result.RequestOpenFishing = true;
            return result;
        }

        private static bool IsCargoFull(SaveDataV1 save, Func<string, int> cargoCapacityResolver, out int fishCount, out int cargoCapacity)
        {
            fishCount = CountCargoFish(save);
            cargoCapacity = cargoCapacityResolver != null
                ? Mathf.Max(1, cargoCapacityResolver(save != null ? save.equippedShipId : string.Empty))
                : 1;
            return fishCount >= cargoCapacity;
        }

        private static int CountCargoFish(SaveDataV1 save)
        {
            if (save == null || save.fishInventory == null || save.fishInventory.Count == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < save.fishInventory.Count; i++)
            {
                var entry = save.fishInventory[i];
                if (entry == null)
                {
                    continue;
                }

                count += Mathf.Max(0, entry.count);
            }

            return count;
        }
    }
}
