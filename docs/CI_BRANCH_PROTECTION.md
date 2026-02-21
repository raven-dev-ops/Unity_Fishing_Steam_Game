# CI and Branch Protection Baseline

## Enforcement Date
- Branch protection enforced on `main`: **2026-02-20 (UTC)**.

## Required Checks (Enforced)
- `Build Windows x64`
- `Unity editmode`
- `Unity playmode`
- `Validate content catalog`
- `Gitleaks`

## Enforced Protection Policy
Configured via:
- `PUT /repos/raven-dev-ops/Unity_Fishing_Steam_Game/branches/main/protection`

Current effective policy (`GET /branches/main/protection`):
1. Require status checks and up-to-date branch (`strict=true`).
2. Enforce admins.
3. Require linear history.
4. Require conversation resolution.
5. Disallow force push and branch deletion.
6. Pull-request review gate is currently not configured (`required_pull_request_reviews` unset).

## Protection Profiles
Use one of these documented profiles when applying branch protection updates.

### Profile A: CI-only (current)
- Keeps merge gates based on required checks, linear history, conversation resolution, and admin enforcement.
- Does not require PR approvals/CODEOWNERS review.

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
  "required_pull_request_reviews": null,
  "enforce_admins": true,
  "required_linear_history": true,
  "required_conversation_resolution": true,
  "allow_force_pushes": false,
  "allow_deletions": false
}
```

### Profile B: Strict reviews
- Adds required PR approval and CODEOWNERS review on top of CI-only controls.

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

### Apply Profile via API
- CI-only profile:
  - `gh api -X PUT repos/raven-dev-ops/Unity_Fishing_Steam_Game/branches/main/protection --input <ci-only.json>`
- Strict review profile:
  - `gh api -X PUT repos/raven-dev-ops/Unity_Fishing_Steam_Game/branches/main/protection --input <strict-reviews.json>`

## Branch Protection Audit Checklist
1. Fetch effective protection:
   - `gh api repos/raven-dev-ops/Unity_Fishing_Steam_Game/branches/main/protection`
2. Confirm required status contexts match intended profile.
3. Confirm `required_pull_request_reviews` is either configured (strict profile) or null (CI-only profile).
4. Confirm `enforce_admins`, `required_linear_history`, and `required_conversation_resolution` are enabled.
5. Confirm force-push and deletion remain disabled.

## Verification Evidence
### API evidence
`gh api repos/raven-dev-ops/Unity_Fishing_Steam_Game/branches/main/protection` returned:
- `required_status_checks.strict: true`
- `required_status_checks.contexts: [Build Windows x64, Unity editmode, Unity playmode, Validate content catalog, Gitleaks]`
- `enforce_admins.enabled: true`
- `allow_force_pushes.enabled: false`
- `allow_deletions.enabled: false`
- `required_linear_history.enabled: true`
- `required_conversation_resolution.enabled: true`
- `required_pull_request_reviews: (not configured)`

### Merge/push blocking smoke
Command:
- `git push origin chore/branch-protection-smoke:main`

Observed server response:
- `GH006: Protected branch update failed for refs/heads/main`
- `5 of 5 required status checks are expected`

## Workflow Inventory
- `.github/workflows/ci-build.yml`
- `.github/workflows/ci-tests.yml`
- `.github/workflows/ci-content-validator.yml`
- `.github/workflows/ci-perf-budget.yml`
- `.github/workflows/secret-scan.yml`
- `.github/workflows/release-steampipe.yml`

## Notes
- Unity workflows classify trusted contexts as protected refs (`github.ref_protected == true`) or manual `workflow_dispatch`.
- Unity workflows also enforce a version contract via `scripts/ci/validate-unity-version.sh` (expected `2022.3.16f1`).
- Unity workflows enforce package determinism via `scripts/ci/validate-package-lock.sh` against `Packages/packages-lock.json`.
- Optional execution enforcement variable `UNITY_EXECUTION_ENFORCE=true` fails trusted contexts when Unity execution is skipped.
- Unity license and trust-context matrix/source-of-truth: `docs/UNITY_CI_LICENSE_POLICY.md`.
- Write-capable automation token/source-of-truth: `docs/CI_AUTOMATION_TOKEN_POLICY.md`.
- In any context, missing `UNITY_LICENSE` yields warning + skip for Unity-dependent steps.
- Secret-scan write features (PR comments/SARIF upload) are automatically reduced in untrusted contexts.
- Content validator workflow runs asset import audit in fail-on-warning mode, with controlled exceptions in `ci/asset-import-warning-allowlist.json`, and uploads `Artifacts/AssetImportAudit/*` report artifacts.
- Perf budget workflow auto-ingests captured logs and publishes normalized summary artifacts.
- Release workflow is environment-gated via `steam-release`.
