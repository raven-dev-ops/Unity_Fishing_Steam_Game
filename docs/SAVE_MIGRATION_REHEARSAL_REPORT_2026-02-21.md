# Save Migration Rehearsal Report (2026-02-21)

## Scope
- Issue: `#208`
- Manifest: `Assets/Tests/EditMode/Fixtures/Rehearsal/save_rehearsal_manifest.json`
- Historical note: this report predates trusted-context Unity enforcement rollout (effective 2026-02-24).

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

## Unity-Gated Evidence Follow-Up
- Original report did not include trusted-context Unity CI evidence.
- Required follow-up under current policy:
  - run `SaveMigrationRehearsalTests` in Unity-enabled CI context,
  - append run URL and artifacts to this report.

## Signoff
- Engineering: GO (migration safety rehearsal corpus and rollback drill complete in deterministic harness).
- QA: GO (legacy rehearsal accepted; trusted-context Unity CI evidence follow-up required for RC bundle completeness).
