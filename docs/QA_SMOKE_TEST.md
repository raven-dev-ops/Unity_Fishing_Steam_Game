# QA Smoke Test Checklist

## Scope
End-to-end regression checklist for MVP loop.

## Run Order
1. Launch build and verify no boot errors.
2. Main Menu keyboard navigation (arrows + Enter + Esc).
3. Harbor movement and interactable selection.
4. Hook shop purchase/equip flow.
5. Boat shop purchase/equip flow.
6. Fishing departure and in-scene controls.
7. Catch flow to inventory and return to harbor.
8. Sell all flow and copec update.
9. Pause menu resume/town harbor/exit behavior.
10. Save/relaunch/load profile continuity.

## Pass Criteria
- No blocking regressions in core loop.
- No repeated console error spam in normal path.
- All keyboard-first actions are reachable without mouse.
