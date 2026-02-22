# Addressables Pilot Evaluation

## Scope
Post-1.0 Addressables rollout for scalable content loading beyond MVP eager-catalog approach.

## Pilot Artifacts
- `Assets/Scripts/Data/AddressablesPilotCatalogLoader.cs`
  - Async label-based load path behind `ENABLE_ADDRESSABLES`
  - Resource fallback paths for non-addressables runtime
  - Phase-two audio and environment bundle loading with instrumentation
- `Assets/Scripts/Data/CatalogService.cs`
  - Applies phase-one fish overlay after async pilot load completion
  - Caches phase-two audio/environment lookups and logs source/error details
  - Retains base catalog fallback when pilot load is unavailable/empty
- `Assets/Scripts/Audio/SceneMusicController.cs`
  - Resolves music loops through phase-two audio keys with serialized fallback clips
- `Assets/Scripts/Audio/SfxTriggerRouter.cs`
  - Resolves SFX events through phase-two audio keys with serialized fallback clips
- `Assets/Scripts/Fishing/FishingEnvironmentSliceController.cs`
  - Resolves optional phase-two skybox material key with fallback material behavior
- `Assets/Scripts/Bootstrap/RuntimeServicesBootstrap.cs`
  - Registers pilot loader as global runtime service

## Target Content Set
- Fish definition content set (pilot label: `pilot/fish-definitions`)
- Audio pack content set (phase-two label: `pilot/audio-packs`)
- Environment bundle content set (phase-two label: `pilot/environment-bundles`)

Fallback resource paths:
- Fish: `Resources/Pilot/FishDefinitions`
- Audio: `Resources/Pilot/Audio`
- Environment materials: `Resources/Pilot/Environment/Materials`

## Label/Profile Baseline
Recommended Addressables labels:
- `pilot/fish-definitions`
- `pilot/audio-packs`
- `pilot/environment-bundles`

Recommended profile split:
- `Dev`: local fast iteration, uncached catalog updates.
- `QA`: deterministic content hash and reproducible test captures.
- `Release`: immutable content bundles aligned with release branch tagging.

## Baseline vs Pilot

### Baseline (Current)
- Content loaded via direct catalog references/Resources-driven patterns.
- Simpler setup and fewer build pipeline concerns.
- Cost: startup memory scales with total catalog size.

### Pilot (Addressables)
- Async on-demand content loading by address/label.
- Lower upfront memory pressure for large content sets.
- Added complexity:
  - group/profile/build configuration
  - content update workflow
  - runtime load failure handling

## Preliminary Tradeoff Summary
- **Pros**:
  - better large-catalog scalability
  - improved control over load timing
  - clearer path to DLC/live-content drops
- **Cons**:
  - higher tooling complexity
  - stricter content pipeline discipline required
  - added QA matrix for catalog/update scenarios

## Recommendation
- Adopt Addressables **post-1.0 in phases**:
1. Fish catalog and icon bundles.
2. Audio packs and environment bundles (implemented runtime path + fallback).
3. Additional catalog/audio/environment packs for post-launch content drops.

## Migration Path
1. Install Addressables package and configure profiles/groups.
2. Label pilot fish assets and load through `AddressablesPilotCatalogLoader`.
3. Label phase-two audio/environment assets and verify key mapping in `CatalogService`.
4. Verify catalog overlay path through `CatalogService` (base -> pilot overlay).
5. Validate runtime fallback behavior by disabling Addressables and confirming Resources-based loads.
6. Compare startup time/memory against baseline using profiler captures.
7. Promote pilot path into production catalog service once stability criteria are met.

## Measurement Checklist
1. Capture startup memory baseline before pilot migration.
2. Capture memory after deferring pilot fish set to async load.
3. Capture memory after phase-two audio/environment labels are active.
4. Record load latency and hitch behavior in fishing scene transitions.
5. Document failure mode behavior (missing/invalid label content).

## Failure Handling and Instrumentation
- `AddressablesPilotCatalogLoader` logs source and error for each phase:
  - fish
  - phase-two audio
  - phase-two environment
- `CatalogService` logs normalized completion records including:
  - asset count
  - source (`addressables` vs `fallback`)
  - last loader error code/message
- Consumers retain serialized fallback assets when phase-two keys are missing.

## Baseline vs Phase-2 Metrics Template
Capture and retain these rows in PR/release notes for phase-two rollout changes:

| Date (UTC) | Build Profile | Addressables Mode | Scene/Flow | Startup Memory MB | Scene Transition p95 ms | Notes |
|---|---|---|---|---:|---:|---|
| 2026-02-20 | QA | Baseline (no phase-2 labels) | MainMenu -> Harbor -> Fishing | _fill_ | _fill_ | _machine + resolution_ |
| 2026-02-20 | QA | Phase-2 enabled | MainMenu -> Harbor -> Fishing | _fill_ | _fill_ | _machine + resolution_ |
