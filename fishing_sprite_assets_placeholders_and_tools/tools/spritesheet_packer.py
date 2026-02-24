#!/usr/bin/env python3
"""
spritesheet_packer.py

A small, dependency-light sprite-sheet packer for grid-based sheets.

Typical uses:
  1) Pack a single animation folder into a grid sheet + JSON metadata
  2) Pack multiple animation folders as rows (e.g., swim/caught/escape) into ONE sheet + JSON metadata

Requires: Pillow (PIL)

Examples:

  # Single folder -> auto cell size, 8 columns
  python spritesheet_packer.py pack \
    --input ./frames/swim \
    --output ./out/tuna_swim.png \
    --json   ./out/tuna_swim.json \
    --columns 8

  # Multi-row (swim/caught/escape in one sheet)
  python spritesheet_packer.py pack-rows \
    --rows swim=./frames/swim caught=./frames/caught escape=./frames/escape \
    --output ./out/tuna.png \
    --json   ./out/tuna.json \
    --columns 8

Notes:
- Images are sorted by filename within each folder.
- Cell size defaults to the max width/height of all frames (so nothing is cropped).
- Frames are centered inside each cell.
"""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Dict, List, Tuple

from PIL import Image

SUPPORTED_EXTS = {".png", ".webp", ".jpg", ".jpeg"}

def _list_images(folder: Path) -> List[Path]:
    if not folder.exists() or not folder.is_dir():
        raise FileNotFoundError(f"Folder not found: {folder}")
    files = [p for p in folder.iterdir() if p.suffix.lower() in SUPPORTED_EXTS and p.is_file()]
    files.sort(key=lambda p: p.name)
    if not files:
        raise FileNotFoundError(f"No images found in: {folder}")
    return files

def _ensure_parent(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)

def _compute_cell_size(paths: List[Path]) -> Tuple[int, int]:
    max_w, max_h = 1, 1
    for p in paths:
        with Image.open(p) as im:
            w, h = im.size
            max_w = max(max_w, w)
            max_h = max(max_h, h)
    return max_w, max_h

def _paste_center(sheet: Image.Image, frame: Image.Image, x0: int, y0: int, cell_w: int, cell_h: int) -> Tuple[int,int,int,int]:
    fw, fh = frame.size
    dx = (cell_w - fw) // 2
    dy = (cell_h - fh) // 2
    sheet.alpha_composite(frame, (x0 + dx, y0 + dy))
    return (x0 + dx, y0 + dy, fw, fh)

def pack_single(input_dir: Path, output_png: Path, output_json: Path | None, columns: int | None, margin: int, spacing: int) -> None:
    frames = _list_images(input_dir)
    cell_w, cell_h = _compute_cell_size(frames)
    n = len(frames)
    cols = columns or n
    cols = max(1, cols)
    rows = math.ceil(n / cols)

    sheet_w = margin * 2 + cols * cell_w + (cols - 1) * spacing
    sheet_h = margin * 2 + rows * cell_h + (rows - 1) * spacing
    sheet = Image.new("RGBA", (sheet_w, sheet_h), (0, 0, 0, 0))

    frame_entries = []
    for i, p in enumerate(frames):
        r = i // cols
        c = i % cols
        x = margin + c * (cell_w + spacing)
        y = margin + r * (cell_h + spacing)
        with Image.open(p).convert("RGBA") as im:
            fx, fy, fw, fh = _paste_center(sheet, im, x, y, cell_w, cell_h)
        frame_entries.append({
            "index": i,
            "filename": p.name,
            "cell": {"x": x, "y": y, "w": cell_w, "h": cell_h},
            "frame": {"x": fx, "y": fy, "w": fw, "h": fh},
        })

    _ensure_parent(output_png)
    sheet.save(output_png)

    if output_json:
        _ensure_parent(output_json)
        data = {
            "meta": {
                "image": output_png.name,
                "size": {"w": sheet_w, "h": sheet_h},
                "cell": {"w": cell_w, "h": cell_h},
                "columns": cols,
                "rows": rows,
                "margin": margin,
                "spacing": spacing,
                "source": str(input_dir),
            },
            "animations": {
                "default": {"frames": [e["index"] for e in frame_entries]}
            },
            "frames": frame_entries,
        }
        output_json.write_text(json.dumps(data, indent=2), encoding="utf-8")

def pack_rows(rows_spec: List[str], output_png: Path, output_json: Path | None, columns: int | None, margin: int, spacing: int) -> None:
    # rows_spec items like "swim=./frames/swim"
    row_names: List[str] = []
    row_dirs: List[Path] = []

    for item in rows_spec:
        if "=" not in item:
            raise ValueError(f"Bad --rows entry (expected name=dir): {item}")
        name, dir_s = item.split("=", 1)
        name = name.strip()
        dir_s = dir_s.strip()
        if not name:
            raise ValueError(f"Row name missing in: {item}")
        row_names.append(name)
        row_dirs.append(Path(dir_s))

    row_frames: List[List[Path]] = []
    all_frames: List[Path] = []
    max_len = 0
    for d in row_dirs:
        fr = _list_images(d)
        row_frames.append(fr)
        all_frames.extend(fr)
        max_len = max(max_len, len(fr))

    cell_w, cell_h = _compute_cell_size(all_frames)
    cols = columns or max_len
    cols = max(1, cols)
    rows = len(row_frames)

    sheet_w = margin * 2 + cols * cell_w + (cols - 1) * spacing
    sheet_h = margin * 2 + rows * cell_h + (rows - 1) * spacing
    sheet = Image.new("RGBA", (sheet_w, sheet_h), (0, 0, 0, 0))

    frames_out = []
    animations: Dict[str, Dict[str, List[int]]] = {}

    idx = 0
    for r, (name, frames) in enumerate(zip(row_names, row_frames)):
        animations[name] = {"frames": []}
        for c in range(cols):
            x = margin + c * (cell_w + spacing)
            y = margin + r * (cell_h + spacing)

            if c < len(frames):
                p = frames[c]
                with Image.open(p).convert("RGBA") as im:
                    fx, fy, fw, fh = _paste_center(sheet, im, x, y, cell_w, cell_h)
                frames_out.append({
                    "index": idx,
                    "animation": name,
                    "filename": f"{name}/{p.name}",
                    "cell": {"x": x, "y": y, "w": cell_w, "h": cell_h},
                    "frame": {"x": fx, "y": fy, "w": fw, "h": fh},
                })
                animations[name]["frames"].append(idx)
            else:
                # empty slot (still counts in the grid but not referenced by animation frames)
                frames_out.append({
                    "index": idx,
                    "animation": name,
                    "filename": None,
                    "cell": {"x": x, "y": y, "w": cell_w, "h": cell_h},
                    "frame": None,
                })
            idx += 1

    _ensure_parent(output_png)
    sheet.save(output_png)

    if output_json:
        _ensure_parent(output_json)
        data = {
            "meta": {
                "image": output_png.name,
                "size": {"w": sheet_w, "h": sheet_h},
                "cell": {"w": cell_w, "h": cell_h},
                "columns": cols,
                "rows": rows,
                "margin": margin,
                "spacing": spacing,
                "row_order": row_names,
                "sources": {n: str(d) for n, d in zip(row_names, row_dirs)},
            },
            "animations": animations,
            "frames": frames_out,
        }
        output_json.write_text(json.dumps(data, indent=2), encoding="utf-8")

def main() -> None:
    ap = argparse.ArgumentParser(prog="spritesheet_packer.py")
    sub = ap.add_subparsers(dest="cmd", required=True)

    ap_pack = sub.add_parser("pack", help="Pack a single folder of frames into a grid sheet")
    ap_pack.add_argument("--input", required=True, type=Path, help="Folder containing frames")
    ap_pack.add_argument("--output", required=True, type=Path, help="Output PNG path")
    ap_pack.add_argument("--json", dest="json_path", type=Path, default=None, help="Optional output JSON path")
    ap_pack.add_argument("--columns", type=int, default=None, help="Grid columns (default: all frames in one row)")
    ap_pack.add_argument("--margin", type=int, default=0, help="Outer margin in pixels")
    ap_pack.add_argument("--spacing", type=int, default=0, help="Spacing between cells in pixels")

    ap_rows = sub.add_parser("pack-rows", help="Pack multiple animation folders as rows in ONE sheet")
    ap_rows.add_argument("--rows", nargs="+", required=True, help="Row specs like swim=./swim caught=./caught escape=./escape")
    ap_rows.add_argument("--output", required=True, type=Path, help="Output PNG path")
    ap_rows.add_argument("--json", dest="json_path", type=Path, default=None, help="Optional output JSON path")
    ap_rows.add_argument("--columns", type=int, default=None, help="Grid columns (default: max frames in any row)")
    ap_rows.add_argument("--margin", type=int, default=0, help="Outer margin in pixels")
    ap_rows.add_argument("--spacing", type=int, default=0, help="Spacing between cells in pixels")

    args = ap.parse_args()

    if args.cmd == "pack":
        pack_single(args.input, args.output, args.json_path, args.columns, args.margin, args.spacing)
    elif args.cmd == "pack-rows":
        pack_rows(args.rows, args.output, args.json_path, args.columns, args.margin, args.spacing)
    else:
        raise SystemExit(f"Unknown command: {args.cmd}")

if __name__ == "__main__":
    main()
