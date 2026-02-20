# Unity Fishing Steam Game

Keyboard-first single-player fishing loop for Steam.

## Current Status
- GitHub tracker is fully closed: `109 closed / 0 open` (as of `2026-02-20` UTC).
- Core loop scaffolding is in place for Boot -> Cinematic -> Main Menu -> Harbor -> Fishing.

## Project Setup
- Unity version: `2022.3.16f1` (LTS)
- Rendering pipeline: `URP`
- Key packages (from `Packages/manifest.json`):
  - `com.unity.render-pipelines.universal`
  - `com.unity.textmeshpro`
  - `com.unity.inputsystem`
  - `com.unity.timeline`
  - `com.unity.ugui`

## Scene Order
1. `Assets/Scenes/00_Boot.unity`
2. `Assets/Scenes/01_Cinematic.unity`
3. `Assets/Scenes/02_MainMenu.unity`
4. `Assets/Scenes/03_Harbor.unity`
5. `Assets/Scenes/04_Fishing.unity`

## Input Baseline
- Input actions asset: `Assets/Resources/InputActions_Gameplay.inputactions`
- UI: arrows navigate, Enter submit, Esc cancel
- Harbor: WASD move, Enter interact, Esc pause
- Fishing: Left/Right move ship, Up/Down move hook, Space action, Esc pause

## Build and Validation
- Windows build command in Unity menu: `Raven > Build > Build Windows x64`
- Content validator command in Unity menu: `Raven > Validate Content Catalog`
- Smoke test checklist: `docs/QA_SMOKE_TEST.md`

## Documentation Index
- Content workflow: `docs/CONTENT_PIPELINE.md`, `docs/ADD_CONTENT_GUIDE.md`
- Input and controls: `docs/INPUT_MAP.md`
- Build and Steam: `docs/BUILD_PIPELINE_WINDOWS.md`, `docs/STEAMWORKS_BASELINE.md`, `docs/STEAMPIPE_UPLOAD_TEST.md`
- Release and operations: `docs/RELEASE_TAGGING.md`, `docs/HOTFIX_PROCESS.md`

## Known Limitation
- Day progression uses local system date (`careerStartLocalDate` to current local date).
- Manual system clock changes can affect displayed day count until an online time source is introduced.

## Repository Rules
- Commit Unity `.meta` files with their assets.
- Replace assets in-place to preserve GUID references.
- Large binary assets (audio/video/textures/models) are tracked with Git LFS.
