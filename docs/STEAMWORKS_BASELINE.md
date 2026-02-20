# Steamworks Runtime Baseline

## Runtime Script
- `Assets/Scripts/Steam/SteamBootstrap.cs`

## Behavior
- Uses `STEAMWORKS_NET` symbol to compile Steamworks.NET calls.
- Performs guarded init on boot:
  - `Packsize.Test()`
  - `DllCheck.Test()`
  - `SteamAPI.Init()`
- Runs `SteamAPI.RunCallbacks()` every frame while initialized.
- Shuts down cleanly (`SteamAPI.Shutdown()`) on destroy/quit.
- Prevents duplicate initialization across scene transitions.

## Fallback Mode
Fallback is explicit and non-crashing when:
- `STEAMWORKS_NET` is not defined
- Steamworks DLL checks fail
- `SteamAPI.Init()` fails (for example launching outside Steam)

Runtime state can be inspected:
- `SteamBootstrap.IsSteamInitialized`
- `SteamBootstrap.LastFallbackReason`

## App ID Notes
- Inspector `_steamAppId` defaults to `480` for local/dev validation.
- Runtime log includes requested app ID and runtime Steam app ID after init.
- Production release should use the real app ID and Steam-launched execution path.

## Validation Steps
1. Install Steamworks.NET and define `STEAMWORKS_NET`.
2. Launch build through Steam client.
3. Verify init log and overlay (`Shift+Tab`).
4. Close game and verify no repeated init/shutdown errors.
5. Launch outside Steam and verify fallback warning is explicit and gameplay remains stable.
