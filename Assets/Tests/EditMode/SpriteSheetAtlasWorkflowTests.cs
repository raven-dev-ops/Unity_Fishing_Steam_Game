using NUnit.Framework;
using RavenDevOps.Fishing.EditorTools;
using UnityEngine;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class SpriteSheetAtlasWorkflowTests
    {
        [Test]
        public void NormalizeCategory_CompactsAndLowercases()
        {
            Assert.That(SpriteSheetAtlasWorkflow.NormalizeCategory(" Fish-Type A "), Is.EqualTo("fish_type_a"));
            Assert.That(SpriteSheetAtlasWorkflow.NormalizeCategory(string.Empty), Is.EqualTo("uncategorized"));
        }

        [Test]
        public void BuildAssetPaths_UseExpectedConventions()
        {
            Assert.That(
                SpriteSheetAtlasWorkflow.BuildSheetAssetPath("Fish"),
                Is.EqualTo("Assets/Art/Sheets/Icons/icons_fish_sheet_v01.png"));

            Assert.That(
                SpriteSheetAtlasWorkflow.BuildAtlasAssetPath("Fish"),
                Is.EqualTo("Assets/Art/Atlases/Icons/icons_fish.spriteatlas"));
        }

        [Test]
        public void ResolveGrid_ProducesSquareBiasedLayout()
        {
            Assert.That(SpriteSheetAtlasWorkflow.ResolveGrid(0), Is.EqualTo(Vector2Int.one));
            Assert.That(SpriteSheetAtlasWorkflow.ResolveGrid(1), Is.EqualTo(new Vector2Int(1, 1)));
            Assert.That(SpriteSheetAtlasWorkflow.ResolveGrid(4), Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(SpriteSheetAtlasWorkflow.ResolveGrid(5), Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(SpriteSheetAtlasWorkflow.ResolveGrid(15), Is.EqualTo(new Vector2Int(4, 4)));
        }
    }
}
