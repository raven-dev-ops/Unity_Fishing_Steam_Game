# Store Assets Checklist

## Required Assets
Steam graphical constraints were re-verified on **2026-02-25** using:
- https://partner.steamgames.com/doc/store/assets/standard
- https://partner.steamgames.com/doc/store/assets/libraryassets
- https://partner.steamgames.com/doc/store/assets/rules

Versioned source + export contract:
- Spec: `marketing/steam/store_assets/store_asset_spec.json`
- Source doc: `marketing/steam/store_assets/README.md`
- RC export package: `release/steam_store_assets/rc-2026-02-25/`
- Immutable lock manifest: `release/steam_store_assets/rc-2026-02-25/export_manifest.lock.json`

| Asset | Steam Requirement | Export Path | Validation Result |
|---|---|---|---|
| Main capsule | `1232x706` | `release/steam_store_assets/rc-2026-02-25/store_capsule_main_1232x706.png` | PASS |
| Header capsule | `920x430` | `release/steam_store_assets/rc-2026-02-25/store_capsule_header_920x430.png` | PASS |
| Small capsule | `462x174` | `release/steam_store_assets/rc-2026-02-25/store_capsule_small_462x174.png` | PASS |
| Vertical capsule | `748x896` | `release/steam_store_assets/rc-2026-02-25/store_capsule_vertical_748x896.png` | PASS |
| Library capsule | `600x900` | `release/steam_store_assets/rc-2026-02-25/library_capsule_600x900.png` | PASS |
| Library header | `920x430` | `release/steam_store_assets/rc-2026-02-25/library_header_920x430.png` | PASS |
| Library hero | `3840x1240` | `release/steam_store_assets/rc-2026-02-25/library_hero_3840x1240.png` | PASS |
| Library logo | `1280x720` (transparent) | `release/steam_store_assets/rc-2026-02-25/library_logo_1280x720.png` | PASS |
| Screenshots | at least `5`, minimum `1920x1080`, 16:9 | `release/steam_store_assets/rc-2026-02-25/screenshots/*.png` | PASS |

Validation evidence:
- `docs/STORE_ASSET_VALIDATION_REPORT_2026-02-25.md`
- `release/steam_store_assets/rc-2026-02-25/export_manifest.lock.json`

## Copy Checklist
- Versioned copy package:
  - `marketing/steam/store_copy/rc-2026-02-25/`
- Compliance results:
  - URL/link prohibition: PASS
  - Faux-UI phrase prohibition: PASS
  - Embedded image markup prohibition: PASS
  - Screenshot payload alignment (`5` screenshots): PASS
  - Trailer/GIF claim alignment (none claimed): PASS
- Reviewer/date signoff:
  - `marketing/steam/store_copy/rc-2026-02-25/review_checklist.md`
- Validation evidence:
  - `docs/STORE_COPY_COMPLIANCE_REPORT_2026-02-25.md`

## Review Gates
Graphical rule gates (Steam):
- Capsules and screenshots do not include discount claims or review score text.
- Capsules and screenshots do not include platform badges.
- Capsule text/logo content is limited to game title/official subtitle only.
- Existing in-game UI text was reviewed for legibility after export scaling/cropping.

Process gates:
- Export set is generated from versioned spec (`store_asset_spec.json`) and committed package output.
- RC signoff references immutable package hash in `export_manifest.lock.json`.
- Ownership and escalation are documented in `docs/STEAM_RELEASE_COMPLIANCE_CHECKLIST.md`.
