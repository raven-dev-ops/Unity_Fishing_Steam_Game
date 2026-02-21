# CI Automation Token Policy

## Goal
- Keep branch protection intact while allowing trusted CI contexts to perform scoped write operations (for example publishing Unity test check-runs).

## Credential Model
- Default token:
  - `GITHUB_TOKEN` (ephemeral, auto-generated per workflow run).
  - Used for read-only workflow operations by default.
- Write automation token:
  - `AUTOMATION_WRITE_TOKEN` repository secret.
  - Must be a least-privilege credential (fine-grained PAT or GitHub App installation token), not a broad admin token.

## Trusted-Context Gate
- Trusted contexts:
  - Protected refs (`github.ref_protected == true`).
  - `workflow_dispatch`.
- Untrusted contexts:
  - All other contexts (including fork PRs unless explicitly protected).
- Write-capable automation is enabled only when:
  - context is trusted, and
  - `AUTOMATION_WRITE_TOKEN` is configured.
- Otherwise write-capable steps are skipped safely with explicit diagnostics.

## Workflow Permission Audit
| Workflow | Write Operation | Required Scope/Permission | Token Strategy | Fallback |
|---|---|---|---|---|
| `.github/workflows/ci-tests.yml` | Publish Unity test check-runs via `game-ci/unity-test-runner` | `checks: write` | Use `AUTOMATION_WRITE_TOKEN` only in trusted contexts | Run tests without check publication; rely on uploaded artifacts/logs |
| `.github/workflows/ci-scene-capture.yml` | Publish optional PlayMode check-run metadata via `game-ci/unity-test-runner` | `checks: write` | Use `AUTOMATION_WRITE_TOKEN` only in trusted contexts | Run capture without check publication; rely on uploaded artifacts/logs |
| `.github/workflows/secret-scan.yml` | Optional PR comments + SARIF artifact upload by gitleaks action | `security-events: write` for SARIF upload | Trusted contexts keep comments/upload enabled; untrusted contexts disable write-capable gitleaks features | Secret scan still runs; logs/summary remain available |
| `.github/workflows/release-steampipe.yml` | Release upload actions to Steam (external system) | No extra GitHub write scope required beyond read for repo contents | Uses GitHub environment approvals + Steam secrets | Manual release retry after credential correction |

## Ownership and Rotation
- Owner: repository operations owner (`@raven-dev-ops`).
- Rotation interval: every 90 days or immediately after suspected exposure.
- Rotation checklist:
  1. Create replacement token with minimum required scopes.
  2. Update repository secret `AUTOMATION_WRITE_TOKEN`.
  3. Re-run `ci-tests` on a trusted branch to verify check publication.
  4. Revoke old token and confirm no workflows still depend on it.

## Revocation Procedure
1. Revoke compromised token at source (GitHub App install token path or PAT settings).
2. Remove/replace `AUTOMATION_WRITE_TOKEN` in repository secrets.
3. Re-run trusted CI to verify diagnostics show expected state.
4. Record incident details and remediation in internal ops notes.

## Diagnostics and Manual Fallback
- Shared resolver script: `scripts/ci/resolve-write-automation-token.sh`.
- When write automation is disabled, workflows emit:
  - reason (`untrusted_context` or `missing_automation_token`),
  - notice to use uploaded artifacts/logs,
  - manual fallback instruction for PR annotations/comments.
