# Nightly Full Regression Runbook

## Workflow
- `.github/workflows/nightly-full-regression.yml`
- Triggers:
  - nightly schedule (`cron`)
  - manual `workflow_dispatch`

## Coverage
- Unity EditMode tests
- Unity PlayMode tests
- Content validator + asset import compliance audit
- Headless scene capture + baseline diff
- Perf log ingestion and budget parse
- Consolidated summary artifact with job status + artifact inventory

## Required Inputs
- `UNITY_LICENSE` secret for Unity execution jobs
- Optional dispatch input:
  - `explicit_perf_log_file`
  - `simulate_failure` (triage drill)

## Artifacts
- `nightly-editmode-<sha>`
- `nightly-playmode-<sha>`
- `nightly-content-validator-<sha>`
- `nightly-scene-capture-<sha>`
- `nightly-perf-<sha>`
- `nightly-full-regression-summary-<sha>`

## Triage Flow
1. Open `nightly-full-regression-summary-<sha>/nightly_full_regression_summary.md`.
2. Identify failing job(s).
3. Download corresponding job artifact bundle.
4. File/append tracking issue with:
   - failing job
   - first bad run timestamp
   - regression class (test/content/scene/perf)
   - owner and ETA

## Intentional Failure Drill
- Dispatch workflow with `simulate_failure=true`.
- Workflow completes all jobs, uploads consolidated summary artifact, then fails intentionally.
- Use this path to verify alerting + triage process works.
