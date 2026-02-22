# Unity Fishing Steam Game

Keyboard-first single-player fishing loop for Steam.

## Developer Quick Start

### Prerequisites
- Unity `6000.3.9f1` (LTS)
- Git LFS
- Windows 10/11 for local Windows player builds

### Clone and Open
1. Clone repository and pull LFS assets.
2. Double-click `OPEN_UNITY_PROJECT.bat` from the repo root (Windows) to open the project directly.
3. Let Unity import and compile scripts.
4. Open `Assets/Scenes/00_Boot.unity` and press Play.

Launcher detection checks Unity Hub metadata (`%APPDATA%/UnityHub/editors-v2.json`) and common Hub install roots.
If Unity is installed in a custom path, set `UNITY_EDITOR_PATH` to `Unity.exe` before running `OPEN_UNITY_PROJECT.bat`.
`OPEN_UNITY_PROJECT.bat` also validates launcher script API marker compatibility and stops with an explicit update message if local scripts are stale.

### Validate Before PR
1. Run content validator: `Raven > Validate Content Catalog`.
2. Run smoke checklist in `docs/QA_SMOKE_TEST.md`.
3. If build-related changes exist, run `Raven > Build > Build Windows x64`.

## Build and CI
- Local menu build: `Raven > Build > Build Windows x64`
- Batch build entrypoint: `RavenDevOps.Fishing.EditorTools.BuildCommandLine.BuildWindowsBatchMode`
- Batch content validator: `RavenDevOps.Fishing.EditorTools.ContentValidatorRunner.ValidateCatalogBatchMode`
- Batch asset import audit: `RavenDevOps.Fishing.EditorTools.AssetImportComplianceRunner.ValidateAssetImportsBatchMode`
- Batch sprite sheet/atlas rebuild: `RavenDevOps.Fishing.EditorTools.SpriteSheetAtlasWorkflow.RebuildSheetsAndAtlasesBatchMode`
- Project CLI wrapper: `scripts/unity-cli.ps1` (`build`, `validate`, `rebuild-sheets`, `test-edit`, `test-play`) with `-BuildProfile Dev|QA|Release`
- CI workflows are defined under `.github/workflows/`.
- Perf budget parser workflow: `.github/workflows/ci-perf-budget.yml` (auto-ingests captured logs from `PerfLogs/**` and supports manual `explicit_log_file` override).
- Memory + Addressables duplication gates: `.github/workflows/ci-memory-duplication.yml`.
- Economy/progression simulation gate: `.github/workflows/ci-balance-simulation.yml`.
- Hardware baseline lock gate: `.github/workflows/ci-hardware-baseline-lock.yml`.
- Content lock/source-art audit gate: `.github/workflows/ci-content-lock-audit.yml`.
- Scheduled full-stack regression: `.github/workflows/nightly-full-regression.yml`.
- Headless scene capture workflow: `.github/workflows/ci-scene-capture.yml` (manual dispatch only; PlayMode screenshot artifacts for key scenes).
- Scene capture workflow runs baseline visual diffing via `scripts/ci/compare-scene-captures.py` against `ci/scene-capture-baseline/`.
- Optional scene diff enforcement toggle: set repository variable `SCENE_CAPTURE_DIFF_ENFORCE=true`.
- Unity CI preflight enforces editor version contract (`6000.3.9f1`) via `scripts/ci/validate-unity-version.sh`.
- Unity CI preflight enforces package-lock contract via `scripts/ci/validate-package-lock.sh`.
- Build size report script: `scripts/ci/build-size-report.sh` with baseline `ci/build-size-baseline.json`.
- Unity workflows require `UNITY_LICENSE` to execute Unity-dependent steps; when absent they emit warnings and skip those steps.
- Optional execution enforcement toggle: set repository variable `UNITY_EXECUTION_ENFORCE=true` to fail trusted contexts when Unity execution is skipped.
- Trusted write-capable check publication uses `AUTOMATION_WRITE_TOKEN` (`docs/CI_AUTOMATION_TOKEN_POLICY.md`).

## Project Structure
- `Assets/Scenes/`: gameplay scenes (Boot -> Cinematic -> MainMenu -> Harbor -> Fishing)
- `Assets/Scripts/Bootstrap/`: runtime service composition root
- `Assets/Scripts/Core/`: shared runtime services, settings, and infrastructure
- `Assets/Scripts/Systems/`: flow orchestration and objective systems
- `Assets/Scripts/Fishing/`: fishing loop runtime systems and domain logic
- `Assets/Scripts/Economy/`: sell economy and meta-loop systems
- `Assets/Scripts/Data/`: catalog/content data services
- `Assets/Scripts/Save/`: profile/save persistence and migration pipeline
- `Assets/Scripts/Input/`: input routing, action maps, and rebinding services
- `Assets/Scripts/UI/`: HUD/menu/settings/presentation controllers
- `Assets/Editor/`: Unity editor tooling for build/validation
- `docs/`: operations, QA, release, and content pipeline documentation
- `.github/`: CI workflows, CODEOWNERS, issue templates, PR template

## Troubleshooting
- Compile errors after pull:
  - Reimport project and verify Unity version is `6000.3.9f1`.
- Missing assets or pink materials:
  - Run `git lfs pull`, then reopen project.
- Input not responding in play mode:
  - Verify `Assets/Resources/InputActions_Gameplay.inputactions` exists and is assigned.
- Content validator failures:
  - Fix duplicate IDs, missing icons, or invalid value ranges in ScriptableObject definitions.
- Save issues during local testing:
  - Delete local save file at `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/save_v1.json` and rerun smoke flow.

## Documentation Index
- Addressables pilot: `docs/ADDRESSABLES_PILOT.md`
- Architecture: `docs/ARCHITECTURE.md`
- Industry quality roadmap: `docs/INDUSTRY_QUALITY_ROADMAP.md`
- Asset import standards: `docs/ASSET_IMPORT_STANDARDS.md`
- Assembly boundaries: `docs/ASSEMBLY_BOUNDARIES.md`
- Hardware baseline lock: `docs/HARDWARE_BASELINE_LOCK.md`
- Contribution workflow: `CONTRIBUTING.md`
- Build pipeline: `docs/BUILD_PIPELINE_WINDOWS.md`
- Build profiles: `docs/BUILD_PROFILES.md`
- CI and branch protection: `docs/CI_BRANCH_PROTECTION.md`
- CI automation token policy: `docs/CI_AUTOMATION_TOKEN_POLICY.md`
- Catch log baseline: `docs/CATCH_LOG.md`
- Crash diagnostics/privacy: `docs/CRASH_REPORTING.md`
- Content pipeline: `docs/CONTENT_PIPELINE.md`
- Content lock checklist: `docs/CONTENT_LOCK_CHECKLIST.md`
- Content lock audit report (2026-02-21): `docs/CONTENT_LOCK_AUDIT_REPORT_2026-02-21.md`
- Save migration rehearsal: `docs/SAVE_MIGRATION_REHEARSAL.md`
- Save migration rehearsal report (2026-02-21): `docs/SAVE_MIGRATION_REHEARSAL_REPORT_2026-02-21.md`
- Accessibility baseline: `docs/ACCESSIBILITY_BASELINE.md`
- Accessibility conformance mapping: `docs/ACCESSIBILITY_CONFORMANCE.md`
- UX/accessibility 1.0 signoff: `docs/UX_ACCESSIBILITY_SIGNOFF.md`
- Fishing environment slice: `docs/FISHING_ENVIRONMENT_SLICE.md`
- Fishing combat model: `docs/FISHING_COMBAT_MODEL.md`
- Fishing conditions: `docs/FISHING_CONDITIONS.md`
- Fishing tutorial flow: `docs/FISHING_TUTORIAL.md`
- Meta-loop systems: `docs/META_LOOP_SYSTEM.md`
- Input baseline: `docs/INPUT_MAP.md`
- Nightly full regression runbook: `docs/NIGHTLY_FULL_REGRESSION.md`
- Objectives system: `docs/OBJECTIVES_SYSTEM.md`
- Memory and Addressables quality gates: `docs/MEMORY_AND_ADDRESSABLES_GATES.md`
- Balance simulation tooling: `docs/BALANCE_SIMULATION.md`
- Performance baseline and budgets: `docs/PERF_BASELINE.md`
- Photo mode and capture: `docs/PHOTO_MODE.md`
- Progression system baseline: `docs/PROGRESSION_SYSTEM.md`
- Settings system: `docs/SETTINGS_SYSTEM.md`
- Scene capture baseline diffing: `docs/SCENE_CAPTURE_BASELINE.md`
- Steam achievements/stats: `docs/STEAM_ACHIEVEMENTS_STATS.md`
- Steam Cloud sync: `docs/STEAM_CLOUD_SYNC.md`
- Steam baseline: `docs/STEAMWORKS_BASELINE.md`
- Steam release compliance checklist: `docs/STEAM_RELEASE_COMPLIANCE_CHECKLIST.md`
- Steam rich presence: `docs/STEAM_RICH_PRESENCE.md`
- SteamPipe upload rehearsal: `docs/STEAMPIPE_UPLOAD_TEST.md`
- Release ops dry run and hotfix drill: `docs/RELEASE_OPS_DRY_RUN_AND_HOTFIX_DRILL.md`
- Testing baseline: `docs/TESTING_BASELINE.md`
- Unity CI license/trusted-context policy: `docs/UNITY_CI_LICENSE_POLICY.md`
- Release tagging/checklist: `docs/RELEASE_TAGGING.md`
- RC validation/signoff bundle: `docs/RC_VALIDATION_BUNDLE.md`
- Security release workflow: `docs/SECURITY_RELEASE_WORKFLOW.md`
- Security disclosure policy: `SECURITY.md`
- UI architecture notes: `docs/UI_ARCHITECTURE.md`
- QA smoke checklist: `docs/QA_SMOKE_TEST.md`

## Repository Rules
- Commit Unity `.meta` files with associated assets.
- Replace assets in-place when possible to preserve GUID references.
- Track large binary assets (audio/video/textures/models) with Git LFS.

