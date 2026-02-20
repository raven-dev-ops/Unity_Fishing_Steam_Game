# Progression System Baseline

## Runtime Components
- `Assets/Scripts/Save/SaveManager.cs`
- `Assets/Scripts/Save/ProgressionRules.cs`
- `Assets/Scripts/UI/ProfileMenuController.cs`

## XP Model
- XP is granted on each landed catch.
- Formula (`ProgressionRules.CalculateCatchXp`):
  - base XP: `10`
  - distance bonus: `(distanceTier - 1) * 5`
  - weight bonus: `round(weightKg * 3)`
  - value bonus: `round(valueCopecs / 25)`
- XP gain is clamped to `[5, 200]` per catch.

## Levels
- Default cumulative XP thresholds:
  - Level 1: `0`
  - Level 2: `100`
  - Level 3: `250`
  - Level 4: `450`
  - Level 5: `700`
  - Level 6: `1000`
- Thresholds are serialized in `SaveManager` and can be tuned in inspector.

## Unlock Mapping (Configured Stubs)
- Unlock definitions are configured in `SaveManager` (`_progressionUnlocks`).
- Default unlock stubs:
  - Level 2: `hook_lv2`
  - Level 3: `ship_lv2`
  - Level 4: `hook_lv3`
  - Level 5: `ship_lv3`
- Unlocks are recorded in `SaveDataV1.progression.unlockedContentIds`.
- Shop purchase/equip calls are blocked for tracked content until unlocked.

## Persistence
- Save payload fields:
  - `progression.level`
  - `progression.totalXp`
  - `progression.xpIntoLevel`
  - `progression.xpToNextLevel`
  - `progression.unlockedContentIds`
  - `progression.lastUnlockId`

## UI Surface
- Profile view displays:
  - current level
  - XP progress
  - next unlock target (`SaveManager.GetNextUnlockDescription`)

## Validation
1. Catch fish repeatedly and confirm XP increments/level-up.
2. Confirm locked ship/hook cannot be bought pre-unlock.
3. Reach unlock threshold and verify purchase path opens.
4. Relaunch and verify progression persists.
