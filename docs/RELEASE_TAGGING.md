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
6. Trigger guarded Steam release workflow (`.github/workflows/release-steampipe.yml`) using tag push or manual dispatch.
7. Confirm workflow artifact handoff:
   - `Build Windows release artifact` creates `windows-release-<tag>-<sha>`.
   - `SteamPipe upload` downloads artifact to `Artifacts/ReleaseBuild/Windows`.
8. Review release build-size report (`Artifacts/BuildSize/build_size_report.md`) and confirm threshold status.
9. Confirm tiered perf budget compliance from latest perf ingestion summary:
   - `Artifacts/Perf/perf_ingestion_summary.json`
   - Ensure no `failed` tier status and any `warning` tier has approved waiver note.
10. Review provenance evidence:
   - SBOM artifact: `provenance-release-<tag>-<sha>/release_windows_sbom.spdx.json`
   - Build attestation: verify with `gh attestation verify --repo raven-dev-ops/Unity_Fishing_Steam_Game Artifacts/ReleaseBuild/Windows/**`
11. Attach build metadata/checksums to release notes.

## Optional Local Rehearsal
- Local release build is still useful for QA rehearsal:
  - `scripts/unity-cli.ps1 -Task build -BuildProfile Release -LogFile release_build.log`
