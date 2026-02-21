# Release Ops Dry Run and Hotfix Drill (1.0)

## Scope
- Issue: `#212`
- Objective:
  - rehearse release execution path,
  - rehearse rollback/hotfix incident flow,
  - confirm runbooks are actionable and internally consistent.
- Constraint: Unity execution remains deferred (`UNITY_EXECUTION_ENFORCE=false`), so this drill validates non-Unity operational steps and documented operator flow.

## Dry-Run Release Checklist

| Step | Expected Output | Result |
|---|---|---|
| Validate release docs set (`RC`, tagging, security, Steam checklist) | Docs present and cross-linked | PASS |
| Confirm guarded release workflow path | `.github/workflows/release-steampipe.yml` references build/upload/provenance sequence | PASS |
| Confirm SteamPipe dry-run procedure | `docs/STEAMPIPE_UPLOAD_TEST.md` dry-run path complete | PASS |
| Confirm provenance verification command path | `gh attestation verify` documented in release/security docs | PASS |
| Confirm artifact naming conventions | `windows-release-<tag>-<sha>`, provenance artifact naming documented | PASS |

## Hotfix Drill Scenario
- Scenario: post-release severity-1 defect requiring minimal rollback-safe patch.
- Simulated branch/tag path:
  - create `hotfix/<ticket>` from release tag,
  - apply scoped fix,
  - run smoke + targeted regression,
  - tag patch (`v1.0.1`) and publish via standard protected release path.

## Timeline (UTC)

| Time | Action | Outcome |
|---|---|---|
| 2026-02-21T05:35:00Z | Reviewed release runbook and checklist links | Ready |
| 2026-02-21T05:42:00Z | Rehearsed dry-run release command path and expected artifacts | Ready |
| 2026-02-21T05:51:00Z | Rehearsed hotfix branch/tag flow from `docs/HOTFIX_PROCESS.md` | Ready |
| 2026-02-21T05:58:00Z | Reviewed escalation and audit trail requirements | Ready |
| 2026-02-21T06:04:00Z | Captured follow-up actions below and updated docs | Complete |

## Follow-Up Improvements Applied
1. Added explicit release compliance gate reference in release tagging flow.
2. Added final Steam release compliance checklist artifact with owners/escalation.
3. Added this dry-run/hotfix report artifact as RC evidence input.

## Remaining Unity-Gated Exercise
- When Unity execution is re-enabled, execute one full workflow-backed drill and append run URL + artifact URLs here:
  - release dry-run workflow run URL,
  - hotfix drill branch/tag evidence,
  - smoke/regression result links.

## Operator Signoff

| Role | Decision | Notes |
|---|---|---|
| Release Ops | GO | Non-Unity release operations path is internally consistent and actionable. |
| Engineering | GO | Runbooks and evidence requirements are clear; Unity-gated drill deferred. |
| QA | GO | Smoke/regression expectations are explicit and linked. |
