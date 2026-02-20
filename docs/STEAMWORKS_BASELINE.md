# Steamworks Baseline Integration

## Runtime Hook
- Steam bootstrap component: `Assets/Scripts/Steam/SteamBootstrap.cs`
- Uses compile symbol `STEAMWORKS_NET` when Steamworks.NET package is installed.

## App ID Handling
- Dev fallback App ID is configurable (`_steamAppId`, default 480).
- Production build should use real app ID and release config.

## Overlay Validation
- Launch via Steam client.
- Verify Shift+Tab overlay opens in build.
