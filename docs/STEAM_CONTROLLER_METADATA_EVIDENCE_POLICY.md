# Steam Controller Metadata Evidence Policy

## Purpose
- Define acceptable Steamworks partner metadata state for controller support and Steam Input declarations.
- Standardize drift handling when partner metadata and runtime behavior diverge.

## Evidence Bundle Contract
- Bundle root: `release/steam_metadata/<rc-tag>/`
- Required files:
  - `manifest.json`
  - `controller_support.png`
  - `steam_input_settings.png`
  - `summary.md`
- Manifest schema template:
  - `release/steam_metadata/rc-template/manifest.json`

## Acceptable Metadata State
- Controller support is explicitly declared in Steamworks metadata.
- Steam Input configuration is enabled and aligned with runtime controller/rebinding behavior (`#228` QA evidence).
- Verification result in bundle manifest is `pass`.

## Drift Handling and Escalation
1. If captured metadata mismatches runtime behavior, set `verification_result` to `fail` in `manifest.json`.
2. Block release signoff for the affected RC.
3. Create/update remediation issue with:
   - mismatch summary,
   - owner,
   - target fix date,
   - updated evidence path.
4. Recapture screenshots after remediation and update bundle `summary.md` + `manifest.json`.

## Validation Command
```powershell
./scripts/ci/verify-steam-metadata-evidence.ps1 -EvidenceRoot "release/steam_metadata"
```
