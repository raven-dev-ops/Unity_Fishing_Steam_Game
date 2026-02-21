using RavenDevOps.Fishing.Fishing;

namespace RavenDevOps.Fishing.Core
{
    public interface IFishingHudOverlay
    {
        void SetFishingTelemetry(int distanceTier, float depth);
        void SetFishingTension(float normalizedTension, FishingTensionState tensionState);
        void SetFishingStatus(string status);
        void SetFishingFailure(string failure);
        void SetFishingConditions(string conditionLabel);
    }
}
