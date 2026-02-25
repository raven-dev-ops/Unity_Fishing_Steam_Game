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
Trusted contexts require `UNITY_LICENSE`; missing or invalid license fails Unity workflows.
Untrusted contexts emit warnings and can skip Unity test execution when `UNITY_LICENSE` is unavailable.
Check-run publication in trusted contexts uses `AUTOMATION_WRITE_TOKEN`; without it, tests still run and artifacts are uploaded (no check-run write).
The same workflow also runs a deterministic PlayMode launch-path subset with `-testCategory LaunchRegression`.

Headless scene screenshots:
- Workflow: `.github/workflows/ci-scene-capture.yml`
- Trigger: manual `workflow_dispatch` only
- Uses PlayMode test `Assets/Tests/PlayMode/SceneCapturePlayModeTests.cs`
- Screenshot test is enabled by default; set `RAVEN_SCENE_CAPTURE_ENABLED=0` to disable explicitly
- In `-nographics` environments with `GraphicsDeviceType.Null`, scene capture auto-skips because rendering is unavailable
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
  - PlayMode launch-path regression subset (`-testCategory LaunchRegression`)
  - content validator + bootstrap asset contract validator + asset import audit
  - headless scene capture + diff
  - perf parsing summary
- Consolidated runbook: `docs/NIGHTLY_FULL_REGRESSION.md`

Non-Unity deterministic gates:
- Perf tier ingestion: `.github/workflows/ci-perf-budget.yml`
- Memory + Addressables duplication gates: `.github/workflows/ci-memory-duplication.yml`
- Economy/progression balance simulation: `.github/workflows/ci-balance-simulation.yml`
- Hardware baseline lock matrix validation: `.github/workflows/ci-hardware-baseline-lock.yml`
- Content lock/source-art audit: `.github/workflows/ci-content-lock-audit.yml`

Project wrapper (recommended):

```powershell
.\scripts\unity-cli.ps1 -Task test-edit -LogFile editmode_tests.log
.\scripts\unity-cli.ps1 -Task test-play -LogFile playmode_tests.log
.\scripts\unity-cli.ps1 -Task test-play -LogFile playmode_launch_regression.log -ExtraArgs @('-testCategory','LaunchRegression')
.\scripts\unity-cli.ps1 -Task validate-bootstrap -LogFile validate_bootstrap_assets.log
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
  - Accessibility default/persistence guardrails (subtitles, readability options)
  - Fishing assist anti-frustration default settings guardrails
  - Scene contract resolver fallback and required-missing diagnostics checks
  - Bootstrap asset contract validation seams (required resource path contract + missing-asset diagnostics)
  - Harbor presenter formatting seams (`HarborShopViewPresenter`, `HarborFisheryCardViewPresenter`)
- PlayMode:
  - Deterministic launch-path regression suite (`Assets/Tests/PlayMode/LaunchPathRegressionPlayModeTests.cs`, category `LaunchRegression`) covering:
    - Boot -> Cinematic -> MainMenu transition path
    - Main-menu selection and intro-replay return routing (including #216 profile/settings intent)
    - Save startup behavior for clean and existing profiles
    - Steam-disabled startup/runtime fallback path
  - Game flow pause/resume transitions
  - Pause-to-harbor transition behavior
  - Intro replay exit-route regression coverage for Settings/Profile/default MainMenu follow-up behavior
  - Catch -> inventory -> sell regression path
  - Harbor router transaction flow checks (purchase, equip, fish sale, charter accept/claim, sail guard/route)
  - CatchResolver explicit dependency-bundle catch-flow checks without auto-attached setup dependencies
  - Internal seam-based controller checks for tutorial/catch/cast/runtime-composition paths (no private-member reflection in critical regression suites)
  - Purchase ownership/equip regression path
  - Save/load roundtrip across scene transitions
  - Non-Steam fallback guard-path checks
  - Main-menu runtime composition checks (Profile panel telemetry/actions + Settings controls/rebinds)
  - Main-menu runtime composition idempotence checks (re-applying composition does not duplicate runtime objects/components)
  - Harbor scene runtime composition checks (action panel, harbor status telemetry, pause menu)
  - Fishing scene runtime composition checks (HUD/objective/pause controls/controllers)
  - Fishing backdrop camera coverage checks
  - Fishing ambient fish concurrency cap checks
  - Fishing tutorial explicit dependency-bundle wiring + deterministic demo/hands-on progression checks
  - Headless scene screenshot capture (default-on; explicit env opt-out)
