# Save Migration Rehearsal and Rollback Drill

## Purpose
- Rehearse migration safety against representative legacy/current/failure payloads.
- Validate rollback behavior for malformed saves without destructive data loss.
- Standardize support triage signatures for migration incidents.

## Rehearsal Corpus
- Fixture root: `Assets/Tests/EditMode/Fixtures/Rehearsal/`
- Manifest: `Assets/Tests/EditMode/Fixtures/Rehearsal/save_rehearsal_manifest.json`

Current corpus cases:
- `legacy_minimal` (`save_v0_minimal.json`) -> expected `v0 -> v1` success
- `legacy_extended` (`save_v0_extended.json`) -> expected `v0 -> v1` success
- `current_v1` (`save_v1_current.json`) -> expected pass-through success
- `future_version` (`save_v99_future.json`) -> expected failure (`newer than supported version`)
- `invalid_json` (`save_invalid.json`) -> expected failure (`json parse exception`)

## Automated Rehearsal Tests
- Test file: `Assets/Tests/EditMode/SaveMigrationRehearsalTests.cs`
- Coverage:
  - Corpus manifest replay with expected success/failure outcomes.
  - Idempotent re-run checks for successful migrations.
  - Rollback drill using in-memory save filesystem to verify corrupt backup preservation path.

## Rollback Drill Procedure (Operator)
1. Back up current profile save before any rehearsal.
2. Inject malformed payload in isolated test location.
3. Execute migration/load path in test harness.
4. Verify:
   - load returns safe failure path,
   - source save remains intact,
   - `.corrupt_<timestamp>` backup copy is created,
   - fresh profile recovery path is available.
5. Archive drill results and attach to issue/release notes.

## Triage Signatures
| Signature | Typical Cause | Expected Behavior | Support Action |
|---|---|---|---|
| `save version X is newer than supported version Y` | Client downgrade/opening newer save | Load rejected safely | Ask user to run matching/newer client build |
| `json parse exception` | Truncated or malformed save file | Load rejected safely, corrupt backup attempted | Restore from backup/cloud, collect corrupted payload |
| `migration ... failed` | Migrator regression or unexpected payload shape | Load rejected safely, source retained | Escalate with payload + migration report + build SHA |
| `no migrator registered for version X` | Missing migration chain step | Load rejected safely | Add migrator and patch with regression test |

## Evidence Expectations for #208 Closure
- Rehearsal report with manifest case outcomes and UTC date.
- Rollback drill evidence (backup file path/signature and expected log markers).
- Updated migration policy/runbooks linked from issue closure.

Current report:
- `docs/SAVE_MIGRATION_REHEARSAL_REPORT_2026-02-21.md`
