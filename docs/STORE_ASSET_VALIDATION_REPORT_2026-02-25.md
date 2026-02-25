# Store Asset Validation Report (2026-02-25)

## Scope
- Issue: `#223`
- Goal: validate required Steam store graphical assets and lock a reproducible RC export set.

## Steam Rule Sources
- https://partner.steamgames.com/doc/store/assets/standard
- https://partner.steamgames.com/doc/store/assets/libraryassets
- https://partner.steamgames.com/doc/store/assets/rules

## Source + Export Contract
- Source spec: `marketing/steam/store_assets/store_asset_spec.json`
- Export script: `scripts/ci/export-steam-store-assets.py`
- Validation script: `scripts/ci/validate-steam-store-assets.py`
- RC package: `release/steam_store_assets/rc-2026-02-25/`
- Lock manifest: `release/steam_store_assets/rc-2026-02-25/export_manifest.lock.json`

## Validation Commands
```powershell
python scripts/ci/export-steam-store-assets.py --spec marketing/steam/store_assets/store_asset_spec.json
python scripts/ci/validate-steam-store-assets.py `
  --spec marketing/steam/store_assets/store_asset_spec.json `
  --summary-json Artifacts/StoreAssets/store_asset_validation_summary.json `
  --summary-md Artifacts/StoreAssets/store_asset_validation_summary.md
```

## Validation Result
- Status: **PASS**
- Required capsule/library assets: **8/8 PASS**
- Screenshot gate (5 files, minimum 1920x1080 16:9): **PASS**
- Library logo transparency gate: **PASS**

## Immutable Package Reference
- Package SHA256: `40767fe901c37514d723ec118c63e930251b80a3cbfcac7bc857d4def725346d`
- Source revision captured in lock manifest at generation time.
