# Addressables Pilot Evaluation

## Scope
Post-1.0 pilot for scalable content loading beyond MVP eager-catalog approach.

## Pilot Artifacts
- `Assets/Scripts/Data/AddressablesPilotCatalogLoader.cs`
  - Async label-based load path behind `ENABLE_ADDRESSABLES`
  - Resource fallback path for non-addressables runtime

## Target Content Set
- Fish definition content set (pilot label: `pilot/fish-definitions`)
- Chosen because fish catalog growth directly affects startup memory and load behavior.

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
2. Optional audio packs and biome/environment bundles.
3. Mod/UGC-compatible content packaging once validation pipeline exists.

## Migration Path
1. Install Addressables package and configure profiles/groups.
2. Label pilot fish assets and load through `AddressablesPilotCatalogLoader`.
3. Compare startup time/memory against baseline using profiler captures.
4. Promote pilot path into production catalog service once stability criteria are met.

## Measurement Checklist
1. Capture startup memory baseline before pilot migration.
2. Capture memory after deferring pilot fish set to async load.
3. Record load latency and hitch behavior in fishing scene transitions.
4. Document failure mode behavior (missing/invalid label content).
