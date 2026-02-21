# Add Content Guide

This guide is for adding fish, ships, and hooks without code changes.

## 1) Add art/audio assets
- Follow naming rules in `docs/ART_SPECS.md` and `docs/AUDIO_SPECS.md`.
- Keep pivots consistent by type (fish center, ship center-bottom, hook top).
- For icon placeholders, add/update source PNGs in `Assets/Art/Placeholders/Icons/<Category>/`.

## 2) Rebuild sheets + atlases
- Run: `Raven > Art > Rebuild Placeholder Icon Sheets + Atlases`.
- Or batch: `.\scripts\unity-cli.ps1 -Task rebuild-sheets -LogFile rebuild_sheets.log`.
- Confirm updated outputs in:
  - `Assets/Art/Sheets/Icons/`
  - `Assets/Art/Atlases/Icons/`

## 3) Create ScriptableObject definitions
- Use Unity create menus:
  - `Create > Raven > Fish Definition`
  - `Create > Raven > Ship Definition`
  - `Create > Raven > Hook Definition`
- Use stable `id` values. Do not rename IDs after release.
- Assign `icon` for every definition (validator requires icons).

## 4) Register in `GameConfigSO`
- Open the `GameConfigSO` asset.
- Add new assets to the matching arrays:
  - `fishDefinitions`
  - `shipDefinitions`
  - `hookDefinitions`

## 5) Validate catalogs
- Run Unity menu: `Raven > Validate Content Catalog`.
- Resolve all `ERROR` entries before commit.

## 6) Verify in gameplay
- Harbor shops list and equip new ships/hooks.
- Fishing spawner can roll new fish at valid distance/depth.
- Fish sell flow calculates expected value and updates copecs.

## 7) Save and commit
- Commit both assets and `.meta` files.
- Keep IDs stable to avoid save migration work.
- Run `docs/QA_SMOKE_TEST.md` for regression coverage.
