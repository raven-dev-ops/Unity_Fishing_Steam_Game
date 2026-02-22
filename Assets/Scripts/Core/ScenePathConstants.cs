namespace RavenDevOps.Fishing.Core
{
    public static class ScenePathConstants
    {
        public const string Boot = "Assets/Scenes/00_Boot.unity";
        public const string Cinematic = "Assets/Scenes/01_Cinematic.unity";
        public const string MainMenu = "Assets/Scenes/02_MainMenu.unity";
        public const string Harbor = "Assets/Scenes/03_Harbor.unity";
        public const string Fishing = "Assets/Scenes/04_Fishing.unity";

        public static bool IsSceneBackedState(GameFlowState state)
        {
            switch (state)
            {
                case GameFlowState.Cinematic:
                case GameFlowState.MainMenu:
                case GameFlowState.Harbor:
                case GameFlowState.Fishing:
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryGetScenePathForState(GameFlowState state, out string scenePath)
        {
            switch (state)
            {
                case GameFlowState.Cinematic:
                    scenePath = Cinematic;
                    return true;
                case GameFlowState.MainMenu:
                    scenePath = MainMenu;
                    return true;
                case GameFlowState.Harbor:
                    scenePath = Harbor;
                    return true;
                case GameFlowState.Fishing:
                    scenePath = Fishing;
                    return true;
                default:
                    scenePath = null;
                    return false;
            }
        }

        public static string GetScenePathForState(GameFlowState state)
        {
            TryGetScenePathForState(state, out var scenePath);
            return scenePath;
        }

        public static bool TryGetStateForScenePath(string scenePath, out GameFlowState state)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                state = GameFlowState.None;
                return false;
            }

            switch (scenePath)
            {
                case Cinematic:
                    state = GameFlowState.Cinematic;
                    return true;
                case MainMenu:
                    state = GameFlowState.MainMenu;
                    return true;
                case Harbor:
                    state = GameFlowState.Harbor;
                    return true;
                case Fishing:
                    state = GameFlowState.Fishing;
                    return true;
                default:
                    state = GameFlowState.None;
                    return false;
            }
        }

        public static GameFlowState GetStateForScenePath(string scenePath)
        {
            return TryGetStateForScenePath(scenePath, out var state)
                ? state
                : GameFlowState.None;
        }
    }
}
