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
./scripts/ci/verify-steam-metadata-evidence.ps1 -EvidenceRoot "release/steam_metadata"
```

This check validates manifest structure and required screenshot/summary files for each non-template RC bundle.
