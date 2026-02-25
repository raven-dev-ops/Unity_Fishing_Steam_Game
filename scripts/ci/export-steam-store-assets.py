#!/usr/bin/env python3
"""Export deterministic Steam store assets from in-repo source art."""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
from typing import Dict, List

from PIL import Image, ImageDraw, ImageFont


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Export Steam store assets from store_asset_spec.json")
    parser.add_argument(
        "--spec",
        default="marketing/steam/store_assets/store_asset_spec.json",
        help="Path to store asset spec JSON.",
    )
    parser.add_argument(
        "--output-dir",
        default="",
        help="Optional output directory override.",
    )
    return parser.parse_args()


def ensure_parent(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)


def cover_resize_crop(image: Image.Image, target_width: int, target_height: int) -> Image.Image:
    if target_width <= 0 or target_height <= 0:
        raise ValueError(f"Invalid target size {target_width}x{target_height}.")

    source_width, source_height = image.size
    if source_width <= 0 or source_height <= 0:
        raise ValueError(f"Invalid source image size {source_width}x{source_height}.")

    scale = max(target_width / source_width, target_height / source_height)
    resized_width = int(round(source_width * scale))
    resized_height = int(round(source_height * scale))
    resized = image.resize((resized_width, resized_height), Image.Resampling.LANCZOS)

    left = max(0, (resized_width - target_width) // 2)
    top = max(0, (resized_height - target_height) // 2)
    right = left + target_width
    bottom = top + target_height
    return resized.crop((left, top, right, bottom))


def resolve_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        "arial.ttf",
        "Arial.ttf",
        "DejaVuSans-Bold.ttf",
        "LiberationSans-Bold.ttf",
    ]
    for candidate in candidates:
        try:
            return ImageFont.truetype(candidate, size=size)
        except OSError:
            continue

    return ImageFont.load_default()


def render_transparent_logo(width: int, height: int, text: str) -> Image.Image:
    image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    font = resolve_font(size=max(42, min(width // 9, height // 3)))
    stroke = max(1, width // 320)

    bbox = draw.textbbox((0, 0), text, font=font, stroke_width=stroke)
    text_width = bbox[2] - bbox[0]
    text_height = bbox[3] - bbox[1]
    x = (width - text_width) // 2
    y = (height - text_height) // 2

    draw.text(
        (x, y),
        text,
        font=font,
        fill=(245, 250, 255, 255),
        stroke_width=stroke,
        stroke_fill=(5, 20, 34, 220),
    )
    return image


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        while True:
            chunk = handle.read(1024 * 1024)
            if not chunk:
                break
            digest.update(chunk)
    return digest.hexdigest()


def detect_git_revision(repo_root: Path) -> str:
    head_path = repo_root / ".git" / "HEAD"
    if not head_path.exists():
        return "unknown"

    try:
        head = head_path.read_text(encoding="utf-8").strip()
    except OSError:
        return "unknown"

    if head.startswith("ref: "):
        ref = head[5:].strip()
        ref_path = repo_root / ".git" / ref
        if ref_path.exists():
            try:
                return ref_path.read_text(encoding="utf-8").strip()[:40]
            except OSError:
                return "unknown"
    return head[:40] if head else "unknown"


def export_assets(spec_path: Path, output_root: Path) -> Dict:
    spec = json.loads(spec_path.read_text(encoding="utf-8"))
    source = spec["source"]
    keyart_path = Path(source["keyart"])
    if not keyart_path.exists():
        raise FileNotFoundError(f"Key art source file not found: {keyart_path}")

    with Image.open(keyart_path) as keyart_source:
        keyart_source = keyart_source.convert("RGB")
        asset_entries: List[Dict] = []
        for export in spec["exports"]:
            export_id = export["id"]
            width = int(export["width"])
            height = int(export["height"])
            mode = export.get("mode", "cover_from_keyart")
            relative_path = Path(export["path"])
            output_path = output_root / relative_path
            ensure_parent(output_path)

            if mode == "cover_from_keyart":
                image = cover_resize_crop(keyart_source, width, height).convert("RGB")
            elif mode == "transparent_logo":
                logo_text = source.get("logo_text", "Raven DevOps Fishing")
                image = render_transparent_logo(width, height, logo_text)
            else:
                raise ValueError(f"Unsupported export mode '{mode}' for asset '{export_id}'.")

            image.save(output_path, format="PNG", optimize=True)
            asset_entries.append(
                {
                    "id": export_id,
                    "path": output_path.as_posix(),
                    "width": width,
                    "height": height,
                    "mode": mode,
                    "sha256": sha256_file(output_path),
                    "bytes": output_path.stat().st_size,
                }
            )

    screenshot_entries: List[Dict] = []
    screenshots_output = output_root / "screenshots"
    screenshots_output.mkdir(parents=True, exist_ok=True)

    for index, screenshot_source_path in enumerate(source.get("screenshots", []), start=1):
        source_path = Path(screenshot_source_path)
        if not source_path.exists():
            raise FileNotFoundError(f"Screenshot source file not found: {source_path}")

        screenshot_name = f"screenshot_{index:02d}_{source_path.stem}_1920x1080.png"
        output_path = screenshots_output / screenshot_name
        with Image.open(source_path) as screenshot_image:
            export_image = screenshot_image.convert("RGB")
            if export_image.size != (1920, 1080):
                export_image = cover_resize_crop(export_image, 1920, 1080)
            export_image.save(output_path, format="PNG", optimize=True)

        screenshot_entries.append(
            {
                "id": f"screenshot_{index:02d}",
                "source": source_path.as_posix(),
                "path": output_path.as_posix(),
                "width": 1920,
                "height": 1080,
                "sha256": sha256_file(output_path),
                "bytes": output_path.stat().st_size,
            }
        )

    return {
        "spec": spec,
        "assets": asset_entries,
        "screenshots": screenshot_entries,
    }


def write_lock_manifest(spec_path: Path, output_root: Path, export_data: Dict) -> Path:
    spec = export_data["spec"]
    assets = export_data["assets"]
    screenshots = export_data["screenshots"]
    repo_root = Path(".").resolve()

    package_lines = []
    for entry in sorted(assets + screenshots, key=lambda item: item["path"]):
        package_lines.append(f'{entry["path"]}:{entry["sha256"]}')
    package_digest = hashlib.sha256("\n".join(package_lines).encode("utf-8")).hexdigest()

    lock = {
        "lock_version": 1,
        "generated_utc": dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat(),
        "rc_id": spec.get("rc_id", "unknown"),
        "spec_path": spec_path.as_posix(),
        "source_revision": detect_git_revision(repo_root),
        "steam_rules_checked_utc": spec.get("steam_rules_checked_utc", ""),
        "steam_rules_sources": spec.get("steam_rules_sources", []),
        "assets": assets,
        "screenshots": screenshots,
        "package_sha256": package_digest,
        "export_command": "python scripts/ci/export-steam-store-assets.py --spec marketing/steam/store_assets/store_asset_spec.json",
    }

    lock_path = output_root / "export_manifest.lock.json"
    lock_path.write_text(json.dumps(lock, indent=2) + "\n", encoding="utf-8")
    return lock_path


def main() -> int:
    args = parse_args()
    spec_path = Path(args.spec)
    if not spec_path.exists():
        raise FileNotFoundError(f"Spec file not found: {spec_path}")

    spec = json.loads(spec_path.read_text(encoding="utf-8"))
    output_dir = Path(args.output_dir) if args.output_dir else Path(spec["output_directory"])
    output_dir.mkdir(parents=True, exist_ok=True)

    export_data = export_assets(spec_path, output_dir)
    lock_path = write_lock_manifest(spec_path, output_dir, export_data)

    print(f"steam_store_assets_exported={output_dir.as_posix()}")
    print(f"steam_store_assets_lock_manifest={lock_path.as_posix()}")
    print(f"steam_store_assets_count={len(export_data['assets']) + len(export_data['screenshots'])}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
