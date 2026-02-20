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
    AM[AudioManager]
  end

  GS --> GFM
  GS --> GFO
  GS --> ICR
  GS --> IAMC
  GS --> SM
  GS --> AM

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
- Save and audio services are global and survive scene changes.

## Data/Content Notes
- Fish/ship/hook definitions are ScriptableObject-driven.
- `ContentValidator` enforces ID/range/reference rules pre-merge and in CI.
- Content drops should require no code when extending only data assets.
