# Input Prompt Rebind QA Report (2026-02-25)

## Scope
Issue: `#228`  
Goal: verify keyboard/gamepad prompt parity after rebinding and document Steam controller metadata alignment evidence.

## Implementation Coverage
- Added device/composite binding display resolvers in `Assets/Scripts/Input/InputRebindingService.cs`.
- Replaced hardcoded tutorial prompt key labels with binding-aware labels:
  - `Assets/Scripts/Fishing/FishingLoopTutorialController.cs`
- Replaced fishing HUD cast/reel key labels with binding-aware labels:
  - `Assets/Scripts/Fishing/CatchResolver.cs`
- Replaced harbor interact hint key labels with binding-aware labels:
  - `Assets/Scripts/Bootstrap/HarborSceneInteractionRouter.cs`
- Removed fixed-key copy from static instruction text surfaces:
  - `Assets/Scripts/Bootstrap/SceneRuntimeCompositionBootstrap.cs`

## QA Matrix
| Surface | Action Path(s) | Validation Method | Result |
|---|---|---|---|
| Settings bindings row | `Fishing/Action`, `Harbor/Interact`, `UI/Cancel`, `UI/ReturnHarbor` | Rebind in Settings and verify live label refresh | PASS |
| Tutorial hands-on prompts | `Fishing/MoveShip`, `Fishing/MoveHook` | Override composite keyboard bindings and verify prompt label update | PASS |
| Tutorial demo prompts | `Fishing/MoveShip`, `Fishing/MoveHook` | Verify demo copy resolves current labels via same binding source | PASS |
| Fishing HUD cast/reel hints | `Fishing/MoveHook` | Override up/down bindings and verify HUD instruction copy | PASS |
| Harbor interaction hint | `Harbor/Interact` | Override interaction binding and verify hint label | PASS |

## Automation Evidence
- `./scripts/unity-cli.ps1 -Task test-edit -LogFile issue-228-input-prompts-editmode.log -ExtraArgs @('-testFilter','InputActionArchitectureTests|InputPromptBindingLabelTests')`
  - Result: `total=6 passed=6 failed=0`
- `./scripts/unity-cli.ps1 -Task test-play -LogFile issue-228-launch-regression-playmode.log -ExtraArgs @('-testFilter','LaunchPathRegressionPlayModeTests')`
  - Result: `total=6 passed=6 failed=0`
- `./scripts/unity-cli.ps1 -Task test-play -LogFile issue-228-gameplay-regression-playmode.log -ExtraArgs @('-testFilter','GameplayRegressionPlayModeTests')`
  - Result: `total=9 passed=9 failed=0`

## Steam Metadata Alignment Notes
- In-repo release checklist metadata line remains `PASS` for controller support (`docs/STEAM_RELEASE_COMPLIANCE_CHECKLIST.md`).
- Runtime evidence in this pass confirms controller-capable navigation and gameplay prompts remain aligned with binding overrides.
- Steamworks partner-site metadata state is not queryable from this repository automation.

## Limitation and Follow-up
- Limitation: no automated capture of Steamworks controller/Steam Input metadata screenshots from partner portal.
- Follow-up issue: `#245` to require and version metadata screenshot evidence in release artifacts.
