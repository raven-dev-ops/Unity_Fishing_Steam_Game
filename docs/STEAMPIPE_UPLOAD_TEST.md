# SteamPipe Upload Rehearsal

## Scripts and Templates
- Upload script: `scripts/release/steam_upload.sh`
- App build template: `scripts/release/steampipe/app_build_template.vdf`
- Depot template: `scripts/release/steampipe/depot_build_windows_template.vdf`
- Script reference: `scripts/release/README.md`

## Required Secrets/Env
- `STEAM_APP_ID`
- `STEAM_DEPOT_WINDOWS_ID`
- `STEAM_USERNAME`
- `STEAM_CONFIG_VDF`

## Rehearsal Path (Dry Run)
1. Build release candidate via standard build pipeline (`Builds/Windows` default output).
2. Run guarded release workflow with `dry_run=true` (`.github/workflows/release-steampipe.yml`).
3. Confirm generated VDF layouts and SteamCMD command summary in workflow log.
4. Confirm no secrets are printed in logs.

## Live Beta Upload Path
1. Run same workflow with `dry_run=false`.
2. Upload to beta branch (`STEAM_BETA_BRANCH`, default `beta`).
3. Install/update from clean test environment.
4. Verify launch, update continuity, and save integrity.

## Security Controls
- Secrets are consumed from GitHub protected environment only.
- Upload script rejects missing secret/env variables.
- Script has no hardcoded credentials.
- Credentials are read from secret-provided `STEAM_CONFIG_VDF` at runtime.

## Pass Criteria
- Rehearsal and live paths are repeatable from workflow + script.
- Upload succeeds without depot config blockers.
- Beta install launches and updates cleanly.
- Save continuity remains intact after update.

## Security References
- `docs/SECURITY_RELEASE_WORKFLOW.md`
