# Economy and Progression Simulation

## Goal
- Provide deterministic, repeatable balance evidence for catch/sell/progression tuning.

## Tooling
- Script: `scripts/simulate-economy-progression.ps1`
- Config: `ci/balance-sim-config.json`
- CI workflow: `.github/workflows/ci-balance-simulation.yml`

## Deterministic Inputs
- Fixed random seed (`seed`)
- Session duration + trip/catch cadence
- Catch fail rate
- Value/weight ranges
- Distance-tier distribution
- Distance multiplier step
- Level threshold table
- Regression thresholds (warn/fail)

All tuning knobs are externalized in `ci/balance-sim-config.json`.

## Outputs
- JSON report: `Artifacts/BalanceSim/balance_simulation_report.json`
- Markdown report: `Artifacts/BalanceSim/balance_simulation_report.md`

Metrics include:
- currency/hour
- observed fail-rate
- minutes to level milestones (e.g., level 3)
- total XP progression
- pass/warn/fail threshold decision

## Local Usage
```powershell
./scripts/simulate-economy-progression.ps1 `
  -ConfigPath ci/balance-sim-config.json `
  -ReportJsonPath Artifacts/BalanceSim/balance_simulation_report.json `
  -ReportMarkdownPath Artifacts/BalanceSim/balance_simulation_report.md
```

## Regression Policy
- `failed`: blocking regression (CI fails).
- `warning`: non-blocking by default; requires explicit tuning review note.
- Threshold updates require same-PR rationale and reviewer approval.
