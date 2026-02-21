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
1. Require pull request before merge (direct pushes blocked).
2. Require status checks and up-to-date branch (`strict=true`).
3. Require at least 1 approving review.
4. Require CODEOWNERS review.
5. Dismiss stale approvals on new commits.
6. Enforce admins.
7. Require linear history.
8. Require conversation resolution.
9. Disallow force push and branch deletion.

## Verification Evidence
### API evidence
`gh api repos/raven-dev-ops/Unity_Fishing_Steam_Game/branches/main/protection` returned:
- `required_status_checks.strict: true`
- `required_status_checks.contexts: [Build Windows x64, Unity editmode, Unity playmode, Validate content catalog, Gitleaks]`
- `required_pull_request_reviews.required_approving_review_count: 1`
- `required_pull_request_reviews.require_code_owner_reviews: true`
- `enforce_admins.enabled: true`
- `allow_force_pushes.enabled: false`
- `allow_deletions.enabled: false`
- `required_linear_history.enabled: true`
- `required_conversation_resolution.enabled: true`

### Merge/push blocking smoke
Command:
- `git push origin chore/branch-protection-smoke:main`

Observed server response:
- `GH006: Protected branch update failed for refs/heads/main`
- `Changes must be made through a pull request`
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
- Unity license and trust-context matrix/source-of-truth: `docs/UNITY_CI_LICENSE_POLICY.md`.
- Write-capable automation token/source-of-truth: `docs/CI_AUTOMATION_TOKEN_POLICY.md`.
- In any context, missing `UNITY_LICENSE` yields warning + skip for Unity-dependent steps.
- Secret-scan write features (PR comments/SARIF upload) are automatically reduced in untrusted contexts.
- Content validator workflow runs asset import audit in fail-on-warning mode, with controlled exceptions in `ci/asset-import-warning-allowlist.json`, and uploads `Artifacts/AssetImportAudit/*` report artifacts.
- Perf budget workflow auto-ingests captured logs and publishes normalized summary artifacts.
- Release workflow is environment-gated via `steam-release`.
