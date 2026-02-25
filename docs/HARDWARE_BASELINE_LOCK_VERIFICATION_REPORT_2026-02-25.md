# Hardware Baseline Lock Verification Report (2026-02-25)

## Scope
- Issue: `#236` (`1.0-031 - Resolve hardware baseline waiver expiry before 2026-03-06`)
- Matrix: `ci/hardware-baseline-matrix.json`
- Validation script: `scripts/ci/hardware-baseline-lock-check.ps1`

## Matrix Updates
- Renewed `minimum` waiver as explicit approved time-boxed exception:
  - `expires_on`: `2026-03-10`
  - `ticket`: `#236`
  - `approved_on_utc`: `2026-02-25T03:00:00Z`
- Renewed `recommended` waiver as explicit approved time-boxed exception:
  - `expires_on`: `2026-03-10`
  - `ticket`: `#236`
  - `approved_on_utc`: `2026-02-25T03:00:00Z`

## Validation
- Non-strict lock check:
  - Command: `powershell -ExecutionPolicy Bypass -File scripts/ci/hardware-baseline-lock-check.ps1`
  - Exit code: `0`
  - Status: `warning` (`waived` minimum/recommended tiers, no failures)
- Strict lock check:
  - Command: `powershell -ExecutionPolicy Bypass -File scripts/ci/hardware-baseline-lock-check.ps1 -RequireAllTiersValidated`
  - Exit code: `0`
  - Status: `warning` (`waived` minimum/recommended tiers, no failures)

## Artifacts
- `Artifacts/Hardware/hardware_baseline_lock_summary.json`
- `Artifacts/Hardware/hardware_baseline_lock_summary.md`

## Remaining Action
- Capture and mark validated `minimum`/`recommended` tier evidence before waiver expiry (`2026-03-10`) or submit another explicit approved renewal within policy bounds.
