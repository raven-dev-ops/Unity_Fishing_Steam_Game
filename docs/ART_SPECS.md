# Art Specs (MVP)

## MVP Rules
- Use readable silhouettes at 1080p.
- Prioritize function over detail for first pass assets.
- Keep art directional coherence with nautical fantasy tone.

## File Naming
- Format: `<category>_<subject>_<variant>_<vNN>`
- Examples:
  - `fish_cod_common_v01`
  - `ship_lvl1_basic_v01`
  - `hook_lvl2_barbed_v01`

## Pivot Guidance
- Fish sprites: pivot at center.
- Ships: pivot at center-bottom.
- Hooks: pivot at top where line attaches.

## Export Defaults
- Texture format: PNG.
- Source files: keep PSD/ASE in source archive.
- Ensure transparent backgrounds and tight bounds.

## Icon Sheet Workflow
- Author icon source sprites as individual PNGs in `Assets/Art/Placeholders/Icons/<Category>/`.
- Keep icon dimensions consistent per category (placeholder baseline is `512x512`).
- Do not hand-edit generated sheet textures.
- Regenerate sheets/atlases through: `Raven > Art > Rebuild Placeholder Icon Sheets + Atlases`.
- Generated assets are emitted to:
  - `Assets/Art/Sheets/Icons/`
  - `Assets/Art/Atlases/Icons/`

## Import Baseline
- Follow import policy in `docs/ASSET_IMPORT_STANDARDS.md`.
- UI sprites/icons:
  - Mipmaps disabled.
  - Max size constrained to required display resolution.
- Environment textures:
  - Mipmaps enabled.
  - Read/Write disabled unless runtime processing explicitly needs it.
