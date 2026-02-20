using System;
using System.Text;
using RavenDevOps.Fishing.Tools;

namespace RavenDevOps.Fishing.UI
{
    public static class ModDiagnosticsTextFormatter
    {
        public static string BuildSummary(ModRuntimeCatalogLoadResult result, bool safeModeActive, string safeModeReason)
        {
            if (result == null)
            {
                return "Mods: unavailable";
            }

            var safeModeLabel = safeModeActive
                ? $"ON ({NormalizeLabel(safeModeReason, "unknown source")})"
                : "OFF";

            return $"Mods: enabled={result.modsEnabled}, safeMode={safeModeLabel}, accepted={result.acceptedMods.Count}, rejected={result.rejectedMods.Count}";
        }

        public static string BuildAcceptedModsText(ModRuntimeCatalogLoadResult result)
        {
            if (result == null || result.acceptedMods.Count == 0)
            {
                return "Accepted Mods: none";
            }

            var builder = new StringBuilder();
            builder.AppendLine("Accepted Mods");
            for (var i = 0; i < result.acceptedMods.Count; i++)
            {
                var pack = result.acceptedMods[i];
                var modId = NormalizeLabel(pack.modId, "unknown_mod");
                var modVersion = NormalizeLabel(pack.modVersion, "?.?.?");
                builder.AppendLine($"- {modId} {modVersion}");
            }

            return builder.ToString().TrimEnd();
        }

        public static string BuildRejectedModsText(ModRuntimeCatalogLoadResult result)
        {
            if (result == null || result.rejectedMods.Count == 0)
            {
                return "Rejected Mods: none";
            }

            var builder = new StringBuilder();
            builder.AppendLine("Rejected Mods");
            for (var i = 0; i < result.rejectedMods.Count; i++)
            {
                var pack = result.rejectedMods[i];
                var directory = NormalizeLabel(pack.directoryPath, "unknown_directory");
                var reason = NormalizeLabel(pack.reason, "unspecified reason");
                builder.AppendLine($"- {directory}: {reason}");
            }

            return builder.ToString().TrimEnd();
        }

        public static string BuildMessagesText(ModRuntimeCatalogLoadResult result, bool includeInfoMessages, int maxLines)
        {
            if (result == null || result.messages.Count == 0)
            {
                return "Mod Loader Messages: none";
            }

            var limit = Math.Max(1, maxLines);
            var filtered = new StringBuilder();
            filtered.AppendLine("Mod Loader Messages");

            var linesAdded = 0;
            for (var i = 0; i < result.messages.Count; i++)
            {
                var message = result.messages[i] ?? string.Empty;
                if (!includeInfoMessages && message.StartsWith("INFO:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                filtered.AppendLine($"- {message}");
                linesAdded++;
                if (linesAdded >= limit)
                {
                    break;
                }
            }

            if (linesAdded == 0)
            {
                return "Mod Loader Messages: none";
            }

            return filtered.ToString().TrimEnd();
        }

        public static string BuildSafeModeStatus(bool safeModePreferenceEnabled, bool safeModeActive, string safeModeReason)
        {
            if (safeModeActive)
            {
                var reason = NormalizeLabel(safeModeReason, "unknown source");
                return $"Safe Mode is active now ({reason}). Mods are disabled for this launch.";
            }

            if (safeModePreferenceEnabled)
            {
                return "Safe Mode is scheduled for next launch.";
            }

            return "Safe Mode is off.";
        }

        private static string NormalizeLabel(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
