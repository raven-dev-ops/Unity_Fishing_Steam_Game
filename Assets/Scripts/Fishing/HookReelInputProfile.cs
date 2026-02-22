namespace RavenDevOps.Fishing.Fishing
{
    internal enum HookReelInputMode
    {
        Legacy = 0,
        Level1Tap = 1,
        Level2Hold = 2,
        Level3Auto = 3
    }

    internal static class HookReelInputProfile
    {
        public static HookReelInputMode Resolve(string equippedHookId)
        {
            var normalizedId = string.IsNullOrWhiteSpace(equippedHookId)
                ? string.Empty
                : equippedHookId.Trim().ToLowerInvariant();

            if (normalizedId.Contains("hook_lv3"))
            {
                return HookReelInputMode.Level3Auto;
            }

            if (normalizedId.Contains("hook_lv2"))
            {
                return HookReelInputMode.Level2Hold;
            }

            if (normalizedId.Contains("hook_lv1"))
            {
                return HookReelInputMode.Level1Tap;
            }

            return HookReelInputMode.Legacy;
        }
    }
}
