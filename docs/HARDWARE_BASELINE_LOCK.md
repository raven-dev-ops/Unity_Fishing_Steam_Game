# Hardware Baseline Lock (1.0)

## Purpose
- Lock perf/memory calibration to representative hardware classes before 1.0.
- Keep tier baseline updates auditable with capture IDs, dates, owners, and waiver policy.

## Source of Truth
- Hardware matrix: `ci/hardware-baseline-matrix.json`
- Validation script: `scripts/ci/hardware-baseline-lock-check.ps1`
- Fingerprint helper: `scripts/ci/collect-hardware-fingerprint.ps1`
- CI workflow: `.github/workflows/ci-hardware-baseline-lock.yml`

## Required Tiers
- `minimum`
- `recommended`
- `reference`

Each tier must define:
- target OS/CPU/GPU/RAM specification
- one or more capture entries with:
  - `capture_id`
  - `captured_utc`
  - `source`
  - `owner`
  - `machine` block (`os`, `cpu`, `gpu`, `ram_gb`)
  - `validated` flag

## Capture Protocol
1. Select tier machine matching target spec in `ci/hardware-baseline-matrix.json`.
2. Record hardware fingerprint:
   - `scripts/ci/collect-hardware-fingerprint.ps1 -Tier <minimum|recommended|reference> -OutputPath Artifacts/Hardware/<capture_id>_fingerprint.json`
3. Run perf/memory/duplication evidence capture using current release-candidate scenario set.
4. Add capture entry to matrix with:
   - capture metadata,
   - artifact links/paths for perf/memory/duplication summaries,
   - `validated=true` only after review signoff.
5. Run lock check:
   - Non-blocking mode:
     - `scripts/ci/hardware-baseline-lock-check.ps1`
   - Strict tier-complete mode:
     - `scripts/ci/hardware-baseline-lock-check.ps1 -RequireAllTiersValidated`

## Waiver Policy
- Temporary gaps (for example, missing physical tier access) must be added to matrix `waivers`.
- Required fields:
  - `tier`
  - `owner`
  - `reason`
  - `expires_on`
  - `ticket`
- Waiver expiry must not exceed policy window (`max_waiver_days`).
- Expired waivers are treated as failures.

## Baseline Update Governance
1. Capture evidence first; never update thresholds without supporting artifacts.
2. Include delta rationale and approval owner in PR description/issue comment.
3. Keep updates scoped:
   - threshold changes in `ci/perf-tier-budgets.json` and/or `ci/memory-budget-baseline.json`
   - matrix updates in `ci/hardware-baseline-matrix.json`
4. Re-run baseline checks after update and archive resulting summaries.

## Current State (2026-02-21)
- `reference` tier has a recorded local hardware fingerprint capture entry.
- `minimum` and `recommended` are currently covered by time-boxed waivers pending physical capture runs.
