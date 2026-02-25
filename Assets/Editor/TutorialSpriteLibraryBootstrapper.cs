#if UNITY_EDITOR
using System.IO;
using RavenDevOps.Fishing.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RavenDevOps.Fishing.EditorTools
{
    public static class TutorialSpriteLibraryBootstrapper
    {
        private const string LibraryAssetPath = "Assets/Resources/Pilot/Tutorial/SO_TutorialSpriteLibrary.asset";
        private const string ShipSheetPath = "Assets/Art/Sheets/Fishing/ship_source_icons_sheet_v01.png";
        private const string HookSheetPath = "Assets/Art/Sheets/Fishing/hook_source_icons_sheet_v01.png";
        private const string FishSheetPath = "Assets/Art/Sheets/Fishing/fish_source_icons_sheet_v01.png";
        private const string ShipFrameName = "ship_source_icons_sheet_v01_0";
        private const string HookFrameName = "hook_source_icons_sheet_v01_0";
        private const string FishFrameName = "fish_source_icons_sheet_v01_0";

        [MenuItem("Raven/Art/Refresh Tutorial Sprite Library")]
        public static void RefreshFromMenu()
        {
            if (!RefreshTutorialSpriteLibrary())
            {
                Debug.LogError("TutorialSpriteLibraryBootstrapper: refresh failed. See console for details.");
            }
        }

        public static void RefreshTutorialSpriteLibraryBatchMode()
        {
            if (!RefreshTutorialSpriteLibrary())
            {
                throw new BuildFailedException("Tutorial sprite library refresh failed.");
            }
        }

        public static bool RefreshTutorialSpriteLibrary()
        {
            EnsureAssetFolderPath(Path.GetDirectoryName(LibraryAssetPath));

            var harborShipSprite = LoadSpriteByName(ShipSheetPath, ShipFrameName);
            var fishingShipSprite = harborShipSprite;
            var hookSprite = LoadSpriteByName(HookSheetPath, HookFrameName);
            var fishSprite = LoadSpriteByName(FishSheetPath, FishFrameName);
            if (harborShipSprite == null || hookSprite == null || fishSprite == null)
            {
                Debug.LogError("TutorialSpriteLibraryBootstrapper: one or more tutorial sprites are missing.");
                return false;
            }

            var library = AssetDatabase.LoadAssetAtPath<TutorialSpriteLibrary>(LibraryAssetPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<TutorialSpriteLibrary>();
                AssetDatabase.CreateAsset(library, LibraryAssetPath);
            }

            var so = new SerializedObject(library);
            so.FindProperty("_harborShipSprite").objectReferenceValue = harborShipSprite;
            so.FindProperty("_fishingShipSprite").objectReferenceValue = fishingShipSprite;
            so.FindProperty("_hookSprite").objectReferenceValue = hookSprite;
            so.FindProperty("_fishSprite").objectReferenceValue = fishSprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(LibraryAssetPath, ImportAssetOptions.ForceUpdate);

            return true;
        }

        private static Sprite LoadSpriteByName(string assetPath, string spriteName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (var i = 0; i < assets.Length; i++)
            {
                var sprite = assets[i] as Sprite;
                if (sprite == null)
                {
                    continue;
                }

                if (string.Equals(sprite.name, spriteName, System.StringComparison.Ordinal))
                {
                    return sprite;
                }
            }

            Debug.LogError($"TutorialSpriteLibraryBootstrapper: sprite '{spriteName}' not found in '{assetPath}'.");
            return null;
        }

        private static void EnsureAssetFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            var normalized = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            var segments = normalized.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
#endif
