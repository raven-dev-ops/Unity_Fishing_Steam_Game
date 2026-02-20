# Mod Support Strategy (Post-1.0)

## Decision Summary
- Launch target: **data-only modding** (no arbitrary scripting in first mod phase).
- Distribution baseline: **manual folder-based mod packs** first.
- Steam Workshop integration: optional phase after data-only pipeline stabilizes.

## Scope

### In Scope (Phase 1)
- Modded data catalogs for:
  - fish definitions
  - ship/hook definitions
  - non-code objective presets
- Optional asset overrides for approved sprite/audio folders.
- Local validation pass before content is accepted by runtime.

### Out of Scope (Phase 1)
- Native code plugins.
- Runtime C# script execution.
- Direct access to save internals beyond stable data contracts.

## Security and Risk Boundaries
- Reject executable/script payloads from mod folders.
- Only parse signed/validated JSON or ScriptableObject-exported data bundles.
- Enforce file extension allowlist for mod assets.
- Keep mods in isolated mod directory:
  - `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/Mods`
- Require strict schema validation and ID conflict resolution rules.
- Maintain disable-safe fallback when a mod fails validation.

## Validation Model
1. Discover mod packs from mod directory.
2. Validate manifest + schema version compatibility.
3. Validate content IDs, ranges, and asset references.
4. Build merged runtime catalog with deterministic override order.
5. Log accepted/rejected mods in support-friendly report.

## Distribution Strategy
- Phase 1: manual `.zip`/folder install with clear manifest format.
- Phase 2: optional Steam Workshop publishing/subscribe flow once validation and moderation workflow are mature.
- Keep both channels compatible via the same manifest/schema.

## Lifecycle and Support
- Versioned mod schema with migration guidance.
- Compatibility table per game version.
- Safe-mode startup switch to disable all mods for troubleshooting.

## Follow-up Backlog Proposals
1. `MOD-001` Mod manifest/schema spec + validator implementation.
2. `MOD-002` Runtime mod discovery and merge pipeline.
3. `MOD-003` Mod failure reporting UI + safe mode toggle.
4. `MOD-004` Mod packaging guide and sample templates.
5. `MOD-005` Workshop feasibility spike and publish/subscription flow.
