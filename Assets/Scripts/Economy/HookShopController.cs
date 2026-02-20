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

        public bool BuyOrEquip(string hookId)
        {
            if (_saveManager == null || string.IsNullOrWhiteSpace(hookId))
            {
                return false;
            }

            var save = _saveManager.Current;
            var price = ResolvePrice(hookId);
            if (price <= 0)
            {
                return false;
            }

            if (!save.ownedHooks.Contains(hookId))
            {
                if (save.copecs < price)
                {
                    return false;
                }

                save.copecs -= price;
                save.ownedHooks.Add(hookId);
            }

            save.equippedHookId = hookId;
            _saveManager.Save();
            return true;
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

            return 0;
        }
    }
}
