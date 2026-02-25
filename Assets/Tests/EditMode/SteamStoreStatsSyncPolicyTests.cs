using NUnit.Framework;
using RavenDevOps.Fishing.Steam;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class SteamStoreStatsSyncPolicyTests
    {
        [Test]
        public void Success_Path_EnforcesMinimumStoreInterval()
        {
            var policy = new SteamStoreStatsSyncPolicy(
                minimumIntervalSeconds: 15f,
                failureInitialBackoffSeconds: 2f,
                failureMaxBackoffSeconds: 30f,
                failureBackoffMultiplier: 2f);

            policy.MarkPending();
            Assert.That(policy.GetGateReason(now: 0f), Is.EqualTo(SteamStoreStatsGateReason.None));

            policy.RecordStoreAttempt(now: 0f, success: true);
            Assert.That(policy.HasPendingSync, Is.False);
            Assert.That(policy.StoreAttemptCount, Is.EqualTo(1));
            Assert.That(policy.StoreSuccessCount, Is.EqualTo(1));
            Assert.That(policy.StoreFailureCount, Is.EqualTo(0));

            policy.MarkPending();
            Assert.That(policy.GetGateReason(now: 5f), Is.EqualTo(SteamStoreStatsGateReason.MinimumInterval));
            Assert.That(policy.GetGateReason(now: 15f), Is.EqualTo(SteamStoreStatsGateReason.None));
        }

        [Test]
        public void Failure_Path_AppliesExponentialBackoffWithCap()
        {
            var policy = new SteamStoreStatsSyncPolicy(
                minimumIntervalSeconds: 10f,
                failureInitialBackoffSeconds: 2f,
                failureMaxBackoffSeconds: 5f,
                failureBackoffMultiplier: 2f);

            policy.MarkPending();
            policy.RecordStoreAttempt(now: 1f, success: false);
            Assert.That(policy.HasPendingSync, Is.True);
            Assert.That(policy.GetGateReason(now: 2f), Is.EqualTo(SteamStoreStatsGateReason.Backoff));
            Assert.That(policy.GetGateReason(now: 3f), Is.EqualTo(SteamStoreStatsGateReason.None));
            Assert.That(policy.ActiveBackoffSeconds, Is.EqualTo(2f).Within(0.0001f));

            policy.RecordStoreAttempt(now: 3f, success: false);
            Assert.That(policy.ActiveBackoffSeconds, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(policy.GetGateReason(now: 6f), Is.EqualTo(SteamStoreStatsGateReason.Backoff));
            Assert.That(policy.GetGateReason(now: 7f), Is.EqualTo(SteamStoreStatsGateReason.None));

            policy.RecordStoreAttempt(now: 7f, success: false);
            Assert.That(policy.ActiveBackoffSeconds, Is.EqualTo(5f).Within(0.0001f), "Backoff should clamp to configured max.");
            Assert.That(policy.StoreAttemptCount, Is.EqualTo(3));
            Assert.That(policy.StoreFailureCount, Is.EqualTo(3));
            Assert.That(policy.StoreSuccessCount, Is.EqualTo(0));
        }

        [Test]
        public void CallbackFailure_RestoresPendingSyncAndBackoff()
        {
            var policy = new SteamStoreStatsSyncPolicy(
                minimumIntervalSeconds: 10f,
                failureInitialBackoffSeconds: 1.5f,
                failureMaxBackoffSeconds: 8f,
                failureBackoffMultiplier: 2f);

            policy.MarkPending();
            policy.RecordStoreAttempt(now: 2f, success: true);
            Assert.That(policy.HasPendingSync, Is.False);

            policy.RecordStoreCallbackFailure(now: 3f);
            Assert.That(policy.HasPendingSync, Is.True);
            Assert.That(policy.StoreFailureCount, Is.EqualTo(1));
            Assert.That(policy.GetGateReason(now: 4f), Is.EqualTo(SteamStoreStatsGateReason.Backoff));
            Assert.That(policy.GetGateReason(now: 4.5f), Is.EqualTo(SteamStoreStatsGateReason.None));
        }

        [Test]
        public void ThrottledWrites_AreTracked()
        {
            var policy = new SteamStoreStatsSyncPolicy(
                minimumIntervalSeconds: 10f,
                failureInitialBackoffSeconds: 1f,
                failureMaxBackoffSeconds: 8f,
                failureBackoffMultiplier: 2f);

            policy.MarkPending();
            policy.RecordStoreAttempt(now: 0f, success: true);
            policy.MarkPending();

            Assert.That(policy.GetGateReason(now: 1f), Is.EqualTo(SteamStoreStatsGateReason.MinimumInterval));
            policy.RecordThrottledWrite();
            Assert.That(policy.StoreThrottledCount, Is.EqualTo(1));
        }
    }
}
