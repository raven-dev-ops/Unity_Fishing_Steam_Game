# Security Release Workflow

## Goals
- Keep Steam credentials out of repository history.
- Restrict release/upload execution to approved and auditable contexts.
- Define minimum incident response and secret rotation process.
- Align public disclosure guidance with `SECURITY.md`.
- Keep CI write-capable automation scoped and documented (`docs/CI_AUTOMATION_TOKEN_POLICY.md`).

## Secret Storage Policy
- Store Steam credentials only in GitHub Actions secrets and protected environments.
- Required secrets for release workflow:
  - `STEAM_APP_ID`
  - `STEAM_DEPOT_WINDOWS_ID`
  - `STEAM_USERNAME`
  - `STEAM_CONFIG_VDF`
- Never commit raw credential files, usernames/passwords, or machine auth blobs.

## Protected Release Path
- Use `.github/workflows/release-steampipe.yml` for release uploads.
- Workflow builds Windows release artifact and hands it off to upload job (`Artifacts/ReleaseBuild/Windows`).
- Release job is bound to `environment: steam-release`.
- Configure environment reviewers for manual approval before upload.
- Trigger from protected semver tags (`v*`) or manual dispatch with approval.

## Branch Protection Baseline
Configure in GitHub repository settings:
1. Protect `main`.
2. Require status checks:
   - `Build Windows x64`
   - `Unity editmode`
   - `Unity playmode`
   - `Validate content catalog`
   - `Gitleaks`
3. Require pull request reviews.
4. Require CODEOWNERS review.
5. Disallow force pushes.

## Secret Rotation
- Rotate Steam secrets at least every 90 days or after contributor role changes.
- Rotate immediately after suspected exposure.
- Record rotation date and operator in internal ops notes.

## Incident Response (Credential Exposure)
1. Revoke/rotate exposed credentials in Steam partner portal.
2. Rotate GitHub secrets and environment secrets.
3. Invalidate all active release sessions and rerun secret scan.
4. Open internal incident ticket with timeline and remediation.
5. Add follow-up hardening action items to backlog.

## Audit Trail
- Release workflow run history is retained in GitHub Actions.
- Each approved release includes actor, tag, and workflow log evidence.
