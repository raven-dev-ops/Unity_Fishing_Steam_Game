# Third-Party Notices

This project depends on third-party tools and packages. This file tracks attribution and notice requirements for current repository dependencies.

## Engine and Core Tooling

### Unity Engine and Unity Packages
- Provider: Unity Technologies
- Website: https://unity.com
- Usage: Game engine and official Unity package ecosystem
- Package examples from `Packages/manifest.json`:
  - `com.unity.inputsystem`
  - `com.unity.render-pipelines.universal`
  - `com.unity.textmeshpro`
  - `com.unity.ugui`
  - `com.unity.timeline`
- License/Terms: Unity Editor and runtime use are governed by Unity license terms.

## Platform SDK

### Steamworks SDK / Steam APIs
- Provider: Valve Corporation
- Website: https://partner.steamgames.com
- Usage: Steam platform integration (initialization, achievements/stats, cloud, build upload)
- License/Terms: Governed by Steamworks SDK agreement and Steam partner terms.

## CI/CD Tooling

### GameCI GitHub Actions
- Repository: https://github.com/game-ci
- Usage: Unity CI build/test workflows (`unity-builder`, `unity-test-runner`)
- License/Terms: See each action repository license.

### Gitleaks
- Repository: https://github.com/gitleaks/gitleaks
- Usage: Secret scanning in CI pipelines
- License/Terms: See repository license.

## Notice Maintenance Policy
- Update this file when introducing new third-party code, SDKs, assets, or CI tools.
- Verify required attributions before each tagged release.
- Ensure release checklist includes license/notice review (`docs/RELEASE_TAGGING.md`).
