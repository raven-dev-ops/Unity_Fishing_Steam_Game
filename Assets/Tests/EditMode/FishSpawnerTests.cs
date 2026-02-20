using System.Collections.Generic;
using NUnit.Framework;
using RavenDevOps.Fishing.Fishing;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class FishSpawnerTests
    {
        [Test]
        public void RollFishDeterministic_UsesWeightedSelection()
        {
            var root = new GameObject("FishSpawnerWeightedTests");
            try
            {
                var spawner = root.AddComponent<FishSpawner>();
                spawner.SetFallbackDefinitions(new List<FishDefinition>
                {
                    new FishDefinition { id = "fish_light", minDistanceTier = 1, maxDistanceTier = 3, minDepth = 0f, maxDepth = 10f, rarityWeight = 1 },
                    new FishDefinition { id = "fish_heavy", minDistanceTier = 1, maxDistanceTier = 3, minDepth = 0f, maxDepth = 10f, rarityWeight = 3 }
                });

                var first = spawner.RollFishDeterministic(1, 2f, 0);
                var second = spawner.RollFishDeterministic(1, 2f, 1);
                var third = spawner.RollFishDeterministic(1, 2f, 3);

                Assert.That(first.id, Is.EqualTo("fish_light"));
                Assert.That(second.id, Is.EqualTo("fish_heavy"));
                Assert.That(third.id, Is.EqualTo("fish_heavy"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RollFishDeterministic_RespectsDistanceAndDepthFilters()
        {
            var root = new GameObject("FishSpawnerRangeTests");
            try
            {
                var spawner = root.AddComponent<FishSpawner>();
                spawner.SetFallbackDefinitions(new List<FishDefinition>
                {
                    new FishDefinition { id = "fish_surface", minDistanceTier = 1, maxDistanceTier = 1, minDepth = 0f, maxDepth = 2f, rarityWeight = 1 }
                });

                var inRange = spawner.RollFishDeterministic(1, 1f, 0);
                var wrongDistance = spawner.RollFishDeterministic(2, 1f, 0);
                var wrongDepth = spawner.RollFishDeterministic(1, 5f, 0);

                Assert.That(inRange, Is.Not.Null);
                Assert.That(wrongDistance, Is.Null);
                Assert.That(wrongDepth, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
