# Mod Runtime Discovery and Merge

## Scope
Runtime loader for data-only mod packs discovered from:

`%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/Mods`

Implementation files:
- `Assets/Scripts/Core/ModRuntimeCatalogService.cs`
- `Assets/Scripts/Tools/ModRuntimeCatalogLoader.cs`
- `Assets/Scripts/Tools/ModRuntimeCatalogModels.cs`

## Discovery Rules
1. Enumerate direct subdirectories under `Mods`.
2. Require `manifest.json` in each pack root.
3. Validate manifest via `ModManifestValidator`.
4. Validate and parse listed `dataCatalogs` JSON files.
5. Reject invalid packs and continue startup.

## Merge Rules
- Pack application order is deterministic by `manifest.modId` (ordinal sort).
- Later packs override earlier packs on ID conflicts.
- Conflict events are logged as warnings for diagnostics.

Applied catalogs:
- `fishDefinitions`
- `shipDefinitions`
- `hookDefinitions`

## Safe Mode
Disable all mod loading at startup with any of:
- persisted setting: `settings.modSafeModeEnabled=1` (set via in-game settings/profile toggle)
- env: `RAVEN_DISABLE_MODS=1`
- CLI args:
  - `-safeMode=true`
  - `-disableMods=true`
  - `-mods=false`

Priority order:
1. `RAVEN_DISABLE_MODS`
2. safe-mode CLI args
3. persisted setting (`settings.modSafeModeEnabled`)

When safe mode is active, runtime skips mod discovery and uses base catalogs only.
`ModRuntimeCatalogService` exposes `SafeModeActive` + `SafeModeReason` for UI messaging.

## Runtime Behavior
- Valid packs contribute override/additive entries into `CatalogService`.
- Invalid packs are rejected without blocking startup.
- Optional icon paths are only used when declared in manifest `assetOverrides`.
- Runtime diagnostics UI can render accepted/rejected packs and loader messages via:
  - `Assets/Scripts/UI/ModDiagnosticsPanelController.cs`
  - `Assets/Scripts/UI/ModDiagnosticsTextFormatter.cs`

## Tests
- `Assets/Tests/EditMode/ModRuntimeCatalogLoaderTests.cs`
  - deterministic override order
  - reject missing manifest
  - disabled/safe-mode baseline behavior

## Authoring Templates
- Packaging guide: `docs/MOD_PACKAGING_GUIDE.md`
- Sample templates: `mods/templates/`
