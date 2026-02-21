# Accessibility Baseline (MVP)

## Runtime Components
- `Assets/Scripts/Core/UserSettingsService.cs`
- `Assets/Scripts/UI/SettingsMenuController.cs`
- `Assets/Scripts/UI/DialogueBubbleController.cs`
- `Assets/Scripts/UI/HudOverlayController.cs`
- `Assets/Scripts/UI/GlobalUiAccessibilityService.cs`
- `Assets/Scripts/UI/UiAccessibilityCanvasRegistrant.cs` (for dynamically instantiated canvases)

## Features

### Subtitles Toggle
- Setting: `settings.subtitlesEnabled`
- Controls dialogue subtitle visibility in `DialogueBubbleController`.
- Voice playback remains available when subtitles are disabled.

### High-Contrast Fishing Cues
- Setting: `settings.highContrastFishingCues`
- Adds color-independent tension/failure markers in HUD:
  - `[=]` safe
  - `[!!]` warning
  - `[!!!]` critical
  - `[ALERT]` failure prefix

### UI Scale
- Setting: `settings.uiScale` (`0.8x` to `1.5x`)
- Applied globally by `GlobalUiAccessibilityService` to root non-world-space canvases.
- Scene canvases are registered on scene-load events (event-driven).
- Dynamically instantiated canvases can auto-register through `UiAccessibilityCanvasRegistrant`.

## Persistence
All accessibility options persist through `UserSettingsService` + `PlayerPrefs` and are restored on boot.

## Test Flow
1. Toggle subtitles/high-contrast/UI scale and verify immediate effect.
2. Relaunch and verify settings persistence.
3. Validate fishing HUD remains readable under high-contrast mode.
4. Validate UI readability at minimum and maximum supported UI scale.
