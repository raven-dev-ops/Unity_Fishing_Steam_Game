# Performance Sanity Checklist

## Test Cases
- Harbor idle for 2 minutes.
- Fishing loop repeated cast/hook/catch for 5+ minutes.
- Rapid menu open/close while changing focus.

## Tooling
- Runtime FPS sampling: `Assets/Scripts/Performance/PerfSanityRunner.cs`.
- Structured sample logs (`PERF_SANITY`) every configured frame window.
- Optional regression parser: `scripts/perf-budget-check.ps1`.
- Captured-log ingestion wrapper: `scripts/perf-ingest-captured.ps1`.
- Baseline and budgets: `docs/PERF_BASELINE.md`.

## Metrics
- `avg_fps` should meet baseline budget.
- `p95_frame_ms` should remain within baseline budget.
- `gc_delta_kb` should remain within baseline budget.
- Tier metadata (`tier=<minimum|recommended|reference>`) should be present in `PERF_SANITY` lines.
- Fish roll hot path should show no avoidable per-roll managed allocations in steady state.
- Ignore first sample window after scene load; use warmed windows for budget evidence.

## Failure Logging
Capture scene, timestamp, action sequence, and relevant `PERF_SANITY` / `PERF_SANITY_BUDGET_FAIL` lines.

## CI Integration Path
1. Save captured perf logs to one of the standard locations:
   - `PerfLogs/perf_sanity.log`
   - `PerfLogs/**/perf*.log`
   - `Artifacts/Perf/Captured/**/perf*.log`
2. Trigger `.github/workflows/ci-perf-budget.yml` (automatically via `PerfLogs/**` change or manual dispatch).
3. Review uploaded artifacts:
   - `Artifacts/Perf/perf_ingestion_summary.json`
   - `Artifacts/Perf/perf_ingestion_summary.md`
   - `Artifacts/Perf/Ingested/**`

## Tier Waiver Path
1. If summary status is `warning`, open/update tracking ticket with:
   - owner
   - reason
   - expiration date (<=14 days)
2. Add waiver note in release/ops checklist.
3. Fail-level (`status=failed`) results are blocking and require fix or threshold policy update with explicit approval.
