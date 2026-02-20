using System.Collections.Generic;
using NUnit.Framework;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class SellSummaryCalculatorTests
    {
        [Test]
        public void Calculate_UsesTierOneAsBaselineMultiplier()
        {
            var root = new GameObject("SellSummaryCalculatorTests");
            try
            {
                var calculator = root.AddComponent<SellSummaryCalculator>();
                calculator.SetDistanceTierStep(0.5f);

                var inventory = new List<FishInventoryEntry>
                {
                    new FishInventoryEntry { fishId = "fish_a", distanceTier = 1, count = 2 },
                    new FishInventoryEntry { fishId = "fish_b", distanceTier = 2, count = 1 }
                };

                var summary = calculator.Calculate(inventory);

                Assert.That(summary.itemCount, Is.EqualTo(3));
                Assert.That(summary.totalEarned, Is.EqualTo(35));
                Assert.That(calculator.CalculateDistanceMultiplier(1), Is.EqualTo(1f));
                Assert.That(calculator.CalculateDistanceMultiplier(2), Is.EqualTo(1.5f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Calculate_ClampsNegativeCountsAndDistanceTierBelowOne()
        {
            var root = new GameObject("SellSummaryCalculatorNegativeTests");
            try
            {
                var calculator = root.AddComponent<SellSummaryCalculator>();
                calculator.SetDistanceTierStep(0.25f);

                var inventory = new List<FishInventoryEntry>
                {
                    new FishInventoryEntry { fishId = "fish_a", distanceTier = -3, count = -2 },
                    new FishInventoryEntry { fishId = "fish_b", distanceTier = 0, count = 1 }
                };

                var summary = calculator.Calculate(inventory);

                Assert.That(summary.itemCount, Is.EqualTo(1));
                Assert.That(summary.totalEarned, Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Calculate_RoundsMultiplierOutputAsExpected()
        {
            var root = new GameObject("SellSummaryCalculatorRoundingTests");
            try
            {
                var calculator = root.AddComponent<SellSummaryCalculator>();
                calculator.SetDistanceTierStep(0.2f);

                var inventory = new List<FishInventoryEntry>
                {
                    new FishInventoryEntry { fishId = "fish_a", distanceTier = 3, count = 1 }
                };

                var summary = calculator.Calculate(inventory);

                Assert.That(summary.totalEarned, Is.EqualTo(14));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
