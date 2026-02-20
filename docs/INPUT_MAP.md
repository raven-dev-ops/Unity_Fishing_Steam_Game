# Input Map (MVP)

## MVP Rules
- Full keyboard-only navigation must remain functional.
- Context switching must activate exactly one gameplay map at a time.
- Pause and cancel (`Esc`) behavior must remain consistent.

## File Naming
- Input Actions asset: `InputActions_Gameplay.inputactions`
- Action maps: `UI`, `Harbor`, `Fishing`
- Actions: PascalCase (`Navigate`, `Submit`, `Pause`)

## Pivot Guidance (Control Focus)
- UI focus should always have a selected element when menus open.
- Harbor interactables should use nearest valid target.
- Fishing controls prioritize deterministic movement updates.

## Baseline Actions
- UI: Navigate, Submit (Enter), Cancel (Esc)
- Harbor: Move, Interact (Enter), Pause (Esc)
- Fishing: MoveShip (Left/Right), MoveHook (Up/Down), Action (Space), Pause (Esc)
