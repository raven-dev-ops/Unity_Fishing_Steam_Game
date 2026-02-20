using System.Collections.Generic;
using NUnit.Framework;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Tools;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class ContentValidatorTests
    {
        private readonly List<Object> _createdAssets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _createdAssets)
            {
                if (asset != null)
                {
                    Object.DestroyImmediate(asset);
                }
            }

            _createdAssets.Clear();
        }

        [Test]
        public void Validate_ReturnsErrors_ForDuplicateAndInvalidFishEntries()
        {
            var config = Create<GameConfigSO>();
            var fishA = Create<FishDefinitionSO>();
            var fishB = Create<FishDefinitionSO>();

            fishA.id = "fish_test";
            fishA.icon = null;
            fishA.rarityWeight = 0;
            fishA.baseValue = -1;

            fishB.id = "fish_test";
            fishB.icon = null;

            config.fishDefinitions = new[] { fishA, fishB };
            config.shipDefinitions = new ShipDefinitionSO[0];
            config.hookDefinitions = new HookDefinitionSO[0];

            var messages = ContentValidator.Validate(config);

            Assert.That(messages, Has.Some.Contains("Duplicate id 'fish_test'"));
            Assert.That(messages, Has.Some.Contains("missing icon/sprite"));
            Assert.That(messages, Has.Some.Contains("non-positive rarityWeight"));
            Assert.That(messages, Has.Some.Contains("non-positive baseValue"));
            Assert.That(ContentValidator.CountErrors(messages), Is.GreaterThan(0));
        }

        [Test]
        public void Validate_ReturnsNoErrors_ForValidMinimalCatalog()
        {
            var config = Create<GameConfigSO>();
            var fish = Create<FishDefinitionSO>();
            var ship = Create<ShipDefinitionSO>();
            var hook = Create<HookDefinitionSO>();

            fish.id = "fish_valid";
            fish.icon = BuildSprite();
            fish.minDistanceTier = 0;
            fish.maxDistanceTier = 2;
            fish.minDepth = 0f;
            fish.maxDepth = 10f;
            fish.rarityWeight = 1;
            fish.baseValue = 10;

            ship.id = "ship_valid";
            ship.icon = BuildSprite();
            ship.price = 100;
            ship.maxDistanceTier = 2;
            ship.moveSpeed = 5f;

            hook.id = "hook_valid";
            hook.icon = BuildSprite();
            hook.price = 100;
            hook.maxDepth = 8f;

            config.fishDefinitions = new[] { fish };
            config.shipDefinitions = new[] { ship };
            config.hookDefinitions = new[] { hook };

            var messages = ContentValidator.Validate(config);

            Assert.That(ContentValidator.CountErrors(messages), Is.EqualTo(0));
        }

        [Test]
        public void Validate_ReturnsErrors_ForInvalidFishBehaviorFields()
        {
            var config = Create<GameConfigSO>();
            var fish = Create<FishDefinitionSO>();

            fish.id = "fish_behavior_invalid";
            fish.icon = BuildSprite();
            fish.minDistanceTier = 0;
            fish.maxDistanceTier = 1;
            fish.minDepth = 0f;
            fish.maxDepth = 5f;
            fish.rarityWeight = 1;
            fish.baseValue = 5;
            fish.minBiteDelaySeconds = 3f;
            fish.maxBiteDelaySeconds = 1f;
            fish.fightStamina = 0f;
            fish.pullIntensity = -1f;
            fish.escapeSeconds = 0f;
            fish.minCatchWeightKg = 2f;
            fish.maxCatchWeightKg = 1f;

            config.fishDefinitions = new[] { fish };
            config.shipDefinitions = new ShipDefinitionSO[0];
            config.hookDefinitions = new HookDefinitionSO[0];

            var messages = ContentValidator.Validate(config);

            Assert.That(messages, Has.Some.Contains("invalid bite delay range"));
            Assert.That(messages, Has.Some.Contains("non-positive fightStamina"));
            Assert.That(messages, Has.Some.Contains("non-positive pullIntensity"));
            Assert.That(messages, Has.Some.Contains("non-positive escapeSeconds"));
            Assert.That(messages, Has.Some.Contains("invalid catch weight range"));
        }

        private T Create<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            _createdAssets.Add(instance);
            return instance;
        }

        private Sprite BuildSprite()
        {
            var texture = new Texture2D(2, 2);
            _createdAssets.Add(texture);
            var rect = new Rect(0f, 0f, texture.width, texture.height);
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
            _createdAssets.Add(sprite);
            return sprite;
        }
    }
}
