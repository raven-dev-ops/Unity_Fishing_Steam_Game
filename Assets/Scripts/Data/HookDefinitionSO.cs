using UnityEngine;

namespace RavenDevOps.Fishing.Data
{
    [CreateAssetMenu(menuName = "Raven/Hook Definition", fileName = "SO_HookDefinition")]
    public sealed class HookDefinitionSO : ScriptableObject
    {
        public string id;
        public Sprite icon;
        public int price = 100;
        public float maxDepth = 8f;
    }
}
