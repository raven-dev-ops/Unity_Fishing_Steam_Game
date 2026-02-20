# Content Pipeline (MVP)

## Rules
- All fish/ship/hook content is data-driven via ScriptableObjects.
- Use stable string IDs (never display names) for save compatibility.
- Content drops should avoid code changes whenever possible.
- Follow import optimization baseline in `docs/ASSET_IMPORT_STANDARDS.md`.

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
3. Run `Raven > Validate Asset Import Compliance`.
4. Apply texture/audio import settings per `docs/ASSET_IMPORT_STANDARDS.md`.
5. Fix any duplicate ID, missing icon, invalid range, or import audit warnings.
6. Run smoke checks before merge (`docs/QA_SMOKE_TEST.md`).

## Headless Validation (CI)
Run catalog validation in Unity batch mode:

```powershell
Unity.exe -batchmode -nographics -quit `
  -projectPath "C:\path\to\Unity_Fishing_Steam_Game" `
  -executeMethod RavenDevOps.Fishing.EditorTools.ContentValidatorRunner.ValidateCatalogBatchMode `
  -logFile validate_content.log
```

Project wrapper (recommended):

```powershell
.\scripts\unity-cli.ps1 -Task validate -LogFile validate_content.log
```

Run asset import audit in Unity batch mode:

```powershell
Unity.exe -batchmode -nographics -quit `
  -projectPath "C:\path\to\Unity_Fishing_Steam_Game" `
  -executeMethod RavenDevOps.Fishing.EditorTools.AssetImportComplianceRunner.ValidateAssetImportsBatchMode `
  -logFile validate_imports.log
```

### CI Gate Policy
- Any validator `ERROR` fails the job and blocks merge.
- Import audit runs in warning-first mode and uploads audit report artifacts.
- `WARN` output is allowed for merge, but should be triaged.
- Manual menu validation and batch validation use shared validator/audit code paths.

## Post-1.0 Addressables Pilot
- Reference: `docs/ADDRESSABLES_PILOT.md`
- Pilot loader artifact: `Assets/Scripts/Data/AddressablesPilotCatalogLoader.cs`

## Add Fish (No Code) Quick Checklist
1. Create `FishDefinitionSO` and set stable `id`.
2. Set distance/depth range and rarity weight.
3. Register asset in `GameConfigSO.fishDefinitions`.
4. Validate catalog and verify catch/sell flow.
