using NUnit.Framework;
using RavenDevOps.Fishing.Save;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class SaveWriteThrottleTests
    {
        [Test]
        public void Request_DebouncesWithinInterval_AndFlushesWhenIntervalElapsed()
        {
            var throttle = new SaveWriteThrottle(1f);

            Assert.That(throttle.Request(0f, forceImmediate: false), Is.True);
            throttle.MarkPersisted(0f);

            Assert.That(throttle.Request(0.25f, forceImmediate: false), Is.False);
            Assert.That(throttle.HasPendingRequest, Is.True);
            Assert.That(throttle.TryFlush(0.75f), Is.False);
            Assert.That(throttle.TryFlush(1.05f), Is.True);
        }

        [Test]
        public void Request_ForceImmediate_BypassesDebounce()
        {
            var throttle = new SaveWriteThrottle(5f);
            throttle.MarkPersisted(0f);

            Assert.That(throttle.Request(0.5f, forceImmediate: true), Is.True);
        }

        [Test]
        public void MarkPending_RetriesOnNextEligibleFlush()
        {
            var throttle = new SaveWriteThrottle(1f);
            throttle.MarkPersisted(10f);
            throttle.MarkPending();

            Assert.That(throttle.TryFlush(10.4f), Is.False);
            Assert.That(throttle.TryFlush(11.1f), Is.True);
        }
    }
}
