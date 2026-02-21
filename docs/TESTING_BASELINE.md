# Testing Baseline

## Test Assemblies
- Runtime:
  - `Assets/Scripts/Core/RavenDevOps.Fishing.Core.asmdef`
  - `Assets/Scripts/Save/RavenDevOps.Fishing.Save.asmdef`
  - `Assets/Scripts/Data/RavenDevOps.Fishing.Data.asmdef`
  - `Assets/Scripts/Fishing/RavenDevOps.Fishing.Fishing.asmdef`
  - `Assets/Scripts/Economy/RavenDevOps.Fishing.Economy.asmdef`
  - `Assets/Scripts/UI/RavenDevOps.Fishing.UI.asmdef`
  - `Assets/Scripts/Steam/RavenDevOps.Fishing.Steam.asmdef`
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
- Compares captures to approved baseline in `ci/scene-capture-baseline/` using `scripts/ci/compare-scene-captures.py`
- Diff thresholds:
  - warn: `SCENE_CAPTURE_WARN_THRESHOLD` (default `0.015`)
  - fail: `SCENE_CAPTURE_FAIL_THRESHOLD` (default `0.030`)
- Optional enforcement: repository variable `SCENE_CAPTURE_DIFF_ENFORCE=true` fails the workflow on severe regressions
- Uploads artifacts:
  - `scene-capture-playmode-<sha>`
  - `scene-captures-<sha>` (PNG scene screenshots)
  - `scene-capture-diff-<sha>` (visual diff panels + summary)

Nightly full regression:
- Workflow: `.github/workflows/nightly-full-regression.yml`
- Includes:
  - EditMode tests
  - PlayMode tests
  - content validator + asset import audit
  - headless scene capture + diff
  - perf parsing summary
- Consolidated runbook: `docs/NIGHTLY_FULL_REGRESSION.md`

Non-Unity deterministic gates:
- Perf tier ingestion: `.github/workflows/ci-perf-budget.yml`
- Memory + Addressables duplication gates: `.github/workflows/ci-memory-duplication.yml`
- Economy/progression balance simulation: `.github/workflows/ci-balance-simulation.yml`
- Hardware baseline lock matrix validation: `.github/workflows/ci-hardware-baseline-lock.yml`
- Content lock/placeholder audit: `.github/workflows/ci-content-lock-audit.yml`

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
  - Save migration rehearsal corpus and rollback drill checks
  - Input context map activation and rebinding override persistence
- PlayMode:
  - Game flow pause/resume transitions
  - Pause-to-harbor transition behavior
  - Catch -> inventory -> sell regression path
  - Purchase ownership/equip regression path
  - Save/load roundtrip across scene transitions
  - Non-Steam fallback guard-path checks
  - Headless scene screenshot capture (env-gated in CI)
