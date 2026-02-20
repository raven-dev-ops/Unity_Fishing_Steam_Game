using System;
using NUnit.Framework;
using RavenDevOps.Fishing.Core;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class ModRuntimeCatalogServiceTests
    {
        [Test]
        public void EvaluateSafeModeRequest_UsesPersistedPreference_WhenNoEnvOrArgs()
        {
            var isSafeMode = ModRuntimeCatalogService.EvaluateSafeModeRequest(
                persistedSafeModeEnabled: true,
                envDisableModsValue: string.Empty,
                commandLineArgs: Array.Empty<string>(),
                out var reason);

            Assert.That(isSafeMode, Is.True);
            Assert.That(reason, Does.Contain("playerprefs"));
        }

        [Test]
        public void EvaluateSafeModeRequest_PrioritizesEnvironmentFlag()
        {
            var isSafeMode = ModRuntimeCatalogService.EvaluateSafeModeRequest(
                persistedSafeModeEnabled: false,
                envDisableModsValue: "true",
                commandLineArgs: Array.Empty<string>(),
                out var reason);

            Assert.That(isSafeMode, Is.True);
            Assert.That(reason, Is.EqualTo("env:RAVEN_DISABLE_MODS"));
        }

        [Test]
        public void EvaluateSafeModeRequest_DetectsCommandLineFlag()
        {
            var isSafeMode = ModRuntimeCatalogService.EvaluateSafeModeRequest(
                persistedSafeModeEnabled: false,
                envDisableModsValue: string.Empty,
                commandLineArgs: new[] { "-batchmode", "-safeMode=1" },
                out var reason);

            Assert.That(isSafeMode, Is.True);
            Assert.That(reason, Is.EqualTo("command-line"));
        }

        [Test]
        public void EvaluateSafeModeRequest_ReturnsFalse_WhenNoSafeModeSource()
        {
            var isSafeMode = ModRuntimeCatalogService.EvaluateSafeModeRequest(
                persistedSafeModeEnabled: false,
                envDisableModsValue: string.Empty,
                commandLineArgs: Array.Empty<string>(),
                out var reason);

            Assert.That(isSafeMode, Is.False);
            Assert.That(reason, Is.EqualTo(string.Empty));
        }
    }
}
