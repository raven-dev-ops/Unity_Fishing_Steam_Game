# Steam Workshop Feasibility (MOD-005)

## Decision Summary
- Recommendation: **GO for a controlled, data-only Workshop pilot**.
- Recommendation is conditional on keeping the current mod safety boundaries:
  - no executable/script payloads
  - schema-validated JSON catalogs only
  - explicit asset allowlist
- Recommendation is **NO-GO** for arbitrary code mods in post-launch wave 2.

## Goal
Determine whether Steam Workshop can be added without weakening runtime safety, supportability, or deterministic content merge behavior.

## Primary Steamworks API Surface
Reference docs:
- Steam Workshop implementation guide:
  - https://partner.steamgames.com/doc/features/workshop/implementation
- ISteamUGC API reference:
  - https://partner.steamgames.com/doc/api/ISteamUGC

Upload/publish lifecycle:
1. `ISteamUGC::CreateItem`
2. `ISteamUGC::StartItemUpdate`
3. `ISteamUGC::SetItem[...]` (`SetItemContent`, `SetItemPreview`, `SetItemTags`, `SetItemMetadata`, etc.)
4. `ISteamUGC::SubmitItemUpdate`
5. Progress/status via `ISteamUGC::GetItemUpdateProgress`
6. Legal agreement checks:
   - `CreateItemResult_t::m_bUserNeedsToAcceptWorkshopLegalAgreement`
   - Workshop legal agreement flow in implementation guide

Subscription/install lifecycle:
1. Discover subscriptions:
   - `ISteamUGC::GetNumSubscribedItems`
   - `ISteamUGC::GetSubscribedItems`
2. Check state/download:
   - `ISteamUGC::GetItemState` (`EItemState` flags)
   - `ISteamUGC::DownloadItem`
   - `DownloadItemResult_t`
3. Resolve installed content path:
   - `ISteamUGC::GetItemInstallInfo`
   - `ItemInstalled_t`

## Mapping to Current Mod Pipeline
Existing runtime model already aligns well:
- Manifest schema + validation:
  - `Assets/Scripts/Tools/ModManifestV1.cs`
  - `Assets/Scripts/Tools/ModManifestValidator.cs`
- Runtime discovery/merge:
  - `Assets/Scripts/Core/ModRuntimeCatalogService.cs`
  - `Assets/Scripts/Tools/ModRuntimeCatalogLoader.cs`
- Diagnostics + safe mode:
  - `Assets/Scripts/UI/ModDiagnosticsPanelController.cs`
  - `Assets/Scripts/Core/UserSettingsService.cs` (`settings.modSafeModeEnabled`)

Proposed Workshop mapping:
- Installed Workshop item folder is treated as another mod pack root.
- Each item must contain the same `manifest.json` + `dataCatalogs` structure used for manual mods.
- Existing validator remains the single gate before merge.
- Existing deterministic override rules remain unchanged.

## Moderation and Safety Model
### Content Boundaries
- Permit only data-only Workshop packs that pass current schema and path safety checks.
- Block executable/script extensions (`.dll`, `.exe`, `.bat`, `.ps1`, `.js`, etc.).
- Keep current allowed asset types for overrides (`.png`, `.jpg`, `.jpeg`, `.wav`, `.ogg`, `.mp3`).

### Intake and Quarantine
- Newly installed/updated Workshop items load into a quarantined validation pass first.
- Only validated items become eligible for runtime merge.
- Failed items remain disabled and visible in diagnostics UI with explicit reason.

### User Controls
- Per-item enable/disable list in profile/settings (phase 2+).
- Existing global safe mode remains fallback path.
- Clear “source = workshop/manual” labels in diagnostics.

### Moderation Operations
- Require mandatory metadata fields via manifest + Workshop tags:
  - `modId`, `modVersion`, min/max game version, content class tags.
- Add in-game “Report problem with mod” action (links to item page/report workflow).
- Maintain denylist capability (published file IDs) for severe policy violations.

## Feasibility Risks
1. Moderation overhead:
   - Risk: increased support load from broken or abusive content.
   - Mitigation: strict validator + quarantine + denylist + clear rejection reasons.
2. Version compatibility:
   - Risk: mods built for old game versions degrade UX.
   - Mitigation: enforce `minGameVersion` / `maxGameVersion`, show user-facing reason codes.
3. Install/update timing:
   - Risk: items still downloading while game expects content ready.
   - Mitigation: rely on `EItemState` + `DownloadItemResult_t` / `ItemInstalled_t` before activation.
4. Content bloat:
   - Risk: large subscriptions impact disk and startup behavior.
   - Mitigation: size thresholds, lazy activation, explicit per-item toggles.

## Phased Implementation Plan
Effort estimates assume one experienced gameplay/tools engineer plus QA support.

Phase 0: Technical spike (1 week, medium risk)
- Implement read-only subscription discovery and state logging.
- Validate `GetSubscribedItems` + `GetItemInstallInfo` path handoff.
- Exit criteria: stable local enumeration and path-resolution prototype.

Phase 1: Consumer ingest pilot (2 weeks, medium risk)
- Add Workshop source adapter -> validator -> mod runtime loader.
- Quarantine invalid packs; display diagnostics with source labels.
- Exit criteria: subscribed data-only packs load with existing rules; invalid packs safely rejected.

Phase 2: UX controls + telemetry (2 weeks, medium risk)
- Per-item enable/disable controls and source-aware diagnostics.
- Basic metrics: subscribed count, accepted/rejected count, top rejection reasons.
- Exit criteria: users can self-recover from bad subscriptions without external support.

Phase 3: Authoring/publish flow (2-3 weeks, high risk)
- Optional in-tool publish/update helpers around `CreateItem`/`SubmitItemUpdate`.
- Include legal agreement handling and metadata/tag enforcement.
- Exit criteria: internal creator path can publish compliant data-only test items.

Phase 4: Moderation hardening (1-2 weeks, medium risk)
- Denylist and escalation workflow.
- Operational runbook for support/community moderation triage.
- Exit criteria: repeatable response path for abuse/compatibility incidents.

Total projected scope: **8-10 engineering weeks** (not including localization/community staffing).

## Go/No-Go Recommendation
- **GO**:
  - data-only Workshop ingest pilot (Phases 0-2) is technically feasible with current architecture.
  - current manifest validator + runtime safe mode reduce rollout risk significantly.
- **NO-GO (for now)**:
  - script/native-code mods
  - broad creator publish tooling before moderation/runbook foundations are in place.

## Acceptance Criteria Coverage
- Written feasibility report with recommendation: complete.
- Clear phased plan with effort/risk estimates: complete.
