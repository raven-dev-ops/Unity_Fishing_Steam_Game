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
2. Main Menu keyboard navigation (arrows + Enter + Esc), including Profile and Settings submenu open/close.
3. Main Menu controller navigation (stick/dpad + South + East).
4. Harbor movement and interactable selection (world highlights + left action panel buttons).
5. Hook shop purchase/equip flow.
6. Boat shop purchase/equip flow.
7. Fishing departure and in-scene controls.
8. Trigger safe/warning/critical line tension states and verify readability.
9. Validate fail reason feedback (missed hook, line snap, fish escape).
10. Verify fishing tutorial prompts (cast/hook/reel), skip/replay, and failure recovery behavior.
11. Switch fishing conditions (time/weather) and verify HUD + behavior changes.
12. Catch flow to inventory and return to harbor.
13. Sell-all flow and copecs update.
14. Verify XP/level progression updates and next unlock text in profile.
15. Complete objective milestones and verify objective progress/reward updates.
16. Purchase locked/unlocked gear paths behave per progression level.
17. Pause menu resume/return harbor/main-menu/exit behavior.
18. Rebind core actions in Settings, relaunch, and verify persistence.
19. Toggle subtitles/high-contrast/UI scale, relaunch, and verify accessibility persistence.
20. Toggle reel input mode (hold/toggle) and verify fishing reel behavior updates correctly.
21. Toggle reduced motion and verify camera/wave motion reduction in fishing scene.
22. Adjust subtitle scale/background opacity and confirm readability changes apply immediately and persist.
23. Toggle readability boost and verify improved legibility across HUD/dialogue text.
24. Complete accessibility pass checklist in `docs/ACCESSIBILITY_CONFORMANCE.md`.
25. Save, relaunch, verify continuity and catch log/progression/objective persistence.
26. Steam run (if available): verify first-catch/first-purchase/trip stats updates.
27. Steam run (if available): verify Rich Presence state updates + disable toggle behavior.
28. Cloud run (if available): verify save continuity across two machines.
29. Spot-check new texture/audio imports against `docs/ASSET_IMPORT_STANDARDS.md`.
30. Enter photo mode (`F9`), capture screenshot (`F12`), verify output folder and HUD restore.
31. Complete and attach `docs/UX_ACCESSIBILITY_SIGNOFF.md` for 1.0 UX/accessibility signoff.

## Pass Criteria
- No blocker regressions in core gameplay loop.
- No repeating Console error spam in normal flow.
- All keyboard-first actions are reachable without mouse.
- Controller can navigate gameplay UI and core loop actions end-to-end.
- Reel input mode is verified in both hold and toggle variants.
- Reduced motion visibly lowers camera/wave motion in fishing.
- 1.0 UX/accessibility signoff artifact is completed and linked.
- Fishing HUD shows tension state and explicit fail reasons during reel failures.
- Fishing HUD shows active condition state and objective status updates.
- Runtime log file is generated and readable at:
  - `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/raven_runtime.log`
- Crash artifact file is generated on controlled exception path at:
  - `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/last_crash_report.json`

## Report Format
- Scene:
- Steps:
- Expected:
- Actual:
- Console errors (if any):
