# Architecture Overview

```mermaid
flowchart LR
  Boot[00_Boot] --> Cinematic[01_Cinematic]
  Cinematic --> MainMenu[02_MainMenu]
  MainMenu --> Harbor[03_Harbor]
  Harbor --> Fishing[04_Fishing]
  Fishing --> Harbor

  subgraph Runtime Services
    GS[RuntimeServicesBootstrap]
    GFM[GameFlowManager]
    GFO[GameFlowOrchestrator]
    ICR[InputContextRouter]
    IAMC[InputActionMapController]
    SM[SaveManager]
    OBJ[ObjectivesService]
    UAS[GlobalUiAccessibilityService]
    AM[AudioManager]
    CR[CrashDiagnosticsService]
    ST[SteamBootstrap]
    SS[SteamStatsService]
    SC[SteamCloudSyncService]
    SR[SteamRichPresenceService]
  end

  GS --> GFM
  GS --> GFO
  GS --> ICR
  GS --> IAMC
  GS --> SM
  GS --> OBJ
  GS --> UAS
  GS --> AM
  GS --> CR
  GS --> ST
  GS --> SS
  GS --> SC
  GS --> SR

  GFM --> GFO
  GFO --> Boot
  GFO --> MainMenu
  GFO --> Harbor
  GFO --> Fishing

  Catalog[CatalogService + GameConfigSO] --> FishingSystems[Fishing + Economy systems]
  SM --> UISystems[Menu/Profile/Settings UI]
  AM --> UISystems
```

## Runtime Notes
- `RuntimeServicesBootstrap` creates persistent services before scene load.
- `RuntimeServiceRegistry` provides explicit runtime dependency wiring and replaces scene-search based lookups.
- `GameFlowManager` owns flow state and pause transitions.
- `GameFlowOrchestrator` maps states to scenes and input contexts.
- Input is context-driven through action maps (`UI`, `Harbor`, `Fishing`).
- Input rebinding overrides are persisted via `InputRebindingService`.
- Save and audio services are global and survive scene changes.
- Objectives service tracks non-Steam gameplay goals and persists objective progress/rewards.
- Global UI accessibility service applies persisted UI scaling at runtime.
- Crash diagnostics writes local-only artifact files for exception/error logs.
- UI update pathways prefer event-driven refresh (`SaveDataChanged`, flow state events) over always-on polling for menu/profile/HUD data.
- Fishing combat uses data-driven bite/fight parameters with `FishEncounterModel`.
- Fishing conditions (time/weather) apply modifier layers to fish spawn/fight behavior.
- Catch history is persisted in `SaveDataV1.catchLog` and surfaced in profile UI.
- Progression state (`XP`, `level`, unlock stubs) is persisted in `SaveDataV1.progression`.
- Steam stats/achievements mirror local save stats, Steam Cloud sync follows newest-wins policy, and optional Rich Presence tracks flow state.

## Data/Content Notes
- Fish/ship/hook definitions are ScriptableObject-driven.
- `ContentValidator` enforces ID/range/reference rules pre-merge and in CI.
- Content drops should require no code when extending only data assets.
