# Release Tagging and Build Upload

## Tagging Standard
- Use semantic version tags (`vMAJOR.MINOR.PATCH`).
- Launch tag for first public release: `v1.0.0`.
- Use annotated tags.

## Steps
1. Confirm release branch is green.
2. Finalize release notes.
3. Create tag:
   - `git tag -a v1.0.0 -m "Release v1.0.0"`
4. Push tag:
   - `git push origin v1.0.0`
5. Build Windows release (`Raven > Build > Build Windows x64`).
6. Upload build via SteamPipe.
7. Attach build metadata/checksums to release notes.
