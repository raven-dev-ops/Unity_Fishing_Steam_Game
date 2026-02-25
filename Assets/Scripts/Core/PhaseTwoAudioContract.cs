using System;

namespace RavenDevOps.Fishing.Core
{
    public static class PhaseTwoAudioContract
    {
        public const string ResourcesAudioPath = "Pilot/Audio";
        public const string GeneratedFallbackUserDataMarker = "RAVEN_GENERATED_PHASE_TWO_FALLBACK";

        public static readonly string[] RequiredAudioKeys =
        {
            "menu_music_loop",
            "harbor_music_loop",
            "fishing_music_loop",
            "sfx_ui_navigate",
            "sfx_ui_select",
            "sfx_ui_cancel",
            "sfx_cast",
            "sfx_hooked",
            "sfx_catch",
            "sfx_sell",
            "sfx_purchase",
            "sfx_depart",
            "sfx_return"
        };

        public static bool IsRequiredKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            for (var i = 0; i < RequiredAudioKeys.Length; i++)
            {
                if (string.Equals(RequiredAudioKeys[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
