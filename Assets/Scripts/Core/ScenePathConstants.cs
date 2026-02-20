namespace RavenDevOps.Fishing.Core
{
    public static class ScenePathConstants
    {
        public const string Boot = "Assets/Scenes/00_Boot.unity";
        public const string Cinematic = "Assets/Scenes/01_Cinematic.unity";
        public const string MainMenu = "Assets/Scenes/02_MainMenu.unity";
        public const string Harbor = "Assets/Scenes/03_Harbor.unity";
        public const string Fishing = "Assets/Scenes/04_Fishing.unity";

        public static string GetScenePathForState(GameFlowState state)
        {
            switch (state)
            {
                case GameFlowState.Cinematic:
                    return Cinematic;
                case GameFlowState.MainMenu:
                    return MainMenu;
                case GameFlowState.Harbor:
                    return Harbor;
                case GameFlowState.Fishing:
                    return Fishing;
                default:
                    return null;
            }
        }
    }
}
