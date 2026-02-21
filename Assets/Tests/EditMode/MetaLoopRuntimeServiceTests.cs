using NUnit.Framework;
using RavenDevOps.Fishing.Economy;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class MetaLoopRuntimeServiceTests
    {
        [Test]
        public void GetGearSynergyMultiplier_UsesDefaultSeededSynergy()
        {
            var root = new GameObject("MetaLoopRuntimeServiceTests");
            try
            {
                var service = root.AddComponent<MetaLoopRuntimeService>();
                var multiplier = service.GetGearSynergyMultiplier("ship_lv1", "hook_lv1", out var label);

                Assert.That(multiplier, Is.GreaterThan(1f));
                Assert.That(label, Is.Not.Empty);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GetMarketDemandMultiplier_IsDeterministicPerFishId()
        {
            var root = new GameObject("MetaLoopDemandDeterminismTests");
            try
            {
                var service = root.AddComponent<MetaLoopRuntimeService>();
                var first = service.GetMarketDemandMultiplier("fish_cod");
                var second = service.GetMarketDemandMultiplier("fish_cod");
                var other = service.GetMarketDemandMultiplier("fish_a");

                Assert.That(first, Is.EqualTo(second).Within(0.0001f));
                Assert.That(other, Is.Not.EqualTo(first).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BuildModifierLabel_IncludesDemandAndSynergyText()
        {
            var root = new GameObject("MetaLoopModifierLabelTests");
            try
            {
                var service = root.AddComponent<MetaLoopRuntimeService>();
                var label = service.BuildModifierLabel("fish_cod", "ship_lv1", "hook_lv1");

                Assert.That(label, Does.Contain("Demand x"));
                Assert.That(label, Does.Contain("x"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
