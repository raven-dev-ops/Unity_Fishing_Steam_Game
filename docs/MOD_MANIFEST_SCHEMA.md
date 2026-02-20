# Mod Manifest Schema (v1.0)

## Scope
Defines the baseline manifest format and validator rules for data-only mod packs.

## File Format
- JSON document parsed into `ModManifestV1` (`Assets/Scripts/Tools/ModManifestV1.cs`).
- Validator entrypoint: `ModManifestValidator.Validate(...)` (`Assets/Scripts/Tools/ModManifestValidator.cs`).

## Required Fields
- `schemaVersion`: must be `1.0`.
- `modId`: lowercase stable ID matching `^[a-z0-9_.-]+$`.
- `modVersion`: semver format `MAJOR.MINOR.PATCH`.
- `displayName`: non-empty.

## Optional Fields
- `author`
- `description`
- `minGameVersion` (semver)
- `maxGameVersion` (semver)
- `dataCatalogs` (list of relative paths)
- `assetOverrides` (list of relative paths)

## Validation Rules
1. `schemaVersion` must be supported.
2. `modId` must be unique in runtime load set and pattern-safe.
3. `modVersion`, `minGameVersion`, `maxGameVersion` use semver.
4. If both min/max versions exist, min must be <= max.
5. `dataCatalogs` entries:
   - relative safe paths only
   - allowed extension: `.json`
6. `assetOverrides` entries:
   - relative safe paths only
   - allowed extensions: `.png`, `.jpg`, `.jpeg`, `.wav`, `.ogg`, `.mp3`
7. Duplicate path entries within each list are rejected.

## Example
```json
{
  "schemaVersion": "1.0",
  "modId": "coastal_pack",
  "modVersion": "1.2.0",
  "displayName": "Coastal Pack",
  "author": "Raven Dev Ops",
  "description": "Adds coastal fish and icon/audio overrides.",
  "minGameVersion": "1.0.0",
  "maxGameVersion": "1.5.0",
  "dataCatalogs": [
    "Data/fish_pack.json"
  ],
  "assetOverrides": [
    "Sprites/fish_icon.png",
    "Audio/splash.ogg"
  ]
}
```
