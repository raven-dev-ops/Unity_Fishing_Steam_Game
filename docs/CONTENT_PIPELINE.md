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

## Headless Validation (CI)
Run catalog validation in Unity batch mode:

```powershell
Unity.exe -batchmode -nographics -quit `
  -projectPath "C:\path\to\Unity_Fishing_Steam_Game" `
  -executeMethod RavenDevOps.Fishing.EditorTools.ContentValidatorRunner.ValidateCatalogBatchMode `
  -logFile validate_content.log
```

### CI Gate Policy
- Any validator `ERROR` fails the job and blocks merge.
- `WARN` output is allowed for merge, but should be triaged.
- Manual menu validation and batch validation use the same validator code path.

## Add Fish (No Code) Quick Checklist
1. Create `FishDefinitionSO` and set stable `id`.
2. Set distance/depth range and rarity weight.
3. Register asset in `GameConfigSO.fishDefinitions`.
4. Validate catalog and verify catch/sell flow.
