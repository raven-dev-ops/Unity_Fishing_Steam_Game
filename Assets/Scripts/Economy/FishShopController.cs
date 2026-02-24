using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Economy
{
    [Serializable]
    public sealed class FishShopSaleResult
    {
        public int itemCount;
        public int baseEarnedCopecs;
        public int dailyBonusEarnedCopecs;
        public int totalEarnedCopecs;
    }

    [Serializable]
    public sealed class FishMarketSnapshot
    {
        public string dailyFishId = string.Empty;
        public int dailyProgressCount;
        public int dailyRequiredCount;
        public int dailyBonusCopecs;
        public bool dailyBonusGranted;

        public string questFishId = string.Empty;
        public int questProgressCount;
        public int questRequiredCount;
        public int questRewardCopecs;
        public bool questAccepted;
        public bool questCompleted;
        public bool questClaimed;

        public List<FishSaleHistoryEntry> recentSales = new List<FishSaleHistoryEntry>();
    }

    public sealed class FishShopController : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private SellSummaryCalculator _sellSummaryCalculator;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private int _dailyFishRequiredCount = 5;
        [SerializeField] private int _dailyFishBonusCopecs = 90;
        [SerializeField] private int _questRequiredCount = 5;
        [SerializeField] private int _questRewardCopecs = 140;
        [SerializeField] private int _maxSaleHistoryEntries = 160;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _sellSummaryCalculator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
        }

        public int SellAll()
        {
            return SellAllDetailed().totalEarnedCopecs;
        }

        public FishShopSaleResult SellAllDetailed()
        {
            var result = new FishShopSaleResult();
            var save = _saveManager != null ? _saveManager.Current : null;
            if (save == null)
            {
                return result;
            }

            var changed = EnsureDailyMarketState(save);
            var summary = _sellSummaryCalculator != null
                ? _sellSummaryCalculator.Calculate(save.fishInventory)
                : new SellSummary();

            result.itemCount = Mathf.Max(0, summary.itemCount);
            result.baseEarnedCopecs = Mathf.Max(0, summary.totalEarned);
            if (result.itemCount <= 0)
            {
                if (changed)
                {
                    _saveManager.Save();
                }

                return result;
            }

            var dailyBonusAwarded = 0;
            var soldAtUtc = DateTime.UtcNow.ToString("O");
            AppendSaleHistoryAndProgress(save, summary, soldAtUtc, ref dailyBonusAwarded);

            result.dailyBonusEarnedCopecs = Mathf.Max(0, dailyBonusAwarded);
            result.totalEarnedCopecs = result.baseEarnedCopecs + result.dailyBonusEarnedCopecs;

            save.copecs += result.totalEarnedCopecs;
            save.fishInventory ??= new List<FishInventoryEntry>();
            save.fishInventory.Clear();
            _saveManager.Save();
            return result;
        }

        public SellSummary PreviewSellAll()
        {
            if (_saveManager == null)
            {
                return new SellSummary();
            }

            return _sellSummaryCalculator != null
                ? _sellSummaryCalculator.Calculate(_saveManager.Current.fishInventory)
                : new SellSummary();
        }

        public bool AcceptQuest(out string statusMessage)
        {
            statusMessage = "Fishery unavailable.";
            var save = _saveManager != null ? _saveManager.Current : null;
            if (save == null)
            {
                return false;
            }

            var changed = EnsureDailyMarketState(save);
            save.fishingMarketQuest ??= new FishingMarketQuestState();
            var quest = save.fishingMarketQuest;
            if (quest.claimed)
            {
                statusMessage = "Today's Fishing Charter was already fulfilled.";
                if (changed)
                {
                    _saveManager.Save();
                }

                return false;
            }

            if (quest.accepted)
            {
                statusMessage = $"Fishing Charter already accepted: sell {Mathf.Max(1, quest.requiredCount)} {ToDisplayLabel(quest.fishId)}.";
                if (changed)
                {
                    _saveManager.Save();
                }

                return false;
            }

            quest.accepted = true;
            _saveManager.Save();
            statusMessage = $"Fishing Charter accepted: sell {Mathf.Max(1, quest.requiredCount)} {ToDisplayLabel(quest.fishId)} for +{Mathf.Max(0, quest.rewardCopecs)}c.";
            return true;
        }

        public bool ClaimQuestReward(out int rewardCopecs, out string statusMessage)
        {
            rewardCopecs = 0;
            statusMessage = "Fishery unavailable.";
            var save = _saveManager != null ? _saveManager.Current : null;
            if (save == null)
            {
                return false;
            }

            var changed = EnsureDailyMarketState(save);
            save.fishingMarketQuest ??= new FishingMarketQuestState();
            var quest = save.fishingMarketQuest;
            if (!quest.accepted && !quest.completed)
            {
                statusMessage = "Accept today's Fishing Charter in the Fishery first.";
                if (changed)
                {
                    _saveManager.Save();
                }

                return false;
            }

            if (!quest.completed)
            {
                statusMessage = $"Fishing Charter progress: {Mathf.Clamp(quest.progressCount, 0, Mathf.Max(1, quest.requiredCount))}/{Mathf.Max(1, quest.requiredCount)} {ToDisplayLabel(quest.fishId)} sold.";
                if (changed)
                {
                    _saveManager.Save();
                }

                return false;
            }

            if (quest.claimed)
            {
                statusMessage = "Fishing Charter reward already claimed.";
                if (changed)
                {
                    _saveManager.Save();
                }

                return false;
            }

            rewardCopecs = Mathf.Max(0, quest.rewardCopecs);
            save.copecs += rewardCopecs;
            quest.claimed = true;
            quest.accepted = false;
            _saveManager.Save();
            statusMessage = $"Fishing Charter fulfilled: +{rewardCopecs} copecs.";
            return true;
        }

        public FishMarketSnapshot BuildMarketSnapshot(int maxHistoryEntries = 6)
        {
            var snapshot = new FishMarketSnapshot();
            var save = _saveManager != null ? _saveManager.Current : null;
            if (save == null)
            {
                return snapshot;
            }

            var changed = EnsureDailyMarketState(save);
            if (changed)
            {
                _saveManager.Save();
            }

            var daily = save.dailyFishBonus ?? new DailyFishBonusState();
            snapshot.dailyFishId = daily.fishId ?? string.Empty;
            snapshot.dailyProgressCount = Mathf.Max(0, daily.progressCount);
            snapshot.dailyRequiredCount = Mathf.Max(1, daily.requiredCount);
            snapshot.dailyBonusCopecs = Mathf.Max(0, daily.bonusCopecs);
            snapshot.dailyBonusGranted = daily.rewardGranted;

            var quest = save.fishingMarketQuest ?? new FishingMarketQuestState();
            snapshot.questFishId = quest.fishId ?? string.Empty;
            snapshot.questProgressCount = Mathf.Max(0, quest.progressCount);
            snapshot.questRequiredCount = Mathf.Max(1, quest.requiredCount);
            snapshot.questRewardCopecs = Mathf.Max(0, quest.rewardCopecs);
            snapshot.questAccepted = quest.accepted;
            snapshot.questCompleted = quest.completed;
            snapshot.questClaimed = quest.claimed;

            save.fishSaleHistory ??= new List<FishSaleHistoryEntry>();
            var historyCount = Mathf.Clamp(maxHistoryEntries, 1, 24);
            for (var i = save.fishSaleHistory.Count - 1; i >= 0 && snapshot.recentSales.Count < historyCount; i--)
            {
                var entry = save.fishSaleHistory[i];
                if (entry == null)
                {
                    continue;
                }

                snapshot.recentSales.Add(new FishSaleHistoryEntry
                {
                    fishId = entry.fishId ?? string.Empty,
                    distanceTier = Mathf.Max(1, entry.distanceTier),
                    count = Mathf.Max(0, entry.count),
                    earnedCopecs = Mathf.Max(0, entry.earnedCopecs),
                    timestampUtc = entry.timestampUtc ?? string.Empty,
                    dailyFishTarget = entry.dailyFishTarget
                });
            }

            return snapshot;
        }

        private void AppendSaleHistoryAndProgress(SaveDataV1 save, SellSummary summary, string soldAtUtc, ref int dailyBonusAwarded)
        {
            if (save == null || summary == null)
            {
                return;
            }

            save.fishSaleHistory ??= new List<FishSaleHistoryEntry>();
            save.dailyFishBonus ??= new DailyFishBonusState();
            save.fishingMarketQuest ??= new FishingMarketQuestState();
            var daily = save.dailyFishBonus;
            var quest = save.fishingMarketQuest;

            var dailySoldCount = 0;
            var questSoldCount = 0;

            if (summary.lines != null)
            {
                for (var i = 0; i < summary.lines.Count; i++)
                {
                    var line = summary.lines[i];
                    if (line == null || string.IsNullOrWhiteSpace(line.fishId) || line.count <= 0)
                    {
                        continue;
                    }

                    var normalizedCount = Mathf.Max(0, line.count);
                    var isDailyFish = !string.IsNullOrWhiteSpace(daily.fishId) &&
                                      string.Equals(line.fishId, daily.fishId, StringComparison.Ordinal);
                    if (isDailyFish)
                    {
                        dailySoldCount += normalizedCount;
                    }

                    var isQuestFish = quest.accepted && !quest.claimed && !string.IsNullOrWhiteSpace(quest.fishId) &&
                                      string.Equals(line.fishId, quest.fishId, StringComparison.Ordinal);
                    if (isQuestFish)
                    {
                        questSoldCount += normalizedCount;
                    }

                    save.fishSaleHistory.Add(new FishSaleHistoryEntry
                    {
                        fishId = line.fishId,
                        distanceTier = Mathf.Max(1, line.distanceTier),
                        count = normalizedCount,
                        earnedCopecs = Mathf.Max(0, line.totalEarned),
                        timestampUtc = soldAtUtc,
                        dailyFishTarget = isDailyFish
                    });
                }
            }

            TrimSaleHistory(save.fishSaleHistory);

            daily.requiredCount = Mathf.Max(1, daily.requiredCount <= 0 ? _dailyFishRequiredCount : daily.requiredCount);
            daily.bonusCopecs = Mathf.Max(0, daily.bonusCopecs <= 0 ? _dailyFishBonusCopecs : daily.bonusCopecs);
            if (!daily.rewardGranted && dailySoldCount > 0)
            {
                daily.progressCount = Mathf.Max(0, daily.progressCount) + dailySoldCount;
                if (daily.progressCount >= daily.requiredCount)
                {
                    daily.progressCount = daily.requiredCount;
                    daily.completed = true;
                    daily.rewardGranted = true;
                    dailyBonusAwarded += Mathf.Max(0, daily.bonusCopecs);
                }
            }

            quest.requiredCount = Mathf.Max(1, quest.requiredCount <= 0 ? _questRequiredCount : quest.requiredCount);
            quest.rewardCopecs = Mathf.Max(0, quest.rewardCopecs <= 0 ? _questRewardCopecs : quest.rewardCopecs);
            if (quest.accepted && !quest.claimed && questSoldCount > 0)
            {
                quest.progressCount = Mathf.Max(0, quest.progressCount) + questSoldCount;
                if (quest.progressCount >= quest.requiredCount)
                {
                    quest.progressCount = quest.requiredCount;
                    quest.completed = true;
                }
            }
        }

        private bool EnsureDailyMarketState(SaveDataV1 save)
        {
            if (save == null)
            {
                return false;
            }

            var changed = false;
            save.dailyFishBonus ??= new DailyFishBonusState();
            save.fishingMarketQuest ??= new FishingMarketQuestState();
            save.fishSaleHistory ??= new List<FishSaleHistoryEntry>();
            TrimSaleHistory(save.fishSaleHistory);

            var fishIds = ResolveOrderedFishIds(save);
            if (fishIds.Count == 0)
            {
                fishIds.Add("fish_cod");
            }

            var today = DateTime.Now.ToString("yyyy-MM-dd");

            var daily = save.dailyFishBonus;
            if (!string.Equals(daily.localDate, today, StringComparison.Ordinal))
            {
                daily.localDate = today;
                daily.fishId = ResolveFishIdForSeed(fishIds, $"daily|{today}", offset: 0);
                daily.requiredCount = Mathf.Max(1, _dailyFishRequiredCount);
                daily.progressCount = 0;
                daily.bonusCopecs = Mathf.Max(0, _dailyFishBonusCopecs);
                daily.completed = false;
                daily.rewardGranted = false;
                changed = true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(daily.fishId))
                {
                    daily.fishId = ResolveFishIdForSeed(fishIds, $"daily|{today}", offset: 0);
                    changed = true;
                }

                var normalizedRequired = Mathf.Max(1, daily.requiredCount <= 0 ? _dailyFishRequiredCount : daily.requiredCount);
                if (normalizedRequired != daily.requiredCount)
                {
                    daily.requiredCount = normalizedRequired;
                    changed = true;
                }

                var normalizedBonus = Mathf.Max(0, daily.bonusCopecs <= 0 ? _dailyFishBonusCopecs : daily.bonusCopecs);
                if (normalizedBonus != daily.bonusCopecs)
                {
                    daily.bonusCopecs = normalizedBonus;
                    changed = true;
                }

                if (daily.progressCount < 0)
                {
                    daily.progressCount = 0;
                    changed = true;
                }

                var shouldBeCompleted = daily.progressCount >= daily.requiredCount;
                if (shouldBeCompleted != daily.completed)
                {
                    daily.completed = shouldBeCompleted;
                    changed = true;
                }
            }

            var quest = save.fishingMarketQuest;
            if (!string.Equals(quest.questDateLocal, today, StringComparison.Ordinal))
            {
                quest.questDateLocal = today;
                quest.fishId = ResolveFishIdForSeed(fishIds, $"quest|{today}", offset: 1);
                quest.requiredCount = Mathf.Max(1, _questRequiredCount);
                quest.progressCount = 0;
                quest.rewardCopecs = Mathf.Max(0, _questRewardCopecs);
                quest.accepted = false;
                quest.completed = false;
                quest.claimed = false;
                changed = true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(quest.fishId))
                {
                    quest.fishId = ResolveFishIdForSeed(fishIds, $"quest|{today}", offset: 1);
                    changed = true;
                }

                var normalizedRequired = Mathf.Max(1, quest.requiredCount <= 0 ? _questRequiredCount : quest.requiredCount);
                if (normalizedRequired != quest.requiredCount)
                {
                    quest.requiredCount = normalizedRequired;
                    changed = true;
                }

                var normalizedReward = Mathf.Max(0, quest.rewardCopecs <= 0 ? _questRewardCopecs : quest.rewardCopecs);
                if (normalizedReward != quest.rewardCopecs)
                {
                    quest.rewardCopecs = normalizedReward;
                    changed = true;
                }

                if (quest.progressCount < 0)
                {
                    quest.progressCount = 0;
                    changed = true;
                }

                var shouldBeCompleted = quest.progressCount >= quest.requiredCount;
                if (shouldBeCompleted != quest.completed)
                {
                    quest.completed = shouldBeCompleted;
                    changed = true;
                }

                if (quest.claimed && quest.accepted)
                {
                    quest.accepted = false;
                    changed = true;
                }
            }

            return changed;
        }

        private List<string> ResolveOrderedFishIds(SaveDataV1 save)
        {
            var ids = new List<string>();
            if (_catalogService != null && _catalogService.FishById != null)
            {
                foreach (var pair in _catalogService.FishById)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || ids.Contains(pair.Key))
                    {
                        continue;
                    }

                    ids.Add(pair.Key);
                }
            }

            if (save != null && save.catchLog != null)
            {
                for (var i = 0; i < save.catchLog.Count; i++)
                {
                    var entry = save.catchLog[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.fishId) || ids.Contains(entry.fishId))
                    {
                        continue;
                    }

                    ids.Add(entry.fishId);
                }
            }

            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        private string ResolveFishIdForSeed(List<string> fishIds, string seed, int offset)
        {
            if (fishIds == null || fishIds.Count == 0)
            {
                return "fish_cod";
            }

            var hash = ComputeStableHash32(seed ?? string.Empty);
            var baseIndex = (int)(hash % (uint)fishIds.Count);
            var index = Mathf.FloorToInt(Mathf.Repeat(baseIndex + offset, fishIds.Count));
            return fishIds[Mathf.Clamp(index, 0, fishIds.Count - 1)];
        }

        private void TrimSaleHistory(List<FishSaleHistoryEntry> history)
        {
            if (history == null)
            {
                return;
            }

            var maxEntries = Mathf.Max(20, _maxSaleHistoryEntries);
            if (history.Count <= maxEntries)
            {
                return;
            }

            history.RemoveRange(0, history.Count - maxEntries);
        }

        private static uint ComputeStableHash32(string value)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            var hash = offset;
            if (string.IsNullOrEmpty(value))
            {
                return hash;
            }

            for (var i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= prime;
            }

            return hash;
        }

        private static string ToDisplayLabel(string rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId))
            {
                return "None";
            }

            var tokens = rawId.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return rawId;
            }

            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                if (token.Length == 0)
                {
                    continue;
                }

                tokens[i] = token.Length == 1
                    ? char.ToUpperInvariant(token[0]).ToString()
                    : char.ToUpperInvariant(token[0]) + token.Substring(1);
            }

            return string.Join(" ", tokens);
        }
    }
}
