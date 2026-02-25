# EULA and Privacy Disclosure Requirements (2026-02-25)

## Scope
- Issue: `#232`
- Release scope: Steam desktop launch package.

## EULA Baseline Requirements
- Include end-user usage terms in store/release materials.
- Disclose license grant scope, prohibited use, warranty disclaimer, and limitation of liability.
- Ensure EULA version/date is tracked in release compliance package.

## Privacy Disclosure Baseline Requirements
- Disclose what telemetry/data is produced by the title in current release scope.
- Current baseline behavior:
  - local save data stored in platform-local profile path,
  - local crash diagnostics artifacts (`docs/CRASH_REPORTING.md`),
  - optional Steam platform integration governed by Steamworks/Valve services.
- If any new analytics/remote telemetry is introduced, privacy disclosure must be updated before release.

## Artifact Linkage
- Compliance package manifest:
  - `release/compliance/rc-2026-02-25/compliance_manifest.json`
- Legal signoff:
  - `release/compliance/rc-2026-02-25/legal_signoff.md`

## Owner and Review
- Owner: `@raven-dev-ops`
- Reviewed at (UTC): `2026-02-25T00:00:00Z`
- Reassessment triggers:
  - new third-party SDK/data flow,
  - new platform/legal policy requirements,
  - launch-region expansion requiring disclosure changes.
