# Add Content Guide

This guide is designed for non-programmers to add fish/ship/hook content safely.

## 1) Add art/audio assets
- Follow naming patterns in `docs/ART_SPECS.md` and `docs/AUDIO_SPECS.md`.
- Keep pivot conventions consistent for each asset type.

## 2) Create ScriptableObject definitions
- In Unity Project window: `Create > Raven > Fish Definition` (or Ship/Hook).
- Fill in a stable `id` (never change IDs once shipped).
- Set gameplay fields (tiers/depth/price/speed) as needed.

## 3) Register items in GameConfig
- Open your `GameConfigSO` asset.
- Add the new definition into the relevant catalog list.

## 4) Validate catalogs
- Run menu: `Raven > Validate Content Catalog`.
- Resolve all `ERROR` messages before committing.

## 5) Verify in gameplay
- Harbor shops should list new ships/hooks.
- Fishing spawn should include new fish if depth/distance ranges match.

## 6) Save and commit
- Ensure both asset and `.meta` files are committed.
- Keep IDs stable to avoid save migration problems.
