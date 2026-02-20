# Objectives System (Non-Steam)

## Runtime Components
- `Assets/Scripts/Core/ObjectivesService.cs`
- `Assets/Scripts/Save/SaveDataV1.cs` (`objectiveProgress`)
- `Assets/Scripts/UI/HudOverlayController.cs`
- `Assets/Scripts/UI/ProfileMenuController.cs`

## Objective Model
Objective definitions include:
- `id`
- `description`
- `objectiveType`
- `targetCount`
- `rewardCopecs`

Types:
- `CatchCount`
- `CompleteTrips`
- `CatchValueCopecs`

## Default MVP Objectives
- `obj_catch_3` - Catch 3 fish (+120c)
- `obj_trip_2` - Complete 2 fishing trips (+90c)
- `obj_value_250` - Land 250 copecs of catch value (+160c)

## Persistence
Stored in `SaveDataV1.objectiveProgress`:
- objective entry list
- per-entry progress/completion
- completed objective count

## UI Surface
- HUD active objective line:
  - `Objective: <description> (<current>/<target>) +<reward>c`
- Profile panel can show active objective and expose reset hook for QA.

## QA Reset
- `ProfileMenuController.ResetObjectivesForQa()`
- `ObjectivesService.ResetObjectiveProgressForQA()`

## Test Flow
1. Complete each objective in normal gameplay and verify reward application.
2. Relaunch and verify objective state persists.
3. Reset objective state and verify clean restart for QA.
