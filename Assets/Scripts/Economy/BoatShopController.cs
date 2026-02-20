using System.Collections.Generic;
using System.Linq;
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
            _saveManager ??= FindObjectOfType<SaveManager>();
            _catalogService ??= FindObjectOfType<CatalogService>();
        }

        public bool BuyOrEquip(string boatId)
        {
            if (_saveManager == null || string.IsNullOrWhiteSpace(boatId))
            {
                return false;
            }

            var save = _saveManager.Current;
            var price = ResolvePrice(boatId);
            if (price <= 0)
            {
                return false;
            }

            if (!save.ownedShips.Contains(boatId))
            {
                if (save.copecs < price)
                {
                    return false;
                }

                save.copecs -= price;
                save.ownedShips.Add(boatId);
            }

            save.equippedShipId = boatId;
            _saveManager.Save();
            return true;
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

            return 0;
        }
    }
}
