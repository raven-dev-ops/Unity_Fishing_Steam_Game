# Input Map (MVP)

## Source Asset
- `Assets/Resources/InputActions_Gameplay.inputactions`

## Action Maps
- `UI`
  - `Navigate` -> Arrow keys (2D vector)
  - `Submit` -> Enter
  - `Cancel` -> Escape
- `Harbor`
  - `Move` -> WASD (2D vector)
  - `Interact` -> Enter
  - `Pause` -> Escape
- `Fishing`
  - `MoveShip` -> Left/Right arrows (1D axis)
  - `MoveHook` -> Down/Up arrows (1D axis)
  - `Action` -> Space
  - `Pause` -> Escape

## Runtime Context Switching
- Map switching is handled by `Assets/Scripts/Input/InputActionMapController.cs`.
- Flow-level keyboard overrides are handled by `Assets/Scripts/Input/KeyboardFlowInputDriver.cs`.
- Only one map should be active at a time.

## UX Requirements
- Keyboard-only flow must be possible in all gameplay states.
- UI focus must be visible on open and never dead-end.
- `Esc` behavior must stay consistent for cancel/pause.

## Input Settings
- Input sensitivity is configurable in `SettingsMenuController`.
- Sensitivity persists across sessions via runtime user settings service.
- Current sensitivity affects movement speed scaling in Harbor and Fishing movement controllers.
