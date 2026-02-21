using NUnit.Framework;
using RavenDevOps.Fishing.Fishing;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class FishingOutcomeDomainServiceTests
    {
        [Test]
        public void ResolveHookWindowFailure_ReturnsMissedHook_WhenFishMissing()
        {
            var domain = new FishingOutcomeDomainService();

            var fail = domain.ResolveHookWindowFailure(
                hasHookedFish: false,
                elapsedSeconds: 0.1f,
                reactionWindowSeconds: 1.2f);

            Assert.That(fail, Is.EqualTo(FishingFailReason.MissedHook));
        }

        [Test]
        public void ResolveHookWindowFailure_ReturnsMissedHook_AfterWindowElapsed()
        {
            var domain = new FishingOutcomeDomainService();

            var fail = domain.ResolveHookWindowFailure(
                hasHookedFish: true,
                elapsedSeconds: 1.4f,
                reactionWindowSeconds: 1.0f);

            Assert.That(fail, Is.EqualTo(FishingFailReason.MissedHook));
        }

        [Test]
        public void ResolveHookWindowFailure_ReturnsNone_WithinWindow()
        {
            var domain = new FishingOutcomeDomainService();

            var fail = domain.ResolveHookWindowFailure(
                hasHookedFish: true,
                elapsedSeconds: 0.3f,
                reactionWindowSeconds: 1.0f);

            Assert.That(fail, Is.EqualTo(FishingFailReason.None));
        }

        [Test]
        public void BuildCatchReward_UsesProvidedRandomSource()
        {
            var domain = new FishingOutcomeDomainService();
            var fish = new FishDefinition
            {
                id = "fish_test",
                baseValue = 100,
                minCatchWeightKg = 1f,
                maxCatchWeightKg = 3f
            };

            var random = new SequenceRandomSource(2.5f, 1.1f);
            var reward = domain.BuildCatchReward(fish, random);

            Assert.That(reward.WeightKg, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(reward.ValueCopecs, Is.EqualTo(110));
        }

        [Test]
        public void BuildFailureReasonText_MapsExpectedReasons()
        {
            var domain = new FishingOutcomeDomainService();

            Assert.That(domain.BuildFailureReasonText(FishingFailReason.MissedHook), Does.StartWith("Missed hook"));
            Assert.That(domain.BuildFailureReasonText(FishingFailReason.LineSnap), Does.StartWith("Line snapped"));
            Assert.That(domain.BuildFailureReasonText(FishingFailReason.FishEscaped), Does.StartWith("Fish escaped"));
            Assert.That(domain.BuildFailureReasonText(FishingFailReason.None), Is.EqualTo("Catch failed."));
        }

        private sealed class SequenceRandomSource : IFishingRandomSource
        {
            private readonly float[] _sequence;
            private int _index;

            public SequenceRandomSource(params float[] sequence)
            {
                _sequence = sequence ?? new float[0];
                _index = 0;
            }

            public float Range(float minInclusive, float maxInclusive)
            {
                if (_sequence.Length == 0)
                {
                    return minInclusive;
                }

                var value = _sequence[_index % _sequence.Length];
                _index++;
                return value;
            }
        }
    }
}
