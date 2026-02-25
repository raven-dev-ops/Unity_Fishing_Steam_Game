# Sprite Sheet Standard

This is the project baseline for gameplay sprite sheets (fish, ships, hooks, and shop icons).

Use this standard unless a runtime system is explicitly hard-coded to different dimensions.

## Canonical Bundle
- Bundle root: `fishing_sprite_assets_placeholders_and_tools/`
- Contents manifest: `ZIP_CONTENTS.md`
- Packer tool: `fishing_sprite_assets_placeholders_and_tools/tools/spritesheet_packer.py`
- Placeholder sheets + JSON: `fishing_sprite_assets_placeholders_and_tools/placeholders/`
- Reference spec: `fishing_sprite_assets_placeholders_and_tools/docs/sprite_sheet_spec.md`

## Baseline Layouts
Baseline placeholders in this document are tooling references only.
Runtime/player-facing content must not depend on `Assets/Art/Sheets/Fishing/Placeholders/*`.

### Fish
- Sheet: `2048x384`
- Cell: `256x128`
- Grid: `8x3`
- Row order:
  - row 0: `swim` (frames `0-7`)
  - row 1: `caught` (frames `8-15`)
  - row 2: `escape` (frames `16-23`)
- Baseline placeholder:
  - `fishing_sprite_assets_placeholders_and_tools/placeholders/fish_placeholder.png`
  - `fishing_sprite_assets_placeholders_and_tools/placeholders/fish_placeholder.json`

### Ship
- Sheet: `4096x512`
- Cell: `512x512`
- Grid: `8x1`
- Animation: `idle` (frames `0-7`)
- Baseline placeholder:
  - `fishing_sprite_assets_placeholders_and_tools/placeholders/ship_placeholder_idle.png`
  - `fishing_sprite_assets_placeholders_and_tools/placeholders/ship_placeholder_idle.json`

### Hook
- Sheet: `2048x256`
- Cell: `256x256`
- Grid: `8x1`
- Animation: `idle` (frames `0-7`)
- Baseline placeholder:
  - `fishing_sprite_assets_placeholders_and_tools/placeholders/hook_placeholder_idle.png`
  - `fishing_sprite_assets_placeholders_and_tools/placeholders/hook_placeholder_idle.json`

### Shop Icons
- Sheet: `1024x512`
- Cell: `512x512`
- Grid: `2x1`
- Baseline placeholders:
  - ships: `fishing_sprite_assets_placeholders_and_tools/placeholders/shop_icons_ships.png`
  - hooks: `fishing_sprite_assets_placeholders_and_tools/placeholders/shop_icons_hooks.png`

## Naming Conventions
- Fish sheet: `fish_<id>.png` (rows map to `swim/caught/escape`)
- Ship sheet: `ship_<id>_idle.png`
- Hook sheet: `hook_<id>_idle.png`
- Shop icon sheet: `shop_icons_<category>.png`
- Sidecar metadata: same file stem, `.json`

## Packer Workflow
Use either the Python tool directly or the repository wrapper script.

Direct tool usage:
```powershell
python .\fishing_sprite_assets_placeholders_and_tools\tools\spritesheet_packer.py pack `
  --input .\frames\hook\idle `
  --output .\Assets\Art\Sheets\Fishing\hook_basic_idle.png `
  --json .\Assets\Art\Sheets\Fishing\hook_basic_idle.json `
  --columns 8
```

Row-based fish sheet:
```powershell
python .\fishing_sprite_assets_placeholders_and_tools\tools\spritesheet_packer.py pack-rows `
  --rows swim=.\frames\fish\cod\swim caught=.\frames\fish\cod\caught escape=.\frames\fish\cod\escape `
  --output .\Assets\Art\Sheets\Fishing\fish_cod.png `
  --json .\Assets\Art\Sheets\Fishing\fish_cod.json `
  --columns 8
```

Wrapper usage:
```powershell
.\scripts\sprite-sheet-packer.ps1 -Command pack -InputPath .\frames\hook\idle -Output .\Assets\Art\Sheets\Fishing\hook_basic_idle.png -Json .\Assets\Art\Sheets\Fishing\hook_basic_idle.json -Columns 8
```

## Unity Import Baseline
- `Texture Type`: `Sprite (2D and UI)`
- `Sprite Mode`: `Multiple`
- `Alpha Is Transparency`: enabled
- `Mip Maps`: disabled
- `Read/Write`: disabled
- `Wrap Mode`: `Clamp`
- `Filter Mode`: `Bilinear`

Grid slicing baselines:
- fish: `256x128`
- ship: `512x512`
- hook: `256x256`
- shop icons: `512x512`

For broader policy and audit guidance, use `docs/ASSET_IMPORT_STANDARDS.md`.
