# Steam Controller Metadata Drift Rehearsal Report (2026-02-25)

## Scope
- Issue: `#245`
- Goal: verify drift-handling gate behavior for Steam controller metadata evidence.
- Method: simulate an RC bundle with `verification_result=fail` and assert strict release gate blocks.

## Command
```powershell
./scripts/ci/rehearse-steam-metadata-drift.ps1
```

## Result
- Rehearsal summary: `Artifacts/SteamMetadataRehearsal/steam_metadata_drift_rehearsal_summary.json`
- Strict verifier summary: `Artifacts/SteamMetadataRehearsal/steam_metadata_evidence_strict_summary.json`
- Expected strict failure observed: `true`
- Strict overall status: `fail`
- Blocking reason confirmed: `No bundle with verification_result='pass' is available.`

## Interpretation
- Drift-path rehearsal is functioning as expected:
  - bundles marked `verification_result=fail` can be captured for triage evidence,
  - release gate still blocks until at least one passing RC bundle is present.
- Remaining unblock requirement is unchanged: capture and commit a real RC bundle under `release/steam_metadata/<rc-tag>/` with Steam Partner screenshots and `verification_result=pass`.
