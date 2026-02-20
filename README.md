# Unity Fishing Steam Game

Keyboard-first single-player fishing loop for Steam.

## Developer Quick Start

### Prerequisites
- Unity `2022.3.16f1` (LTS)
- Git LFS
- Windows 10/11 for local Windows player builds

### Clone and Open
1. Clone repository and pull LFS assets.
2. Open project in Unity Hub with `2022.3.16f1`.
3. Let Unity import and compile scripts.
4. Open `Assets/Scenes/00_Boot.unity` and press Play.

### Validate Before PR
1. Run content validator: `Raven > Validate Content Catalog`.
2. Run smoke checklist in `docs/QA_SMOKE_TEST.md`.
3. If build-related changes exist, run `Raven > Build > Build Windows x64`.

## Build and CI
- Local menu build: `Raven > Build > Build Windows x64`
- Batch build entrypoint: `RavenDevOps.Fishing.EditorTools.BuildCommandLine.BuildWindowsBatchMode`
- Batch content validator: `RavenDevOps.Fishing.EditorTools.ContentValidatorRunner.ValidateCatalogBatchMode`
- Batch asset import audit: `RavenDevOps.Fishing.EditorTools.AssetImportComplianceRunner.ValidateAssetImportsBatchMode`
- Project CLI wrapper: `scripts/unity-cli.ps1` (`build`, `validate`, `test-edit`, `test-play`) with `-BuildProfile Dev|QA|Release`
- CI workflows are defined under `.github/workflows/`.
- Perf budget parser workflow: `.github/workflows/ci-perf-budget.yml` (auto-ingests captured logs from `PerfLogs/**` and supports manual `explicit_log_file` override).
- Headless scene capture workflow: `.github/workflows/ci-scene-capture.yml` (PlayMode screenshot artifacts for key scenes).
- Unity workflows enforce `UNITY_LICENSE` in trusted contexts (protected refs or manual dispatch), and warn/skip in untrusted contexts.

## Project Structure
- `Assets/Scenes/`: gameplay scenes (Boot -> Cinematic -> MainMenu -> Harbor -> Fishing)
- `Assets/Scripts/Core/`: runtime bootstrap and flow state orchestration
- `Assets/Scripts/Fishing/`: fishing loop runtime systems
- `Assets/Scripts/Save/`: profile/save persistence
- `Assets/Scripts/Input/`: input routing and action-map context switching
- `Assets/Editor/`: Unity editor tooling for build/validation
- `docs/`: operations, QA, release, and content pipeline documentation
- `.github/`: CI workflows, CODEOWNERS, issue templates, PR template

## Troubleshooting
- Compile errors after pull:
  - Reimport project and verify Unity version is `2022.3.16f1`.
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
- Asset import standards: `docs/ASSET_IMPORT_STANDARDS.md`
- Assembly boundaries: `docs/ASSEMBLY_BOUNDARIES.md`
- Contribution workflow: `CONTRIBUTING.md`
- Build pipeline: `docs/BUILD_PIPELINE_WINDOWS.md`
- Build profiles: `docs/BUILD_PROFILES.md`
- CI and branch protection: `docs/CI_BRANCH_PROTECTION.md`
- Catch log baseline: `docs/CATCH_LOG.md`
- Crash diagnostics/privacy: `docs/CRASH_REPORTING.md`
- Content pipeline: `docs/CONTENT_PIPELINE.md`
- Accessibility baseline: `docs/ACCESSIBILITY_BASELINE.md`
- Fishing environment slice: `docs/FISHING_ENVIRONMENT_SLICE.md`
- Fishing combat model: `docs/FISHING_COMBAT_MODEL.md`
- Fishing conditions: `docs/FISHING_CONDITIONS.md`
- Fishing tutorial flow: `docs/FISHING_TUTORIAL.md`
- Input baseline: `docs/INPUT_MAP.md`
- Mod support strategy: `docs/MOD_SUPPORT_STRATEGY.md`
- Mod manifest schema: `docs/MOD_MANIFEST_SCHEMA.md`
- Mod runtime merge: `docs/MOD_RUNTIME_MERGE.md`
- Mod packaging guide: `docs/MOD_PACKAGING_GUIDE.md`
- Mod templates: `mods/templates/`
- Objectives system: `docs/OBJECTIVES_SYSTEM.md`
- Performance baseline and budgets: `docs/PERF_BASELINE.md`
- Photo mode and capture: `docs/PHOTO_MODE.md`
- Progression system baseline: `docs/PROGRESSION_SYSTEM.md`
- Settings system: `docs/SETTINGS_SYSTEM.md`
- Steam achievements/stats: `docs/STEAM_ACHIEVEMENTS_STATS.md`
- Steam Cloud sync: `docs/STEAM_CLOUD_SYNC.md`
- Steam baseline: `docs/STEAMWORKS_BASELINE.md`
- Steam Workshop feasibility: `docs/STEAM_WORKSHOP_FEASIBILITY.md`
- Steam rich presence: `docs/STEAM_RICH_PRESENCE.md`
- SteamPipe upload rehearsal: `docs/STEAMPIPE_UPLOAD_TEST.md`
- Testing baseline: `docs/TESTING_BASELINE.md`
- Release tagging/checklist: `docs/RELEASE_TAGGING.md`
- Security release workflow: `docs/SECURITY_RELEASE_WORKFLOW.md`
- Testing baseline: `docs/TESTING_BASELINE.md`
- UI architecture notes: `docs/UI_ARCHITECTURE.md`
- QA smoke checklist: `docs/QA_SMOKE_TEST.md`

## Repository Rules
- Commit Unity `.meta` files with associated assets.
- Replace assets in-place when possible to preserve GUID references.
- Track large binary assets (audio/video/textures/models) with Git LFS.
