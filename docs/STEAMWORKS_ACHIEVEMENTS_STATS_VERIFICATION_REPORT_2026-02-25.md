# Steamworks Achievements/Stats Verification Report (2026-02-25)

## Scope
Issue: `#226`  
Goal: verify runtime achievements/stats key/type parity and release app/depot mapping contract, and capture backend publish evidence state.

## Implemented
- Added versioned backend contract:
  - `release/steamworks/achievements_stats/backend_contract.json`
  - `release/steamworks/achievements_stats/README.md`
- Added deterministic validator script:
  - `scripts/ci/verify-steamworks-achievements-stats.ps1`
- Script verifies:
  - runtime key parity from `Assets/Scripts/Steam/SteamStatsService.cs` vs contract
  - stat type contract (`INT`)
  - default bootstrap app ID parity from `Assets/Scripts/Steam/SteamBootstrap.cs`
  - release workflow secret mapping references in `.github/workflows/release-steampipe.yml`
  - backend publish metadata completeness

## Validation Evidence
- Command:
  - `powershell -ExecutionPolicy Bypass -File scripts/ci/verify-steamworks-achievements-stats.ps1`
- Result:
  - `status=warning`
- Artifacts:
  - `Artifacts/Steamworks/steamworks_achievements_stats_contract_summary.json`
  - `Artifacts/Steamworks/steamworks_achievements_stats_contract_summary.md`
- Summary snapshot:
  - stat key parity: `missing=0`, `extra=0`
  - achievement key parity: `missing=0`, `extra=0`
  - stat type violations: `0`
  - bootstrap app id parity: `contract=480`, `runtime=480`
  - release workflow secret mapping: `STEAM_APP_ID=true`, `STEAM_DEPOT_WINDOWS_ID=true`

## Publish Evidence Status
- Backend publish metadata in `backend_contract.json` is currently incomplete:
  - `steamworks_change_number`
  - `published_at_utc`
  - `verified_by`
  - `verification_artifacts`
- Current state is therefore **not ready for strict publish gate** (`-RequirePublishedMetadata`).

## Next Required Operator Step
1. Perform Steamworks backend publish in partner portal.
2. Fill publish metadata fields in `backend_contract.json`.
3. Attach screenshot/note artifacts and reference paths in `verification_artifacts`.
4. Run strict gate:
   - `powershell -ExecutionPolicy Bypass -File scripts/ci/verify-steamworks-achievements-stats.ps1 -RequirePublishedMetadata`
