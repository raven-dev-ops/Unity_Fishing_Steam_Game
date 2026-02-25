# 1.0 RC Validation Bundle and Signoff

## Purpose
- Provide one auditable release-candidate checklist for 1.0 signoff.
- Standardize required workflow evidence, ownership, and blocker triage.

## Scope and Constraints
- Scope: 1.0 readiness evidence, including trusted-context Unity execution gates.
- Constraint: none for trusted contexts; Unity execution is enforced in trusted CI/release flows.

## Required Workflow Matrix

| Area | Workflow | Trigger | Required Result | Required Artifacts |
|---|---|---|---|---|
| Build | `.github/workflows/ci-build.yml` | push/manual | Success | Build logs, build-size summary |
| Tests | `.github/workflows/ci-tests.yml` | push/manual | Success | EditMode/PlayMode artifacts |
| UX/accessibility signoff | `docs/UX_ACCESSIBILITY_SIGNOFF.md` | release prep | Completed checklist with known exceptions | Signoff doc revision + linked QA notes |
| Content | `.github/workflows/ci-content-validator.yml` | push/manual | Success | Validator/audit logs |
| Secret scan | `.github/workflows/secret-scan.yml` | push | Success | Scan summary |
| Perf budget | `.github/workflows/ci-perf-budget.yml` | push/manual | Success | `Artifacts/Perf` summary |
| Memory + duplication | `.github/workflows/ci-memory-duplication.yml` | push/manual | Success | `Artifacts/Memory`, `Artifacts/Addressables` summaries |
| Content lock audit | `.github/workflows/ci-content-lock-audit.yml` | push/manual | Success (zero active waivers) | `Artifacts/ContentLock/content_lock_summary.*` |
| Hardware baseline lock | `.github/workflows/ci-hardware-baseline-lock.yml` | push/manual | Success or approved waiver path | `Artifacts/Hardware/hardware_baseline_lock_summary.*` |
| Balance simulation | `.github/workflows/ci-balance-simulation.yml` | push/manual | Success | `Artifacts/BalanceSim` report |
| Scene capture diff | `.github/workflows/ci-scene-capture.yml` | manual | Success | scene captures + diff summary |
| Nightly regression | `.github/workflows/nightly-full-regression.yml` | schedule/manual | Success and consolidated summary | nightly summary artifact |
| RC blocker issue gate | `.github/workflows/release-steampipe.yml` (`preflight`) | tag/manual | Success (no open `P0-blocker` + `scope:1.0` issues in `M9.1 - 1.0 Launch Remediation`) or explicit emergency override with reason | `rc-blocker-gate-<tag>-<sha>` artifact (`rc_blocker_gate_summary.md/json`) |
| RC validation bundle gate | `.github/workflows/release-steampipe.yml` (`rc_validation_bundle`) | tag/manual | Success | `rc-validation-bundle-<tag>-<sha>` artifact (`rc_validation_bundle.md/json`) |
| Steamworks achievements/stats publish contract | `.github/workflows/release-steampipe.yml` (`steam_release_metadata_gates`) + `scripts/ci/verify-steamworks-achievements-stats.ps1` | tag/manual | Success (`-RequirePublishedMetadata`) | `steam-metadata-gates-<tag>-<sha>` artifact (`Artifacts/Steamworks/*`) |
| Steam controller metadata evidence | `.github/workflows/release-steampipe.yml` (`steam_release_metadata_gates`) + `scripts/ci/verify-steam-metadata-evidence.ps1` | tag/manual | Success (`-RequireAtLeastOneBundle -RequireAtLeastOnePassingBundle`) | `steam-metadata-gates-<tag>-<sha>` artifact (`Artifacts/SteamMetadata/*`) |
| Release provenance | `.github/workflows/release-steampipe.yml` | tag/manual | Success | release artifact, SBOM, provenance/attestation, SteamPipe logs |
| Release ops dry run + hotfix drill | `docs/RELEASE_OPS_DRY_RUN_AND_HOTFIX_DRILL.md` | release prep | Completed drill report with follow-ups | Drill report revision + run URLs/evidence |

## RC Execution Procedure
1. Freeze candidate commit SHA and create RC tracking note.
2. Run/pin required workflows for that SHA (or attached tag where applicable).
3. Trigger release workflow preflight (`release-steampipe`) and confirm both gates pass:
   - `preflight` RC blocker issue gate
   - `rc_validation_bundle` gate
4. Collect artifact URLs and paste into the signoff template below.
5. Mark each area `PASS`, `WAIVER`, or `BLOCKER`.
6. Resolve all blockers or defer with explicit owner/date before go/no-go.

## Artifact Inventory Template

| Area | Run URL | Artifact URL(s) | Status (`PASS`/`WAIVER`/`BLOCKER`) | Owner | Notes |
|---|---|---|---|---|---|
| Build |  |  |  |  |  |
| Tests |  |  |  |  |  |
| UX/accessibility signoff |  |  |  |  |  |
| Content |  |  |  |  |  |
| Secret scan |  |  |  |  |  |
| Perf budget |  |  |  |  |  |
| Memory + duplication |  |  |  |  |  |
| Content lock audit |  |  |  |  |  |
| Hardware baseline lock |  |  |  |  |  |
| Balance simulation |  |  |  |  |  |
| Scene capture diff |  |  |  |  |  |
| Nightly regression |  |  |  |  |  |
| RC blocker issue gate |  |  |  |  |  |
| RC validation bundle gate |  |  |  |  |  |
| Steam controller metadata evidence |  |  |  |  |  |
| Release provenance |  |  |  |  |  |
| Release ops dry run + hotfix drill |  |  |  |  |  |

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
- UX/accessibility signoff: `docs/UX_ACCESSIBILITY_SIGNOFF.md`
- Steam release compliance: `docs/STEAM_RELEASE_COMPLIANCE_CHECKLIST.md`
- Steam metadata evidence policy: `docs/STEAM_CONTROLLER_METADATA_EVIDENCE_POLICY.md`
- Steam external evidence intake: `docs/STEAM_EXTERNAL_EVIDENCE_INTAKE.md`
- Release ops dry run/hotfix drill: `docs/RELEASE_OPS_DRY_RUN_AND_HOTFIX_DRILL.md`
