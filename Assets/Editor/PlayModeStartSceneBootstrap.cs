using UnityEditor;
using UnityEditor.SceneManagement;

namespace RavenDevOps.Fishing.Editor
{
    [InitializeOnLoad]
    internal static class PlayModeStartSceneBootstrap
    {
        private const string BootScenePath = "Assets/Scenes/00_Boot.unity";

        static PlayModeStartSceneBootstrap()
        {
            EditorApplication.delayCall += EnsurePlayModeStartScene;
        }

        private static void EnsurePlayModeStartScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (IsRunningTestsFromCommandLine())
            {
                if (EditorSceneManager.playModeStartScene != null)
                {
                    EditorSceneManager.playModeStartScene = null;
                }

                return;
            }

            var bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (bootScene == null)
            {
                return;
            }

            if (EditorSceneManager.playModeStartScene == bootScene)
            {
                return;
            }

            EditorSceneManager.playModeStartScene = bootScene;
        }

        private static bool IsRunningTestsFromCommandLine()
        {
            var args = System.Environment.GetCommandLineArgs();
            if (args == null || args.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }

                if (string.Equals(arg, "-runTests", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
