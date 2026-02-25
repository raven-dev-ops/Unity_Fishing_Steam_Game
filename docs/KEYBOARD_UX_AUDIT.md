# Keyboard UX Audit

## Areas
- Main Menu
- Settings, Profile, and Pause menus
- Harbor interactables and shops
- Fishing controls and pause flow

## Audit Checklist
- Initial focus is visible when each screen opens.
- Arrow/WASD navigation does not get stuck.
- Enter confirms the currently focused option.
- Esc consistently backs out or pauses.
- Focus aura follows active selection without jitter.
- No keyboard-only dead ends (must be able to recover and continue).
- Rebinding updates visible labels in Settings and runtime prompts without restart.
- Tutorial, fishing HUD, and harbor interaction prompts show current keyboard/gamepad mappings.
- Storefront controller metadata notes are checked against in-game behavior evidence for the current build.

## Prompt Rebinding Matrix (2026-02-25)
| Surface | Action Path(s) | Rebind Validation | Result |
|---|---|---|---|
| Settings bindings row | `Fishing/Action`, `Harbor/Interact`, `UI/Cancel`, `UI/ReturnHarbor` | Rebind action, reopen Settings panel, verify label refresh | PASS |
| Fishing tutorial hands-on prompt | `Fishing/MoveShip`, `Fishing/MoveHook` | Override keyboard composite parts and verify prompt copy reflects overrides | PASS |
| Fishing tutorial demo prompt | `Fishing/MoveShip`, `Fishing/MoveHook` | Trigger demo copy and confirm it mirrors current override labels | PASS |
| Fishing HUD cast/reel hints | `Fishing/MoveHook` | Override up/down bindings and verify cast/reel HUD strings update | PASS |
| Harbor selection hint | `Harbor/Interact` | Override interact binding and verify harbor hint string updates | PASS |

## Severity Tags
- `BLOCKER`: Player cannot proceed without mouse.
- `MAJOR`: Core navigation is inconsistent or misleading.
- `MINOR`: Cosmetic issue with no gameplay lock.

## Result Logging
Record scene, key sequence, expected result, actual result, and severity.
