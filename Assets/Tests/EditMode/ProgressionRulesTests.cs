using NUnit.Framework;
using RavenDevOps.Fishing.Save;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class ProgressionRulesTests
    {
        [Test]
        public void CalculateCatchXp_IncreasesWithValueAndDistance()
        {
            var low = ProgressionRules.CalculateCatchXp(distanceTier: 1, weightKg: 0.5f, valueCopecs: 10);
            var high = ProgressionRules.CalculateCatchXp(distanceTier: 3, weightKg: 2.2f, valueCopecs: 120);

            Assert.That(high, Is.GreaterThan(low));
            Assert.That(low, Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void ResolveLevel_UsesThresholds()
        {
            Assert.That(ProgressionRules.ResolveLevel(0, ProgressionRules.Defaults), Is.EqualTo(1));
            Assert.That(ProgressionRules.ResolveLevel(100, ProgressionRules.Defaults), Is.EqualTo(2));
            Assert.That(ProgressionRules.ResolveLevel(250, ProgressionRules.Defaults), Is.EqualTo(3));
        }

        [Test]
        public void ResolveXpProgress_ComputesIntoLevelAndNextLevel()
        {
            ProgressionRules.ResolveXpProgress(
                totalXp: 140,
                levelThresholds: ProgressionRules.Defaults,
                out var level,
                out var xpIntoLevel,
                out var xpToNextLevel);

            Assert.That(level, Is.EqualTo(2));
            Assert.That(xpIntoLevel, Is.EqualTo(40));
            Assert.That(xpToNextLevel, Is.EqualTo(150));
        }
    }
}
