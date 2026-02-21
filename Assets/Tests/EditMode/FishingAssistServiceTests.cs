using NUnit.Framework;
using RavenDevOps.Fishing.Fishing;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class FishingAssistServiceTests
    {
        [Test]
        public void DefaultSettings_MatchLaunchTunedAssistValues()
        {
            var settings = new FishingAssistSettings();

            Assert.That(settings.EnableNoBitePity, Is.True);
            Assert.That(settings.NoBitePityThresholdCasts, Is.EqualTo(2));
            Assert.That(settings.PityBiteDelayScale, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(settings.EnableAdaptiveHookWindow, Is.True);
            Assert.That(settings.AdaptiveFailureThreshold, Is.EqualTo(2));
            Assert.That(settings.AdaptiveHookWindowBonusSeconds, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(settings.AssistCooldownCatches, Is.EqualTo(2));
        }

        [Test]
        public void TryActivateNoBitePity_ActivatesAtConfiguredStreak()
        {
            var service = new FishingAssistService();
            service.Configure(new FishingAssistSettings
            {
                EnableNoBitePity = true,
                NoBitePityThresholdCasts = 3
            });

            Assert.That(service.TryActivateNoBitePity(), Is.False);
            service.RecordCastResult(biteOccurred: false);
            Assert.That(service.TryActivateNoBitePity(), Is.False);
            service.RecordCastResult(biteOccurred: false);
            Assert.That(service.TryActivateNoBitePity(), Is.True);
        }

        [Test]
        public void ResolveHookWindow_ActivatesAfterFailureThreshold()
        {
            var service = new FishingAssistService();
            service.Configure(new FishingAssistSettings
            {
                EnableAdaptiveHookWindow = true,
                AdaptiveFailureThreshold = 2,
                AdaptiveHookWindowBonusSeconds = 0.4f
            });

            service.RecordCatchOutcome(success: false);
            service.RecordCatchOutcome(success: false);

            var resolved = service.ResolveHookWindow(1.2f, out var activated);

            Assert.That(activated, Is.True);
            Assert.That(resolved, Is.EqualTo(1.6f).Within(0.001f));
        }

        [Test]
        public void ResolveHookWindow_ClampsBonusToGuardrail()
        {
            var service = new FishingAssistService();
            service.Configure(new FishingAssistSettings
            {
                EnableAdaptiveHookWindow = true,
                AdaptiveFailureThreshold = 1,
                AdaptiveHookWindowBonusSeconds = 2.5f
            });

            service.RecordCatchOutcome(success: false);
            var resolved = service.ResolveHookWindow(1.0f, out var activated);

            Assert.That(activated, Is.True);
            Assert.That(resolved, Is.EqualTo(1.75f).Within(0.001f));
        }

        [Test]
        public void AssistCooldown_BlocksActivationUntilSuccessfulCatchesConsumeCooldown()
        {
            var service = new FishingAssistService();
            service.Configure(new FishingAssistSettings
            {
                EnableNoBitePity = true,
                NoBitePityThresholdCasts = 1,
                AssistCooldownCatches = 2
            });

            Assert.That(service.TryActivateNoBitePity(), Is.True);
            service.RecordCatchOutcome(success: false);

            Assert.That(service.TryActivateNoBitePity(), Is.False);

            service.RecordCatchOutcome(success: true);
            Assert.That(service.TryActivateNoBitePity(), Is.False);

            service.RecordCatchOutcome(success: true);
            Assert.That(service.TryActivateNoBitePity(), Is.True);
        }
    }
}
