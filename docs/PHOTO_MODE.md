# Photo Mode and Screenshot Capture

## Runtime Components
- `Assets/Scripts/UI/PhotoModeRuntimeService.cs`
- `Assets/Scripts/UI/PhotoModeController.cs`

## Controls (Default)
- Toggle photo mode: `F9`
- Capture screenshot: `F12`
- Move camera: `W/A/S/D`
- Move vertically: `Q` (down), `E` (up)
- Boost movement speed: `Left Shift`
- Look around: hold right mouse button + move mouse

## Behavior
- Entering photo mode stores current camera transform.
- HUD canvases are hidden while photo mode is active for clean capture.
- Exiting photo mode restores camera transform and HUD visibility.

## Screenshot Output
- Output folder:
  - `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/Screenshots`
- File format: `.png`
- Filename format: `photo_YYYYMMDD_HHMMSS_mmm.png`

## Validation
1. Toggle photo mode and confirm HUD hides.
2. Move camera and verify free-camera controls.
3. Capture multiple screenshots and confirm files appear in output folder.
4. Exit photo mode and verify camera/HUD restore.
