using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    [CreateAssetMenu(menuName = "Raven/Tutorial Sprite Library", fileName = "SO_TutorialSpriteLibrary")]
    public sealed class TutorialSpriteLibrary : ScriptableObject
    {
        [SerializeField] private Sprite _harborShipSprite;
        [SerializeField] private Sprite _fishingShipSprite;
        [SerializeField] private Sprite _hookSprite;
        [SerializeField] private Sprite _fishSprite;

        public Sprite HarborShipSprite => _harborShipSprite;
        public Sprite FishingShipSprite => _fishingShipSprite;
        public Sprite HookSprite => _hookSprite;
        public Sprite FishSprite => _fishSprite;
    }
}
