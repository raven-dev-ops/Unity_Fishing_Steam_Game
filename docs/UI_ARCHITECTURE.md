# UI Architecture Notes

## Goals
- Keep UI display updates event-driven where practical.
- Route gameplay state mutation through explicit services/commands.
- Ensure listeners are always detached on screen disable/destroy.
- Maintain English-only launch copy consistency (see `docs/LOCALIZATION_SCOPE_DECISION_2026-02-25.md`).
- Keep player-facing runtime text on `TMP_Text`/`TextMeshProUGUI` only; do not introduce `UnityEngine.UI.Text` in gameplay scenes.

## Text Rendering Policy
- Runtime scope: Boot, Cinematic, Main Menu, Harbor, and Fishing all use TMP components for player-facing text surfaces.
- Allowed exceptions: none for player-facing runtime paths. Any future non-player-facing/editor diagnostics must be explicitly documented at introduction time.

## Current Command Pathways
- Main menu profile entry:
  - `MainMenuController.OpenProfile()` -> opens `ProfilePanel` (stats + tutorial controls surface)
- Intro replay follow-up routing:
  - `GameFlowOrchestrator` -> typed seam `IMainMenuNavigator.TryOpenSettingsPanel()/TryOpenProfilePanel()` implemented by `MainMenuController`
- Profile reset:
  - `ProfileMenuController.ResetProfile()` -> `SaveManager.ResetProfileStats()`
- Tutorial controls:
  - `TutorialControlPanel.SkipTutorial()/ReplayTutorial()` -> `SaveManager.SetTutorialSeen(bool)` + `GameFlowOrchestrator` replay route from profile context
- Catch persistence:
  - `CatchResolver.ResolveCatch()` -> `SaveManager.RecordCatch(fishId, distanceTier)`
  - `CatchResolver` receives HUD/runtime collaborators through explicit composition-time seams (`Configure` + `ConfigureDependencies`) instead of scene-wide fallback scans.

## Current Event Pathways
- Save data refresh:
  - `SaveManager.SaveDataChanged` -> `HudOverlayController` / `ProfileMenuController`
- Flow-state UI refresh:
  - `GameFlowManager.StateChanged` -> `HudOverlayController`

## Lifecycle Rules
- Subscribe in `OnEnable`.
- Unsubscribe in `OnDisable`.
- Avoid long-lived static listeners in scene-bound UI controllers.
- `FishingLoopTutorialController` dependencies are injected from scene composition (`ConfigureDependencies`) and event subscriptions are refreshed only during initialization lifecycle (not per-frame).

## Regression Checklist
1. Open/close profile and settings screens repeatedly; verify no listener duplication errors.
2. Trigger save-producing actions (catch fish, reset profile, tutorial toggle); verify UI refreshes without frame polling loops.
3. Pause/resume and scene transitions should not leak stale UI listeners.
