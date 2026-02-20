using NUnit.Framework;
using RavenDevOps.Fishing.Fishing;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class FishingConditionControllerTests
    {
        [Test]
        public void GetCombinedModifier_UsesConfiguredTimeAndWeather()
        {
            var root = new GameObject("ConditionControllerTests");
            try
            {
                var controller = root.AddComponent<FishingConditionController>();
                controller.SetTimeOfDay(FishingTimeOfDay.Dusk);
                controller.SetWeather(FishingWeatherState.Rain);

                var modifier = controller.GetCombinedModifier();

                Assert.That(modifier.rarityWeightMultiplier, Is.GreaterThan(1f));
                Assert.That(modifier.biteDelayMultiplier, Is.LessThan(1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GetConditionLabel_ReturnsReadableValue()
        {
            var root = new GameObject("ConditionControllerLabelTests");
            try
            {
                var controller = root.AddComponent<FishingConditionController>();
                controller.SetTimeOfDay(FishingTimeOfDay.Night);
                controller.SetWeather(FishingWeatherState.Storm);

                Assert.That(controller.GetConditionLabel(), Is.EqualTo("Night | Storm"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
