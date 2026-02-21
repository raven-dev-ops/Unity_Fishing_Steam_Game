# Memory and Addressables CI Gates

## Goal
- Catch memory regression and content duplication bloat early with reproducible CI artifacts.

## Workflow
- `.github/workflows/ci-memory-duplication.yml`
- Triggers:
  - push/PR on gating scripts/config/docs and artifact paths
  - manual dispatch with explicit input path overrides

## Memory Budget Gate
- Script: `scripts/ci/memory-budget-check.ps1`
- Baseline policy: `ci/memory-budget-baseline.json`
- Input formats:
  - JSON object with `scene`, `tier`, `total_mb`
  - JSON array of those objects
  - JSON object with `samples: [...]`
- Outputs:
  - `Artifacts/Memory/memory_budget_summary.json`
  - `Artifacts/Memory/memory_budget_summary.md`

## Addressables Duplication Gate
- Script: `scripts/ci/addressables-duplication-check.ps1`
- Baseline policy: `ci/addressables-duplication-baseline.json`
- Input formats:
  - JSON object with `duplicate_total_mb`/`duplicate_total_bytes`
  - JSON object with `bundles` entries containing `duplicate_bytes`/`duplicate_asset_count`
- Outputs:
  - `Artifacts/Addressables/duplication_summary.json`
  - `Artifacts/Addressables/duplication_summary.md`

## Status Semantics
- `passed`: within warn thresholds.
- `warning`: over warn threshold, under fail threshold.
- `failed`: over fail threshold or malformed input.

CI behavior:
- Fail-level breaches are blocking.
- Warn-level breaches are non-blocking by default and must follow waiver policy.

## Baseline and Waiver Governance
- Baseline files:
  - `ci/memory-budget-baseline.json`
  - `ci/addressables-duplication-baseline.json`
  - `ci/hardware-baseline-matrix.json`
- Waiver requirements (for warnings):
  - owner
  - reason
  - ticket
  - expiry date (<=14 days)
- Failing results require fix or explicit baseline/policy update in PR with rationale.

Representative hardware lock:
- Validate tier coverage and waiver policy:
  - `scripts/ci/hardware-baseline-lock-check.ps1`
- CI workflow:
  - `.github/workflows/ci-hardware-baseline-lock.yml`

## Baseline Update Process
1. Capture representative memory/duplication report inputs.
2. Run workflow/manual dispatch and inspect summary artifacts.
3. Update baseline JSON thresholds in same PR.
4. Include rationale and reviewer signoff.

## Hardware Evidence Requirement for 1.0
- Memory/duplication thresholds should be calibrated from representative tier captures:
  - `minimum`
  - `recommended`
  - `reference`
- Capture metadata and artifact references must be recorded in `ci/hardware-baseline-matrix.json`.
