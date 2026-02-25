using System.Collections.Generic;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class HarborViewPresenterTests
    {
        [Test]
        public void HarborShopViewPresenter_BuildFishShopDetails_ContainsCharterAndSalesSections()
        {
            var presenter = new HarborShopViewPresenter();
            var snapshot = new FishMarketSnapshot
            {
                dailyFishId = "fish_cod",
                dailyProgressCount = 2,
                dailyRequiredCount = 5,
                dailyBonusCopecs = 90,
                dailyBonusGranted = false,
                questFishId = "fish_herring",
                questProgressCount = 1,
                questRequiredCount = 3,
                questRewardCopecs = 140,
                questAccepted = true,
                questCompleted = false,
                questClaimed = false,
                recentSales = new List<FishSaleHistoryEntry>
                {
                    new FishSaleHistoryEntry
                    {
                        fishId = "fish_cod",
                        count = 2,
                        distanceTier = 2,
                        earnedCopecs = 30,
                        timestampUtc = "2026-02-25T12:00:00Z",
                        dailyFishTarget = true
                    }
                }
            };

            var output = presenter.BuildFishShopDetails(
                new HarborShopViewPresenter.FishShopDetailsRequest
                {
                    MarketSnapshot = snapshot,
                    PendingSaleSummary = new SellSummary { totalEarned = 45 },
                    FishCount = 3,
                    CargoCapacity = 12,
                    BalanceCopecs = 100
                });

            Assert.That(output, Does.Contain("Balance: 100 copecs"));
            Assert.That(output, Does.Contain("Daily Fish: Fish Cod"));
            Assert.That(output, Does.Contain("Fishing Charter Target: Fish Herring"));
            Assert.That(output, Does.Contain("Recent Sales:"));
            Assert.That(output, Does.Contain("[Daily]"));
        }

        [Test]
        public void HarborFisheryCardViewPresenter_BuildCardText_ContainsLatestAndLifetimeData()
        {
            var presenter = new HarborFisheryCardViewPresenter();
            var fishDefinition = ScriptableObject.CreateInstance<FishDefinitionSO>();
            fishDefinition.id = "fish_cod";
            fishDefinition.description = "Classic white fish.";
            fishDefinition.baseValue = 12;
            fishDefinition.rarityWeight = 3;
            fishDefinition.minDistanceTier = 1;
            fishDefinition.maxDistanceTier = 3;
            fishDefinition.minDepth = 4f;
            fishDefinition.maxDepth = 18f;
            fishDefinition.minCatchWeightKg = 0.5f;
            fishDefinition.maxCatchWeightKg = 2.2f;

            try
            {
                var output = presenter.BuildCardText(
                    new HarborFisheryCardViewPresenter.CardRequest
                    {
                        CardIndex = 0,
                        TotalCards = 4,
                        FishId = "fish_cod",
                        FishDefinition = fishDefinition,
                        LatestCatch = new CatchLogEntry
                        {
                            fishId = "fish_cod",
                            landed = true,
                            timestampUtc = "2026-02-24T22:00:00Z",
                            distanceTier = 2,
                            depthMeters = 10f,
                            weightKg = 1.8f,
                            valueCopecs = 18
                        },
                        HasLifetimeStats = true,
                        LandedCount = 3,
                        BestWeightKg = 2.1f,
                        BestValueCopecs = 24
                    });

                Assert.That(output, Does.Contain("Card 1/4"));
                Assert.That(output, Does.Contain("Fish: Fish Cod"));
                Assert.That(output, Does.Contain("Latest Capture:"));
                Assert.That(output, Does.Contain("Lifetime Records:"));
                Assert.That(output, Does.Contain("Landed: 3"));
                Assert.That(output, Does.Contain("Best Value: 24 copecs"));
            }
            finally
            {
                Object.DestroyImmediate(fishDefinition);
            }
        }
    }
}
