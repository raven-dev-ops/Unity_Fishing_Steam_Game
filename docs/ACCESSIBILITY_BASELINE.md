# Accessibility Baseline (MVP)

## Runtime Components
- `Assets/Scripts/Core/UserSettingsService.cs`
- `Assets/Scripts/UI/SettingsMenuController.cs`
- `Assets/Scripts/UI/DialogueBubbleController.cs`
- `Assets/Scripts/UI/HudOverlayController.cs`
- `Assets/Scripts/UI/GlobalUiAccessibilityService.cs`
- `Assets/Scripts/UI/UiAccessibilityCanvasRegistrant.cs` (for dynamically instantiated canvases)
- `Assets/Scripts/Fishing/CatchResolver.cs`
- `Assets/Scripts/Fishing/WaveAnimator.cs`
- `Assets/Scripts/Fishing/FishingCameraController.cs`

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

### Reel Input Mode (Hold/Toggle)
- Setting: `settings.reelInputToggle`
- `false`: hold Action to reel.
- `true`: press Action to toggle reel on/off.
- Runtime application in `CatchResolver`.

### Reduced Motion
- Setting: `settings.reducedMotion`
- Reduces wave-layer motion speed and camera movement responsiveness in fishing runtime.
- Runtime application in `WaveAnimator` and `FishingCameraController`.

### Subtitle Quality Controls
- Setting: `settings.subtitleScale` (`0.8x` to `1.5x`)
- Setting: `settings.subtitleBackgroundOpacity` (`0` to `1`)
- Applied in `DialogueBubbleController` for subtitle readability tuning.

### Readability Boost
- Setting: `settings.readabilityBoost`
- Increases baseline UI scale floor and boosts subtitle text contrast/outline.
- Runtime application in `GlobalUiAccessibilityService` + `DialogueBubbleController`.

## Persistence
All accessibility options persist through `UserSettingsService` + `PlayerPrefs` and are restored on boot.

## Test Flow
1. Toggle subtitles/high-contrast/UI scale and verify immediate effect.
2. Toggle hold/toggle reel mode and verify fishing input behavior.
3. Toggle reduced motion and verify lower perceived camera/wave motion.
4. Adjust subtitle scale/background opacity and verify subtitle readability changes.
5. Toggle readability boost and verify legibility improvements.
6. Relaunch and verify settings persistence.
7. Validate fishing HUD remains readable under high-contrast mode.
8. Validate UI readability at minimum and maximum supported UI scale.

## Conformance Mapping
- See `docs/ACCESSIBILITY_CONFORMANCE.md` for category mapping, before/after audit, and known exceptions.
