# Steam Store Asset Export Package (`rc-2026-02-25`)

This directory is the immutable export set referenced by the 1.0 RC store-asset signoff.

## Reproducible Source
- Spec: `marketing/steam/store_assets/store_asset_spec.json`
- Export command:
  - `python scripts/ci/export-steam-store-assets.py --spec marketing/steam/store_assets/store_asset_spec.json`
- Validation command:
  - `python scripts/ci/validate-steam-store-assets.py --spec marketing/steam/store_assets/store_asset_spec.json --summary-json Artifacts/StoreAssets/store_asset_validation_summary.json --summary-md Artifacts/StoreAssets/store_asset_validation_summary.md`

## Lock Manifest
- `export_manifest.lock.json`
- Package SHA256: `40767fe901c37514d723ec118c63e930251b80a3cbfcac7bc857d4def725346d`

## Contents
- Required store capsules:
  - `store_capsule_main_1232x706.png`
  - `store_capsule_header_920x430.png`
  - `store_capsule_small_462x174.png`
  - `store_capsule_vertical_748x896.png`
- Required library assets:
  - `library_capsule_600x900.png`
  - `library_header_920x430.png`
  - `library_hero_3840x1240.png`
  - `library_logo_1280x720.png`
- Store screenshots:
  - `screenshots/*.png` (5 files, 1920x1080, 16:9)
