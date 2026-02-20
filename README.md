# Unity Fishing Steam Game

Keyboard-first single-player fishing loop for Steam.

## Project Setup
- Unity version: `2022.3.16f1` (LTS)
- Rendering pipeline: `URP`
- Required packages: TextMeshPro, Input System, Timeline, Universal RP

## Scene Order
1. `Assets/Scenes/00_Boot.unity`
2. `Assets/Scenes/01_Cinematic.unity`
3. `Assets/Scenes/02_MainMenu.unity`
4. `Assets/Scenes/03_Harbor.unity`
5. `Assets/Scenes/04_Fishing.unity`

## Input Baseline
- UI: arrows/WASD navigation, Enter submit, Esc cancel
- Harbor: WASD/arrows move, Enter interact, Esc pause
- Fishing: Left/Right move ship, Up/Down move hook, Space action, Esc pause

## Repository Rules
- Commit Unity `.meta` files with their assets.
- Replace assets in-place to preserve GUID references.
- Large binary assets (audio/video/textures/models) are tracked with Git LFS.
