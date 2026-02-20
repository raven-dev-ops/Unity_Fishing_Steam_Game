using System;

namespace RavenDevOps.Fishing.Save
{
    public static class DayCounterService
    {
        public static int ComputeDayNumber(string careerStartLocalDate)
        {
            if (!DateTime.TryParse(careerStartLocalDate, out var startDate))
            {
                return 1;
            }

            var days = (DateTime.Now.Date - startDate.Date).Days;
            return Math.Max(1, days + 1);
        }
    }
}
