# UI Architecture Notes

## Goals
- Keep UI display updates event-driven where practical.
- Route gameplay state mutation through explicit services/commands.
- Ensure listeners are always detached on screen disable/destroy.

## Current Command Pathways
- Profile reset:
  - `ProfileMenuController.ResetProfile()` -> `SaveManager.ResetProfileStats()`
- Tutorial controls:
  - `TutorialControlPanel.SkipTutorial()/ReplayTutorial()` -> `SaveManager.SetTutorialSeen(bool)`
- Catch persistence:
  - `CatchResolver.ResolveCatch()` -> `SaveManager.RecordCatch(fishId, distanceTier)`

## Current Event Pathways
- Save data refresh:
  - `SaveManager.SaveDataChanged` -> `HudOverlayController` / `ProfileMenuController`
- Flow-state UI refresh:
  - `GameFlowManager.StateChanged` -> `HudOverlayController`

## Lifecycle Rules
- Subscribe in `OnEnable`.
- Unsubscribe in `OnDisable`.
- Avoid long-lived static listeners in scene-bound UI controllers.

## Regression Checklist
1. Open/close profile and settings screens repeatedly; verify no listener duplication errors.
2. Trigger save-producing actions (catch fish, reset profile, tutorial toggle); verify UI refreshes without frame polling loops.
3. Pause/resume and scene transitions should not leak stale UI listeners.
