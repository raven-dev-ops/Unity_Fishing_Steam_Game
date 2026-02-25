# Steam Store Copy Packages

Versioned storefront copy packages live under this directory by RC identifier.

Current package:
- `marketing/steam/store_copy/rc-2026-02-25/`

Validation command:
```powershell
python scripts/ci/validate-steam-store-copy.py `
  --copy-dir marketing/steam/store_copy/rc-2026-02-25 `
  --summary-json Artifacts/StoreCopy/store_copy_validation_summary.json `
  --summary-md Artifacts/StoreCopy/store_copy_validation_summary.md
```
