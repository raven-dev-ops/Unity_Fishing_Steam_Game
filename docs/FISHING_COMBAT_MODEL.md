# Fishing Combat Model (MVP)

## Core Flow
1. Player casts (`Cast -> InWater`).
2. Fish bite timing uses per-fish bite window:
   - `minBiteDelaySeconds`
   - `maxBiteDelaySeconds`
3. On bite (`InWater -> Hooked`), player has a hook reaction window.
4. Reel phase (`Hooked -> Reel`) uses per-fish combat parameters:
   - `fightStamina`
   - `pullIntensity`
   - `escapeSeconds`
5. Resolver sets `Resolve` with success/fail outcome and returns to `Cast`.

## Tension States
- `Safe`
- `Warning`
- `Critical`

`Critical` sustained state can trigger `LineSnap` fail.

## Failure Reasons
- `MissedHook`
- `LineSnap`
- `FishEscaped`

Failure reason text is surfaced in HUD feedback and written to catch log entries.

## Data Authoring Fields
Fish data fields (SO + runtime definition):
- Distance/depth filters
- Rarity/value
- Bite window
- Fight stamina
- Pull intensity
- Escape window
- Catch weight range (`minCatchWeightKg`, `maxCatchWeightKg`)

## Camera and Input Stability
- Fishing camera auto-attaches `FishingCameraController` to main camera at runtime.
- Camera tracks ship + hook with bounded smoothing and orthographic size adaptation.
- Ship/hook movement apply input deadzone + axis smoothing for controller stability.
