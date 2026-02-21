# CI and Branch Governance Baseline

## Enforcement Date
- Updated governance on `main`: **2026-02-21 (UTC)**.

## Required Checks
- `Build Windows x64`
- `Unity editmode`
- `Unity playmode`
- `Validate content catalog`
- `Gitleaks`

## PR-Only Governance (Current)
Configured via:
- `PUT /repos/raven-dev-ops/Unity_Fishing_Steam_Game/branches/main/protection`

Effective controls:
1. Strict required checks (`strict=true`) and up-to-date branch enforcement.
2. Pull request reviews required:
   - `required_approving_review_count=1`
   - `dismiss_stale_reviews=true`
   - `require_code_owner_reviews=true`
3. Enforce admins.
4. Require linear history.
5. Require conversation resolution.
6. Disallow force-push and branch deletion.

Result:
- Direct push path to `main` is blocked by policy.
- `main` updates are gated through PR review + required checks.

## Merge Queue / Queue Discipline
- Queue discipline is enforced by:
  - strict up-to-date required checks, and
  - linear-history-only merges.
- If GitHub Merge Queue is enabled for this repository, it should be used as the default merge path for `main`.

## Apply Payload (Strict)
```json
{
  "required_status_checks": {
    "strict": true,
    "contexts": [
      "Build Windows x64",
      "Unity editmode",
      "Unity playmode",
      "Validate content catalog",
      "Gitleaks"
    ]
  },
  "required_pull_request_reviews": {
    "dismiss_stale_reviews": true,
    "require_code_owner_reviews": true,
    "required_approving_review_count": 1
  },
  "enforce_admins": true,
  "required_linear_history": true,
  "required_conversation_resolution": true,
  "allow_force_pushes": false,
  "allow_deletions": false
}
```

## Verification Checklist
1. `gh api repos/raven-dev-ops/Unity_Fishing_Steam_Game/branches/main/protection`
2. Confirm:
   - `required_status_checks.strict=true`
   - required check context list matches policy
   - `required_pull_request_reviews.required_approving_review_count=1`
   - `required_pull_request_reviews.require_code_owner_reviews=true`
   - `required_linear_history.enabled=true`
   - `required_conversation_resolution.enabled=true`
   - `allow_force_pushes.enabled=false`
   - `allow_deletions.enabled=false`

## Workflow Inventory (Relevant)
- `.github/workflows/ci-build.yml`
- `.github/workflows/ci-tests.yml`
- `.github/workflows/ci-content-validator.yml`
- `.github/workflows/ci-perf-budget.yml`
- `.github/workflows/ci-memory-duplication.yml`
- `.github/workflows/ci-balance-simulation.yml`
- `.github/workflows/nightly-full-regression.yml`
- `.github/workflows/secret-scan.yml`

## Emergency Bypass Policy
- Emergency fixes still follow PR flow (`hotfix/*` branch -> PR to `main`).
- If an admin bypass is absolutely required, post-incident audit is mandatory:
  - incident reference
  - actor
  - UTC timestamp
  - rationale
  - follow-up hardening PR
- See `docs/HOTFIX_PROCESS.md` for operational flow.
