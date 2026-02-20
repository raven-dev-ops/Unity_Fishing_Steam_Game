# Steamworks Baseline Integration

## Current Implementation
- Bootstrap script: `Assets/Scripts/Steam/SteamBootstrap.cs`
- Uses compile symbol `STEAMWORKS_NET` to gate Steamworks-specific startup.
- When symbol is absent, app runs in non-Steam fallback mode with diagnostic logs.

## App ID Handling
- Default dev App ID is configurable in inspector (`_steamAppId`, default `480`).
- Production builds must use the real App ID and Steam launch path.

## Baseline Validation
1. Install Steamworks.NET package in project.
2. Define `STEAMWORKS_NET` scripting symbol.
3. Launch via Steam client.
4. Verify overlay (`Shift+Tab`) and startup logs.

## Notes
- This is a baseline integration layer, not a full achievement/stats/cloud implementation.
