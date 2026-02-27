using NUnit.Framework;
using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class TutorialSpriteLibraryTests
    {
        private const string ResourcePath = "Pilot/Tutorial/SO_TutorialSpriteLibrary";

        [Test]
        public void TutorialSpriteLibrary_RuntimeSprites_AreSingleIcons()
        {
            var library = Resources.Load<TutorialSpriteLibrary>(ResourcePath);
            Assert.That(library, Is.Not.Null, $"Expected tutorial sprite library at Resources/{ResourcePath}.");

            AssertSpriteLooksLikeSingleIcon(library.HarborShipSprite, "HarborShipSprite");
            AssertSpriteLooksLikeSingleIcon(library.FishingShipSprite, "FishingShipSprite");
            AssertSpriteLooksLikeSingleIcon(library.HookSprite, "HookSprite");
            AssertSpriteLooksLikeSingleIcon(library.FishSprite, "FishSprite");
        }

        private static void AssertSpriteLooksLikeSingleIcon(Sprite sprite, string fieldName)
        {
            Assert.That(sprite, Is.Not.Null, $"Expected non-null sprite for {fieldName}.");

            var rect = sprite.rect;
            Assert.That(rect.height, Is.GreaterThan(1f), $"Expected non-empty sprite height for {fieldName}.");
            var aspect = rect.width / rect.height;
            Assert.That(
                aspect,
                Is.LessThanOrEqualTo(2.25f),
                $"{fieldName} appears to reference a wide strip sprite (aspect {aspect:0.00}).");
        }
    }
}
