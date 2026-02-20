using System.Collections.Generic;
using UnityEngine;

namespace RavenDevOps.Fishing.Data
{
    public sealed class CatalogService : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _gameConfig;

        private readonly Dictionary<string, FishDefinitionSO> _fishById = new Dictionary<string, FishDefinitionSO>();
        private readonly Dictionary<string, ShipDefinitionSO> _shipById = new Dictionary<string, ShipDefinitionSO>();
        private readonly Dictionary<string, HookDefinitionSO> _hookById = new Dictionary<string, HookDefinitionSO>();

        public IReadOnlyDictionary<string, FishDefinitionSO> FishById => _fishById;
        public IReadOnlyDictionary<string, ShipDefinitionSO> ShipById => _shipById;
        public IReadOnlyDictionary<string, HookDefinitionSO> HookById => _hookById;

        private void Awake()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _fishById.Clear();
            _shipById.Clear();
            _hookById.Clear();

            if (_gameConfig == null)
            {
                Debug.LogWarning("CatalogService: Missing GameConfigSO reference.");
                return;
            }

            BuildFishCatalog();
            BuildShipCatalog();
            BuildHookCatalog();
        }

        public bool TryGetFish(string id, out FishDefinitionSO fish)
        {
            return _fishById.TryGetValue(id, out fish);
        }

        public bool TryGetShip(string id, out ShipDefinitionSO ship)
        {
            return _shipById.TryGetValue(id, out ship);
        }

        public bool TryGetHook(string id, out HookDefinitionSO hook)
        {
            return _hookById.TryGetValue(id, out hook);
        }

        private void BuildFishCatalog()
        {
            if (_gameConfig.fishDefinitions == null)
            {
                return;
            }

            foreach (var fish in _gameConfig.fishDefinitions)
            {
                if (fish == null || string.IsNullOrWhiteSpace(fish.id))
                {
                    Debug.LogError("CatalogService: Fish definition missing or has empty id.");
                    continue;
                }

                if (_fishById.ContainsKey(fish.id))
                {
                    Debug.LogError($"CatalogService: Duplicate fish id '{fish.id}'.");
                    continue;
                }

                _fishById.Add(fish.id, fish);
            }
        }

        private void BuildShipCatalog()
        {
            if (_gameConfig.shipDefinitions == null)
            {
                return;
            }

            foreach (var ship in _gameConfig.shipDefinitions)
            {
                if (ship == null || string.IsNullOrWhiteSpace(ship.id))
                {
                    Debug.LogError("CatalogService: Ship definition missing or has empty id.");
                    continue;
                }

                if (_shipById.ContainsKey(ship.id))
                {
                    Debug.LogError($"CatalogService: Duplicate ship id '{ship.id}'.");
                    continue;
                }

                _shipById.Add(ship.id, ship);
            }
        }

        private void BuildHookCatalog()
        {
            if (_gameConfig.hookDefinitions == null)
            {
                return;
            }

            foreach (var hook in _gameConfig.hookDefinitions)
            {
                if (hook == null || string.IsNullOrWhiteSpace(hook.id))
                {
                    Debug.LogError("CatalogService: Hook definition missing or has empty id.");
                    continue;
                }

                if (_hookById.ContainsKey(hook.id))
                {
                    Debug.LogError($"CatalogService: Duplicate hook id '{hook.id}'.");
                    continue;
                }

                _hookById.Add(hook.id, hook);
            }
        }
    }
}
