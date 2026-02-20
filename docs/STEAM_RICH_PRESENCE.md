# Steam Rich Presence (Optional)

## Runtime Component
- `Assets/Scripts/Steam/SteamRichPresenceService.cs`

## Dependencies
- `SteamBootstrap` initialization (`STEAMWORKS_NET` path)
- `GameFlowManager` state transitions
- `UserSettingsService` toggle control

## Presence Fields
Set through Steam Friends rich presence:
- `status`:
  - `Browsing menus`
  - `At harbor`
  - `Fishing at sea`
  - `Paused`
  - `Watching intro`
- `details`:
  - `Level <n> | <copecs> copecs`

## Rate Limiting
- Updates are cooldown-gated (`_updateCooldownSeconds`) to prevent API spam on rapid transitions.

## Feature Toggle
- Setting key: `settings.steamRichPresenceEnabled`
- Runtime path: `UserSettingsService.SteamRichPresenceEnabled`
- Disabling clears active rich presence keys without affecting gameplay.

## Test Flow
1. Launch through Steam and move through main menu/harbor/fishing/pause.
2. Confirm state text updates in Steam profile/friends.
3. Rapidly change states and verify updates remain throttled.
4. Disable rich presence in settings and verify keys clear with no runtime errors.
