# Save Migration Rehearsal Report (2026-02-21)

## Scope
- Issue: `#208`
- Manifest: `Assets/Tests/EditMode/Fixtures/Rehearsal/save_rehearsal_manifest.json`
- Constraint: Unity execution enforcement deferred (`UNITY_EXECUTION_ENFORCE=false`).

## Case Outcomes

| Case ID | Fixture | Expected | Outcome |
|---|---|---|---|
| legacy_minimal | `save_v0_minimal.json` | migrate `v0 -> v1` | PASS |
| legacy_extended | `save_v0_extended.json` | migrate `v0 -> v1` | PASS |
| current_v1 | `save_v1_current.json` | pass-through | PASS |
| future_version | `save_v99_future.json` | safe failure | PASS |
| invalid_json | `save_invalid.json` | safe failure | PASS |

## Rollback Drill Summary
- Scenario: malformed payload in isolated test path.
- Expected behavior:
  - source save remains intact,
  - corrupted payload backup is created with `.corrupt_<timestamp>`,
  - load path fails safely without destructive overwrite.
- Result: PASS in deterministic rehearsal harness.

## Deferred Unity-Gated Evidence
- Unity EditMode execution run URL/artifacts are deferred under current repo mode.
- Required follow-up when Unity execution is re-enabled:
  - run `SaveMigrationRehearsalTests` in Unity-enabled CI context,
  - append run URL and artifacts to this report.

## Signoff
- Engineering: GO (migration safety rehearsal corpus and rollback drill complete in deterministic harness).
- QA: GO (Unity-backed execution evidence deferred and tracked, non-blocking under current mode).
