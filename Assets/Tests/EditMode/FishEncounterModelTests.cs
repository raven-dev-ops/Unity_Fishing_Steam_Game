using NUnit.Framework;
using RavenDevOps.Fishing.Fishing;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class FishEncounterModelTests
    {
        [Test]
        public void Step_WithConstantReel_CanLandFish()
        {
            var model = new FishEncounterModel();
            var fish = new FishDefinition
            {
                id = "fish_test",
                fightStamina = 1.5f,
                pullIntensity = 0.6f,
                escapeSeconds = 15f
            };

            model.Begin(fish, initialTension: 0.2f);

            var landed = false;
            var failReason = FishingFailReason.None;
            for (var i = 0; i < 600 && !landed && failReason == FishingFailReason.None; i++)
            {
                model.Step(0.02f, isReeling: true, out landed, out failReason);
            }

            Assert.That(landed, Is.True);
            Assert.That(failReason, Is.EqualTo(FishingFailReason.None));
        }

        [Test]
        public void Step_WithoutReeling_CanEscapeFish()
        {
            var model = new FishEncounterModel();
            var fish = new FishDefinition
            {
                id = "fish_test",
                fightStamina = 12f,
                pullIntensity = 1f,
                escapeSeconds = 2f
            };

            model.Begin(fish, initialTension: 0.1f);

            var landed = false;
            var failReason = FishingFailReason.None;
            for (var i = 0; i < 300 && !landed && failReason == FishingFailReason.None; i++)
            {
                model.Step(0.02f, isReeling: false, out landed, out failReason);
            }

            Assert.That(landed, Is.False);
            Assert.That(failReason, Is.EqualTo(FishingFailReason.FishEscaped));
        }

        [Test]
        public void Step_WithHighPullIntensity_CanSnapLine()
        {
            var model = new FishEncounterModel();
            var fish = new FishDefinition
            {
                id = "fish_line_snap",
                fightStamina = 50f,
                pullIntensity = 3f,
                escapeSeconds = 30f
            };

            model.Begin(fish, initialTension: 0.85f);

            var landed = false;
            var failReason = FishingFailReason.None;
            for (var i = 0; i < 120 && !landed && failReason == FishingFailReason.None; i++)
            {
                model.Step(0.02f, isReeling: true, out landed, out failReason);
            }

            Assert.That(landed, Is.False);
            Assert.That(failReason, Is.EqualTo(FishingFailReason.LineSnap));
        }

        [Test]
        public void ResolveTensionState_MapsExpectedThresholds()
        {
            Assert.That(FishEncounterModel.ResolveTensionState(0.1f), Is.EqualTo(FishingTensionState.Safe));
            Assert.That(FishEncounterModel.ResolveTensionState(0.6f), Is.EqualTo(FishingTensionState.Warning));
            Assert.That(FishEncounterModel.ResolveTensionState(0.9f), Is.EqualTo(FishingTensionState.Critical));
        }
    }
}
