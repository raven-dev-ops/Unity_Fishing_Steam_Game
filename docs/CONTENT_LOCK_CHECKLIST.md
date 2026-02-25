# Content Lock Checklist (1.0)

## Purpose
- Enforce a repeatable final pass for source-art readiness before 1.0.
- Track replacement ownership, scope exceptions, and validation evidence.

## Source Files
- Art inventory: `Assets/Art/Source/art_manifest.json`
- Replacement/waiver plan: `ci/content-lock-replacements.json`
- Audit script: `scripts/ci/content-lock-audit.ps1`
- CI workflow: `.github/workflows/ci-content-lock-audit.yml`
- Latest report: `docs/CONTENT_LOCK_AUDIT_REPORT_2026-02-24.md`

## Operator Checklist
1. Run content lock audit:
   - `scripts/ci/content-lock-audit.ps1 -FailOnFindings -FailOnActiveWaivers`
2. Confirm unresolved source-art findings are either:
   - replaced and marked complete in `ci/content-lock-replacements.json`, or
   - explicitly scoped out as non-shipping with approval and linked issue evidence.
3. Validate replacement assets follow naming/import standards:
   - `docs/CONTENT_PIPELINE.md`
   - `docs/ASSET_IMPORT_STANDARDS.md`
4. Run Unity content validators when available:
   - `Raven > Validate Content Catalog`
   - `Raven > Validate Asset Import Compliance`
5. Attach audit summaries and validator outputs to release-candidate signoff bundle:
   - `docs/RC_VALIDATION_BUNDLE.md`

## Exception Policy
- Waivers are temporary and must include:
  - `id` (source asset ID or `*`)
  - `owner`
  - `reason`
  - `expires_on`
  - `ticket`
- Waiver policy window is capped at `14` days (`ci/content-lock-replacements.json`).
- Waivers exceeding policy window are warnings.
- Expired or malformed waivers are failures.
- Release path must have zero active content-lock waivers.
- Strict release lock command:
  - `scripts/ci/content-lock-audit.ps1 -FailOnFindings -FailOnActiveWaivers`

## Signoff Fields
| Area | Owner | Status (`PASS`/`WAIVER`/`BLOCKER`) | Evidence Link | Notes |
|---|---|---|---|---|
| Source art replacement audit |  |  |  |  |
| Content validator |  |  |  |  |
| Asset import audit |  |  |  |  |
| Exception review |  |  |  |  |

## Current State (2026-02-24)
- Source art inventory is present and tracked in source control.
- Replacement plan includes per-item `complete` entries with concrete replacement paths for all tracked assets.
- Active waiver list is empty for content lock, with strict CI enforcement enabled.
- Placeholder sheet runtime references are `0` (editor/runtime dependency removed from tutorial sprite library asset).
