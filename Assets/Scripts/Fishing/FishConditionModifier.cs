namespace RavenDevOps.Fishing.Fishing
{
    public struct FishConditionModifier
    {
        public float rarityWeightMultiplier;
        public float biteDelayMultiplier;
        public float fightStaminaMultiplier;
        public float pullIntensityMultiplier;
        public float escapeSecondsMultiplier;

        public static FishConditionModifier Identity => new FishConditionModifier
        {
            rarityWeightMultiplier = 1f,
            biteDelayMultiplier = 1f,
            fightStaminaMultiplier = 1f,
            pullIntensityMultiplier = 1f,
            escapeSecondsMultiplier = 1f
        };
    }
}
