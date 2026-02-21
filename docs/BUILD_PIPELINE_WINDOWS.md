# Windows Build Pipeline

## Build Method
- Unity menu: `Raven > Build > Build Windows x64`
- Builder entrypoint: `Assets/Editor/BuildCommandLine.cs`

## Unity Version Contract
- Pinned editor version: `2022.3.16f1` (`ProjectSettings/ProjectVersion.txt`).
- Unity CI workflows run `scripts/ci/validate-unity-version.sh` before license/build steps.
- Unity CI workflows run `scripts/ci/validate-package-lock.sh` to fail fast on missing/malformed `Packages/packages-lock.json`.
- Guard failures are intentional when editor version drifts without workflow alignment.
- Intentional Unity upgrade checklist:
  1. Update `ProjectSettings/ProjectVersion.txt`.
  2. Update `EXPECTED_UNITY_VERSION` in Unity workflows under `.github/workflows/`.
  3. Regenerate and commit `Packages/packages-lock.json` after opening in the new editor.
  4. Refresh CI cache key assumptions if package/editor behavior changed.

## Package Lock Policy
- `Packages/packages-lock.json` is tracked and required in CI.
- Allowed lock-file change triggers:
  1. `Packages/manifest.json` dependency changes.
  2. Unity editor version upgrade (for this project, currently `2022.3.16f1`).
- Lock-file diffs should be reviewed for:
  - unexpected package additions/removals,
  - unexpected version changes outside intended package updates.

## Headless CI/CLI Build
Use Unity batch mode with the static build method:

```powershell
Unity.exe -batchmode -nographics -quit `
  -projectPath "C:\path\to\Unity_Fishing_Steam_Game" `
  -executeMethod RavenDevOps.Fishing.EditorTools.BuildCommandLine.BuildWindowsBatchMode `
  -buildOutput=Builds/Windows `
  -buildExeName=UnityFishingSteamGame.exe `
  -buildProfile=QA `
  -buildVersion=0.1.0 `
  -buildNumber=123 `
  -buildCommit=abcdef1234567890 `
  -buildBranch=main `
  -logFile build_windows.log
```

Project wrapper (recommended):

```powershell
.\scripts\unity-cli.ps1 -Task build -BuildProfile QA -LogFile build_windows.log
```

### Command Arguments
- `-buildOutput` optional output folder (default `Builds/Windows`)
- `-buildExeName` optional executable filename (default `UnityFishingSteamGame.exe`)
- `-buildProfile` optional profile (`Dev`, `QA`, `Release`; default `Dev` from editor entrypoint)
- `-buildVersion` optional version override (falls back to `PlayerSettings.bundleVersion`)
- `-buildNumber` optional CI build number (falls back to `GITHUB_RUN_NUMBER` or `local`)
- `-buildCommit` optional commit SHA (falls back to `GITHUB_SHA` or `local`)
- `-buildBranch` optional branch name (falls back to `GITHUB_REF_NAME` or `local`)

### Build Profiles
- `Dev`: development build with debugging + profiler connection.
- `QA`: development build with profiler connection for validation sweeps.
- `Release`: production profile with dev diagnostics disabled by default.
- Full profile details: `docs/BUILD_PROFILES.md`.

### Deterministic Scene Source
- Scenes are read from enabled entries in `EditorBuildSettings`.
- Keep scene ordering managed in Unity Build Settings only.

## Build Output
- Folder: `Builds/Windows`
- Executable: `Builds/Windows/UnityFishingSteamGame.exe`
- Metadata: `Builds/Windows/build_metadata.json`

## Build Size Reporting
- Baseline file: `ci/build-size-baseline.json`
- Report script: `scripts/ci/build-size-report.sh`
- CI build workflow emits:
  - `Artifacts/BuildSize/build_size_report.json`
  - `Artifacts/BuildSize/build_size_report.md`
- Threshold policy:
  - warning: `> 8%` increase over baseline
  - fail (release workflow): `> 15%` increase over baseline

### Baseline Update Procedure
1. Run an approved build and review generated `build_size_report.json`.
2. Update `ci/build-size-baseline.json` `total_bytes` to the approved reference value.
3. Commit baseline update with rationale (new content/features/compression changes).
4. Re-run CI to confirm deltas evaluate against the new baseline.

## Metadata Fields
- `productName`
- `companyName`
- `bundleVersion`
- `unityVersion`
- `buildNumber`
- `commitSha`
- `branch`
- `buildProfile`
- `buildTimestampUtc`
- `outputExecutable`

## Failure Behavior
- Build method exits non-zero on failures in batch mode.
- Common hard failures:
  - no enabled scenes in `EditorBuildSettings`
  - Unity build pipeline result is not `Succeeded`

## Validation After Build
1. Launch build and run smoke checklist (`docs/QA_SMOKE_TEST.md`).
2. Confirm save write/read path is valid on Windows.
3. Verify no missing-scene or initialization errors.

## Save Path (Windows)
- `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/save_v1.json`
