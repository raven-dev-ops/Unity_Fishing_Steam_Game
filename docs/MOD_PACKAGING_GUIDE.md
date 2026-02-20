# Mod Packaging Guide (Data-Only Packs)

## Goal
Provide a reproducible authoring path for valid data-only mod packs that pass runtime validation.

## Baseline Structure
Each mod pack is a folder under:

`%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/Mods`

Example:

```text
Mods/
  coastal_pack/
    manifest.json
    Data/
      catalog.json
    Sprites/
      fish_icon_coastal.png
    Audio/
      splash_coastal.ogg
```

## Required Files
1. `manifest.json` at pack root.
2. At least one JSON file listed in `dataCatalogs` (for example `Data/catalog.json`).

## Authoring Steps
1. Copy templates from `mods/templates/`.
2. Rename `manifest.sample.json` -> `manifest.json`.
3. Rename `Data/catalog.sample.json` -> `Data/catalog.json`.
4. Replace sample IDs and values with your pack values.
5. Ensure all IDs match `^[a-z0-9_]+$` for content entries.
6. If using icon/audio overrides, list paths in `assetOverrides` and place files in matching relative paths.

## Validation Checklist
- `schemaVersion` is `1.0`.
- `modId` matches `^[a-z0-9_.-]+$` and is unique.
- `modVersion` is semver (`MAJOR.MINOR.PATCH`).
- `dataCatalogs` uses relative `.json` paths only.
- `assetOverrides` uses allowed extensions only:
  - `.png`, `.jpg`, `.jpeg`, `.wav`, `.ogg`, `.mp3`
- No unsafe paths (`../`, absolute paths).

## Runtime Behavior Reference
- Discovery and merge rules: `docs/MOD_RUNTIME_MERGE.md`
- Manifest schema rules: `docs/MOD_MANIFEST_SCHEMA.md`

## Troubleshooting
- Pack rejected with missing manifest:
  - verify `manifest.json` exists at mod root.
- Pack rejected for catalog parse:
  - validate JSON syntax and required fields.
- Asset override ignored:
  - ensure path is declared in `assetOverrides` and file exists.
- Pack not loading:
  - check safe mode flags:
    - `RAVEN_DISABLE_MODS=1`
    - `-safeMode=true`
    - `-disableMods=true`
