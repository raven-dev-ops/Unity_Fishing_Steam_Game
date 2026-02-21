# Assembly Boundaries

## Runtime Assemblies
- `RavenDevOps.Fishing.Core`
  - Folder: `Assets/Scripts/Core/`
  - Scope: foundational services/contracts (`GameFlowManager`, `RuntimeServiceRegistry`, settings, scene loader).
  - Depends on: none (runtime project assemblies).

- `RavenDevOps.Fishing.Save`
  - Folder: `Assets/Scripts/Save/`
  - Scope: save data model, persistence, migration pipeline.
  - Depends on: `Core`.

- `RavenDevOps.Fishing.Data`
  - Folder: `Assets/Scripts/Data/`
  - Scope: content catalogs, addressables pilot loader, mod catalog runtime surface.
  - Depends on: `Core`.

- `RavenDevOps.Fishing.Fishing`
  - Folder: `Assets/Scripts/Fishing/`
  - Scope: fishing loop runtime systems and domain models.
  - Depends on: `Core`, `Save`, `Data`, `Input`, `Audio`, `Systems`.

- `RavenDevOps.Fishing.Economy`
  - Folder: `Assets/Scripts/Economy/`
  - Scope: buy/equip/sell and value calculators.
  - Depends on: `Core`, `Save`, `Data`.

- `RavenDevOps.Fishing.UI`
  - Folder: `Assets/Scripts/UI/`
  - Scope: menu/HUD/controllers and accessibility runtime behavior.
  - Depends on: `Core`, `Save`, `Data`, `Input`, `Audio`, `Systems`, `Tools`.

- `RavenDevOps.Fishing.Steam`
  - Folder: `Assets/Scripts/Steam/`
  - Scope: Steam bootstrap/stats/cloud/rich presence wrappers.
  - Depends on: `Core`, `Save`.

## Supporting Runtime Assemblies
- `RavenDevOps.Fishing.Input` (`Assets/Scripts/Input/`) depends on `Core`.
- `RavenDevOps.Fishing.Audio` (`Assets/Scripts/Audio/`) depends on `Core`, `Data`.
- `RavenDevOps.Fishing.Harbor` (`Assets/Scripts/Harbor/`) depends on `Core`, `Input`, `Save`, `UI`.
- `RavenDevOps.Fishing.Tools` (`Assets/Scripts/Tools/`) depends on `Core`, `Save`, `Data`, `Economy`, `Fishing`.
- `RavenDevOps.Fishing.Performance` (`Assets/Scripts/Performance/`) has no runtime assembly deps.
- `RavenDevOps.Fishing.Systems` (`Assets/Scripts/Systems/`) orchestration layer depending on `Core`, `Input`, `Save`.
- `RavenDevOps.Fishing.Bootstrap` (`Assets/Scripts/Bootstrap/`) composition root depending on runtime feature assemblies.

## Editor and Tests
- `RavenDevOps.Fishing.Editor` references `Data`, `Tools`.
- `RavenDevOps.Fishing.Tests.EditMode` and `RavenDevOps.Fishing.Tests.PlayMode` reference split runtime assemblies.

## Rules
- `Core` is dependency-base and must not depend on gameplay feature assemblies.
- Runtime composition/bootstrap code belongs in `Systems`/`Bootstrap`, not `Core`.
- Avoid cross-feature circular references; use contracts/events from lower layers.
- UI adapters should consume runtime services/contracts instead of being imported directly into gameplay-domain assemblies.
