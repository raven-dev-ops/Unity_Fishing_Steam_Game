# Steamworks Runtime Baseline

## Runtime Script
- `Assets/Scripts/Steam/SteamBootstrap.cs`
- `Assets/Scripts/Steam/SteamStatsService.cs`
- `Assets/Scripts/Steam/SteamCloudSyncService.cs`
- `Assets/Scripts/Steam/SteamRichPresenceService.cs` (optional)

## Behavior
- Uses `STEAMWORKS_NET` symbol to compile Steamworks.NET calls.
- Performs guarded init on boot:
  - optional `SteamAPI.RestartAppIfNecessary(appId)` relaunch guard (config-gated)
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
- Relaunch guard controls:
  - `_enforceSteamRelaunch` (default `false`)
  - `_allowRelaunchInDevelopmentBuild` (default `false`)
- Runtime log includes requested app ID and runtime Steam app ID after init.
- Production release should use the real app ID and Steam-launched execution path.
- Editor path always skips relaunch guard.
- Release app/depot mapping contract is versioned in:
  - `release/steamworks/achievements_stats/backend_contract.json`
- Release workflow consumes mapping from protected secrets:
  - `STEAM_APP_ID`
  - `STEAM_DEPOT_WINDOWS_ID`
- Contract verification script (runtime keys + release mapping + publish metadata completeness):
  - `scripts/ci/verify-steamworks-achievements-stats.ps1`
- `backend_publish` contract fields must use:
  - numeric `steamworks_change_number`
  - UTC timestamp format `yyyy-MM-ddTHH:mm:ssZ`
  - repo-relative `verification_artifacts` paths that resolve to committed files

## Validation Steps
1. Install Steamworks.NET and define `STEAMWORKS_NET`.
2. Run contract verification before partner publish:
   - `powershell -ExecutionPolicy Bypass -File scripts/ci/verify-steamworks-achievements-stats.ps1`
3. Launch build through Steam client.
4. Verify relaunch guard log path and init log/overlay (`Shift+Tab`).
5. Trigger catch/purchase/trip events and verify Steam stats sync path is active.
6. Validate save cloud sync pull/push path if Steam Cloud is enabled for app/depot.
7. Verify rich presence state transitions (menu/harbor/fishing/pause) if enabled.
8. Close game and verify no repeated init/shutdown errors.
9. Launch outside Steam:
   - with relaunch guard enabled in production settings: app should request restart via Steam.
   - with relaunch guard disabled (or dev build without override): fallback warning remains explicit and non-crashing.
10. After Steamworks backend publish, update `backend_contract.json` publish metadata and rerun verification with strict gate:
   - `powershell -ExecutionPolicy Bypass -File scripts/ci/verify-steamworks-achievements-stats.ps1 -RequirePublishedMetadata -SummaryJsonPath "Artifacts/Steamworks/steamworks_achievements_stats_contract_summary.json" -SummaryMarkdownPath "Artifacts/Steamworks/steamworks_achievements_stats_contract_summary.md"`
11. Confirm release workflow strict gate passes:
   - `.github/workflows/release-steampipe.yml` -> `steam_release_metadata_gates`
12. Optional one-command external artifact intake helper:
   - `docs/STEAM_EXTERNAL_EVIDENCE_INTAKE.md`
