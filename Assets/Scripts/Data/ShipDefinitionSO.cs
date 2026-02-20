using UnityEngine;

namespace RavenDevOps.Fishing.Data
{
    [CreateAssetMenu(menuName = "Raven/Ship Definition", fileName = "SO_ShipDefinition")]
    public sealed class ShipDefinitionSO : ScriptableObject
    {
        public string id;
        public Sprite icon;
        public int price = 100;
        public int maxDistanceTier = 1;
        public float moveSpeed = 6f;
    }
}
