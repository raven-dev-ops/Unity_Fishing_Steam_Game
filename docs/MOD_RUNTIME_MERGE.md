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
- env: `RAVEN_DISABLE_MODS=1`
- CLI args:
  - `-safeMode=true`
  - `-disableMods=true`
  - `-mods=false`

When safe mode is active, runtime skips mod discovery and uses base catalogs only.

## Runtime Behavior
- Valid packs contribute override/additive entries into `CatalogService`.
- Invalid packs are rejected without blocking startup.
- Optional icon paths are only used when declared in manifest `assetOverrides`.

## Tests
- `Assets/Tests/EditMode/ModRuntimeCatalogLoaderTests.cs`
  - deterministic override order
  - reject missing manifest
  - disabled/safe-mode baseline behavior
