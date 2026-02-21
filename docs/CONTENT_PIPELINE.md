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

## Sprite Sheet + SpriteAtlas Workflow
- Source icon art lives under `Assets/Art/Source/Icons/**`.
- Optional canonical source sheet + rects:
  - `Assets/Art/Sheets/Source/ui_icons_sheet_4096x2048_v01.png`
  - `Assets/Art/Sheets/Source/ui_icons_sheet_4096x2048_v01_rects.json`
- Run Unity menu command: `Raven > Art > Rebuild Source Icon Sheets + Atlases`.
- Batch entrypoint: `RavenDevOps.Fishing.EditorTools.SpriteSheetAtlasWorkflow.RebuildSheetsAndAtlasesBatchMode`.
- CLI wrapper: `.\scripts\unity-cli.ps1 -Task rebuild-sheets -LogFile rebuild_sheets.log`.
- Generated outputs:
  - Sheets: `Assets/Art/Sheets/Icons/icons_<category>_sheet_v01.png`
  - Atlases: `Assets/Art/Atlases/Icons/icons_<category>.spriteatlas`
- Rebuild is deterministic from `Assets/Art/Source/art_manifest.json` and cleans stale generated sheet/atlas assets.

## Validation Gate
1. Add new definitions to `GameConfigSO`.
2. Rebuild sprite sheets and atlases (`Raven > Art > Rebuild Source Icon Sheets + Atlases`).
3. Run `Raven > Validate Content Catalog`.
4. Run `Raven > Validate Asset Import Compliance`.
5. Apply texture/audio import settings per `docs/ASSET_IMPORT_STANDARDS.md`.
6. Fix any duplicate ID, missing icon, invalid range, or import audit warnings.
7. Run smoke checks before merge (`docs/QA_SMOKE_TEST.md`).

## 1.0 Content Lock
- Content lock checklist: `docs/CONTENT_LOCK_CHECKLIST.md`
- Source art inventory: `Assets/Art/Source/art_manifest.json`
- Replacement/waiver tracking: `ci/content-lock-replacements.json`
- Automated audit:
  - script: `scripts/ci/content-lock-audit.ps1`
  - workflow: `.github/workflows/ci-content-lock-audit.yml`

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
- Import audit runs in fail-on-warning mode (with explicit allowlist support) and uploads audit report artifacts.
- Allowed exceptions must be tracked in `ci/asset-import-warning-allowlist.json` with owner/reason/expiry.
- Manual menu validation and batch validation use shared validator/audit code paths.

## Post-1.0 Addressables Pilot
- Reference: `docs/ADDRESSABLES_PILOT.md`
- Pilot loader artifact: `Assets/Scripts/Data/AddressablesPilotCatalogLoader.cs`
- Runtime overlay integration: `Assets/Scripts/Data/CatalogService.cs` (base catalog -> pilot fish overlay -> mod overrides)

## Add Fish (No Code) Quick Checklist
1. Create `FishDefinitionSO` and set stable `id`.
2. Set distance/depth range and rarity weight.
3. Register asset in `GameConfigSO.fishDefinitions`.
4. Validate catalog and verify catch/sell flow.
