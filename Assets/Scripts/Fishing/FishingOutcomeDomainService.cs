using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    public interface IFishingRandomSource
    {
        float Range(float minInclusive, float maxInclusive);
    }

    public sealed class UnityFishingRandomSource : IFishingRandomSource
    {
        public float Range(float minInclusive, float maxInclusive)
        {
            return Random.Range(minInclusive, maxInclusive);
        }
    }

    public readonly struct FishingRewardResult
    {
        public FishingRewardResult(float weightKg, int valueCopecs)
        {
            WeightKg = Mathf.Max(0.1f, weightKg);
            ValueCopecs = Mathf.Max(1, valueCopecs);
        }

        public float WeightKg { get; }
        public int ValueCopecs { get; }
    }

    public sealed class FishingOutcomeDomainService
    {
        public FishingFailReason ResolveHookWindowFailure(bool hasHookedFish, float elapsedSeconds, float reactionWindowSeconds)
        {
            if (!hasHookedFish)
            {
                return FishingFailReason.MissedHook;
            }

            return elapsedSeconds >= Mathf.Max(0.2f, reactionWindowSeconds)
                ? FishingFailReason.MissedHook
                : FishingFailReason.None;
        }

        public FishingRewardResult BuildCatchReward(FishDefinition fish, IFishingRandomSource randomSource)
        {
            if (fish == null)
            {
                return new FishingRewardResult(0.1f, 1);
            }

            var source = randomSource ?? new UnityFishingRandomSource();
            var minWeight = Mathf.Max(0.1f, fish.minCatchWeightKg);
            var maxWeight = Mathf.Max(minWeight, fish.maxCatchWeightKg);
            var weight = source.Range(minWeight, maxWeight);

            var valueVariance = source.Range(0.9f, 1.3f);
            var value = Mathf.RoundToInt(Mathf.Max(1, fish.baseValue) * valueVariance);
            return new FishingRewardResult(weight, value);
        }

        public string BuildFailureReasonText(FishingFailReason failReason)
        {
            switch (failReason)
            {
                case FishingFailReason.MissedHook:
                    return "Missed hook: the fish slipped away before you started reeling.";
                case FishingFailReason.LineSnap:
                    return "Line snapped: tension stayed too high.";
                case FishingFailReason.FishEscaped:
                    return "Fish escaped: it unhooked and swam away before you reeled it in.";
                default:
                    return "Catch failed.";
            }
        }
    }
}
