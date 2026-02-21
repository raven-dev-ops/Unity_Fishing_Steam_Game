# Save Migration Policy

## Current Save Baseline
- Save model: `Assets/Scripts/Save/SaveDataV1.cs`
- Save manager: `Assets/Scripts/Save/SaveManager.cs`
- Migration pipeline: `Assets/Scripts/Save/SaveMigrationPipeline.cs`
- Current schema version: `saveVersion = 1`
- Runtime save file: `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/save_v1.json`

## Versioning Rules
- Increment `saveVersion` when serialized shape changes.
- Keep backward readers for all shipped versions.
- Avoid destructive field removal without fallback mapping.
- Date defaults are initialized by save manager load path (not field initializers).

## Migration Requirements
- Migrations must be deterministic and idempotent.
- Migration should occur before gameplay systems consume save data.
- On migration failure, fail safely and preserve original file for recovery.
- Save writes must be atomic (`.tmp` + replace/move) to reduce corruption risk.
- Corrupt save reads should back up original file and recover to a fresh usable profile.
- Migration reports should log source/target version and applied steps for support diagnostics.

## Implemented Baseline
- Registered migrators are executed by `SaveMigrationPipeline.TryPrepareForLoad(...)`.
- Version detection uses `saveVersion` envelope parsing (missing value treated as `0` legacy).
- Current migration chain:
  - `v0 -> v1` (`SaveV0ToV1Migrator`)
- Unsupported future saves (`saveVersion > current`) fail load safely.
- Migration outcomes emit structured diagnostics via `StructuredLogService` category `save-migration`:
  - `status=success|failed`
  - source/final version
  - migration steps or failure reason
  - save path for triage context

## Write Cadence and Flush Boundaries
- `SaveManager` uses debounced write scheduling for routine gameplay mutations.
- Default minimum write interval: `1.0s` (`_minimumSaveIntervalSeconds`).
- Critical boundaries force immediate flush:
  - `OnApplicationPause(true)`
  - `OnApplicationQuit()`
  - `OnDestroy()` of active save singleton
- If a scheduled write fails, save request remains pending for retry on next flush opportunity.

## Test Policy
- Keep fixture saves for each shipped version.
- Validate boot-time migration for all fixtures.
- Validate post-migration gameplay, economy, and inventory integrity.
- Current baseline tests:
  - `Assets/Tests/EditMode/SaveMigrationPipelineTests.cs`
  - Fixture inputs/snapshots:
    - `Assets/Tests/EditMode/Fixtures/save_v0_legacy.json`
    - `Assets/Tests/EditMode/Fixtures/save_v1_migrated_snapshot.json`

## Release Gate
- Any `saveVersion` bump requires migration notes in release documentation.
