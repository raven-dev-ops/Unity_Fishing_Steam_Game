# Steam 1.0 Release Compliance Checklist

## Scope
- Issue: `#211`
- Goal: finalize store/compliance readiness and release metadata ownership for `v1.0.0`.
- Launch localization stance: English-only (`docs/LOCALIZATION_SCOPE_DECISION_2026-02-25.md`).
- Store language metadata target: Interface=English, Subtitles=English, Full Audio=not claimed.
- Age rating/content descriptor decision record: `docs/AGE_RATING_DESCRIPTOR_DECISION_2026-02-25.md`.
- Descriptor evidence bundle root: `release/steam_descriptors/`.

## Store Asset and Copy Status

| Area | Requirement | Status | Owner | Notes |
|---|---|---|---|---|
| Capsule images | Main/header/small/vertical capsule set prepared | PASS | Product | Tracked via `docs/STORE_ASSETS_CHECKLIST.md`; package: `release/steam_store_assets/rc-2026-02-25/` |
| Store screenshots | Current gameplay screenshots available | PASS | Product | Scene capture baseline + curated store set |
| Trailer | Launch trailer optional but recommended | READY | Product | Optional for first publish, non-blocking |
| Library assets | Library capsule/header/hero/logo prepared | PASS | Product | Package lock: `release/steam_store_assets/rc-2026-02-25/export_manifest.lock.json` (`package_sha256=40767fe901c37514d723ec118c63e930251b80a3cbfcac7bc857d4def725346d`) |
| Copy | Short + long description reviewed | PASS | Product | Versioned package: `marketing/steam/store_copy/rc-2026-02-25/`; report: `docs/STORE_COPY_COMPLIANCE_REPORT_2026-02-25.md` |
| Metadata | Genre/tags/controller support reviewed | READY | Product | Runtime alignment evidence: `docs/INPUT_PROMPT_REBIND_QA_REPORT_2026-02-25.md`; partner screenshot bundle required per `release/steam_metadata/<rc-tag>/` |
| System requirements | Minimum/recommended requirements reviewed | PASS | Engineering | Aligned to hardware baseline lock doc |

## Steam Controller Metadata Evidence
- Required RC artifact path: `release/steam_metadata/<rc-tag>/`
- Required files per RC bundle:
  - `manifest.json`
  - `controller_support.png`
  - `steam_input_settings.png`
  - `summary.md`
- Strict validation command (release gate):
  - `./scripts/ci/verify-steam-metadata-evidence.ps1 -EvidenceRoot "release/steam_metadata" -RequireAtLeastOneBundle -RequireAtLeastOnePassingBundle -SummaryJsonPath "Artifacts/SteamMetadata/steam_metadata_evidence_summary.json" -SummaryMarkdownPath "Artifacts/SteamMetadata/steam_metadata_evidence_summary.md"`
- Release workflow enforcement:
  - `.github/workflows/release-steampipe.yml` (`steam_release_metadata_gates` job)
- Policy and drift escalation:
  - `docs/STEAM_CONTROLLER_METADATA_EVIDENCE_POLICY.md`

## Legal and Compliance Status

| Item | Path | Status | Owner | Notes |
|---|---|---|---|---|
| License | `LICENSE` | PASS | Engineering | Repository license present and current |
| Third-party notices | `THIRD_PARTY_NOTICES.md` | PASS | Engineering | Dependency/tool attribution baseline maintained |
| Compliance package manifest | `release/compliance/rc-2026-02-25/compliance_manifest.json` | PASS | Release Ops | Versioned legal/compliance package inventory for RC |
| Legal signoff record | `release/compliance/rc-2026-02-25/legal_signoff.md` | PASS | Release Ops | Rights/attribution/disclosure signoff captured |
| Security disclosure | `SECURITY.md` | PASS | Engineering | Coordinated disclosure process documented |
| Release secret handling | `docs/SECURITY_RELEASE_WORKFLOW.md` | PASS | Release Ops | Protected environment + approval gates documented |
| Privacy baseline | `docs/CRASH_REPORTING.md` | PASS | Engineering | Local-only crash artifact flow documented |
| EULA/privacy disclosure baseline | `docs/EULA_PRIVACY_DISCLOSURE_REQUIREMENTS_2026-02-25.md` | PASS | Product | Launch disclosure requirement baseline versioned |

## Steam Depot/Branch/Release Metadata Alignment

| Control | Status | Source |
|---|---|---|
| Semver tag release path (`v*`) | PASS | `docs/RELEASE_TAGGING.md` |
| SteamPipe dry-run/live path | PASS | `docs/STEAMPIPE_UPLOAD_TEST.md` |
| Steamworks achievements/stats publish contract gate | READY | `scripts/ci/verify-steamworks-achievements-stats.ps1` + `release/steamworks/achievements_stats/backend_contract.json` |
| Protected release environment + reviewers | PASS | `docs/SECURITY_RELEASE_WORKFLOW.md` |
| RC signoff linkage before tag | PASS | `docs/RC_VALIDATION_BUNDLE.md` |

## Final Go/No-Go Checklist

| Gate | Status | Owner | Escalation |
|---|---|---|---|
| Store assets/copy reviewed and not blocked | PASS | Product | Release Ops |
| Legal/compliance docs current | PASS | Engineering | Product |
| Release metadata aligned with tag/runbooks | PASS | Release Ops | Engineering |
| Age rating/content descriptor decision reviewed and current for target RC | PASS | Product | Release Ops |
| Steam controller metadata evidence bundle captured and verified | READY | Release Ops | Product |
| Steamworks achievements/stats backend publish metadata captured and strict contract gate passes | READY | Release Ops | Engineering |
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
- `docs/AGE_RATING_DESCRIPTOR_DECISION_2026-02-25.md`
- `docs/INPUT_PROMPT_REBIND_QA_REPORT_2026-02-25.md`
- `docs/STEAM_CONTROLLER_METADATA_EVIDENCE_POLICY.md`
- `docs/STEAM_CONTROLLER_METADATA_DRIFT_REHEARSAL_REPORT_2026-02-25.md`
- `release/steamworks/achievements_stats/backend_contract.json`
- `docs/STEAMWORKS_ACHIEVEMENTS_STATS_VERIFICATION_REPORT_2026-02-25.md`
- `release/compliance/rc-2026-02-25/compliance_manifest.json`
- `release/compliance/rc-2026-02-25/legal_signoff.md`
- `docs/EULA_PRIVACY_DISCLOSURE_REQUIREMENTS_2026-02-25.md`
- `release/steam_descriptors/`
- `release/steam_metadata/`
- `docs/RELEASE_TAGGING.md`
- `docs/STEAMPIPE_UPLOAD_TEST.md`
- `docs/SECURITY_RELEASE_WORKFLOW.md`
- `docs/RC_VALIDATION_BUNDLE.md`
