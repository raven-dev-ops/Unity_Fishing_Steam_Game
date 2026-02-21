# Performance Baseline and Regression Budget

## Scope
Baseline covers worst-case MVP loop checks in:
- `03_Harbor`
- `04_Fishing`

Hardware tier policy covers:
- `minimum`
- `recommended`
- `reference`

Hardware capture lock source:
- `ci/hardware-baseline-matrix.json`
- Validation script: `scripts/ci/hardware-baseline-lock-check.ps1`
- Workflow: `.github/workflows/ci-hardware-baseline-lock.yml`

## Capture Method
1. Use QA build profile:
   - `scripts/unity-cli.ps1 -Task build -BuildProfile QA -LogFile build_perf.log`
2. Run scene scenarios from `docs/PERF_SANITY_CHECKLIST.md`.
3. Ensure a `PerfSanityRunner` instance is active in the test scene.
4. Warm up scene for at least one sample window before recording budget evidence.
5. Capture `PERF_SANITY` lines from player/editor log.
6. Run parser gate:
   - Single log: `scripts/perf-budget-check.ps1 -LogFile <path-to-log>`
   - Captured-log ingestion: `scripts/perf-ingest-captured.ps1`
   - Optional ingestion override: `-ExplicitLogFile <file-or-directory>`

`PerfSanityRunner` emits structured lines in this format:
`PERF_SANITY scene=<name> tier=<tier> frames=<n> avg_fps=<v> min_fps=<v> max_fps=<v> avg_frame_ms=<v> p95_frame_ms=<v> gc_delta_kb=<v>`

## Measurement Notes
- `gc_delta_kb` is sampled before log/label formatting so instrumentation allocations do not inflate the metric.
- Percentile sampling uses reusable buffers to avoid per-window list copy/sort allocations.
- For consistent comparisons, capture runs at the same resolution/quality and after warmup.

## Regression Budgets (MVP Gate)
Tier thresholds are source-controlled in `ci/perf-tier-budgets.json`.

Minimum tier defaults:
| Scenario | Warn min avg FPS | Fail min avg FPS | Warn max p95 frame ms | Fail max p95 frame ms | Warn max GC KB | Fail max GC KB |
|---|---:|---:|---:|---:|---:|---:|
| Harbor traversal + menu churn | 60 | 55 | 25 | 30 | 64 | 80 |
| Fishing repeated cast/hook/reel | 60 | 55 | 25 | 30 | 64 | 80 |

## Baseline Session Template
Record one row per scenario and keep the latest approved run:

| Date (UTC) | Build profile | Scene | avg_fps | p95_frame_ms | gc_delta_kb | Tester | Notes |
|---|---|---|---:|---:|---:|---|---|
| 2026-02-20 | QA | 03_Harbor | _fill from log_ | _fill from log_ | _fill from log_ | _name_ | _machine + resolution_ |
| 2026-02-20 | QA | 04_Fishing | _fill from log_ | _fill from log_ | _fill from log_ | _name_ | _machine + resolution_ |

## Variance Guidance
- Treat <=10% drift as normal variance across comparable runs.
- Anything beyond thresholds or repeated >10% drift is a regression candidate and requires triage.
- Tier-waiver policy:
  - Temporary warnings may be waived for up to 14 days.
  - Waiver notes must include owner, ticket, reason, and expiry date.
  - Fail-level regressions are blocking.

Hardware-tier waiver policy:
- Missing representative tier captures (`minimum`/`recommended`/`reference`) require explicit waiver entries in `ci/hardware-baseline-matrix.json`.
- Waivers are time-boxed (max 14 days by policy) and validated by `scripts/ci/hardware-baseline-lock-check.ps1`.

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
  - Parser resolves tier from:
    - `PERF_SANITY ... tier=<tier>` log metadata
    - filename fallback (`minimum|recommended|reference`)
    - default fallback tier (`minimum`)
  - If no matching logs are found, workflow is marked skipped (warning + summary artifact).

## Baseline Lock Verification
- Run non-blocking matrix validation:
  - `scripts/ci/hardware-baseline-lock-check.ps1`
- Run strict 1.0 validation (all tiers must have at least one validated capture):
  - `scripts/ci/hardware-baseline-lock-check.ps1 -RequireAllTiersValidated`
