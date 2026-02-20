# Performance Sanity Checklist

## Test Cases
- Harbor idle for 2 minutes.
- Fishing loop repeated cast/hook/catch for 5+ minutes.
- Rapid menu open/close while changing focus.

## Tooling
- Runtime FPS sampling: `Assets/Scripts/Performance/PerfSanityRunner.cs`.
- Structured sample logs (`PERF_SANITY`) every configured frame window.
- Optional regression parser: `scripts/perf-budget-check.ps1`.
- Baseline and budgets: `docs/PERF_BASELINE.md`.

## Metrics
- `avg_fps` should meet baseline budget.
- `p95_frame_ms` should remain within baseline budget.
- `gc_delta_kb` should remain within baseline budget.
- Fish roll hot path should show no avoidable per-roll managed allocations in steady state.

## Failure Logging
Capture scene, timestamp, action sequence, and relevant `PERF_SANITY` / `PERF_SANITY_BUDGET_FAIL` lines.

## CI Integration Path
1. Save the captured perf log under `PerfLogs/perf_sanity.log` (or another path provided to workflow dispatch).
2. Trigger `.github/workflows/ci-perf-budget.yml` (automatically via `PerfLogs/**` change or manual dispatch).
3. Review uploaded artifacts:
   - `Artifacts/Perf/perf_budget_summary.json`
   - `Artifacts/Perf/perf_budget_summary.txt`
