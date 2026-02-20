# QA Smoke Test Checklist

## Scope
End-to-end regression checklist for the MVP loop.

## Preflight
- Use a clean profile (remove or back up existing save if needed).
- Start from `00_Boot` and follow normal flow.
- Keep Console visible and clear between major phases.
- Use `QA` build profile for routine smoke sweeps and `Release` profile for final release candidate verification.
- In dev/editor builds, toggle in-game log console with backquote key (<code>`</code>) if needed.

## Run Order
1. Launch and verify no boot errors.
2. Main Menu keyboard navigation (arrows + Enter + Esc).
3. Main Menu controller navigation (stick/dpad + South + East).
4. Harbor movement and interactable selection.
5. Hook shop purchase/equip flow.
6. Boat shop purchase/equip flow.
7. Fishing departure and in-scene controls.
8. Trigger safe/warning/critical line tension states and verify readability.
9. Validate fail reason feedback (missed hook, line snap, fish escape).
10. Catch flow to inventory and return to harbor.
11. Sell-all flow and copecs update.
12. Pause menu resume/return harbor/exit behavior.
13. Rebind core actions in Settings, relaunch, and verify persistence.
14. Save, relaunch, verify continuity and catch log persistence.

## Pass Criteria
- No blocker regressions in core gameplay loop.
- No repeating Console error spam in normal flow.
- All keyboard-first actions are reachable without mouse.
- Controller can navigate gameplay UI and core loop actions end-to-end.
- Fishing HUD shows tension state and explicit fail reasons during reel failures.
- Runtime log file is generated and readable at:
  - `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/raven_runtime.log`

## Report Format
- Scene:
- Steps:
- Expected:
- Actual:
- Console errors (if any):
