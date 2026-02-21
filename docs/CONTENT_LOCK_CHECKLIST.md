# Content Lock Checklist (1.0)

## Purpose
- Enforce a repeatable final pass for placeholder elimination and content readiness before 1.0.
- Track replacement ownership, waiver scope, and validation evidence.

## Source Files
- Placeholder inventory: `Assets/Art/Placeholders/placeholder_manifest.json`
- Replacement/waiver plan: `ci/content-lock-replacements.json`
- Audit script: `scripts/ci/content-lock-audit.ps1`
- CI workflow: `.github/workflows/ci-content-lock-audit.yml`
- Latest report: `docs/CONTENT_LOCK_AUDIT_REPORT_2026-02-21.md`

## Operator Checklist
1. Run content lock audit:
   - `scripts/ci/content-lock-audit.ps1`
2. Confirm unresolved placeholder findings are either:
   - replaced and marked complete in `ci/content-lock-replacements.json`, or
   - covered by active waiver entries with owner/reason/ticket/expiry.
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
  - `id` (placeholder ID or `*`)
  - `owner`
  - `reason`
  - `expires_on`
  - `ticket`
- Waivers exceeding policy window are warnings.
- Expired or malformed waivers are failures.
- For final release lock, run strict mode:
  - `scripts/ci/content-lock-audit.ps1 -FailOnFindings`

## Signoff Fields
| Area | Owner | Status (`PASS`/`WAIVER`/`BLOCKER`) | Evidence Link | Notes |
|---|---|---|---|---|
| Placeholder replacement audit |  |  |  |  |
| Content validator |  |  |  |  |
| Asset import audit |  |  |  |  |
| Exception review |  |  |  |  |

## Current State (2026-02-21)
- Placeholder inventory is present and tracked in source control.
- Replacement plan includes per-item deferred entries (owner + target date) for all tracked placeholders.
- Active waiver remains in place for defer mode while Unity-gated validator execution is pending.
