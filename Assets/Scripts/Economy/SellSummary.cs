using System;
using System.Collections.Generic;

namespace RavenDevOps.Fishing.Economy
{
    [Serializable]
    public sealed class SellSummaryLine
    {
        public string fishId;
        public int distanceTier;
        public int count;
        public int unitEarned;
        public int totalEarned;
    }

    [Serializable]
    public sealed class SellSummary
    {
        public int itemCount;
        public int totalEarned;
        public List<SellSummaryLine> lines = new List<SellSummaryLine>();
    }
}
