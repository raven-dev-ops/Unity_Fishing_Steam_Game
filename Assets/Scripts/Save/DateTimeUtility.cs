using System;
using System.Globalization;

namespace RavenDevOps.Fishing.Save
{
    public static class DateTimeUtility
    {
        public const string LocalDateFormat = "yyyy-MM-dd";

        public static string ToLocalDateString(DateTime localNow)
        {
            return localNow.ToString(LocalDateFormat, CultureInfo.InvariantCulture);
        }

        public static bool TryParseLocalDate(string rawValue, out DateTime localDate)
        {
            localDate = default;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            if (DateTime.TryParseExact(
                    rawValue,
                    LocalDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsedExact))
            {
                localDate = parsedExact.Date;
                return true;
            }

            if (DateTime.TryParse(
                    rawValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsedInvariant))
            {
                localDate = parsedInvariant.Date;
                return true;
            }

            if (DateTime.TryParse(
                    rawValue,
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsedCurrentCulture))
            {
                localDate = parsedCurrentCulture.Date;
                return true;
            }

            return false;
        }

        public static bool TryParseUtcTimestampToLocal(string timestampUtc, out DateTime localDateTime)
        {
            localDateTime = default;
            if (string.IsNullOrWhiteSpace(timestampUtc))
            {
                return false;
            }

            const DateTimeStyles utcStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
            if (DateTime.TryParseExact(
                    timestampUtc,
                    "O",
                    CultureInfo.InvariantCulture,
                    utcStyles,
                    out var parsedExact))
            {
                localDateTime = parsedExact.ToLocalTime();
                return true;
            }

            if (DateTime.TryParse(
                    timestampUtc,
                    CultureInfo.InvariantCulture,
                    utcStyles,
                    out var parsedInvariant))
            {
                localDateTime = parsedInvariant.ToLocalTime();
                return true;
            }

            if (DateTime.TryParse(
                    timestampUtc,
                    CultureInfo.CurrentCulture,
                    utcStyles,
                    out var parsedCurrentCulture))
            {
                localDateTime = parsedCurrentCulture.ToLocalTime();
                return true;
            }

            return false;
        }
    }
}
