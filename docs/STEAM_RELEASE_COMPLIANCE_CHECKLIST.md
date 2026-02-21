# Steam 1.0 Release Compliance Checklist

## Scope
- Issue: `#211`
- Goal: finalize store/compliance readiness and release metadata ownership for `v1.0.0`.

## Store Asset and Copy Status

| Area | Requirement | Status | Owner | Notes |
|---|---|---|---|---|
| Capsule images | Main/header/small capsule set prepared | PASS | Product | Tracked via `docs/STORE_ASSETS_CHECKLIST.md` |
| Store screenshots | Current gameplay screenshots available | PASS | Product | Scene capture baseline + curated store set |
| Trailer | Launch trailer optional but recommended | READY | Product | Optional for first publish, non-blocking |
| Library assets | Library hero/logo assets prepared | PASS | Product | Matches current visual baseline |
| Copy | Short + long description reviewed | PASS | Product | Copy consistency check completed |
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
- `docs/RELEASE_TAGGING.md`
- `docs/STEAMPIPE_UPLOAD_TEST.md`
- `docs/SECURITY_RELEASE_WORKFLOW.md`
- `docs/RC_VALIDATION_BUNDLE.md`
