using NUnit.Framework;
using RavenDevOps.Fishing.Performance;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class PerfSanityRunnerTests
    {
        [Test]
        public void ResolvePercentileFrameMsNoAlloc_ResolvesExpectedP95()
        {
            var frameDurations = new[] { 0.016f, 0.020f, 0.012f, 0.030f, 0.018f };
            var scratch = new float[frameDurations.Length];

            var p95 = PerfSanityRunner.ResolvePercentileFrameMsNoAlloc(frameDurations, scratch, frameDurations.Length, 0.95f);

            Assert.That(p95, Is.EqualTo(30f).Within(0.001f));
        }

        [Test]
        public void ResolvePercentileFrameMsNoAlloc_ClampsSampleCountToAvailableBuffers()
        {
            var frameDurations = new[] { 0.010f, 0.015f, 0.020f };
            var scratch = new float[2];

            var medianMs = PerfSanityRunner.ResolvePercentileFrameMsNoAlloc(frameDurations, scratch, sampleCount: 3, percentile: 0.5f);

            Assert.That(medianMs, Is.EqualTo(15f).Within(0.001f));
        }

        [Test]
        public void ResolvePercentileFrameMsNoAlloc_DoesNotAllocateInSteadyState()
        {
            var frameDurations = new float[256];
            var scratch = new float[256];
            for (var i = 0; i < frameDurations.Length; i++)
            {
                frameDurations[i] = 0.010f + (i * 0.0001f);
            }

            for (var i = 0; i < 16; i++)
            {
                PerfSanityRunner.ResolvePercentileFrameMsNoAlloc(frameDurations, scratch, frameDurations.Length, 0.95f);
            }

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            var before = System.GC.GetTotalMemory(true);
            for (var i = 0; i < 512; i++)
            {
                PerfSanityRunner.ResolvePercentileFrameMsNoAlloc(frameDurations, scratch, frameDurations.Length, 0.95f);
            }

            System.GC.Collect();
            var after = System.GC.GetTotalMemory(true);
            Assert.That(after - before, Is.LessThanOrEqualTo(1024L));
        }
    }
}
