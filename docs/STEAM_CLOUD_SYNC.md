# Steam Cloud Save Sync

## Runtime Component
- `Assets/Scripts/Steam/SteamCloudSyncService.cs`

## Active Save Files
- Local canonical file: `save_v1.json`
- Cloud mirrored file: `save_v1.json`
- Cloud manifest file: `save_v1.meta.json`

## Conflict Strategy
- Policy: `newest-wins` with skew tolerance (`_conflictSkewToleranceSeconds`).
- If local and cloud differ:
  - If cloud timestamp is newer, download cloud and reload save.
  - Otherwise, upload local to cloud.
- Non-selected version is preserved as local conflict backup:
  - `save_v1.json.conflict_local_<timestamp>`
  - `save_v1.json.conflict_cloud_<timestamp>`

## Safety Rules
- Cloud sync is best-effort and never blocks gameplay flow.
- If cloud read/write fails, local save remains canonical.
- Sync runs at startup and on save change when Steam is initialized.

## Validation
1. Save on machine A, launch machine B, confirm cloud pull.
2. Create divergent local/cloud states and confirm newest-wins behavior.
3. Verify conflict backup files are generated.
4. Relaunch and validate no save corruption or missing profile data.
