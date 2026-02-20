# Windows Build Pipeline

## Build Method
- Unity menu: `Raven > Build > Build Windows x64`
- Builder entrypoint: `Assets/Editor/BuildCommandLine.cs`

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
