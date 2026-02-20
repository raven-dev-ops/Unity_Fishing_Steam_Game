# Content Pipeline (MVP)

## Rules
- All fish/ship/hook content is data-driven via ScriptableObjects.
- Use stable string IDs (never display names) for save compatibility.
- Content drops should avoid code changes whenever possible.

## Primary Assets
- Config root: `GameConfigSO`
- Definitions:
  - `FishDefinitionSO`
  - `ShipDefinitionSO`
  - `HookDefinitionSO`
- Input actions: `Assets/Resources/InputActions_Gameplay.inputactions`

## Naming
- ScriptableObjects: `SO_<Type>_<Id>.asset`
- Sprites: `<type>_<id>_<variant>_vNN.png`
- Audio: `<type>_<event>_<variant>_vNN.wav`

## Validation Gate
1. Add new definitions to `GameConfigSO`.
2. Run `Raven > Validate Content Catalog`.
3. Fix any duplicate ID, missing icon, or invalid range errors.
4. Run smoke checks before merge (`docs/QA_SMOKE_TEST.md`).

## Add Fish (No Code) Quick Checklist
1. Create `FishDefinitionSO` and set stable `id`.
2. Set distance/depth range and rarity weight.
3. Register asset in `GameConfigSO.fishDefinitions`.
4. Validate catalog and verify catch/sell flow.
