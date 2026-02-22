using System;
using System.Collections.Generic;
using System.Linq;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Economy
{
    public sealed class BoatShopController : MonoBehaviour
    {
        [SerializeField] private List<ShopItem> _items = new List<ShopItem>();
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private CatalogService _catalogService;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
        }

        public void ConfigureItems(List<ShopItem> items)
        {
            _items = items ?? new List<ShopItem>();
        }

        public bool BuyOrEquip(string boatId)
        {
            if (_saveManager == null || string.IsNullOrWhiteSpace(boatId))
            {
                return false;
            }

            var save = _saveManager.Current;
            if (save == null)
            {
                return false;
            }

            save.ownedShips ??= new List<string>();
            if (!_saveManager.IsContentUnlocked(boatId))
            {
                var unlockLevel = _saveManager.GetUnlockLevel(boatId);
                Debug.Log($"BoatShopController: '{boatId}' is locked until level {unlockLevel}.");
                return false;
            }

            var price = ResolvePrice(boatId);
            if (price < 0)
            {
                return false;
            }

            var wasOwned = save.ownedShips.Contains(boatId);
            if (!wasOwned && !HasRequiredPreviousTierOwnership(boatId, save, out var requiredBoatId))
            {
                Debug.Log($"BoatShopController: '{boatId}' requires prior tier '{requiredBoatId}' ownership.");
                return false;
            }

            if (!wasOwned)
            {
                if (save.copecs < price)
                {
                    return false;
                }

                save.copecs -= price;
                save.ownedShips.Add(boatId);
            }

            save.equippedShipId = boatId;
            if (!wasOwned)
            {
                _saveManager.RecordPurchase(boatId, price, saveAfterRecord: false);
            }

            _saveManager.Save();
            return true;
        }

        public int GetPrice(string boatId)
        {
            return ResolvePrice(boatId);
        }

        public string[] GetOrderedItemIds()
        {
            var orderedIds = new List<string>();
            if (_items != null && _items.Count > 0)
            {
                var runtimeItems = new List<ShopItem>();
                for (var i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
                    if (item == null || string.IsNullOrWhiteSpace(item.id))
                    {
                        continue;
                    }

                    runtimeItems.Add(item);
                }

                runtimeItems.Sort((a, b) =>
                {
                    if (a == b)
                    {
                        return 0;
                    }

                    if (a == null)
                    {
                        return 1;
                    }

                    if (b == null)
                    {
                        return -1;
                    }

                    var tierCompare = a.valueTier.CompareTo(b.valueTier);
                    if (tierCompare != 0)
                    {
                        return tierCompare;
                    }

                    return string.Compare(a.id, b.id, StringComparison.Ordinal);
                });

                for (var i = 0; i < runtimeItems.Count; i++)
                {
                    var id = runtimeItems[i].id;
                    if (!orderedIds.Contains(id))
                    {
                        orderedIds.Add(id);
                    }
                }
            }

            if (orderedIds.Count == 0 && _catalogService != null && _catalogService.ShipById != null && _catalogService.ShipById.Count > 0)
            {
                var catalogItems = new List<ShipDefinitionSO>(_catalogService.ShipById.Values);
                catalogItems.Sort((a, b) =>
                {
                    if (a == b)
                    {
                        return 0;
                    }

                    if (a == null)
                    {
                        return 1;
                    }

                    if (b == null)
                    {
                        return -1;
                    }

                    var tierCompare = a.maxDistanceTier.CompareTo(b.maxDistanceTier);
                    if (tierCompare != 0)
                    {
                        return tierCompare;
                    }

                    var capacityCompare = a.cargoCapacity.CompareTo(b.cargoCapacity);
                    if (capacityCompare != 0)
                    {
                        return capacityCompare;
                    }

                    return string.Compare(a.id, b.id, StringComparison.Ordinal);
                });

                for (var i = 0; i < catalogItems.Count; i++)
                {
                    var ship = catalogItems[i];
                    if (ship == null || string.IsNullOrWhiteSpace(ship.id))
                    {
                        continue;
                    }

                    if (!orderedIds.Contains(ship.id))
                    {
                        orderedIds.Add(ship.id);
                    }
                }
            }

            return orderedIds.ToArray();
        }

        private int ResolvePrice(string boatId)
        {
            var item = _items.FirstOrDefault(x => x.id == boatId);
            if (item != null)
            {
                return item.price;
            }

            if (_catalogService != null && _catalogService.TryGetShip(boatId, out var shipDefinition))
            {
                return shipDefinition.price;
            }

            return -1;
        }

        private bool HasRequiredPreviousTierOwnership(string boatId, SaveDataV1 save, out string requiredBoatId)
        {
            requiredBoatId = string.Empty;
            if (save == null || save.ownedShips == null || _items == null || _items.Count == 0)
            {
                return true;
            }

            var target = _items.FirstOrDefault(x => x != null && string.Equals(x.id, boatId, StringComparison.Ordinal));
            if (target == null)
            {
                return true;
            }

            var requiredTier = _items
                .Where(x => x != null && x.valueTier < target.valueTier)
                .OrderByDescending(x => x.valueTier)
                .FirstOrDefault();
            if (requiredTier == null || string.IsNullOrWhiteSpace(requiredTier.id))
            {
                return true;
            }

            requiredBoatId = requiredTier.id;
            return save.ownedShips.Contains(requiredTier.id);
        }
    }
}
