# Catch Log Baseline

## Persistence
- Stored in save data at `SaveDataV1.catchLog`.
- Entry model: `CatchLogEntry` with:
  - `fishId`
  - `distanceTier`
  - `weightKg`
  - `valueCopecs`
  - `timestampUtc`
  - `sessionId`
  - `landed`
  - `failReason`

## Runtime APIs
- Success entry:
  - `SaveManager.RecordCatch(...)`
- Failure entry:
  - `SaveManager.RecordCatchFailure(...)`
- UI snapshot:
  - `SaveManager.GetRecentCatchLogSnapshot(maxEntries)`

## UI
- Profile screen controller (`ProfileMenuController`) can render recent log lines using optional `_catchLogText`.
- Log includes both landed catches and explicit failure reasons.

## Retention
- Catch log is trimmed to most recent 200 entries.
