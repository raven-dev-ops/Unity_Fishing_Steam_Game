# Content Lock Audit Report (2026-02-24)

## Scope
- Issue: `#220`
- Inputs:
  - `Assets/Art/Source/art_manifest.json`
  - `ci/content-lock-replacements.json`
  - `Assets/Art/Sheets/Fishing/Placeholders/*` runtime-reference scan

## Audit Execution
- Command:
  - `scripts/ci/content-lock-audit.ps1 -ArtManifestPath "Assets/Art/Source/art_manifest.json" -ReplacementPlanPath "ci/content-lock-replacements.json" -FailOnFindings -FailOnActiveWaivers -SummaryJsonPath "Artifacts/ContentLock/content_lock_summary.json" -SummaryMarkdownPath "Artifacts/ContentLock/content_lock_summary.md"`
- Result: `PASSED`
- Summary artifact:
  - `Artifacts/ContentLock/content_lock_summary.json`
  - `Artifacts/ContentLock/content_lock_summary.md`

## Findings Snapshot
- Source art entries tracked: `36`
- Replacement entries tracked: `36` (all `complete`)
- Active waivers: `0`
- Failures: `0`
- Warnings: `0`

## Placeholder Runtime Dependency Delta
- Placeholder sheet assets scanned: `10`
- Before remediation:
  - runtime reference files: `1`
  - file: `Assets/Resources/Pilot/Tutorial/SO_TutorialSpriteLibrary.asset`
- After remediation:
  - runtime reference files: `0`

## Policy Outcomes
- Content-lock waiver policy normalized to `14` days (`ci/content-lock-replacements.json`).
- CI content-lock workflow now runs in strict mode:
  - `-FailOnFindings`
  - `-FailOnActiveWaivers`
- Tutorial sprite library bootstrap/asset references now resolve source-sheet sprites, not placeholder sheets.
