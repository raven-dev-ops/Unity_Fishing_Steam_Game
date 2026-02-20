# CI and Branch Protection Baseline

## Required Workflows
These checks should be required on `main`:
- `Build Windows x64`
- `Unity editmode`
- `Unity playmode`
- `Validate content catalog`
- `Gitleaks`

## Branch Protection Settings
1. Require a pull request before merging.
2. Require approvals.
3. Require review from Code Owners.
4. Require status checks to pass.
5. Disable force pushes and deletion.

## Workflow Inventory
- `.github/workflows/ci-build.yml`
- `.github/workflows/ci-tests.yml`
- `.github/workflows/ci-content-validator.yml`
- `.github/workflows/secret-scan.yml`
- `.github/workflows/release-steampipe.yml`

## Notes
- Unity workflows use `UNITY_LICENSE` when available. Without it, workflows complete with warnings and skip Unity-dependent steps.
- Release workflow is environment-gated via `steam-release`.
