using System;

namespace RavenDevOps.Fishing.Economy
{
    [Serializable]
    public sealed class ShopItem
    {
        public string id;
        public int price;
        public int valueTier;
    }
}
