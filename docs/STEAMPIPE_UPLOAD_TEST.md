# SteamPipe Upload Test

## Steps
1. Build Windows release candidate.
2. Prepare SteamPipe depot/build config.
3. Run guarded release workflow in dry-run mode first (`.github/workflows/release-steampipe.yml`).
4. Upload to beta branch.
5. Install/update from clean machine/account.
6. Verify launch + save path + update integrity.

## Pass Criteria
- Upload succeeds without depot errors.
- Beta install launches and updates correctly.
- Release workflow includes approval/audit trace and protected secret usage.

## Security References
- `docs/SECURITY_RELEASE_WORKFLOW.md`
