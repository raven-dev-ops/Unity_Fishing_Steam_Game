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
- Blocking cloud sync runs only in safe windows (`GameFlowState.None`, `MainMenu`, `Cinematic`) when `_restrictBlockingSyncToSafeStates=true`.
- Save-triggered uploads during unsafe gameplay states are deferred and flushed once a safe window is reached.
- Remote read payload is bounded by `_maxCloudReadBytes` (default `262144` bytes) to avoid large blocking reads.
- Sync telemetry is emitted per operation:
  - `op` (`startup_sync` / `deferred_upload`),
  - `duration_ms`,
  - `remote_bytes`,
  - deferred queue status and conflict decision.

## Async Read Migration Plan
- Trigger criteria (any condition sustained across 3+ RC validation runs):
  - startup cloud sync `duration_ms > 12ms` on baseline hardware,
  - remote save payload regularly exceeds `128KB`,
  - observed frame hitch correlation with startup sync in profiling captures.
- Planned migration target:
  - move remote payload reads to asynchronous/background task boundary,
  - keep conflict-resolution/write steps on main thread with deterministic apply point.
- Owner: Engineering (`system:save`, `system:steam`).
- Tracking issue linkage: follow-up under remediation tracker `#235` if trigger criteria are met.

## Validation
1. Save on machine A, launch machine B, confirm cloud pull.
2. Create divergent local/cloud states and confirm newest-wins behavior.
3. Verify conflict backup files are generated.
4. Confirm save events during fishing do not execute immediate blocking cloud sync; deferred upload flushes when returning to menu-safe state.
5. Verify telemetry logs include `duration_ms` and `remote_bytes` for startup sync and deferred upload operations.
6. Relaunch and validate no save corruption or missing profile data.
