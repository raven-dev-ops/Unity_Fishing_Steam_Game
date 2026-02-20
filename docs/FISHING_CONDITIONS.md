# Fishing Conditions (Time of Day + Weather)

## Runtime Components
- `Assets/Scripts/Fishing/FishingConditionController.cs`
- `Assets/Scripts/Fishing/FishSpawner.cs`
- `Assets/Scripts/UI/HudOverlayController.cs`

## Condition Model
- Time of day states:
  - `Dawn`
  - `Day`
  - `Dusk`
  - `Night`
- Weather states:
  - `Clear`
  - `Overcast`
  - `Rain`
  - `Storm`

Combined modifiers affect fish encounter behavior:
- Spawn rarity weighting
- Bite delay window
- Fight stamina
- Pull intensity
- Escape window

## Integration
- `FishSpawner` applies condition multipliers when selecting and shaping encounter fish.
- `CatchResolver` updates HUD with active condition label.
- Optional auto-cycle for time-of-day is available in `FishingConditionController`.

## Test Flow
1. In runtime, switch time/weather via `FishingConditionController` test hooks.
2. Verify bite timing and fight feel differ between calm and storm/night states.
3. Verify HUD condition indicator updates (`Conditions: <Time> | <Weather>`).
4. Run repeated loop transitions and confirm no action-state regressions.
