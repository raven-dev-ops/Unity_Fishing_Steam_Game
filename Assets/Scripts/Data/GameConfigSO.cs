using UnityEngine;

namespace RavenDevOps.Fishing.Data
{
    [CreateAssetMenu(menuName = "Raven/Game Config", fileName = "SO_GameConfig")]
    public sealed class GameConfigSO : ScriptableObject
    {
        public FishDefinitionSO[] fishDefinitions;
        public ShipDefinitionSO[] shipDefinitions;
        public HookDefinitionSO[] hookDefinitions;
    }
}
