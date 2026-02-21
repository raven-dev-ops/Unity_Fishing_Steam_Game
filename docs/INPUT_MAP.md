# Input Map (MVP)

## Source Asset
- `Assets/Resources/InputActions_Gameplay.inputactions`

## Action Maps
- `UI`
  - `Navigate` -> Arrow keys, gamepad left stick, gamepad dpad
  - `Submit` -> Enter, gamepad south button
  - `Cancel` -> Escape, gamepad east button
  - `ReturnHarbor` -> `H`, gamepad north button
- `Harbor`
  - `Move` -> WASD, arrow keys, gamepad left stick, gamepad dpad
  - `Interact` -> Enter, gamepad south button
  - `Pause` -> Escape, gamepad start
- `Fishing`
  - `MoveShip` -> Left/Right arrows, gamepad left stick X, gamepad dpad X
  - `MoveHook` -> Down/Up arrows, gamepad right stick Y, gamepad dpad Y
  - `Action` -> Space, gamepad south button
  - `Pause` -> Escape, gamepad start

## Runtime Context Switching
- Map switching is handled by `Assets/Scripts/Input/InputActionMapController.cs`.
- Flow-level pause handling routes through action maps in `Assets/Scripts/Systems/KeyboardFlowInputDriver.cs`.
- Pause-state return-to-harbor input routes through `Assets/Scripts/Fishing/FishingPauseBridge.cs`.
- Only one map is active at a time.

## Rebinding
- Rebinding persistence service: `Assets/Scripts/Input/InputRebindingService.cs`.
- Binding overrides are stored in PlayerPrefs key:
  - `settings.inputBindingOverridesJson`
- Rebinding UI hooks are exposed via `Assets/Scripts/UI/SettingsMenuController.cs`:
  - `OnRebindFishingActionPressed`
  - `OnRebindHarborInteractPressed`
  - `OnRebindMenuCancelPressed`
  - `OnRebindReturnHarborPressed`
  - `OnResetRebindsPressed`

## UX Requirements
- Keyboard-only flow is possible in all gameplay states.
- Controller can navigate menus and execute core gameplay actions.
- `Esc`/Cancel behavior is mapped consistently by context.
- Pause input is not double-handled across scripts.
