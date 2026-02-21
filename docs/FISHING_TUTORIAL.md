# Fishing Loop Tutorial

## Runtime Components
- `Assets/Scripts/Fishing/FishingLoopTutorialController.cs`
- `Assets/Scripts/Fishing/CatchResolver.cs`
- `Assets/Scripts/UI/TutorialControlPanel.cs`
- `Assets/Scripts/Save/SaveManager.cs`

## Flow
1. `Cast` prompt: tells player to press fishing action to cast.
2. `Hook` prompt: tells player to react when bite occurs.
3. `Reel` prompt: tells player to hold action and manage tension.
4. Completion: tutorial marks complete on success, or auto-completes after repeated failures to avoid soft-lock.

## Recovery Behavior
- Failed tutorial attempts increment retry count.
- At retry cap (`_maxRecoveryFailures`), tutorial auto-completes and gameplay continues.
- This prevents tutorial state from blocking loop progression.

## 1.0 Anti-Frustration Defaults
- Tutorial auto-recovery retry cap remains `3` failed attempts before forced completion.
- Fishing assist defaults in `CatchResolver` are tuned for earlier recovery when players struggle:
  - no-bite pity threshold `2`,
  - pity bite delay scale `0.50`,
  - adaptive hook-window threshold `2`,
  - adaptive hook-window bonus `+0.40s`.

## Skip and Replay
- Skip entry point:
  - `TutorialControlPanel.SkipFishingTutorial()`
- Replay entry point:
  - `TutorialControlPanel.ReplayFishingTutorial()`
- Save flags used:
  - `tutorialFlags.fishingLoopTutorialCompleted`
  - `tutorialFlags.fishingLoopTutorialSkipped`
  - `tutorialFlags.fishingLoopTutorialReplayRequested`

## Validation
1. Start on clean profile and verify prompts for cast/hook/reel.
2. Intentionally fail repeatedly and confirm tutorial recovers/auto-completes.
3. Skip tutorial and confirm normal fishing continues.
4. Trigger replay and confirm tutorial restarts cleanly.
