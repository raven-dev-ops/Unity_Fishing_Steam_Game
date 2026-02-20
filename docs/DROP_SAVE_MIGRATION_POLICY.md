# Save Migration Policy

## Current Save Baseline
- Save model: `Assets/Scripts/Save/SaveDataV1.cs`
- Save manager: `Assets/Scripts/Save/SaveManager.cs`
- Current schema version: `saveVersion = 1`

## Versioning Rules
- Increment `saveVersion` when serialized shape changes.
- Keep backward readers for all shipped versions.
- Avoid destructive field removal without fallback mapping.

## Migration Requirements
- Migrations must be deterministic and idempotent.
- Migration should occur before gameplay systems consume save data.
- On migration failure, fail safely and preserve original file for recovery.

## Test Policy
- Keep fixture saves for each shipped version.
- Validate boot-time migration for all fixtures.
- Validate post-migration gameplay, economy, and inventory integrity.

## Release Gate
- Any `saveVersion` bump requires migration notes in release documentation.
