# Memory and Addressables CI Gates

## Goal
- Catch memory regression and content duplication bloat early with reproducible CI artifacts.

## Workflow
- `.github/workflows/ci-memory-duplication.yml`
- Triggers:
  - push/PR on gating scripts/config/docs and artifact paths
  - manual dispatch with explicit input path overrides
  - manual `release_context` mode for strict no-missing-evidence enforcement

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
- Default thresholds (`ci/memory-budget-baseline.json`):
  - Harbor: warn/fail `850/960` MB
  - Fishing: warn/fail `900/1024` MB
  - Default: warn/fail `900/1024` MB

## Addressables Duplication Gate
- Script: `scripts/ci/addressables-duplication-check.ps1`
- Baseline policy: `ci/addressables-duplication-baseline.json`
- Input formats:
  - JSON object with `duplicate_total_mb`/`duplicate_total_bytes`
  - JSON object with `bundles` entries containing `duplicate_bytes`/`duplicate_asset_count`
- Outputs:
  - `Artifacts/Addressables/duplication_summary.json`
  - `Artifacts/Addressables/duplication_summary.md`
- Default thresholds (`ci/addressables-duplication-baseline.json`):
  - duplicate total MB warn/fail `64/96`
  - duplicate asset count warn/fail `150/250`

## Addressables Delivery Policy Gate
- Script: `scripts/ci/addressables-delivery-policy-check.ps1`
- Policy file: `ci/addressables-delivery-policy.json`
- Launch mode (1.0): `resources_only`
- Enforced checks:
  - Addressables package dependency (`com.unity.addressables`) must not be present.
  - `Assets/AddressableAssetsData` root must not exist in launch train.
  - `AddressablesPilotCatalogLoader` runtime toggle default must remain disabled for launch (`_useAddressablesWhenAvailable=false`).
- Outputs:
  - `Artifacts/AddressablesPolicy/addressables_delivery_policy_summary.json`
  - `Artifacts/AddressablesPolicy/addressables_delivery_policy_summary.md`

## Status Semantics
- `passed`: within warn thresholds.
- `warning`: over warn threshold, under fail threshold.
- `failed`: over fail threshold or malformed input.
- `skipped`: no sample/report files discovered in non-release contexts.

CI behavior:
- Fail-level breaches are blocking.
- Warn-level breaches are non-blocking by default and must follow waiver policy.
- Addressables delivery policy gate failures are blocking in CI and release workflows.
- Missing evidence handling:
  - default CI path: no samples/reports produce `skipped` summary artifacts.
  - release-context path (`release_context=true`): no samples/reports are hard failures (`-FailOnNoSamples` / `-FailOnNoReports`).
  - release workflow (`.github/workflows/release-steampipe.yml`) uses strict release-context flags so memory/duplication evidence cannot be skipped.

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
