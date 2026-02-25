# Steam 1.0 Release Compliance Checklist

## Scope
- Issue: `#211`
- Goal: finalize store/compliance readiness and release metadata ownership for `v1.0.0`.
- Launch localization stance: English-only (`docs/LOCALIZATION_SCOPE_DECISION_2026-02-25.md`).
- Store language metadata target: Interface=English, Subtitles=English, Full Audio=not claimed.

## Store Asset and Copy Status

| Area | Requirement | Status | Owner | Notes |
|---|---|---|---|---|
| Capsule images | Main/header/small/vertical capsule set prepared | PASS | Product | Tracked via `docs/STORE_ASSETS_CHECKLIST.md`; package: `release/steam_store_assets/rc-2026-02-25/` |
| Store screenshots | Current gameplay screenshots available | PASS | Product | Scene capture baseline + curated store set |
| Trailer | Launch trailer optional but recommended | READY | Product | Optional for first publish, non-blocking |
| Library assets | Library capsule/header/hero/logo prepared | PASS | Product | Package lock: `release/steam_store_assets/rc-2026-02-25/export_manifest.lock.json` (`package_sha256=40767fe901c37514d723ec118c63e930251b80a3cbfcac7bc857d4def725346d`) |
| Copy | Short + long description reviewed | PASS | Product | Versioned package: `marketing/steam/store_copy/rc-2026-02-25/`; report: `docs/STORE_COPY_COMPLIANCE_REPORT_2026-02-25.md` |
| Metadata | Genre/tags/controller support reviewed | PASS | Product | Matches current feature set |
| System requirements | Minimum/recommended requirements reviewed | PASS | Engineering | Aligned to hardware baseline lock doc |

## Legal and Compliance Status

| Item | Path | Status | Owner | Notes |
|---|---|---|---|---|
| License | `LICENSE` | PASS | Engineering | Repository license present and current |
| Third-party notices | `THIRD_PARTY_NOTICES.md` | PASS | Engineering | Dependency/tool attribution baseline maintained |
| Security disclosure | `SECURITY.md` | PASS | Engineering | Coordinated disclosure process documented |
| Release secret handling | `docs/SECURITY_RELEASE_WORKFLOW.md` | PASS | Release Ops | Protected environment + approval gates documented |
| Privacy baseline | `docs/CRASH_REPORTING.md` | PASS | Engineering | Local-only crash artifact flow documented |

## Steam Depot/Branch/Release Metadata Alignment

| Control | Status | Source |
|---|---|---|
| Semver tag release path (`v*`) | PASS | `docs/RELEASE_TAGGING.md` |
| SteamPipe dry-run/live path | PASS | `docs/STEAMPIPE_UPLOAD_TEST.md` |
| Protected release environment + reviewers | PASS | `docs/SECURITY_RELEASE_WORKFLOW.md` |
| RC signoff linkage before tag | PASS | `docs/RC_VALIDATION_BUNDLE.md` |

## Final Go/No-Go Checklist

| Gate | Status | Owner | Escalation |
|---|---|---|---|
| Store assets/copy reviewed and not blocked | PASS | Product | Release Ops |
| Legal/compliance docs current | PASS | Engineering | Product |
| Release metadata aligned with tag/runbooks | PASS | Release Ops | Engineering |
| Ownership/escalation contacts confirmed | PASS | Release Ops | Repository owner |

## Ownership and Escalation
- Product owner: store copy/assets readiness and final Steam page review.
- Engineering owner: code + compliance docs + release integrity checks.
- Release Ops owner: release workflow execution, approvals, provenance, SteamPipe upload controls.
- Escalation order:
  1. Release Ops
  2. Engineering
  3. Repository owner (`@raven-dev-ops`)

## Evidence Links
- `docs/STORE_ASSETS_CHECKLIST.md`
- `docs/STORE_ASSET_VALIDATION_REPORT_2026-02-25.md`
- `docs/STORE_COPY_COMPLIANCE_REPORT_2026-02-25.md`
- `docs/LOCALIZATION_SCOPE_DECISION_2026-02-25.md`
- `docs/RELEASE_TAGGING.md`
- `docs/STEAMPIPE_UPLOAD_TEST.md`
- `docs/SECURITY_RELEASE_WORKFLOW.md`
- `docs/RC_VALIDATION_BUNDLE.md`
