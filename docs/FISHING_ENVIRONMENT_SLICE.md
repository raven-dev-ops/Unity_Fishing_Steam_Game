# Fishing Environment Slice

## Runtime Component
- `Assets/Scripts/Fishing/FishingEnvironmentSliceController.cs`

## Baseline Guarantees
- Applies/maintains playable bounds for ship and hook transforms.
- Auto-creates physical boundary colliders around configured play area.
- Applies visual fallback baseline:
  - ensures directional light exists
  - applies fallback skybox if scene skybox is unset

## Integration
- `CatchResolver` auto-attaches this controller in fishing runtime.
- Controller is configured using hook + ship transforms when available.

## QA Checks
1. Run repeated cast/hook/reel loop without leaving playable area.
2. Push ship/hook to edges and confirm clamping/boundary behavior.
3. Build standalone and verify no black scene due to missing directional light/skybox.

## Tunables
- `shipXBounds`
- `hookYBounds`
- `playAreaCenter`
- `playAreaSize`
- `boundaryThickness`
