# Store Copy Compliance Report (2026-02-25)

## Scope
- Issue: `#224`
- Goal: validate Steam store copy compliance and version final storefront text for RC signoff.

## Versioned Copy Source
- Copy package: `marketing/steam/store_copy/rc-2026-02-25/`
- Files:
  - `short_description.txt`
  - `long_description.md`
  - `metadata_copy.json`
  - `media_payload.json`
  - `review_checklist.md`

## Validation Command
```powershell
python scripts/ci/validate-steam-store-copy.py `
  --copy-dir marketing/steam/store_copy/rc-2026-02-25 `
  --summary-json Artifacts/StoreCopy/store_copy_validation_summary.json `
  --summary-md Artifacts/StoreCopy/store_copy_validation_summary.md
```

## Validation Result
- Status: **PASS**
- Short description length gate: PASS (`135/300`)
- URL/link prohibition gate: PASS
- Faux-UI phrase gate: PASS
- Embedded image/tag gate: PASS
- Screenshot payload alignment gate: PASS (`5/5` screenshots present)
- Trailer/GIF claim alignment gate: PASS (no trailer/GIF claims in text and payload)

## Reviewer Signoff
- Reviewer: `raven-dev-ops`
- Date (UTC): `2026-02-25`
- Checklist source: `marketing/steam/store_copy/rc-2026-02-25/review_checklist.md`
