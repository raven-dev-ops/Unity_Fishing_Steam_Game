# Localization Scope Decision (2026-02-25)

## Decision
- **Launch scope**: English-only for `v1.0.0`.
- **Approval**: Product owner (`@raven-dev-ops`) approved on `2026-02-25`.

## Store Metadata Alignment
- Steam store language metadata for launch:
  - Interface: English
  - Full Audio: Not claimed
  - Subtitles: English
- Store copy source package:
  - `marketing/steam/store_copy/rc-2026-02-25/`

## Pipeline Expectations
- No multilingual string-table rollout is in 1.0 scope.
- New user-facing strings must default to English and reuse existing text patterns.
- Any post-1.0 language expansion requires a follow-up issue that defines:
  - localization tooling and ownership,
  - QA coverage,
  - store metadata updates before release.

## Fallback Handling
- If untranslated strings are introduced by mistake, fallback behavior is English literal text.
- Release checks treat missing localized variants as non-blocking for 1.0 only because launch scope is English-only.
