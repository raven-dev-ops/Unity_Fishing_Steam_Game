# Windows Build Pipeline

## Build Method
- Unity menu: `Raven > Build > Build Windows x64`
- Builder script: `Assets/Editor/BuildWindows.cs`

## Build Output
- Folder: `Builds/Windows`
- Executable: `Builds/Windows/UnityFishingSteamGame.exe`

## Scenes Included
- `Assets/Scenes/00_Boot.unity`
- `Assets/Scenes/01_Cinematic.unity`
- `Assets/Scenes/02_MainMenu.unity`
- `Assets/Scenes/03_Harbor.unity`
- `Assets/Scenes/04_Fishing.unity`

## Validation After Build
1. Launch build and run smoke checklist (`docs/QA_SMOKE_TEST.md`).
2. Confirm save write/read path is valid on Windows.
3. Verify no missing-scene or initialization errors.

## Save Path (Windows)
- `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/save_v1.json`
