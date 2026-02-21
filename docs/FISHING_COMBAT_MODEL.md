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

## Domain Service Split
- Catch-outcome rule logic is extracted into `Assets/Scripts/Fishing/FishingOutcomeDomainService.cs`.
- Encounter progression remains deterministic in `Assets/Scripts/Fishing/FishEncounterModel.cs`.
- `CatchResolver` is adapter/orchestration focused (input/audio/save/HUD wiring + side-effect dispatch).
- HUD interaction uses `IFishingHudOverlay` contract (`Assets/Scripts/Core/IFishingHudOverlay.cs`) rather than direct UI assembly coupling.

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

## Anti-Frustration Assist Layer
- Runtime service: `Assets/Scripts/Fishing/FishingAssistService.cs`
- Current assists:
  - no-bite pity activation after configurable dry streak
  - adaptive hook-window bonus after configurable failure streak
- Guardrails:
  - pity delay scale clamped to `[0.25, 1.0]`
  - adaptive hook-window bonus clamped to `<= 0.75s`
  - configurable cooldown catches after assist activation
- Telemetry events:
  - `save-migration` logs remain in structured logs for save pipeline
  - fishing assist activations are logged under `fishing-assist`

## Test Coverage
- EditMode:
  - `Assets/Tests/EditMode/FishingOutcomeDomainServiceTests.cs`
  - `Assets/Tests/EditMode/FishEncounterModelTests.cs`
  - `Assets/Tests/EditMode/FishingAssistServiceTests.cs`
- PlayMode:
  - `Assets/Tests/PlayMode/CatchResolverIntegrationPlayModeTests.cs`

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
