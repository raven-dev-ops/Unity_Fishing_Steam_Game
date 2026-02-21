using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Economy
{
    [Serializable]
    public sealed class GearSynergyDefinition
    {
        public string shipId = string.Empty;
        public string hookId = string.Empty;
        public float sellMultiplier = 1.1f;
        public string label = "Synergy";
    }

    public sealed class MetaLoopRuntimeService : MonoBehaviour
    {
        private const string ContractEntryId = "meta_contract_active";
        private const string CollectionSetId = "coastal_core";
        private const string CollectionTokenPrefix = "collection_token_";
        private const string CollectionCompleteTokenPrefix = "collection_complete_";

        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private int _contractTargetCount = 4;
        [SerializeField] private int _contractRewardCopecs = 150;
        [SerializeField] private int _collectionRewardCopecs = 220;
        [SerializeField] private List<GearSynergyDefinition> _gearSynergies = new List<GearSynergyDefinition>();

        public event Action MetaLoopUpdated;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Register(this);
            SeedDefaultSynergiesIfNeeded();
            EnsureContractEntry(saveIfChanged: true);
        }

        private void OnEnable()
        {
            if (_saveManager != null)
            {
                _saveManager.CatchRecorded += OnCatchRecorded;
                _saveManager.SaveDataChanged += OnSaveDataChanged;
            }
        }

        private void OnDisable()
        {
            if (_saveManager != null)
            {
                _saveManager.CatchRecorded -= OnCatchRecorded;
                _saveManager.SaveDataChanged -= OnSaveDataChanged;
            }
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public float GetMarketDemandMultiplier(string fishId)
        {
            if (string.IsNullOrWhiteSpace(fishId))
            {
                return 1f;
            }

            var day = DayCounterService.ComputeDayNumber(_saveManager != null ? _saveManager.Current.careerStartLocalDate : string.Empty);
            var hash = Mathf.Abs((fishId + day).GetHashCode());
            var bucket = hash % 7;
            var multiplier = 0.85f + (bucket * 0.06f);
            return Mathf.Clamp(multiplier, 0.8f, 1.25f);
        }

        public float GetGearSynergyMultiplier(string shipId, string hookId, out string label)
        {
            label = string.Empty;
            if (string.IsNullOrWhiteSpace(shipId) || string.IsNullOrWhiteSpace(hookId))
            {
                return 1f;
            }

            for (var i = 0; i < _gearSynergies.Count; i++)
            {
                var synergy = _gearSynergies[i];
                if (synergy == null)
                {
                    continue;
                }

                if (!string.Equals(synergy.shipId, shipId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.Equals(synergy.hookId, hookId, StringComparison.Ordinal))
                {
                    continue;
                }

                label = string.IsNullOrWhiteSpace(synergy.label) ? "Synergy" : synergy.label;
                return Mathf.Clamp(synergy.sellMultiplier, 1f, 1.5f);
            }

            return 1f;
        }

        public string BuildContractStatusLabel()
        {
            var entry = GetContractEntry();
            if (entry == null)
            {
                return "Contract: unavailable";
            }

            var current = Mathf.Clamp(entry.currentCount, 0, Mathf.Max(1, entry.targetCount));
            return $"Contract: {entry.description} ({current}/{entry.targetCount}) +{entry.rewardCopecs}c";
        }

        public string BuildCollectionStatusLabel()
        {
            var fishIds = ResolveCollectionFishIds();
            if (fishIds.Count == 0)
            {
                return "Collection: unavailable";
            }

            var unlocked = _saveManager != null && _saveManager.Current != null
                ? _saveManager.Current.progression.unlockedContentIds
                : null;

            var caughtCount = 0;
            for (var i = 0; i < fishIds.Count; i++)
            {
                var token = BuildCollectionToken(fishIds[i]);
                if (unlocked != null && unlocked.Contains(token))
                {
                    caughtCount++;
                }
            }

            var completionToken = $"{CollectionCompleteTokenPrefix}{CollectionSetId}";
            var completed = unlocked != null && unlocked.Contains(completionToken);
            return completed
                ? $"Collection: {CollectionSetId} complete (+{_collectionRewardCopecs}c claimed)"
                : $"Collection: {CollectionSetId} ({caughtCount}/{fishIds.Count}) +{_collectionRewardCopecs}c";
        }

        public string BuildMarketDemandSummary()
        {
            var ids = ResolveCollectionFishIds();
            if (ids.Count == 0)
            {
                return "Demand: neutral";
            }

            var fishId = ids[0];
            var multiplier = GetMarketDemandMultiplier(fishId);
            return $"Demand: {fishId} x{multiplier:0.00}";
        }

        public string BuildModifierLabel(string fishId, string shipId, string hookId)
        {
            var demand = GetMarketDemandMultiplier(fishId);
            var synergy = GetGearSynergyMultiplier(shipId, hookId, out var label);
            if (synergy > 1f && !string.IsNullOrWhiteSpace(label))
            {
                return $"Demand x{demand:0.00} | {label} x{synergy:0.00}";
            }

            return $"Demand x{demand:0.00}";
        }

        private void OnCatchRecorded(CatchLogEntry entry)
        {
            if (_saveManager == null || _saveManager.Current == null || entry == null || !entry.landed)
            {
                return;
            }

            var changed = false;
            changed |= UpdateContractProgress(entry.fishId);
            changed |= UpdateCollectionProgress(entry.fishId);
            if (!changed)
            {
                return;
            }

            _saveManager.Save();
            RaiseMetaLoopUpdated();
        }

        private void OnSaveDataChanged(SaveDataV1 data)
        {
            EnsureContractEntry(saveIfChanged: false);
            RaiseMetaLoopUpdated();
        }

        private bool UpdateContractProgress(string fishId)
        {
            var contract = GetContractEntry();
            if (contract == null || string.IsNullOrWhiteSpace(fishId))
            {
                return false;
            }

            var expectedFishId = ExtractContractFishId(contract.description);
            if (!string.Equals(expectedFishId, fishId, StringComparison.Ordinal))
            {
                return false;
            }

            var target = Mathf.Max(1, contract.targetCount);
            contract.currentCount = Mathf.Clamp(contract.currentCount + 1, 0, target);
            if (contract.currentCount < target)
            {
                return true;
            }

            _saveManager.Current.copecs += Mathf.Max(0, contract.rewardCopecs);
            RotateContract(contract);
            return true;
        }

        private bool UpdateCollectionProgress(string fishId)
        {
            if (string.IsNullOrWhiteSpace(fishId) || _saveManager == null || _saveManager.Current == null)
            {
                return false;
            }

            var fishIds = ResolveCollectionFishIds();
            if (!fishIds.Contains(fishId))
            {
                return false;
            }

            _saveManager.Current.progression ??= new ProgressionData();
            _saveManager.Current.progression.unlockedContentIds ??= new List<string>();

            var changed = false;
            var token = BuildCollectionToken(fishId);
            if (!_saveManager.Current.progression.unlockedContentIds.Contains(token))
            {
                _saveManager.Current.progression.unlockedContentIds.Add(token);
                changed = true;
            }

            var completionToken = $"{CollectionCompleteTokenPrefix}{CollectionSetId}";
            if (_saveManager.Current.progression.unlockedContentIds.Contains(completionToken))
            {
                return changed;
            }

            for (var i = 0; i < fishIds.Count; i++)
            {
                var requiredToken = BuildCollectionToken(fishIds[i]);
                if (!_saveManager.Current.progression.unlockedContentIds.Contains(requiredToken))
                {
                    return changed;
                }
            }

            _saveManager.Current.progression.unlockedContentIds.Add(completionToken);
            _saveManager.Current.copecs += Mathf.Max(0, _collectionRewardCopecs);
            return true;
        }

        private void EnsureContractEntry(bool saveIfChanged)
        {
            if (_saveManager == null || _saveManager.Current == null)
            {
                return;
            }

            _saveManager.Current.objectiveProgress ??= new ObjectiveProgressData();
            _saveManager.Current.objectiveProgress.entries ??= new List<ObjectiveProgressEntry>();

            var existing = GetContractEntry();
            if (existing != null)
            {
                existing.targetCount = Mathf.Max(1, _contractTargetCount);
                existing.rewardCopecs = Mathf.Max(0, _contractRewardCopecs);
                return;
            }

            var fishId = ResolveNextContractFishId();
            _saveManager.Current.objectiveProgress.entries.Add(new ObjectiveProgressEntry
            {
                id = ContractEntryId,
                description = BuildContractDescription(fishId),
                currentCount = 0,
                targetCount = Mathf.Max(1, _contractTargetCount),
                rewardCopecs = Mathf.Max(0, _contractRewardCopecs),
                completed = false
            });

            if (saveIfChanged)
            {
                _saveManager.Save();
            }
        }

        private ObjectiveProgressEntry GetContractEntry()
        {
            if (_saveManager == null || _saveManager.Current == null || _saveManager.Current.objectiveProgress == null || _saveManager.Current.objectiveProgress.entries == null)
            {
                return null;
            }

            for (var i = 0; i < _saveManager.Current.objectiveProgress.entries.Count; i++)
            {
                var entry = _saveManager.Current.objectiveProgress.entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (string.Equals(entry.id, ContractEntryId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private void RotateContract(ObjectiveProgressEntry contract)
        {
            var nextFish = ResolveNextContractFishId();
            contract.description = BuildContractDescription(nextFish);
            contract.targetCount = Mathf.Max(1, _contractTargetCount);
            contract.rewardCopecs = Mathf.Max(0, _contractRewardCopecs);
            contract.currentCount = 0;
            contract.completed = false;
        }

        private string ResolveNextContractFishId()
        {
            var candidates = ResolveCollectionFishIds();
            if (candidates.Count == 0)
            {
                return "fish_cod";
            }

            var day = DayCounterService.ComputeDayNumber(_saveManager != null ? _saveManager.Current.careerStartLocalDate : string.Empty);
            var index = Mathf.Abs(day + _saveManager.Current.stats.totalTrips + _saveManager.Current.stats.totalFishCaught) % candidates.Count;
            return candidates[index];
        }

        private List<string> ResolveCollectionFishIds()
        {
            var ids = new List<string>();
            if (_catalogService != null && _catalogService.FishById != null && _catalogService.FishById.Count > 0)
            {
                foreach (var pair in _catalogService.FishById)
                {
                    if (pair.Value == null || string.IsNullOrWhiteSpace(pair.Value.id))
                    {
                        continue;
                    }

                    ids.Add(pair.Value.id);
                    if (ids.Count >= 3)
                    {
                        break;
                    }
                }
            }

            if (ids.Count == 0)
            {
                ids.Add("fish_cod");
                ids.Add("fish_a");
                ids.Add("fish_b");
            }

            return ids;
        }

        private static string BuildContractDescription(string fishId)
        {
            return $"Catch {fishId}";
        }

        private static string ExtractContractFishId(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return string.Empty;
            }

            var prefix = "Catch ";
            return description.StartsWith(prefix, StringComparison.Ordinal) ? description.Substring(prefix.Length).Trim() : description.Trim();
        }

        private static string BuildCollectionToken(string fishId)
        {
            return $"{CollectionTokenPrefix}{CollectionSetId}_{fishId}";
        }

        private void SeedDefaultSynergiesIfNeeded()
        {
            if (_gearSynergies.Count > 0)
            {
                return;
            }

            _gearSynergies.Add(new GearSynergyDefinition
            {
                shipId = "ship_lv1",
                hookId = "hook_lv1",
                sellMultiplier = 1.06f,
                label = "Starter Synergy"
            });
            _gearSynergies.Add(new GearSynergyDefinition
            {
                shipId = "ship_lv2",
                hookId = "hook_lv2",
                sellMultiplier = 1.1f,
                label = "Coastal Synergy"
            });
        }

        private void RaiseMetaLoopUpdated()
        {
            try
            {
                MetaLoopUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"MetaLoopRuntimeService: MetaLoopUpdated listener failed ({ex.Message}).");
            }
        }
    }
}
