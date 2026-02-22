using System;
using System.Collections.Generic;
using System.Linq;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Economy
{
    public sealed class HookShopController : MonoBehaviour
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

        public bool BuyOrEquip(string hookId)
        {
            if (_saveManager == null || string.IsNullOrWhiteSpace(hookId))
            {
                return false;
            }

            var save = _saveManager.Current;
            if (save == null)
            {
                return false;
            }

            save.ownedHooks ??= new List<string>();
            if (!_saveManager.IsContentUnlocked(hookId))
            {
                var unlockLevel = _saveManager.GetUnlockLevel(hookId);
                Debug.Log($"HookShopController: '{hookId}' is locked until level {unlockLevel}.");
                return false;
            }

            var price = ResolvePrice(hookId);
            if (price < 0)
            {
                return false;
            }

            var wasOwned = save.ownedHooks.Contains(hookId);
            if (!wasOwned && !HasRequiredPreviousTierOwnership(hookId, save, out var requiredHookId))
            {
                Debug.Log($"HookShopController: '{hookId}' requires prior tier '{requiredHookId}' ownership.");
                return false;
            }

            if (!wasOwned)
            {
                if (save.copecs < price)
                {
                    return false;
                }

                save.copecs -= price;
                save.ownedHooks.Add(hookId);
            }

            save.equippedHookId = hookId;
            if (!wasOwned)
            {
                _saveManager.RecordPurchase(hookId, price, saveAfterRecord: false);
            }

            _saveManager.Save();
            return true;
        }

        public int GetPrice(string hookId)
        {
            return ResolvePrice(hookId);
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

            if (orderedIds.Count == 0 && _catalogService != null && _catalogService.HookById != null && _catalogService.HookById.Count > 0)
            {
                var catalogItems = new List<HookDefinitionSO>(_catalogService.HookById.Values);
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

                    var depthCompare = a.maxDepth.CompareTo(b.maxDepth);
                    if (depthCompare != 0)
                    {
                        return depthCompare;
                    }

                    var priceCompare = a.price.CompareTo(b.price);
                    if (priceCompare != 0)
                    {
                        return priceCompare;
                    }

                    return string.Compare(a.id, b.id, StringComparison.Ordinal);
                });

                for (var i = 0; i < catalogItems.Count; i++)
                {
                    var hook = catalogItems[i];
                    if (hook == null || string.IsNullOrWhiteSpace(hook.id))
                    {
                        continue;
                    }

                    if (!orderedIds.Contains(hook.id))
                    {
                        orderedIds.Add(hook.id);
                    }
                }
            }

            return orderedIds.ToArray();
        }

        private int ResolvePrice(string hookId)
        {
            var item = _items.FirstOrDefault(x => x.id == hookId);
            if (item != null)
            {
                return item.price;
            }

            if (_catalogService != null && _catalogService.TryGetHook(hookId, out var hookDefinition))
            {
                return hookDefinition.price;
            }

            return -1;
        }

        private bool HasRequiredPreviousTierOwnership(string hookId, SaveDataV1 save, out string requiredHookId)
        {
            requiredHookId = string.Empty;
            if (save == null || save.ownedHooks == null || _items == null || _items.Count == 0)
            {
                return true;
            }

            var target = _items.FirstOrDefault(x => x != null && string.Equals(x.id, hookId, StringComparison.Ordinal));
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

            requiredHookId = requiredTier.id;
            return save.ownedHooks.Contains(requiredTier.id);
        }
    }
}
