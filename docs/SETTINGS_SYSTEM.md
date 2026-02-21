# Settings System

## Persisted Settings
Settings are stored via `UserSettingsService` and persisted in `PlayerPrefs`.

Persisted keys:
- Master volume
- Music volume
- SFX volume
- VO volume
- Input sensitivity
- Fullscreen/window mode
- Resolution (width/height/refresh)
- Subtitles enabled
- High-contrast fishing cues
- UI scale
- Reel input toggle (hold/toggle)
- Reduced motion
- Subtitle scale
- Subtitle background opacity
- Readability boost
- Steam Rich Presence enabled
- Mod safe mode enabled (`settings.modSafeModeEnabled`)
- Input binding overrides (`settings.inputBindingOverridesJson` via `InputRebindingService`)

## Runtime Integration
- Service: `Assets/Scripts/Core/UserSettingsService.cs`
- UI controller: `Assets/Scripts/UI/SettingsMenuController.cs`
- Audio runtime: `Assets/Scripts/Audio/AudioManager.cs`
- Input rebind runtime: `Assets/Scripts/Input/InputRebindingService.cs`
- Input sensitivity consumers:
  - `Assets/Scripts/Harbor/HarborPlayerController.cs`
  - `Assets/Scripts/Fishing/ShipMovementController.cs`
  - `Assets/Scripts/Fishing/HookMovementController.cs`
- Accessibility consumers:
  - `Assets/Scripts/UI/DialogueBubbleController.cs` (subtitles)
  - `Assets/Scripts/UI/HudOverlayController.cs` (high-contrast fishing cues)
  - `Assets/Scripts/UI/GlobalUiAccessibilityService.cs` (UI scale + readability boost)
  - `Assets/Scripts/Fishing/CatchResolver.cs` (reel toggle behavior)
  - `Assets/Scripts/Fishing/WaveAnimator.cs` (reduced motion)
  - `Assets/Scripts/Fishing/FishingCameraController.cs` (reduced motion)
- Steam Rich Presence consumer:
  - `Assets/Scripts/Steam/SteamRichPresenceService.cs`
- Mod safe mode consumers:
  - `Assets/Scripts/Data/ModRuntimeCatalogService.cs`
  - `Assets/Scripts/UI/SettingsMenuController.cs`
  - `Assets/Scripts/UI/ProfileMenuController.cs`
  - `Assets/Scripts/UI/ModDiagnosticsPanelController.cs`

## Display Controls
- Fullscreen toggle switches between `FullScreenWindow` and `Windowed`.
- Resolution can be cycled from supported platform resolutions.

## Audio Controls
- Master/music/sfx/vo controls are applied immediately and persisted.
- VO playback applies ducking to music lane for intelligibility.

## Validation Checklist
1. Change settings in Main Menu and relaunch.
2. Verify settings in Pause Menu reflect persisted values.
3. Confirm display/audio/input behavior is restored on boot.
4. Toggle subtitles/high-contrast/UI scale and verify immediate + persisted behavior.
5. Toggle reel mode/reduced motion/readability controls and verify immediate + persisted behavior.
6. Toggle Steam Rich Presence and verify Steam service respects setting.
7. Toggle mod safe mode and relaunch to confirm mod runtime starts in safe mode with clear status text.
