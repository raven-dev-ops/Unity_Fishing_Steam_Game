# Add Content Guide

This guide is for adding fish, ships, and hooks without code changes.

## 1) Add art/audio assets
- Follow naming rules in `docs/ART_SPECS.md` and `docs/AUDIO_SPECS.md`.
- Keep pivots consistent by type (fish center, ship center-bottom, hook top).

## 2) Create ScriptableObject definitions
- Use Unity create menus:
  - `Create > Raven > Fish Definition`
  - `Create > Raven > Ship Definition`
  - `Create > Raven > Hook Definition`
- Use stable `id` values. Do not rename IDs after release.
- Assign `icon` for every definition (validator requires icons).

## 3) Register in `GameConfigSO`
- Open the `GameConfigSO` asset.
- Add new assets to the matching arrays:
  - `fishDefinitions`
  - `shipDefinitions`
  - `hookDefinitions`

## 4) Validate catalogs
- Run Unity menu: `Raven > Validate Content Catalog`.
- Resolve all `ERROR` entries before commit.

## 5) Verify in gameplay
- Harbor shops list and equip new ships/hooks.
- Fishing spawner can roll new fish at valid distance/depth.
- Fish sell flow calculates expected value and updates copecs.

## 6) Save and commit
- Commit both assets and `.meta` files.
- Keep IDs stable to avoid save migration work.
- Run `docs/QA_SMOKE_TEST.md` for regression coverage.
