# Testing Baseline

## Test Assemblies
- Runtime: `Assets/Scripts/RavenDevOps.Fishing.Runtime.asmdef`
- Editor tools: `Assets/Editor/RavenDevOps.Fishing.Editor.asmdef`
- EditMode tests: `Assets/Tests/EditMode/RavenDevOps.Fishing.Tests.EditMode.asmdef`
- PlayMode tests: `Assets/Tests/PlayMode/RavenDevOps.Fishing.Tests.PlayMode.asmdef`

## Local Editor Run
1. Open `Window > General > Test Runner`.
2. Run `EditMode` tests.
3. Run `PlayMode` tests.

## Headless CI Run
The CI workflow `.github/workflows/ci-tests.yml` runs both test modes using GameCI.
Unity steps run when `UNITY_LICENSE` is available.
When `UNITY_LICENSE` is missing, workflows emit warnings and skip Unity test execution.
If repository variable `UNITY_EXECUTION_ENFORCE=true`, trusted contexts fail when Unity execution is skipped.
Check-run publication in trusted contexts uses `AUTOMATION_WRITE_TOKEN`; without it, tests still run and artifacts are uploaded (no check-run write).

Headless scene screenshots:
- Workflow: `.github/workflows/ci-scene-capture.yml`
- Trigger: manual `workflow_dispatch` only
- Uses PlayMode test `Assets/Tests/PlayMode/SceneCapturePlayModeTests.cs`
- Enables screenshot test with env var `RAVEN_SCENE_CAPTURE_ENABLED=1`
- Uploads artifacts:
  - `scene-capture-playmode-<sha>`
  - `scene-captures-<sha>` (PNG scene screenshots)

Project wrapper (recommended):

```powershell
.\scripts\unity-cli.ps1 -Task test-edit -LogFile editmode_tests.log
.\scripts\unity-cli.ps1 -Task test-play -LogFile playmode_tests.log
```

Equivalent command pattern (Unity batch mode):

```powershell
Unity.exe -batchmode -nographics -quit `
  -projectPath "C:\path\to\Unity_Fishing_Steam_Game" `
  -runTests -testPlatform editmode -logFile editmode_tests.log
```

```powershell
Unity.exe -batchmode -nographics -quit `
  -projectPath "C:\path\to\Unity_Fishing_Steam_Game" `
  -runTests -testPlatform playmode -logFile playmode_tests.log
```

## Current Coverage Baseline
- EditMode:
  - Content validator rule checks
  - Fish encounter model behavior checks (land, escape, tension state mapping)
  - Progression XP/level threshold rule checks
  - Fishing condition modifier and fish-spawn behavior checks
  - Economy sell-summary multiplier behavior
  - Save fixture deserialization
  - Input context map activation and rebinding override persistence
- PlayMode:
  - Game flow pause/resume transitions
  - Pause-to-harbor transition behavior
  - Headless scene screenshot capture (env-gated in CI)
