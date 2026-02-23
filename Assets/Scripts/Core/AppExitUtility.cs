using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    public static class AppExitUtility
    {
        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
