# Industry Quality and Value Roadmap

## Purpose
- Convert high-value industry practices into an executable backlog with clear sequencing.
- Align engineering quality, operational reliability, and player-facing value growth.

## Tracker
- Program tracker: `#205` POST-004 - Industry-standard quality and value roadmap tracker.

## Current Status (2026-02-21)
- Tracker `#205` is closed.
- Completed child issues:
  - `#192`, `#193`, `#194`, `#195`, `#196`, `#197`, `#198`, `#199`, `#200`, `#201`, `#202`, `#203`, `#204`
- Tracker close criteria:
  - All roadmap child issues implemented in code/docs/workflows.
  - Remaining verification for Unity-dependent execution runs is handled through CI on trusted contexts.

## Milestone Order
### Now (Foundation and Risk Reduction)
1. `#192` Enforce Unity execution in protected CI contexts for required checks.
2. `#193` Implement versioned save migration pipeline with migrator tests.
3. `#194` Split runtime assembly definitions by bounded context.
4. `#195` Extract fishing gameplay domain logic from CatchResolver into testable services.
5. `#200` Enforce PR-only governance and merge queue for main branch.

### Next (Operational Maturity and Balance Tooling)
1. `#196` Add release provenance with SBOM and artifact attestations.
2. `#197` Add platform-target performance capture gates and tiered budgets.
3. `#198` Add memory budget gates and Addressables duplication checks in CI.
4. `#199` Add scheduled nightly full regression workflow.
5. `#201` Add economy and progression simulation tooling with balancing reports.

### Later (Retention and Accessibility Expansion)
1. `#202` Implement anti-frustration systems for fishing loop.
2. `#204` Expand meta-loop with contracts, collections, demand, and gear synergies.
3. `#203` Complete accessibility maturity pass against platform/game accessibility guidelines.

## Success Metrics
- CI fidelity:
  - Protected-branch Unity checks represent actual execution (not skip-pass) in trusted contexts.
- Reliability:
  - Save schema upgrades are migration-tested and failure-safe.
- Throughput:
  - Reduced compile and test iteration friction via bounded runtime assemblies and testable domain services.
- Performance:
  - Tiered perf and memory budgets with actionable CI failures.
- Governance:
  - PR-only path to `main` with documented emergency controls.
- Product quality:
  - Improved retention signals from anti-frustration and deeper meta-loop systems.
- Accessibility:
  - Expanded configurable accessibility options with documented conformance status.

## Delivery Rules
- Each issue must include:
  - implementation notes,
  - test evidence,
  - docs updates where behavior/policy changed.
- Tracker `#205` should be updated on every closure with remaining sequence status.
