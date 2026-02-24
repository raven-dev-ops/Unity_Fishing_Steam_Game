using System;
using NUnit.Framework;
using RavenDevOps.Fishing.Save;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class DayCounterServiceTests
    {
        [Test]
        public void ComputeDayNumber_IsoDate_UsesInclusiveCounting()
        {
            var now = new DateTime(2026, 2, 24, 12, 0, 0, DateTimeKind.Local);
            var day = DayCounterService.ComputeDayNumber("2026-02-20", now);
            Assert.That(day, Is.EqualTo(5));
        }

        [Test]
        public void ComputeDayNumber_InvalidDate_ReturnsOne()
        {
            var now = new DateTime(2026, 2, 24, 12, 0, 0, DateTimeKind.Local);
            var day = DayCounterService.ComputeDayNumber("not-a-date", now);
            Assert.That(day, Is.EqualTo(1));
        }

        [Test]
        public void ComputeDayNumber_FutureCareerStart_ClampsToOne()
        {
            var now = new DateTime(2026, 2, 24, 12, 0, 0, DateTimeKind.Local);
            var day = DayCounterService.ComputeDayNumber("2030-01-01", now);
            Assert.That(day, Is.EqualTo(1));
        }

        [Test]
        public void ComputeDayNumber_IsoTimestampInput_IsAccepted()
        {
            var now = new DateTime(2026, 2, 24, 12, 0, 0, DateTimeKind.Local);
            var day = DayCounterService.ComputeDayNumber("2026-02-20T05:00:00", now);
            Assert.That(day, Is.EqualTo(5));
        }
    }
}
