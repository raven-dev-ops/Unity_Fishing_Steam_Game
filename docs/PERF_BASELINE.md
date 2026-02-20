# Performance Baseline and Regression Budget

## Scope
Baseline covers worst-case MVP loop checks in:
- `03_Harbor`
- `04_Fishing`

## Capture Method
1. Use QA build profile:
   - `scripts/unity-cli.ps1 -Task build -BuildProfile QA -LogFile build_perf.log`
2. Run scene scenarios from `docs/PERF_SANITY_CHECKLIST.md`.
3. Ensure a `PerfSanityRunner` instance is active in the test scene.
4. Capture `PERF_SANITY` lines from player/editor log.
5. Run parser gate:
   - Single log: `scripts/perf-budget-check.ps1 -LogFile <path-to-log>`
   - Captured-log ingestion: `scripts/perf-ingest-captured.ps1`
   - Optional ingestion override: `-ExplicitLogFile <file-or-directory>`

`PerfSanityRunner` emits structured lines in this format:
`PERF_SANITY scene=<name> frames=<n> avg_fps=<v> min_fps=<v> max_fps=<v> avg_frame_ms=<v> p95_frame_ms=<v> gc_delta_kb=<v>`

## Regression Budgets (MVP Gate)
| Scenario | Min avg FPS | Max p95 frame ms | Max GC delta KB/sample window |
|---|---:|---:|---:|
| Harbor traversal + menu churn | 60 | 25 | 64 |
| Fishing repeated cast/hook/reel | 60 | 25 | 64 |

## Baseline Session Template
Record one row per scenario and keep the latest approved run:

| Date (UTC) | Build profile | Scene | avg_fps | p95_frame_ms | gc_delta_kb | Tester | Notes |
|---|---|---|---:|---:|---:|---|---|
| 2026-02-20 | QA | 03_Harbor | _fill from log_ | _fill from log_ | _fill from log_ | _name_ | _machine + resolution_ |
| 2026-02-20 | QA | 04_Fishing | _fill from log_ | _fill from log_ | _fill from log_ | _name_ | _machine + resolution_ |

## Variance Guidance
- Treat <=10% drift as normal variance across comparable runs.
- Anything beyond thresholds or repeated >10% drift is a regression candidate and requires triage.

## CI Budget Gate
- Workflow: `.github/workflows/ci-perf-budget.yml`
- Trigger paths: `PerfLogs/**` (plus workflow/script/doc changes) and manual dispatch.
- Manual dispatch input:
  - `explicit_log_file` (optional file or directory override)
- Behavior:
  - Workflow auto-discovers captured perf logs from:
    - `PerfLogs/**` with `perf*.log` / `*sanity*.log`
    - `Artifacts/Perf/Captured/**` with `perf*.log` / `*sanity*.log`
  - Each discovered log is parsed with `scripts/perf-budget-check.ps1`.
  - Workflow publishes normalized summary artifacts:
    - `Artifacts/Perf/perf_ingestion_summary.json`
    - `Artifacts/Perf/perf_ingestion_summary.md`
    - `Artifacts/Perf/Ingested/**` (per-log parser outputs)
  - If no matching logs are found, workflow is marked skipped (warning + summary artifact).
