# QA Smoke Test Checklist

## Scope
End-to-end regression checklist for the MVP loop.

## Preflight
- Use a clean profile (remove or back up existing save if needed).
- Start from `00_Boot` and follow normal flow.
- Keep Console visible and clear between major phases.

## Run Order
1. Launch and verify no boot errors.
2. Main Menu keyboard navigation (arrows + Enter + Esc).
3. Harbor movement and interactable selection.
4. Hook shop purchase/equip flow.
5. Boat shop purchase/equip flow.
6. Fishing departure and in-scene controls.
7. Catch flow to inventory and return to harbor.
8. Sell-all flow and copecs update.
9. Pause menu resume/return harbor/exit behavior.
10. Save, relaunch, and verify continuity.

## Pass Criteria
- No blocker regressions in core gameplay loop.
- No repeating Console error spam in normal flow.
- All keyboard-first actions are reachable without mouse.

## Report Format
- Scene:
- Steps:
- Expected:
- Actual:
- Console errors (if any):
