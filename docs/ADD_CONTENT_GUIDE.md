# Add Content Guide

This guide is for adding fish, ships, and hooks without code changes.

## 1) Add art/audio assets
- Follow naming rules in `docs/ART_SPECS.md` and `docs/AUDIO_SPECS.md`.
- Use gameplay sheet baseline in `docs/SPRITE_SHEET_STANDARD.md` for fish/ship/hook animation sheets.
- Keep pivots consistent by type (fish center, ship center-bottom, hook top).
- For source icons, add/update PNGs in `Assets/Art/Source/Icons/<Category>/`.
- Optional master source sheet lives at `Assets/Art/Sheets/Source/ui_icons_sheet_4096x2048_v01.png`.
- Placeholder references for slicing and state wiring live in:
  - `fishing_sprite_assets_placeholders_and_tools/placeholders/`

## 2) Pack gameplay sprite sheets (fish/ship/hook)
- Use:
  - `.\scripts\sprite-sheet-packer.ps1`
- Or direct tool:
  - `python .\fishing_sprite_assets_placeholders_and_tools\tools\spritesheet_packer.py`
- Keep generated gameplay sheets in:
  - `Assets/Art/Sheets/Fishing/`

## 3) Rebuild icon sheets + atlases
- Run: `Raven > Art > Rebuild Source Icon Sheets + Atlases`.
- Or batch: `.\scripts\unity-cli.ps1 -Task rebuild-sheets -LogFile rebuild_sheets.log`.
- Confirm updated outputs in:
  - `Assets/Art/Sheets/Icons/`
  - `Assets/Art/Atlases/Icons/`

## 4) Create ScriptableObject definitions
- Use Unity create menus:
  - `Create > Raven > Fish Definition`
  - `Create > Raven > Ship Definition`
  - `Create > Raven > Hook Definition`
- Use stable `id` values. Do not rename IDs after release.
- Assign `icon` for every definition (validator requires icons).

## 5) Register in `GameConfigSO`
- Open the `GameConfigSO` asset.
- Add new assets to the matching arrays:
  - `fishDefinitions`
  - `shipDefinitions`
  - `hookDefinitions`

## 6) Validate catalogs
- Run Unity menu: `Raven > Validate Content Catalog`.
- Resolve all `ERROR` entries before commit.

## 7) Verify in gameplay
- Harbor shops list and equip new ships/hooks.
- Fishing spawner can roll new fish at valid distance/depth.
- Fish sell flow calculates expected value and updates copecs.

## 8) Save and commit
- Commit both assets and `.meta` files.
- Keep IDs stable to avoid save migration work.
- Run `docs/QA_SMOKE_TEST.md` for regression coverage.
