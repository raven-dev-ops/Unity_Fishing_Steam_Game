# Assembly Boundaries

## Assemblies
- `RavenDevOps.Fishing.Runtime`
  - Scope: Runtime gameplay code under `Assets/Scripts/`
  - References: `Unity.InputSystem`, `Unity.TextMeshPro`

- `RavenDevOps.Fishing.Editor`
  - Scope: Editor-only tools under `Assets/Editor/`
  - References: `RavenDevOps.Fishing.Runtime`
  - Platform: Editor only

- `RavenDevOps.Fishing.Tests.EditMode`
  - Scope: EditMode tests under `Assets/Tests/EditMode/`
  - References: `RavenDevOps.Fishing.Runtime`, `RavenDevOps.Fishing.Editor`
  - Platform: Editor only
  - Optional Unity refs: `TestAssemblies`

- `RavenDevOps.Fishing.Tests.PlayMode`
  - Scope: PlayMode tests under `Assets/Tests/PlayMode/`
  - References: `RavenDevOps.Fishing.Runtime`
  - Platform: Editor (PlayMode test execution)
  - Optional Unity refs: `TestAssemblies`

## Rules
- Runtime code must not reference editor-only assemblies.
- Editor and test code may reference runtime assembly.
- Keep new gameplay scripts in runtime assembly paths unless explicitly editor/test-only.
