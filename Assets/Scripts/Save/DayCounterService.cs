using System;

namespace RavenDevOps.Fishing.Save
{
    public static class DayCounterService
    {
        public static int ComputeDayNumber(string careerStartLocalDate)
        {
            return ComputeDayNumber(careerStartLocalDate, DateTime.Now);
        }

        public static int ComputeDayNumber(string careerStartLocalDate, DateTime localNow)
        {
            if (!DateTimeUtility.TryParseLocalDate(careerStartLocalDate, out var startDate))
            {
                return 1;
            }

            var days = (localNow.Date - startDate.Date).Days;
            return Math.Max(1, days + 1);
        }
    }
}
