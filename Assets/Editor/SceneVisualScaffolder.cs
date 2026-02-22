#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
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

        private static readonly string[] SpriteSearchRoots =
        {
            "Assets/Art/Sheets/Icons",
            "Assets/Art/Source/Icons",
            "Assets/Art/Source/Scenes"
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
            for (var i = 0; i < SpriteSearchRoots.Length; i++)
            {
                var searchRoot = SpriteSearchRoots[i];
                var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { searchRoot });
                for (var guidIndex = 0; guidIndex < textureGuids.Length; guidIndex++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(textureGuids[guidIndex]);
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    for (var j = 0; j < assets.Length; j++)
                    {
                        if (assets[j] is Sprite sprite && sprite != null && !lookup.ContainsKey(sprite.name))
                        {
                            lookup.Add(sprite.name, sprite);
                        }
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
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                if (string.Equals(root.name, "__SceneVisuals", StringComparison.Ordinal)
                    || string.Equals(root.name, "Main Camera", StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static void BuildSceneVisuals(string scenePath)
        {
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            var rootGo = new GameObject("__SceneVisuals");
            var root = rootGo.transform;
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

            ApplySceneOrchestration(sceneName, rootGo);
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
            camera.nearClipPlane = -20f;
            camera.farClipPlane = 30f;

            cameraGo.AddComponent<AudioListener>();
        }

        private static void AddBackdrop(string sceneName, Transform root, ref int sorting)
        {
            var bg = ResolveBackdropSprite(sceneName);
            var badge = ResolveSprite("icon_test_v01", "icon_test", "icons_ui_sheet_v01_0");
            var fallbackBackdrop = ResolveSprite("icon_test_v01", "icon_test", "icons_ui_sheet_v01_0", "ship_lv1", "ship_lvl1_basic_v01");
            if (bg == null)
            {
                bg = fallbackBackdrop;
            }

            if (badge == null)
            {
                badge = fallbackBackdrop;
            }

            var farScale = new Vector2(7.4f, 6.6f);
            var veilScale = new Vector2(6.2f, 4.4f);
            var farTint = new Color(0.92f, 0.96f, 1f, 0.92f);
            var veilTint = new Color(0.75f, 0.88f, 1f, 0.35f);
            var badgeTint = new Color(1f, 0.95f, 0.75f, 0.95f);
            var badgePosition = new Vector3(0f, 3.3f, 0f);

            switch (sceneName)
            {
                case "00_Boot":
                    farTint = new Color(0.60f, 0.70f, 0.84f, 0.95f);
                    veilTint = new Color(0.70f, 0.82f, 1f, 0.34f);
                    break;
                case "01_Cinematic":
                    farScale = new Vector2(8.6f, 6.8f);
                    farTint = new Color(0.70f, 0.74f, 0.88f, 0.92f);
                    veilTint = new Color(0.82f, 0.86f, 1f, 0.28f);
                    badgeTint = new Color(1f, 0.93f, 0.72f, 0.92f);
                    break;
                case "02_MainMenu":
                    farTint = new Color(0.62f, 0.76f, 0.90f, 0.93f);
                    veilTint = new Color(0.76f, 0.89f, 1f, 0.30f);
                    break;
                case "03_Harbor":
                    farScale = new Vector2(8.8f, 6.9f);
                    veilScale = new Vector2(7.1f, 4.7f);
                    farTint = new Color(0.58f, 0.78f, 0.90f, 0.92f);
                    veilTint = new Color(0.70f, 0.90f, 1f, 0.32f);
                    badgeTint = new Color(1f, 0.96f, 0.82f, 0.90f);
                    break;
                case "04_Fishing":
                    farScale = new Vector2(9.2f, 7.2f);
                    veilScale = new Vector2(7.3f, 4.9f);
                    farTint = new Color(0.52f, 0.82f, 0.96f, 0.90f);
                    veilTint = new Color(0.62f, 0.89f, 1f, 0.40f);
                    badgeTint = new Color(1f, 0.97f, 0.82f, 0.85f);
                    badgePosition = new Vector3(0f, 3.1f, 0f);
                    break;
            }

            CreateSprite(
                "BackdropFar",
                bg,
                new Vector3(0f, 0f, 0f),
                farScale,
                sorting++,
                farTint,
                root,
                0f,
                false);

            CreateSprite(
                "BackdropVeil",
                bg,
                new Vector3(0f, -0.15f, 0f),
                veilScale,
                sorting++,
                veilTint,
                root,
                0f,
                false);

            CreateSprite(
                "TopBadge",
                badge,
                badgePosition,
                new Vector2(1.2f, 1.2f),
                sorting++,
                badgeTint,
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
            CreateSprite("BootShip", ResolveSprite("ship_lv1", "ship_lvl1_basic_v01", "ship_icon_coastal"), new Vector3(0f, -0.1f, 0f), new Vector2(1.55f, 1.55f), sorting++, Color.white, root, 0f, false);
            CreateSprite("BootFishLeft", ResolveSprite("fish_icon", "fish_cod"), new Vector3(-3.4f, -0.75f, 0f), new Vector2(0.98f, 0.98f), sorting++, Color.white, root, 12f, false);
            CreateSprite("BootFishRight", ResolveSprite("fish_icon_coastal", "fish_coastal_snapper"), new Vector3(3.35f, -1.3f, 0f), new Vector2(1.0f, 1.0f), sorting++, Color.white, root, -12f, true);
            CreateSprite("BootHookLeft", ResolveSprite("hook_lv1", "hook_icon_coastal"), new Vector3(-2.2f, -2.65f, 0f), new Vector2(0.74f, 0.74f), sorting++, new Color(0.9f, 0.97f, 1f, 0.95f), root, 0f, false);
            CreateSprite("BootHookRight", ResolveSprite("hook_lv2", "hook_lv1"), new Vector3(2.25f, -2.55f, 0f), new Vector2(0.74f, 0.74f), sorting++, new Color(0.9f, 0.97f, 1f, 0.95f), root, 0f, true);
        }

        private static void BuildCinematicScene(Transform root, ref int sorting)
        {
            CreateSprite("CineLeadShip", ResolveSprite("ship_lv3", "ship_coastal_runner", "ship_lv2"), new Vector3(0f, 0.95f, 0f), new Vector2(1.6f, 1.6f), sorting++, Color.white, root, 0f, false);
            CreateSprite("CineWingShipL", ResolveSprite("ship_lv2", "ship_lv1"), new Vector3(-4.0f, -0.1f, 0f), new Vector2(1.08f, 1.08f), sorting++, new Color(0.92f, 0.98f, 1f, 0.95f), root, 6f, false);
            CreateSprite("CineWingShipR", ResolveSprite("ship_coastal_runner", "ship_lv2", "ship_lv1"), new Vector3(4.0f, -0.1f, 0f), new Vector2(1.08f, 1.08f), sorting++, new Color(0.92f, 0.98f, 1f, 0.95f), root, -6f, true);

            CreateSprite("CineFishA", ResolveSprite("fish_heavy", "fish_cod"), new Vector3(-4.2f, -2.4f, 0f), new Vector2(0.96f, 0.96f), sorting++, Color.white, root, 10f, false);
            CreateSprite("CineFishB", ResolveSprite("fish_coastal_snapper", "fish_b"), new Vector3(-1.4f, -2.1f, 0f), new Vector2(1.0f, 1.0f), sorting++, Color.white, root, 2f, false);
            CreateSprite("CineFishC", ResolveSprite("fish_cod", "fish_a"), new Vector3(1.4f, -2.25f, 0f), new Vector2(0.98f, 0.98f), sorting++, Color.white, root, -4f, true);
            CreateSprite("CineFishD", ResolveSprite("fish_light", "fish_valid"), new Vector3(4.0f, -1.95f, 0f), new Vector2(0.92f, 0.92f), sorting++, Color.white, root, -10f, true);
        }

        private static void BuildMainMenuScene(Transform root, ref int sorting)
        {
            CreateSprite("MenuHeroShip", ResolveSprite("ship_lv2", "ship_lv1", "ship_icon_coastal"), new Vector3(0f, -0.95f, 0f), new Vector2(1.65f, 1.65f), sorting++, Color.white, root, 0f, false);

            CreateSprite("MenuHookL1", ResolveSprite("hook_lv1", "hook_icon_coastal"), new Vector3(-5.0f, 1.25f, 0f), new Vector2(0.78f, 0.78f), sorting++, new Color(0.88f, 0.97f, 1f, 0.95f), root, 0f, false);
            CreateSprite("MenuHookL2", ResolveSprite("hook_lv2", "hook_lv1"), new Vector3(-5.0f, -0.25f, 0f), new Vector2(0.78f, 0.78f), sorting++, new Color(0.88f, 0.97f, 1f, 0.95f), root, 0f, false);

            CreateSprite("MenuFishR1", ResolveSprite("fish_cod", "fish_a"), new Vector3(5.0f, 1.35f, 0f), new Vector2(0.92f, 0.92f), sorting++, Color.white, root, -10f, true);
            CreateSprite("MenuFishR2", ResolveSprite("fish_coastal_snapper", "fish_b"), new Vector3(5.0f, -0.2f, 0f), new Vector2(0.94f, 0.94f), sorting++, Color.white, root, -4f, true);
        }

        private static void BuildHarborScene(Transform root, ref int sorting)
        {
            var dockColor = new Color(0.58f, 0.43f, 0.27f, 0.92f);
            for (var i = -5; i <= 5; i++)
            {
                CreateSprite($"DockPlank_{i}", ResolveSprite("icon_test", "icons_ui_sheet_v01_0"), new Vector3(i * 1.1f, -3.1f, 0f), new Vector2(1.18f, 0.34f), sorting++, dockColor, root, 0f, false);
            }

            CreateSprite("HarborShipMain", ResolveSprite("ship_lv3", "ship_coastal_runner", "ship_lv2"), new Vector3(1.2f, 0.8f, 0f), new Vector2(1.65f, 1.65f), sorting++, Color.white, root, 0f, false);
            CreateSprite("HarborShipSide", ResolveSprite("ship_lv1", "ship_lvl1_basic_v01"), new Vector3(-3.65f, 0.05f, 0f), new Vector2(1.05f, 1.05f), sorting++, new Color(0.9f, 0.97f, 1f, 0.9f), root, 0f, true);

            CreateSprite("HarborMarketFish1", ResolveSprite("fish_cod", "fish_a"), new Vector3(-2.5f, -2.15f, 0f), new Vector2(0.86f, 0.86f), sorting++, Color.white, root, 8f, false);
            CreateSprite("HarborMarketFish2", ResolveSprite("fish_coastal_snapper", "fish_b"), new Vector3(-1.2f, -2.25f, 0f), new Vector2(0.88f, 0.88f), sorting++, Color.white, root, -6f, true);
            CreateSprite("HarborMarketFish3", ResolveSprite("fish_light", "fish_valid"), new Vector3(0.2f, -2.05f, 0f), new Vector2(0.84f, 0.84f), sorting++, Color.white, root, 10f, false);

            CreateSprite("HarborMarketHook1", ResolveSprite("hook_lv1", "hook_icon_coastal"), new Vector3(2.2f, -2.15f, 0f), new Vector2(0.82f, 0.82f), sorting++, new Color(0.88f, 0.97f, 1f, 0.95f), root, 0f, false);
            CreateSprite("HarborMarketHook2", ResolveSprite("hook_lv2", "hook_lv1"), new Vector3(3.6f, -2.2f, 0f), new Vector2(0.82f, 0.82f), sorting++, new Color(0.88f, 0.97f, 1f, 0.95f), root, 0f, true);
        }

        private static void BuildFishingScene(Transform root, ref int sorting)
        {
            CreateSprite("FishingShip", ResolveSprite("ship_lv2", "ship_lv1", "ship_coastal_runner"), new Vector3(0f, 2.55f, 0f), new Vector2(1.25f, 1.25f), sorting++, Color.white, root, 0f, false);
            CreateSprite("FishingLine", ResolveSprite("icon_test", "icons_ui_sheet_v01_0"), new Vector3(0f, 0.95f, 0f), new Vector2(0.06f, 3.0f), sorting++, new Color(0.94f, 0.98f, 1f, 0.92f), root, 0f, false);
            CreateSprite("FishingHook", ResolveSprite("hook_lv3", "hook_lv2", "hook_lv1"), new Vector3(0f, -1.3f, 0f), new Vector2(0.82f, 0.82f), sorting++, new Color(0.9f, 0.97f, 1f, 0.98f), root, 0f, false);

            CreateSprite("FishingFishLeftA", ResolveSprite("fish_cod", "fish_a"), new Vector3(-4.4f, -2.65f, 0f), new Vector2(0.92f, 0.92f), sorting++, Color.white, root, 8f, false);
            CreateSprite("FishingFishLeftB", ResolveSprite("fish_coastal_snapper", "fish_b"), new Vector3(-2.5f, -2.25f, 0f), new Vector2(0.94f, 0.94f), sorting++, Color.white, root, -8f, true);
            CreateSprite("FishingFishMid", ResolveSprite("fish_heavy", "fish_pack"), new Vector3(0.4f, -2.8f, 0f), new Vector2(1.02f, 1.02f), sorting++, Color.white, root, 3f, false);
            CreateSprite("FishingFishRightA", ResolveSprite("fish_light", "fish_valid"), new Vector3(2.8f, -2.35f, 0f), new Vector2(0.91f, 0.91f), sorting++, Color.white, root, -12f, true);
            CreateSprite("FishingFishRightB", ResolveSprite("fish_surface", "fish_icon_coastal"), new Vector3(4.8f, -2.1f, 0f), new Vector2(0.9f, 0.9f), sorting++, Color.white, root, 12f, false);
        }

        private static void ApplySceneOrchestration(string sceneName, GameObject sceneRoot)
        {
            if (sceneRoot == null)
            {
                return;
            }

            var root = sceneRoot.transform;
            var stageBackdrop = CreateStageRoot(root, "Stage_Backdrop");
            var stageWorld = CreateStageRoot(root, "Stage_World");
            var stageActors = CreateStageRoot(root, "Stage_Actors");
            var stageForeground = CreateStageRoot(root, "Stage_Foreground");

            var children = new List<Transform>();
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child == stageBackdrop || child == stageWorld || child == stageActors || child == stageForeground)
                {
                    continue;
                }

                children.Add(child);
            }

            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                var targetStage = ResolveStageForObject(sceneName, child.name, stageBackdrop, stageWorld, stageActors, stageForeground);
                child.SetParent(targetStage, worldPositionStays: true);
                TryAttachSway(sceneName, child.gameObject);
            }

            var orchestrator = sceneRoot.GetComponent<SceneVisualOrchestrator2D>();
            if (orchestrator == null)
            {
                orchestrator = sceneRoot.AddComponent<SceneVisualOrchestrator2D>();
            }

            SceneVisualOrchestrator2D.StageDefinition[] stageDefinitions;
            var initialDelay = 0.05f;
            var loopSequence = false;
            var loopDelay = 0f;

            switch (sceneName)
            {
                case "00_Boot":
                    initialDelay = 0.20f;
                    loopSequence = true;
                    loopDelay = 0.85f;
                    stageDefinitions = new[]
                    {
                        new SceneVisualOrchestrator2D.StageDefinition("Backdrop", stageBackdrop, 0f, 1.25f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("World", stageWorld, 0.40f, 1.05f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("Actors", stageActors, 0.32f, 1.0f, true, 0.65f),
                        new SceneVisualOrchestrator2D.StageDefinition("Foreground", stageForeground, 0.24f, 0.95f, true, 0.5f)
                    };
                    break;
                case "01_Cinematic":
                    initialDelay = 0.18f;
                    loopSequence = true;
                    loopDelay = 0.9f;
                    stageDefinitions = new[]
                    {
                        new SceneVisualOrchestrator2D.StageDefinition("Backdrop", stageBackdrop, 0f, 1.35f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("World", stageWorld, 0.36f, 1.15f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("Actors", stageActors, 0.34f, 1.05f, true, 0.7f),
                        new SceneVisualOrchestrator2D.StageDefinition("Foreground", stageForeground, 0.26f, 1.0f, true, 0.55f)
                    };
                    break;
                case "02_MainMenu":
                    initialDelay = 0.08f;
                    stageDefinitions = new[]
                    {
                        new SceneVisualOrchestrator2D.StageDefinition("Backdrop", stageBackdrop, 0f, 0.95f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("World", stageWorld, 0.30f, 0.85f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("Actors", stageActors, 0.26f, 0.80f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("Foreground", stageForeground, 0.20f, 0.75f, false, 0f)
                    };
                    break;
                case "03_Harbor":
                    initialDelay = 0.06f;
                    stageDefinitions = new[]
                    {
                        new SceneVisualOrchestrator2D.StageDefinition("Backdrop", stageBackdrop, 0f, 1.0f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("World", stageWorld, 0.34f, 0.92f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("Actors", stageActors, 0.28f, 0.86f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("Foreground", stageForeground, 0.22f, 0.78f, false, 0f)
                    };
                    break;
                case "04_Fishing":
                    initialDelay = 0.06f;
                    stageDefinitions = new[]
                    {
                        new SceneVisualOrchestrator2D.StageDefinition("Backdrop", stageBackdrop, 0f, 1.05f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("World", stageWorld, 0.30f, 0.95f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("Actors", stageActors, 0.26f, 0.90f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("Foreground", stageForeground, 0.22f, 0.82f, false, 0f)
                    };
                    break;
                default:
                    stageDefinitions = new[]
                    {
                        new SceneVisualOrchestrator2D.StageDefinition("Backdrop", stageBackdrop, 0f, 0.9f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("World", stageWorld, 0.24f, 0.8f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("Actors", stageActors, 0.2f, 0.72f, false, 0f),
                        new SceneVisualOrchestrator2D.StageDefinition("Foreground", stageForeground, 0.18f, 0.66f, false, 0f)
                    };
                    break;
            }

            orchestrator.ConfigureStages(stageDefinitions, initialDelay, loopSequence, loopDelay);
            orchestrator.ShowAllImmediate();
        }

        private static Transform CreateStageRoot(Transform parent, string name)
        {
            var stage = new GameObject(name).transform;
            stage.SetParent(parent, worldPositionStays: false);
            stage.localPosition = Vector3.zero;
            stage.localRotation = Quaternion.identity;
            stage.localScale = Vector3.one;
            return stage;
        }

        private static Transform ResolveStageForObject(
            string sceneName,
            string objectName,
            Transform backdrop,
            Transform world,
            Transform actors,
            Transform foreground)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return world;
            }

            if (NameContains(objectName, "Backdrop")
                || NameContains(objectName, "Veil"))
            {
                return backdrop;
            }

            if (NameContains(objectName, "Dock")
                || NameContains(objectName, "Ship"))
            {
                return world;
            }

            if (NameContains(objectName, "Fish"))
            {
                return actors;
            }

            if (NameContains(objectName, "Line")
                || NameContains(objectName, "Surface"))
            {
                return string.Equals(sceneName, "04_Fishing", StringComparison.Ordinal) ? world : foreground;
            }

            if (NameContains(objectName, "Hook")
                || NameContains(objectName, "Badge"))
            {
                return foreground;
            }

            return world;
        }

        private static void TryAttachSway(string sceneName, GameObject spriteObject)
        {
            if (spriteObject == null || spriteObject.GetComponent<SpriteRenderer>() == null)
            {
                return;
            }

            if (spriteObject.GetComponent<SpriteSwayMotion2D>() != null)
            {
                return;
            }

            var name = spriteObject.name ?? string.Empty;
            var phase = ComputePhase(name);

            if (NameContains(name, "Ship"))
            {
                AddSway(spriteObject, new Vector2(0.09f, 0.05f), 0.18f, 2f, 0.12f, 0.02f, 0.10f, phase);
                return;
            }

            if (NameContains(name, "Fish"))
            {
                AddSway(spriteObject, new Vector2(0.20f, 0.11f), 0.29f, 8f, 0.23f, 0.03f, 0.18f, phase);
                return;
            }

            if (NameContains(name, "Hook"))
            {
                AddSway(spriteObject, new Vector2(0.06f, 0.13f), 0.24f, 6f, 0.20f, 0.01f, 0.11f, phase);
                return;
            }

            if (NameContains(name, "Line") || NameContains(name, "Surface"))
            {
                var lineAmplitude = string.Equals(sceneName, "04_Fishing", StringComparison.Ordinal)
                    ? new Vector2(0.02f, 0.06f)
                    : new Vector2(0.02f, 0.03f);
                AddSway(spriteObject, lineAmplitude, 0.22f, 1.5f, 0.14f, 0.005f, 0.08f, phase);
                return;
            }

            if (NameContains(name, "Badge"))
            {
                AddSway(spriteObject, new Vector2(0.03f, 0.04f), 0.12f, 2f, 0.10f, 0.01f, 0.06f, phase);
            }
        }

        private static Sprite ResolveBackdropSprite(string sceneName)
        {
            switch (sceneName)
            {
                case "00_Boot":
                    return ResolveSprite("00_boot_v01_0", "00_boot_v01", "00_boot", "photo_yyyymmdd_hhmmss_mmm_v01", "icons_misc_sheet_v01_0");
                case "01_Cinematic":
                    return ResolveSprite("01_cinematic_v01_0", "01_cinematic_v01", "01_cinematic", "photo_yyyymmdd_hhmmss_mmm_v01", "icons_misc_sheet_v01_0");
                case "02_MainMenu":
                    return ResolveSprite("02_mainmenu_v01_0", "02_mainmenu_v01", "02_mainmenu", "photo_yyyymmdd_hhmmss_mmm_v01", "icons_misc_sheet_v01_0");
                case "03_Harbor":
                    return ResolveSprite("03_harbor_v01_0", "03_harbor_v01", "03_harbor", "photo_yyyymmdd_hhmmss_mmm_v01", "icons_misc_sheet_v01_0");
                case "04_Fishing":
                    return ResolveSprite("04_fishing_v01_0", "04_fishing_v01", "04_fishing", "photo_yyyymmdd_hhmmss_mmm_v01", "icons_misc_sheet_v01_0");
                default:
                    return ResolveSprite("photo_yyyymmdd_hhmmss_mmm_v01", "photo_yyyymmdd_hhmmss_mmm", "icons_misc_sheet_v01_0");
            }
        }

        private static bool NameContains(string source, string token)
        {
            return !string.IsNullOrEmpty(source)
                && !string.IsNullOrEmpty(token)
                && source.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float ComputePhase(string value)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < value.Length; i++)
                {
                    hash = (hash * 31) + value[i];
                }

                var positive = hash & 0x7fffffff;
                return (positive % 720) / 57.29578f;
            }
        }

        private static void AddSway(
            GameObject spriteObject,
            Vector2 positionAmplitude,
            float positionFrequency,
            float rotationAmplitude,
            float rotationFrequency,
            float scaleAmplitude,
            float scaleFrequency,
            float phaseOffset)
        {
            if (spriteObject == null)
            {
                return;
            }

            var sway = spriteObject.AddComponent<SpriteSwayMotion2D>();
            sway.Configure(
                positionAmplitude,
                positionFrequency,
                rotationAmplitude,
                rotationFrequency,
                scaleAmplitude,
                scaleFrequency,
                phaseOffset,
                false);
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
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (_spritesByName.TryGetValue(candidate, out var sprite) && sprite != null)
                {
                    return sprite;
                }

                if (!candidate.EndsWith("_v01", StringComparison.OrdinalIgnoreCase))
                {
                    var versionedCandidate = $"{candidate}_v01";
                    if (_spritesByName.TryGetValue(versionedCandidate, out sprite) && sprite != null)
                    {
                        return sprite;
                    }
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
