# fishing_sprite_assets_placeholders_and_tools.zip — Contents

This ZIP is a starter bundle for a 2D fishing game sprite pipeline. It contains:

- A small Python tool to pack frame folders into grid-based sprite sheets.
- A written spec of recommended sheet/icon conventions.
- Placeholder sprite sheets + JSON mappings so you can validate animation/state wiring before you have final art.

## Folder layout

```
tools/
  spritesheet_packer.py

docs/
  sprite_sheet_spec.md

placeholders/
  fish_placeholder.png
  fish_placeholder.json

  ship_placeholder_idle.png
  ship_placeholder_idle.json

  hook_placeholder_idle.png
  hook_placeholder_idle.json

  shop_icons_ships.png
  shop_icons_ships.json

  shop_icons_hooks.png
  shop_icons_hooks.json
```

## tools/spritesheet_packer.py

A dependency-light grid sprite-sheet packer.

What it does:

- `pack`: Packs a single folder of frames into one grid sheet.
- `pack-rows`: Packs multiple folders into one sheet, with one animation per row (useful for `swim/caught/escape`).

Requirements:

- Python 3
- Pillow (PIL)

Install dependency:

```bash
pip install pillow
```

Usage examples:

Single folder → one sheet:

```bash
python tools/spritesheet_packer.py pack \
  --input ./frames/tuna/swim \
  --output ./out/fish_tuna_swim.png \
  --json   ./out/fish_tuna_swim.json \
  --columns 8
```

Multiple animation folders → one sheet (each animation is a row):

```bash
python tools/spritesheet_packer.py pack-rows \
  --rows swim=./frames/tuna/swim caught=./frames/tuna/caught escape=./frames/tuna/escape \
  --output ./out/fish_tuna.png \
  --json   ./out/fish_tuna.json \
  --columns 8
```

Notes:

- Frames are sorted by filename.
- Cell size defaults to the max width/height of the input frames so nothing is cropped.
- Frames are centered inside each cell.
- Optional `--margin` and `--spacing` control padding.

### Packer JSON format (what the tool outputs)

When you pass `--json`, the tool writes metadata including:

- `meta.size`: total sheet pixel size.
- `meta.cell`: grid cell width/height.
- `meta.columns` / `meta.rows`: grid layout.
- `animations`: maps animation name → list of frame indices.
- `frames`: per-frame details (cell rectangle + the actual pasted frame rectangle).

(Your runtime can use either the `cell` grid slicing alone, or the detailed `frames[]` rectangles if you support trimmed content.)

## docs/sprite_sheet_spec.md

A pragmatic recommendation document for:

- Fish sheets (swim/caught/escape)
- Ship sheets
- Hook sheets
- Shop icons (recommended at 512×512)

Use this as a consistent baseline unless your game code already hard-codes different sizes/counts.

## placeholders/

These are *not* final art. They are intentionally simple “stand-in” sheets so you can test:

- sprite slicing
- animation playback
- state changes (swim → caught → escape)
- shop icon display

### placeholders/fish_placeholder.png (+ fish_placeholder.json)

- Sheet size: **2048×384**
- Cell size: **256×128**
- Layout: **8 columns × 3 rows**
  - Row 0: `swim` frames 0–7
  - Row 1: `caught` frames 8–15
  - Row 2: `escape` frames 16–23

`fish_placeholder.json` is a simplified mapping:

- `cell`: cell size
- `columns`, `rows`
- `row_order`: `swim`, `caught`, `escape`
- `animations`: frame indices per animation

### placeholders/ship_placeholder_idle.png (+ ship_placeholder_idle.json)

- Sheet size: **4096×512**
- Cell size: **512×512**
- Layout: **8 columns × 1 row**
- Animation: `idle` frames 0–7

`ship_placeholder_idle.json` includes:

- `cell`, `columns`, `rows`
- `animations.idle.frames`
- `frames[]` list with the cell rectangles per index

### placeholders/hook_placeholder_idle.png (+ hook_placeholder_idle.json)

- Sheet size: **2048×256**
- Cell size: **256×256**
- Layout: **8 columns × 1 row**
- Animation: `idle` frames 0–7

### placeholders/shop_icons_ships.png (+ shop_icons_ships.json)

- Sheet size: **1024×512**
- Cell size: **512×512**
- Layout: **2 columns × 1 row**
- Frames:
  - `ship_basic` (index 0)
  - `ship_fast` (index 1)

### placeholders/shop_icons_hooks.png (+ shop_icons_hooks.json)

- Sheet size: **1024×512**
- Cell size: **512×512**
- Layout: **2 columns × 1 row**
- Frames:
  - `hook_basic` (index 0)
  - `hook_barbed` (index 1)

## Typical Unity import notes (if you’re using Unity)

- Set Texture Type: **Sprite (2D and UI)**
- Sprite Mode: **Multiple**
- Use Sprite Editor → **Slice** → Grid By Cell Size:
  - Fish: 256×128
  - Ship: 512×512
  - Hook: 256×256
  - Icons: 512×512

If your project uses code-driven slicing/animation, use the JSON alongside the sheet.
