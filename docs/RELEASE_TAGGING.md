# Release Tagging and Build Upload

## Tagging Standard
- Use semantic version tags (`vMAJOR.MINOR.PATCH`).
- Launch tag for first public release: `v1.0.0`.
- Use annotated tags.

## Steps
1. Confirm release branch is green.
2. Finalize release notes.
3. Complete compliance check:
   - Confirm `LICENSE` is present and current.
   - Review `THIRD_PARTY_NOTICES.md` for new dependencies/assets/tools.
   - Confirm required attributions are included in release notes/packaging where needed.
4. Create tag:
   - `git tag -a v1.0.0 -m "Release v1.0.0"`
5. Push tag:
   - `git push origin v1.0.0`
6. Build Windows release using `Release` profile:
   - CLI: `scripts/unity-cli.ps1 -Task build -BuildProfile Release -LogFile release_build.log`
   - Or batch arg: `-buildProfile=Release`
7. Upload build via guarded Steam release workflow (`.github/workflows/release-steampipe.yml`).
8. Attach build metadata/checksums to release notes.
