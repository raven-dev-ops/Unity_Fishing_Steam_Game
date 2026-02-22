#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RavenDevOps.Fishing.EditorTools
{
    public static class SceneVisualScaffolder
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/00_Boot.unity",
            "Assets/Scenes/01_Cinematic.unity",
            "Assets/Scenes/02_MainMenu.unity",
            "Assets/Scenes/03_Harbor.unity",
            "Assets/Scenes/04_Fishing.unity"
        };

        private static readonly string[] SheetPaths =
        {
            "Assets/Art/Sheets/Icons/icons_fish_sheet_v01.png",
            "Assets/Art/Sheets/Icons/icons_hooks_sheet_v01.png",
            "Assets/Art/Sheets/Icons/icons_ships_sheet_v01.png",
            "Assets/Art/Sheets/Icons/icons_ui_sheet_v01.png",
            "Assets/Art/Sheets/Icons/icons_misc_sheet_v01.png"
        };

        private static Dictionary<string, Sprite> _spritesByName;

        [MenuItem("Raven/Scenes/Rebuild Scene Visuals")]
        public static void RebuildSceneVisualsFromMenu()
        {
            RebuildAllScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SceneVisualScaffolder: scene visual scaffolds rebuilt.");
        }

        public static void RebuildSceneVisualsBatchMode()
        {
            try
            {
                RebuildAllScenes();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("SceneVisualScaffolder: scene visual scaffolds rebuilt.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SceneVisualScaffolder: failed to rebuild scenes ({ex.Message}).");
                EditorApplication.Exit(1);
            }
        }

        private static void RebuildAllScenes()
        {
            _spritesByName = BuildSpriteLookup();

            for (var i = 0; i < ScenePaths.Length; i++)
            {
                var scenePath = ScenePaths[i];
                if (!System.IO.File.Exists(scenePath))
                {
                    throw new InvalidOperationException($"Scene path not found: {scenePath}");
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                ClearRootObjects(scene);
                BuildSceneVisuals(scenePath);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static Dictionary<string, Sprite> BuildSpriteLookup()
        {
            var lookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < SheetPaths.Length; i++)
            {
                var path = SheetPaths[i];
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (var j = 0; j < assets.Length; j++)
                {
                    if (assets[j] is Sprite sprite && sprite != null && !lookup.ContainsKey(sprite.name))
                    {
                        lookup.Add(sprite.name, sprite);
                    }
                }
            }

            return lookup;
        }

        private static void ClearRootObjects(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(roots[i]);
            }
        }

        private static void BuildSceneVisuals(string scenePath)
        {
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            var root = new GameObject("__SceneVisuals").transform;
            root.position = Vector3.zero;

            CreateSceneCamera(sceneName);

            var sorting = -20;
            AddBackdrop(sceneName, root, ref sorting);

            switch (sceneName)
            {
                case "00_Boot":
                    BuildBootScene(root, ref sorting);
                    break;
                case "01_Cinematic":
                    BuildCinematicScene(root, ref sorting);
                    break;
                case "02_MainMenu":
                    BuildMainMenuScene(root, ref sorting);
                    break;
                case "03_Harbor":
                    BuildHarborScene(root, ref sorting);
                    break;
                case "04_Fishing":
                    BuildFishingScene(root, ref sorting);
                    break;
            }
        }

        private static void CreateSceneCamera(string sceneName)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";

            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = ResolveCameraSize(sceneName);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ResolveBackgroundColor(sceneName);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void AddBackdrop(string sceneName, Transform root, ref int sorting)
        {
            var bg = ResolveSprite("photo_yyyymmdd_hhmmss_mmm", "icons_misc_sheet_v01_0", "icon_test");
            var badge = ResolveSprite("icon_test", "icons_ui_sheet_v01_0");

            CreateSprite(
                "BackdropFar",
                bg,
                new Vector3(0f, 0f, 0f),
                new Vector2(7.4f, 6.6f),
                sorting++,
                Color.white,
                root,
                0f,
                false);

            CreateSprite(
                "BackdropVeil",
                bg,
                new Vector3(0f, -0.15f, 0f),
                new Vector2(6.2f, 4.4f),
                sorting++,
                new Color(0.75f, 0.88f, 1f, 0.35f),
                root,
                0f,
                false);

            CreateSprite(
                "TopBadge",
                badge,
                new Vector3(0f, 3.3f, 0f),
                new Vector2(1.2f, 1.2f),
                sorting++,
                new Color(1f, 0.95f, 0.75f, 0.95f),
                root,
                0f,
                false);

            if (string.Equals(sceneName, "04_Fishing", StringComparison.Ordinal))
            {
                CreateSprite(
                    "WaterSurfaceBand",
                    badge,
                    new Vector3(0f, 1.65f, 0f),
                    new Vector2(8.6f, 0.34f),
                    sorting++,
                    new Color(0.65f, 0.87f, 1f, 0.55f),
                    root,
                    0f,
                    false);
            }
        }

        private static void BuildBootScene(Transform root, ref int sorting)
        {
            CreateSprite("BootShip", ResolveSprite("ship_lv1", "ship_lvl1_basic_v01", "ship_icon_coastal"), new Vector3(0f, -0.35f, 0f), new Vector2(1.65f, 1.65f), sorting++, Color.white, root, 0f, false);
            CreateSprite("BootFishLeft", ResolveSprite("fish_icon", "fish_cod"), new Vector3(-2.8f, -1.1f, 0f), new Vector2(1.1f, 1.1f), sorting++, Color.white, root, 14f, false);
            CreateSprite("BootFishRight", ResolveSprite("fish_icon_coastal", "fish_coastal_snapper"), new Vector3(2.8f, -1.0f, 0f), new Vector2(1.1f, 1.1f), sorting++, Color.white, root, -14f, true);
            CreateSprite("BootHookLeft", ResolveSprite("hook_lv1", "hook_icon_coastal"), new Vector3(-1.45f, -2.35f, 0f), new Vector2(0.8f, 0.8f), sorting++, new Color(0.9f, 0.97f, 1f, 0.95f), root, 0f, false);
            CreateSprite("BootHookRight", ResolveSprite("hook_lv2", "hook_lv1"), new Vector3(1.45f, -2.35f, 0f), new Vector2(0.8f, 0.8f), sorting++, new Color(0.9f, 0.97f, 1f, 0.95f), root, 0f, true);
        }

        private static void BuildCinematicScene(Transform root, ref int sorting)
        {
            CreateSprite("CineLeadShip", ResolveSprite("ship_lv3", "ship_coastal_runner", "ship_lv2"), new Vector3(0f, 0.55f, 0f), new Vector2(1.75f, 1.75f), sorting++, Color.white, root, 0f, false);
            CreateSprite("CineWingShipL", ResolveSprite("ship_lv2", "ship_lv1"), new Vector3(-3.4f, -0.5f, 0f), new Vector2(1.2f, 1.2f), sorting++, new Color(0.92f, 0.98f, 1f, 0.95f), root, 6f, false);
            CreateSprite("CineWingShipR", ResolveSprite("ship_coastal_runner", "ship_lv2", "ship_lv1"), new Vector3(3.4f, -0.5f, 0f), new Vector2(1.2f, 1.2f), sorting++, new Color(0.92f, 0.98f, 1f, 0.95f), root, -6f, true);

            CreateSprite("CineFishA", ResolveSprite("fish_heavy", "fish_cod"), new Vector3(-2.85f, -2.15f, 0f), new Vector2(1.0f, 1.0f), sorting++, Color.white, root, 12f, false);
            CreateSprite("CineFishB", ResolveSprite("fish_coastal_snapper", "fish_b"), new Vector3(-0.65f, -2.0f, 0f), new Vector2(1.05f, 1.05f), sorting++, Color.white, root, 3f, false);
            CreateSprite("CineFishC", ResolveSprite("fish_cod", "fish_a"), new Vector3(1.75f, -2.2f, 0f), new Vector2(1.0f, 1.0f), sorting++, Color.white, root, -6f, true);
            CreateSprite("CineFishD", ResolveSprite("fish_light", "fish_valid"), new Vector3(3.75f, -1.95f, 0f), new Vector2(0.95f, 0.95f), sorting++, Color.white, root, -12f, true);
        }

        private static void BuildMainMenuScene(Transform root, ref int sorting)
        {
            CreateSprite("MenuHeroShip", ResolveSprite("ship_lv2", "ship_lv1", "ship_icon_coastal"), new Vector3(0f, -0.55f, 0f), new Vector2(1.8f, 1.8f), sorting++, Color.white, root, 0f, false);

            CreateSprite("MenuHookL1", ResolveSprite("hook_lv1", "hook_icon_coastal"), new Vector3(-4.25f, 0.9f, 0f), new Vector2(0.82f, 0.82f), sorting++, new Color(0.88f, 0.97f, 1f, 0.95f), root, 0f, false);
            CreateSprite("MenuHookL2", ResolveSprite("hook_lv2", "hook_lv1"), new Vector3(-4.25f, -0.35f, 0f), new Vector2(0.82f, 0.82f), sorting++, new Color(0.88f, 0.97f, 1f, 0.95f), root, 0f, false);
            CreateSprite("MenuHookL3", ResolveSprite("hook_lv3", "hook_lv2"), new Vector3(-4.25f, -1.6f, 0f), new Vector2(0.82f, 0.82f), sorting++, new Color(0.88f, 0.97f, 1f, 0.95f), root, 0f, false);

            CreateSprite("MenuFishR1", ResolveSprite("fish_cod", "fish_a"), new Vector3(4.25f, 0.9f, 0f), new Vector2(0.96f, 0.96f), sorting++, Color.white, root, -12f, true);
            CreateSprite("MenuFishR2", ResolveSprite("fish_coastal_snapper", "fish_b"), new Vector3(4.25f, -0.35f, 0f), new Vector2(0.96f, 0.96f), sorting++, Color.white, root, -5f, true);
            CreateSprite("MenuFishR3", ResolveSprite("fish_heavy", "fish_valid"), new Vector3(4.25f, -1.6f, 0f), new Vector2(0.96f, 0.96f), sorting++, Color.white, root, 7f, true);
        }

        private static void BuildHarborScene(Transform root, ref int sorting)
        {
            var dockColor = new Color(0.58f, 0.43f, 0.27f, 0.92f);
            for (var i = -4; i <= 4; i++)
            {
                CreateSprite($"DockPlank_{i}", ResolveSprite("icon_test", "icons_ui_sheet_v01_0"), new Vector3(i * 1.15f, -2.95f, 0f), new Vector2(1.25f, 0.34f), sorting++, dockColor, root, 0f, false);
            }

            CreateSprite("HarborShipMain", ResolveSprite("ship_lv3", "ship_coastal_runner", "ship_lv2"), new Vector3(0f, 0.7f, 0f), new Vector2(1.7f, 1.7f), sorting++, Color.white, root, 0f, false);
            CreateSprite("HarborShipSide", ResolveSprite("ship_lv1", "ship_lvl1_basic_v01"), new Vector3(-3.25f, -0.3f, 0f), new Vector2(1.1f, 1.1f), sorting++, new Color(0.9f, 0.97f, 1f, 0.9f), root, 0f, true);

            CreateSprite("HarborMarketFish1", ResolveSprite("fish_cod", "fish_a"), new Vector3(-2.7f, -1.9f, 0f), new Vector2(0.88f, 0.88f), sorting++, Color.white, root, 8f, false);
            CreateSprite("HarborMarketFish2", ResolveSprite("fish_coastal_snapper", "fish_b"), new Vector3(-1.1f, -1.9f, 0f), new Vector2(0.88f, 0.88f), sorting++, Color.white, root, -6f, true);
            CreateSprite("HarborMarketFish3", ResolveSprite("fish_light", "fish_valid"), new Vector3(0.5f, -1.9f, 0f), new Vector2(0.88f, 0.88f), sorting++, Color.white, root, 10f, false);

            CreateSprite("HarborMarketHook1", ResolveSprite("hook_lv1", "hook_icon_coastal"), new Vector3(2.1f, -1.9f, 0f), new Vector2(0.86f, 0.86f), sorting++, new Color(0.88f, 0.97f, 1f, 0.95f), root, 0f, false);
            CreateSprite("HarborMarketHook2", ResolveSprite("hook_lv2", "hook_lv1"), new Vector3(3.45f, -1.9f, 0f), new Vector2(0.86f, 0.86f), sorting++, new Color(0.88f, 0.97f, 1f, 0.95f), root, 0f, true);
        }

        private static void BuildFishingScene(Transform root, ref int sorting)
        {
            CreateSprite("FishingShip", ResolveSprite("ship_lv2", "ship_lv1", "ship_coastal_runner"), new Vector3(0f, 2.4f, 0f), new Vector2(1.35f, 1.35f), sorting++, Color.white, root, 0f, false);
            CreateSprite("FishingLine", ResolveSprite("icon_test", "icons_ui_sheet_v01_0"), new Vector3(0f, 0.55f, 0f), new Vector2(0.06f, 2.8f), sorting++, new Color(0.94f, 0.98f, 1f, 0.92f), root, 0f, false);
            CreateSprite("FishingHook", ResolveSprite("hook_lv3", "hook_lv2", "hook_lv1"), new Vector3(0f, -1.05f, 0f), new Vector2(0.9f, 0.9f), sorting++, new Color(0.9f, 0.97f, 1f, 0.98f), root, 0f, false);

            CreateSprite("FishingFishLeftA", ResolveSprite("fish_cod", "fish_a"), new Vector3(-3.55f, -2.35f, 0f), new Vector2(0.95f, 0.95f), sorting++, Color.white, root, 8f, false);
            CreateSprite("FishingFishLeftB", ResolveSprite("fish_coastal_snapper", "fish_b"), new Vector3(-1.8f, -2.1f, 0f), new Vector2(0.98f, 0.98f), sorting++, Color.white, root, -8f, true);
            CreateSprite("FishingFishMid", ResolveSprite("fish_heavy", "fish_pack"), new Vector3(0.2f, -2.55f, 0f), new Vector2(1.08f, 1.08f), sorting++, Color.white, root, 3f, false);
            CreateSprite("FishingFishRightA", ResolveSprite("fish_light", "fish_valid"), new Vector3(2.25f, -2.2f, 0f), new Vector2(0.95f, 0.95f), sorting++, Color.white, root, -12f, true);
            CreateSprite("FishingFishRightB", ResolveSprite("fish_surface", "fish_icon_coastal"), new Vector3(3.95f, -2.0f, 0f), new Vector2(0.93f, 0.93f), sorting++, Color.white, root, 12f, false);
        }

        private static float ResolveCameraSize(string sceneName)
        {
            switch (sceneName)
            {
                case "00_Boot":
                    return 6f;
                case "01_Cinematic":
                    return 6.8f;
                case "02_MainMenu":
                    return 6.5f;
                case "03_Harbor":
                    return 6.6f;
                case "04_Fishing":
                    return 6.8f;
                default:
                    return 6f;
            }
        }

        private static Color ResolveBackgroundColor(string sceneName)
        {
            switch (sceneName)
            {
                case "00_Boot":
                    return new Color(0.07f, 0.09f, 0.14f, 1f);
                case "01_Cinematic":
                    return new Color(0.08f, 0.12f, 0.20f, 1f);
                case "02_MainMenu":
                    return new Color(0.10f, 0.16f, 0.26f, 1f);
                case "03_Harbor":
                    return new Color(0.09f, 0.18f, 0.28f, 1f);
                case "04_Fishing":
                    return new Color(0.08f, 0.22f, 0.34f, 1f);
                default:
                    return new Color(0.1f, 0.1f, 0.1f, 1f);
            }
        }

        private static Sprite ResolveSprite(params string[] candidates)
        {
            if (_spritesByName == null || candidates == null)
            {
                return null;
            }

            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (!string.IsNullOrWhiteSpace(candidate) && _spritesByName.TryGetValue(candidate, out var sprite) && sprite != null)
                {
                    return sprite;
                }
            }

            foreach (var pair in _spritesByName)
            {
                if (pair.Value != null)
                {
                    return pair.Value;
                }
            }

            return null;
        }

        private static void CreateSprite(
            string objectName,
            Sprite sprite,
            Vector3 position,
            Vector2 scale,
            int sortingOrder,
            Color color,
            Transform parent,
            float rotationZ,
            bool flipX)
        {
            if (sprite == null)
            {
                return;
            }

            var go = new GameObject(objectName);
            if (parent != null)
            {
                go.transform.SetParent(parent, worldPositionStays: false);
            }

            go.transform.position = position;
            go.transform.localScale = new Vector3(Mathf.Max(0.05f, scale.x), Mathf.Max(0.05f, scale.y), 1f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = color;
            renderer.flipX = flipX;
        }
    }
}
#endif
