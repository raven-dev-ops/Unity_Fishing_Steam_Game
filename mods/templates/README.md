# Mod Templates

Copy this template directory into your runtime mods root and rename it to your mod ID folder.

Target runtime location:

`%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/Mods/<your_mod_folder>`

## Included Files
- `manifest.sample.json`: manifest schema v1.0 template
- `Data/catalog.sample.json`: data catalog template for fish/ship/hook entries

## Quick Start
1. Copy templates.
2. Rename:
   - `manifest.sample.json` -> `manifest.json`
   - `Data/catalog.sample.json` -> `Data/catalog.json`
3. Update IDs/values and add optional asset files.
4. Keep all paths relative and consistent with `assetOverrides`.
