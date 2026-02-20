using NUnit.Framework;
using RavenDevOps.Fishing.Tools;
using RavenDevOps.Fishing.UI;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class ModDiagnosticsTextFormatterTests
    {
        [Test]
        public void BuildRejectedModsText_ListsDirectoryAndReason()
        {
            var result = new ModRuntimeCatalogLoadResult();
            result.rejectedMods.Add(new ModRejectedPackInfo
            {
                directoryPath = "Mods/BrokenPack",
                reason = "Missing manifest.json."
            });

            var text = ModDiagnosticsTextFormatter.BuildRejectedModsText(result);

            Assert.That(text, Does.Contain("Mods/BrokenPack"));
            Assert.That(text, Does.Contain("Missing manifest.json."));
        }

        [Test]
        public void BuildMessagesText_FiltersInfoMessages_WhenDisabled()
        {
            var result = new ModRuntimeCatalogLoadResult();
            result.messages.Add("INFO: Startup.");
            result.messages.Add("WARN: Rejected mod pack 'BrokenPack'.");

            var text = ModDiagnosticsTextFormatter.BuildMessagesText(result, includeInfoMessages: false, maxLines: 8);

            Assert.That(text, Does.Not.Contain("INFO: Startup."));
            Assert.That(text, Does.Contain("WARN: Rejected mod pack"));
        }

        [Test]
        public void BuildSafeModeStatus_UsesActiveReason()
        {
            var text = ModDiagnosticsTextFormatter.BuildSafeModeStatus(
                safeModePreferenceEnabled: true,
                safeModeActive: true,
                safeModeReason: "env:RAVEN_DISABLE_MODS");

            Assert.That(text, Does.Contain("env:RAVEN_DISABLE_MODS"));
            Assert.That(text, Does.Contain("active"));
        }
    }
}
