# Steam Controller Metadata Evidence Bundles

This folder versions Steamworks partner metadata evidence per release-candidate run.

## Required Bundle Location
- `release/steam_metadata/<rc-tag>/`
- Example tag format: `rc-2026-03-27`

## Required Files Per Bundle
- `manifest.json`
- `controller_support.png`
- `steam_input_settings.png`
- `summary.md`

## Manifest Contract
The manifest must declare:
- capture metadata (`captured_at_utc`, `captured_by`, `rc_tag`)
- screenshot file names
- expected controller metadata state
- verification result (`pass` or `fail`) and drift action notes

Template bundle:
- `release/steam_metadata/rc-template/`

## Verification
Run:

```powershell
./scripts/ci/verify-steam-metadata-evidence.ps1 -EvidenceRoot "release/steam_metadata" -RequireAtLeastOneBundle -RequireAtLeastOnePassingBundle -SummaryJsonPath "Artifacts/SteamMetadata/steam_metadata_evidence_summary.json" -SummaryMarkdownPath "Artifacts/SteamMetadata/steam_metadata_evidence_summary.md"
```

This check validates manifest structure, UTC timestamp format, repo-relative evidence paths, and required screenshot/summary files for each non-template RC bundle. Release gate mode also requires at least one bundle with `verification_result=pass`.

## Drift Rehearsal
Run a simulated mismatch rehearsal:

```powershell
./scripts/ci/rehearse-steam-metadata-drift.ps1
```

Latest report:
- `docs/STEAM_CONTROLLER_METADATA_DRIFT_REHEARSAL_REPORT_2026-02-25.md`

## Final Intake Helper
- `scripts/release/intake-steam-partner-evidence.ps1`
- runbook: `docs/STEAM_EXTERNAL_EVIDENCE_INTAKE.md`
