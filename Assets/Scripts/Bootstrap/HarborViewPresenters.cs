using System;
using System.Collections.Generic;
using System.Text;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Harbor;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    public static class HarborTextFormatting
    {
        public static string ToDisplayLabel(string rawId)
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

                if (token.Length == 1)
                {
                    tokens[i] = char.ToUpperInvariant(token[0]).ToString();
                    continue;
                }

                tokens[i] = char.ToUpperInvariant(token[0]) + token.Substring(1);
            }

            return string.Join(" ", tokens);
        }

        public static string FormatUtcTimestampToLocalTime(string timestampUtc)
        {
            if (DateTimeUtility.TryParseUtcTimestampToLocal(timestampUtc, out var parsedLocal))
            {
                return parsedLocal.ToString("HH:mm");
            }

            return "--:--";
        }

        public static string FormatUtcTimestampToLocalDate(string timestampUtc)
        {
            if (DateTimeUtility.TryParseUtcTimestampToLocal(timestampUtc, out var parsedLocal))
            {
                return parsedLocal.ToString("yyyy-MM-dd HH:mm");
            }

            return "Unknown";
        }
    }

    public sealed class HarborInteractionViewPresenter
    {
        public string BuildSelectionHint(WorldInteractable interactable, string interactPromptLabel)
        {
            if (interactable == null)
            {
                return "Nearby target: none. Use center menu actions for harbor operations.";
            }

            var label = string.IsNullOrWhiteSpace(interactPromptLabel)
                ? "Enter or South Button"
                : interactPromptLabel;

            return interactable.Type switch
            {
                InteractableType.HookShop => $"Nearby target: Warehouse. Press {label} to buy hooks for Shipyard inventory.",
                InteractableType.BoatShop => $"Nearby target: Dockyard. Press {label} to buy ships for Shipyard inventory.",
                InteractableType.FishShop => $"Nearby target: Fishery. Press {label} to sell all fish cargo.",
                InteractableType.Sail => $"Nearby target: Dock. Press {label} to set sail or use Shipyard in the menu.",
                _ => $"Nearby target: Interaction available. Press {label}."
            };
        }

        public string BuildActivityLog(IReadOnlyList<string> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "Recent Activity:";
            }

            var output = new StringBuilder();
            output.Append("Recent Activity:");
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }

                output.Append("\n- ");
                output.Append(entry.Trim());
            }

            return output.ToString();
        }
    }

    public sealed class HarborShopViewPresenter
    {
        public sealed class FishShopDetailsRequest
        {
            public FishMarketSnapshot MarketSnapshot { get; set; }
            public SellSummary PendingSaleSummary { get; set; }
            public int FishCount { get; set; }
            public int CargoCapacity { get; set; }
            public int BalanceCopecs { get; set; }
        }

        public sealed class FishQuestButtonState
        {
            public bool AcceptInteractable { get; set; }
            public string AcceptLabel { get; set; }
            public bool FulfillInteractable { get; set; }
            public string FulfillLabel { get; set; }
        }

        public string BuildFishShopDetails(FishShopDetailsRequest request)
        {
            if (request == null || request.MarketSnapshot == null)
            {
                return "Fishery summary unavailable.";
            }

            var snapshot = request.MarketSnapshot;
            var pendingValue = request.PendingSaleSummary != null
                ? Mathf.Max(0, request.PendingSaleSummary.totalEarned)
                : 0;
            var fishCount = Mathf.Max(0, request.FishCount);
            var cargoCapacity = Mathf.Max(1, request.CargoCapacity);
            var balance = Mathf.Max(0, request.BalanceCopecs);

            var infoBuilder = new StringBuilder();
            infoBuilder.AppendLine($"Balance: {balance} copecs");
            infoBuilder.AppendLine($"Cargo hold: {fishCount}/{cargoCapacity}");
            if (fishCount <= 0)
            {
                infoBuilder.AppendLine("Cargo is empty. Catch fish before selling.");
            }
            else
            {
                var projectedBalance = balance + pendingValue;
                infoBuilder.AppendLine($"Projected cargo payout: {pendingValue} copecs");
                infoBuilder.AppendLine($"Balance after sale: {projectedBalance} copecs");
            }

            infoBuilder.AppendLine(string.Empty);
            infoBuilder.AppendLine(
                $"Daily Fish: {HarborTextFormatting.ToDisplayLabel(snapshot.dailyFishId)} " +
                $"({Mathf.Clamp(snapshot.dailyProgressCount, 0, Mathf.Max(1, snapshot.dailyRequiredCount))}/{Mathf.Max(1, snapshot.dailyRequiredCount)}) " +
                $"+{Mathf.Max(0, snapshot.dailyBonusCopecs)}c");
            infoBuilder.AppendLine(snapshot.dailyBonusGranted
                ? "Daily bonus complete for today."
                : "Sell daily fish to earn today's bonus.");

            infoBuilder.AppendLine(string.Empty);
            infoBuilder.AppendLine(
                $"Fishing Charter Target: {HarborTextFormatting.ToDisplayLabel(snapshot.questFishId)} " +
                $"({Mathf.Clamp(snapshot.questProgressCount, 0, Mathf.Max(1, snapshot.questRequiredCount))}/{Mathf.Max(1, snapshot.questRequiredCount)}) " +
                $"+{Mathf.Max(0, snapshot.questRewardCopecs)}c");
            if (snapshot.questClaimed)
            {
                infoBuilder.AppendLine("Fishing Charter fulfilled and claimed.");
            }
            else if (snapshot.questCompleted)
            {
                infoBuilder.AppendLine("Fishing Charter complete. Click Fulfill Charter to claim reward.");
            }
            else if (snapshot.questAccepted)
            {
                infoBuilder.AppendLine("Fishing Charter accepted. Sell matching fish in this Fishery.");
            }
            else
            {
                infoBuilder.AppendLine("Fishing Charter not accepted. Click Accept Charter.");
            }

            infoBuilder.AppendLine(string.Empty);
            infoBuilder.AppendLine("Recent Sales:");
            if (snapshot.recentSales == null || snapshot.recentSales.Count == 0)
            {
                infoBuilder.AppendLine("- No fish sold yet.");
            }
            else
            {
                for (var i = 0; i < snapshot.recentSales.Count; i++)
                {
                    var saleEntry = snapshot.recentSales[i];
                    if (saleEntry == null)
                    {
                        continue;
                    }

                    var soldTime = HarborTextFormatting.FormatUtcTimestampToLocalTime(saleEntry.timestampUtc);
                    var dailyTag = saleEntry.dailyFishTarget ? " [Daily]" : string.Empty;
                    infoBuilder.AppendLine(
                        $"- [{soldTime}] {HarborTextFormatting.ToDisplayLabel(saleEntry.fishId)} x{Mathf.Max(0, saleEntry.count)} " +
                        $"(T{Mathf.Max(1, saleEntry.distanceTier)}) +{Mathf.Max(0, saleEntry.earnedCopecs)}c{dailyTag}");
                }
            }

            return infoBuilder.ToString().TrimEnd();
        }

        public FishQuestButtonState BuildFishQuestButtonState(FishMarketSnapshot snapshot)
        {
            var buttonState = new FishQuestButtonState();
            if (snapshot != null && snapshot.questClaimed)
            {
                buttonState.AcceptInteractable = false;
                buttonState.AcceptLabel = "Charter Claimed";
                buttonState.FulfillInteractable = false;
                buttonState.FulfillLabel = "Fulfilled";
                return buttonState;
            }

            if (snapshot != null && snapshot.questAccepted)
            {
                buttonState.AcceptInteractable = false;
                buttonState.AcceptLabel = "Charter Active";
            }
            else
            {
                buttonState.AcceptInteractable = true;
                buttonState.AcceptLabel = "Accept Charter";
            }

            if (snapshot != null && snapshot.questCompleted)
            {
                buttonState.FulfillInteractable = true;
                buttonState.FulfillLabel = $"Fulfill Charter (+{Mathf.Max(0, snapshot.questRewardCopecs)}c)";
            }
            else
            {
                buttonState.FulfillInteractable = false;
                buttonState.FulfillLabel = "Fulfill Charter";
            }

            return buttonState;
        }

        public string BuildShopItemLine(
            string itemId,
            bool owned,
            bool equipped,
            bool unlocked,
            int unlockLevel,
            int price,
            bool hasRequiredPreviousTier,
            string requiredTierId,
            int balance)
        {
            var label = HarborTextFormatting.ToDisplayLabel(itemId);
            if (!unlocked)
            {
                return $"- {label}: Locked until level {Mathf.Max(1, unlockLevel)}";
            }

            if (!owned && !hasRequiredPreviousTier)
            {
                return $"- {label}: Requires {HarborTextFormatting.ToDisplayLabel(requiredTierId)}";
            }

            if (equipped)
            {
                return $"- {label}: Equipped";
            }

            if (owned)
            {
                return $"- {label}: Owned (equip in Shipyard)";
            }

            if (price < 0)
            {
                return $"- {label}: Price unavailable";
            }

            var normalizedPrice = Mathf.Max(0, price);
            if (balance < normalizedPrice)
            {
                return $"- {label}: Buy {normalizedPrice} copecs (need {normalizedPrice - balance} more)";
            }

            return $"- {label}: Buy {normalizedPrice} copecs";
        }

        public string BuildShopButtonLabel(
            bool owned,
            bool equipped,
            bool unlocked,
            int unlockLevel,
            bool hasRequiredPreviousTier,
            int price)
        {
            if (equipped)
            {
                return "Equipped";
            }

            if (owned)
            {
                return "Owned";
            }

            if (!unlocked)
            {
                return $"Locked Lv{Mathf.Max(1, unlockLevel)}";
            }

            if (!hasRequiredPreviousTier)
            {
                return "Locked (Prev)";
            }

            if (price < 0)
            {
                return "Unavailable";
            }

            return $"Buy ({Mathf.Max(0, price)}c)";
        }

        public string BuildShipyardInfoText(string shipId, string hookId, int ownedShips, int ownedHooks)
        {
            return
                $"Selected Ship: {HarborTextFormatting.ToDisplayLabel(shipId)}\n" +
                $"Equipped Hook: {HarborTextFormatting.ToDisplayLabel(hookId)}\n" +
                $"Owned Ships: {Mathf.Max(0, ownedShips)} | Owned Hooks: {Mathf.Max(0, ownedHooks)}";
        }

        public string BuildShipyardCargoText(SaveDataV1 save, int fishCount, int cargoCapacity)
        {
            var clampedFishCount = Mathf.Max(0, fishCount);
            var clampedCapacity = Mathf.Max(1, cargoCapacity);
            var cargoRemaining = Mathf.Max(0, clampedCapacity - clampedFishCount);
            return
                $"Cargo Hold: {clampedFishCount}/{clampedCapacity}\n" +
                $"Space Remaining: {cargoRemaining}\n" +
                BuildCargoManifestSummary(save);
        }

        public string BuildCargoManifestSummary(SaveDataV1 save)
        {
            if (save == null || save.fishInventory == null || save.fishInventory.Count == 0)
            {
                return "Manifest: empty";
            }

            var entries = 0;
            var manifest = "Manifest:";
            for (var i = 0; i < save.fishInventory.Count; i++)
            {
                var entry = save.fishInventory[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.fishId) || entry.count <= 0)
                {
                    continue;
                }

                manifest += $"\n- {HarborTextFormatting.ToDisplayLabel(entry.fishId)} x{entry.count}";
                entries++;
                if (entries >= 4)
                {
                    break;
                }
            }

            if (entries == 0)
            {
                return "Manifest: empty";
            }

            return manifest;
        }
    }

    public sealed class HarborFisheryCardViewPresenter
    {
        public sealed class CardRequest
        {
            public int CardIndex { get; set; }
            public int TotalCards { get; set; }
            public string FishId { get; set; }
            public FishDefinitionSO FishDefinition { get; set; }
            public CatchLogEntry LatestCatch { get; set; }
            public bool HasLifetimeStats { get; set; }
            public int LandedCount { get; set; }
            public float BestWeightKg { get; set; }
            public int BestValueCopecs { get; set; }
        }

        public string BuildCardText(CardRequest request)
        {
            if (request == null || request.TotalCards <= 0)
            {
                return "Fishery catalog unavailable.";
            }

            var cardBuilder = new StringBuilder();
            cardBuilder.AppendLine($"Card {request.CardIndex + 1}/{request.TotalCards}");
            cardBuilder.AppendLine($"Fish: {HarborTextFormatting.ToDisplayLabel(request.FishId)}");

            var fishDefinition = request.FishDefinition;
            var description = fishDefinition != null && !string.IsNullOrWhiteSpace(fishDefinition.description)
                ? fishDefinition.description.Trim()
                : BuildFallbackFishDescription(fishDefinition);
            cardBuilder.AppendLine($"Description: {description}");
            cardBuilder.AppendLine(string.Empty);

            if (fishDefinition != null)
            {
                cardBuilder.AppendLine($"Base Value: {Mathf.Max(1, fishDefinition.baseValue)} copecs");
                cardBuilder.AppendLine($"Rarity Weight: {Mathf.Max(1, fishDefinition.rarityWeight)}");
                cardBuilder.AppendLine($"Distance Range: Tier {Mathf.Max(0, fishDefinition.minDistanceTier)} - {Mathf.Max(0, fishDefinition.maxDistanceTier)}");
                cardBuilder.AppendLine($"Depth Range: {Mathf.Max(0f, fishDefinition.minDepth):0}-{Mathf.Max(0f, fishDefinition.maxDepth):0} m");
                cardBuilder.AppendLine($"Weight Range: {Mathf.Max(0f, fishDefinition.minCatchWeightKg):0.0}-{Mathf.Max(0f, fishDefinition.maxCatchWeightKg):0.0} kg");
            }
            else
            {
                cardBuilder.AppendLine("Stats unavailable for this fish.");
            }

            cardBuilder.AppendLine(string.Empty);
            cardBuilder.AppendLine("Latest Capture:");
            if (request.LatestCatch == null)
            {
                cardBuilder.AppendLine("- Not captured yet.");
            }
            else
            {
                cardBuilder.AppendLine($"- Date: {HarborTextFormatting.FormatUtcTimestampToLocalDate(request.LatestCatch.timestampUtc)}");
                cardBuilder.AppendLine($"- Distance Tier: {Mathf.Max(1, request.LatestCatch.distanceTier)}");
                cardBuilder.AppendLine($"- Depth: {Mathf.Max(0f, request.LatestCatch.depthMeters):0.0} m");
                cardBuilder.AppendLine($"- Weight: {Mathf.Max(0f, request.LatestCatch.weightKg):0.0} kg");
                cardBuilder.AppendLine($"- Value: {Mathf.Max(0, request.LatestCatch.valueCopecs)} copecs");
            }

            cardBuilder.AppendLine(string.Empty);
            cardBuilder.AppendLine("Lifetime Records:");
            if (!request.HasLifetimeStats)
            {
                cardBuilder.AppendLine("- No landed records yet.");
            }
            else
            {
                cardBuilder.AppendLine($"- Landed: {Mathf.Max(0, request.LandedCount)}");
                cardBuilder.AppendLine($"- Best Weight: {Mathf.Max(0f, request.BestWeightKg):0.0} kg");
                cardBuilder.AppendLine($"- Best Value: {Mathf.Max(0, request.BestValueCopecs)} copecs");
            }

            return cardBuilder.ToString().TrimEnd();
        }

        private static string BuildFallbackFishDescription(FishDefinitionSO fishDefinition)
        {
            if (fishDefinition == null)
            {
                return "Catalog description unavailable.";
            }

            return $"A catchable species found between {Mathf.Max(0f, fishDefinition.minDepth):0}-{Mathf.Max(0f, fishDefinition.maxDepth):0}m.";
        }
    }
}
