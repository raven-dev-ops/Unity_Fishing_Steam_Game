# Steam Store Asset Source + Export Spec

This directory contains the versioned source/export contract for Steam store graphics.

## Files
- `store_asset_spec.json`: canonical export specification (required assets, dimensions, source art, output target).

## Reproducible Export
```powershell
python scripts/ci/export-steam-store-assets.py `
  --spec marketing/steam/store_assets/store_asset_spec.json
```

## Validation
```powershell
python scripts/ci/validate-steam-store-assets.py `
  --spec marketing/steam/store_assets/store_asset_spec.json `
  --summary-json Artifacts/StoreAssets/store_asset_validation_summary.json `
  --summary-md Artifacts/StoreAssets/store_asset_validation_summary.md
```

## Rule Sources (checked 2026-02-25)
- https://partner.steamgames.com/doc/store/assets/standard
- https://partner.steamgames.com/doc/store/assets/libraryassets
- https://partner.steamgames.com/doc/store/assets/rules
