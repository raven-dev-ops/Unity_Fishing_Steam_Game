# 1.0 RC Validation Bundle and Signoff

## Purpose
- Provide one auditable release-candidate checklist for 1.0 signoff.
- Standardize required workflow evidence, ownership, and blocker triage.

## Scope and Constraints
- Scope: non-Unity-license-policy work required for 1.0 readiness.
- Current constraint: `UNITY_EXECUTION_ENFORCE=false` (Unity execution enforcement is intentionally deferred for now).

## Required Workflow Matrix

| Area | Workflow | Trigger | Required Result | Required Artifacts |
|---|---|---|---|---|
| Build | `.github/workflows/ci-build.yml` | push/manual | Success | Build logs, build-size summary |
| Tests | `.github/workflows/ci-tests.yml` | push/manual | Success | EditMode/PlayMode artifacts or documented skip reason |
| Content | `.github/workflows/ci-content-validator.yml` | push/manual | Success | Validator/audit logs |
| Secret scan | `.github/workflows/secret-scan.yml` | push | Success | Scan summary |
| Perf budget | `.github/workflows/ci-perf-budget.yml` | push/manual | Success | `Artifacts/Perf` summary |
| Memory + duplication | `.github/workflows/ci-memory-duplication.yml` | push/manual | Success | `Artifacts/Memory`, `Artifacts/Addressables` summaries |
| Balance simulation | `.github/workflows/ci-balance-simulation.yml` | push/manual | Success | `Artifacts/BalanceSim` report |
| Scene capture diff | `.github/workflows/ci-scene-capture.yml` | manual | Success (or approved waiver) | scene captures + diff summary |
| Nightly regression | `.github/workflows/nightly-full-regression.yml` | schedule/manual | Success and consolidated summary | nightly summary artifact |
| Release provenance | `.github/workflows/release-steampipe.yml` | tag/manual | Success | release artifact, SBOM, provenance/attestation, SteamPipe logs |

## RC Execution Procedure
1. Freeze candidate commit SHA and create RC tracking note.
2. Run/pin required workflows for that SHA (or attached tag where applicable).
3. Collect artifact URLs and paste into the signoff template below.
4. Mark each area `PASS`, `WAIVER`, or `BLOCKER`.
5. Resolve all blockers or defer with explicit owner/date before go/no-go.

## Artifact Inventory Template

| Area | Run URL | Artifact URL(s) | Status (`PASS`/`WAIVER`/`BLOCKER`) | Owner | Notes |
|---|---|---|---|---|---|
| Build |  |  |  |  |  |
| Tests |  |  |  |  |  |
| Content |  |  |  |  |  |
| Secret scan |  |  |  |  |  |
| Perf budget |  |  |  |  |  |
| Memory + duplication |  |  |  |  |  |
| Balance simulation |  |  |  |  |  |
| Scene capture diff |  |  |  |  |  |
| Nightly regression |  |  |  |  |  |
| Release provenance |  |  |  |  |  |

## Final Signoff Table

| Role | Owner | Decision (`GO`/`NO-GO`) | UTC Timestamp | Notes |
|---|---|---|---|---|
| Engineering |  |  |  |  |
| QA |  |  |  |  |
| Release Ops |  |  |  |  |
| Product |  |  |  |  |

## Blocker Triage Ownership

| Blocker Class | Primary Owner | Secondary Owner | Required Response |
|---|---|---|---|
| Build/Test/Validator failure | Engineering | QA | Root cause + fix plan + rerun link |
| Perf/Memory/Duplication regression | Engineering | Product | Baseline update rationale or optimization fix |
| Security/provenance failure | Release Ops | Engineering | Corrective action and revalidation |
| SteamPipe/release packaging failure | Release Ops | Engineering | Rehearsal fix + rerun evidence |
| Content lock violation | QA | Engineering | Content correction + validator rerun |

## Waiver Policy
- Waivers are time-boxed and must include:
  - owner,
  - UTC expiry date,
  - risk statement,
  - mitigation plan,
  - follow-up issue link.
- Any expired waiver becomes a blocker.

## Links
- Release tagging: `docs/RELEASE_TAGGING.md`
- Security/provenance workflow: `docs/SECURITY_RELEASE_WORKFLOW.md`
- Nightly runbook: `docs/NIGHTLY_FULL_REGRESSION.md`
- QA smoke checklist: `docs/QA_SMOKE_TEST.md`
