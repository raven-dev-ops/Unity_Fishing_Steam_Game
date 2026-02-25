# Steamworks Achievements/Stats Contract

- Contract file: `backend_contract.json`
- Purpose: lock runtime key/type definitions to Steamworks backend publish metadata.

## Publish Metadata Update Rules
1. After Steamworks backend publish, set:
   - `backend_publish.steamworks_change_number` (numeric string)
   - `backend_publish.published_at_utc` (`yyyy-MM-ddTHH:mm:ssZ`)
   - `backend_publish.verified_by`
   - `backend_publish.verification_artifacts` (repo-relative screenshots/notes paths)
2. Keep previous contract revisions in git history (do not squash away publish evidence).
3. Run `scripts/ci/verify-steamworks-achievements-stats.ps1` after any key/type or publish metadata change.
4. For release strict gate, run:
   - `powershell -ExecutionPolicy Bypass -File scripts/ci/verify-steamworks-achievements-stats.ps1 -RequirePublishedMetadata -SummaryJsonPath "Artifacts/Steamworks/steamworks_achievements_stats_contract_summary.json" -SummaryMarkdownPath "Artifacts/Steamworks/steamworks_achievements_stats_contract_summary.md"`
