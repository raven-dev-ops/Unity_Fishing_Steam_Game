# Steam External Evidence Intake

## Purpose
- Provide one deterministic handoff flow for the external Steam Partner artifacts required to close:
  - `#226` (Steamworks achievements/stats publish evidence)
  - `#245` (Steam controller metadata screenshot evidence)

## Required External Inputs
- Controller metadata screenshots from Steam Partner (PNG):
  - `controller_support.png`
  - `steam_input_settings.png`
- Steamworks publish metadata for achievements/stats:
  - publish change number
  - publish timestamp (UTC)
  - operator handle
- One or more backend verification artifact files (screenshots/notes exported locally).

## One-Command Intake (Metadata + Backend)
```powershell
./scripts/release/intake-steam-partner-evidence.ps1 `
  -RcTag "rc-YYYY-MM-DD" `
  -CapturedBy "@owner" `
  -SteamAppId "<appid>" `
  -ControllerSupportScreenshotSource "C:\path\controller_support.png" `
  -SteamInputSettingsScreenshotSource "C:\path\steam_input_settings.png" `
  -MetadataNotes "Controller metadata matches runtime rebinding behavior." `
  -UpdateBackendPublishMetadata `
  -BackendChangeNumber "<numeric-change-number>" `
  -BackendPublishedAtUtc "YYYY-MM-DDTHH:MM:SSZ" `
  -BackendVerifiedBy "@owner" `
  -BackendVerificationArtifactSources "C:\path\backend_publish_note.md","C:\path\backend_publish_capture.png" `
  -BackendPublishNotes "Published in Steam Partner for RC signoff."
```

## Outputs Written by Intake Script
- Metadata bundle:
  - `release/steam_metadata/<rc-tag>/manifest.json`
  - `release/steam_metadata/<rc-tag>/controller_support.png`
  - `release/steam_metadata/<rc-tag>/steam_input_settings.png`
  - `release/steam_metadata/<rc-tag>/summary.md`
- Backend evidence (when `-UpdateBackendPublishMetadata` is set):
  - `release/steamworks/achievements_stats/<rc-tag>/*`
  - `release/steamworks/achievements_stats/backend_contract.json` (`backend_publish` updated)

## Automatic Validation Performed
- `scripts/ci/verify-steam-metadata-evidence.ps1` in strict mode:
  - `-RequireAtLeastOneBundle -RequireAtLeastOnePassingBundle`
- `scripts/ci/verify-steamworks-achievements-stats.ps1 -RequirePublishedMetadata` when backend update is enabled.

## Dry Run Mode
Use `-DryRun` to validate arguments and planned destinations without writing files.

## Next Step After Intake
1. Commit generated `release/steam_metadata/<rc-tag>/` and backend evidence/contract updates.
2. Post strict-validation evidence in `#226` and `#245`.
3. Close `#226` and `#245`, then close tracker `#235`.
