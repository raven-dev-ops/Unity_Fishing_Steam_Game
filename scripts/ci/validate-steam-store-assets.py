#!/usr/bin/env python3
"""Validate Steam store assets against the repository export specification."""

from __future__ import annotations

import argparse
import hashlib
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List

from PIL import Image


@dataclass
class ValidationResult:
    id: str
    path: str
    status: str
    details: str
    width: int
    height: int
    expected_width: int
    expected_height: int
    sha256: str
    bytes: int


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Validate Steam store assets against store_asset_spec.json")
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
    parser.add_argument(
        "--summary-json",
        default="Artifacts/StoreAssets/store_asset_validation_summary.json",
        help="Summary JSON output path.",
    )
    parser.add_argument(
        "--summary-md",
        default="Artifacts/StoreAssets/store_asset_validation_summary.md",
        help="Summary markdown output path.",
    )
    return parser.parse_args()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        while True:
            chunk = handle.read(1024 * 1024)
            if not chunk:
                break
            digest.update(chunk)
    return digest.hexdigest()


def read_image_dimensions(path: Path) -> tuple[int, int]:
    with Image.open(path) as image:
        return image.size


def validate_required_exports(spec: Dict, output_root: Path) -> List[ValidationResult]:
    results: List[ValidationResult] = []
    for export in spec.get("exports", []):
        export_id = export["id"]
        expected_width = int(export["width"])
        expected_height = int(export["height"])
        relative_path = Path(export["path"])
        asset_path = output_root / relative_path

        if not asset_path.exists():
            results.append(
                ValidationResult(
                    id=export_id,
                    path=asset_path.as_posix(),
                    status="FAIL",
                    details="missing file",
                    width=0,
                    height=0,
                    expected_width=expected_width,
                    expected_height=expected_height,
                    sha256="",
                    bytes=0,
                )
            )
            continue

        width, height = read_image_dimensions(asset_path)
        file_hash = sha256_file(asset_path)
        file_bytes = asset_path.stat().st_size

        if width != expected_width or height != expected_height:
            status = "FAIL"
            details = f"dimension mismatch ({width}x{height})"
        else:
            status = "PASS"
            details = "dimensions match"

        results.append(
            ValidationResult(
                id=export_id,
                path=asset_path.as_posix(),
                status=status,
                details=details,
                width=width,
                height=height,
                expected_width=expected_width,
                expected_height=expected_height,
                sha256=file_hash,
                bytes=file_bytes,
            )
        )

    return results


def validate_screenshots(spec: Dict, output_root: Path) -> tuple[List[ValidationResult], str]:
    screenshots_dir = output_root / "screenshots"
    source_screenshots = spec.get("source", {}).get("screenshots", [])
    minimum_required = max(5, len(source_screenshots))
    screenshot_paths = sorted(screenshots_dir.glob("*.png"))

    results: List[ValidationResult] = []
    if len(screenshot_paths) < minimum_required:
        return (
            results,
            f"FAIL: expected at least {minimum_required} screenshots, found {len(screenshot_paths)}",
        )

    failure_count = 0
    for index, screenshot_path in enumerate(screenshot_paths, start=1):
        width, height = read_image_dimensions(screenshot_path)
        ratio = width / height if height > 0 else 0.0
        ratio_delta = abs(ratio - (16.0 / 9.0))

        details = "dimensions and ratio valid"
        status = "PASS"
        if width < 1920 or height < 1080:
            status = "FAIL"
            details = f"minimum size failure ({width}x{height})"
            failure_count += 1
        elif ratio_delta > 0.02:
            status = "FAIL"
            details = f"ratio mismatch ({ratio:.5f})"
            failure_count += 1

        results.append(
            ValidationResult(
                id=f"screenshot_{index:02d}",
                path=screenshot_path.as_posix(),
                status=status,
                details=details,
                width=width,
                height=height,
                expected_width=1920,
                expected_height=1080,
                sha256=sha256_file(screenshot_path),
                bytes=screenshot_path.stat().st_size,
            )
        )

    if failure_count > 0:
        return results, f"FAIL: screenshot validation failed for {failure_count} file(s)"

    return results, f"PASS: {len(results)} screenshots meet 1920x1080 16:9 minimum"


def validate_logo_alpha(output_root: Path) -> str:
    logo_path = output_root / "library_logo_1280x720.png"
    if not logo_path.exists():
        return "FAIL: missing library logo file"

    with Image.open(logo_path) as logo:
        rgba = logo.convert("RGBA")
        alpha_extrema = rgba.getchannel("A").getextrema()
    if alpha_extrema is None:
        return "FAIL: unable to read alpha channel"

    if alpha_extrema[0] == 255 and alpha_extrema[1] == 255:
        return "FAIL: logo alpha channel is fully opaque"

    return "PASS: logo contains transparency"


def write_summary_json(
    summary_path: Path,
    required_results: List[ValidationResult],
    screenshot_results: List[ValidationResult],
    screenshot_gate: str,
    logo_gate: str,
    status: str,
    output_root: Path,
) -> None:
    summary_path.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "status": status,
        "output_root": output_root.as_posix(),
        "required_assets": [result.__dict__ for result in required_results],
        "screenshots": [result.__dict__ for result in screenshot_results],
        "screenshot_gate": screenshot_gate,
        "logo_gate": logo_gate,
    }
    summary_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_summary_markdown(
    summary_path: Path,
    required_results: List[ValidationResult],
    screenshot_results: List[ValidationResult],
    screenshot_gate: str,
    logo_gate: str,
    status: str,
    output_root: Path,
) -> None:
    summary_path.parent.mkdir(parents=True, exist_ok=True)
    lines: List[str] = []
    lines.append("# Steam Store Asset Validation Summary")
    lines.append("")
    lines.append(f"- Status: **{status}**")
    lines.append(f"- Output root: `{output_root.as_posix()}`")
    lines.append(f"- Screenshot gate: `{screenshot_gate}`")
    lines.append(f"- Library logo alpha gate: `{logo_gate}`")
    lines.append("")
    lines.append("## Required Assets")
    lines.append("")
    lines.append("| ID | Status | Expected | Actual | Path |")
    lines.append("|---|---|---|---|---|")
    for result in required_results:
        lines.append(
            f"| `{result.id}` | {result.status} | {result.expected_width}x{result.expected_height} | "
            f"{result.width}x{result.height} | `{result.path}` |"
        )

    lines.append("")
    lines.append("## Screenshots")
    lines.append("")
    lines.append("| ID | Status | Actual | Path |")
    lines.append("|---|---|---|---|")
    for result in screenshot_results:
        lines.append(
            f"| `{result.id}` | {result.status} | {result.width}x{result.height} | `{result.path}` |"
        )

    lines.append("")
    lines.append("## Hash Inventory")
    lines.append("")
    lines.append("| ID | SHA256 | Bytes |")
    lines.append("|---|---|---|")
    for result in required_results + screenshot_results:
        lines.append(f"| `{result.id}` | `{result.sha256}` | {result.bytes} |")

    lines.append("")
    summary_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    args = parse_args()
    spec_path = Path(args.spec)
    if not spec_path.exists():
        raise FileNotFoundError(f"Spec file not found: {spec_path}")

    spec = json.loads(spec_path.read_text(encoding="utf-8"))
    output_root = Path(args.output_dir) if args.output_dir else Path(spec["output_directory"])
    required_results = validate_required_exports(spec, output_root)
    screenshot_results, screenshot_gate = validate_screenshots(spec, output_root)
    logo_gate = validate_logo_alpha(output_root)

    has_required_failures = any(result.status != "PASS" for result in required_results)
    has_screenshot_failures = screenshot_gate.startswith("FAIL")
    has_logo_failures = logo_gate.startswith("FAIL")
    status = "PASS" if not (has_required_failures or has_screenshot_failures or has_logo_failures) else "FAIL"

    summary_json = Path(args.summary_json)
    summary_md = Path(args.summary_md)
    write_summary_json(summary_json, required_results, screenshot_results, screenshot_gate, logo_gate, status, output_root)
    write_summary_markdown(summary_md, required_results, screenshot_results, screenshot_gate, logo_gate, status, output_root)

    print(f"steam_store_asset_validation_status={status}")
    print(f"steam_store_asset_validation_json={summary_json.as_posix()}")
    print(f"steam_store_asset_validation_md={summary_md.as_posix()}")
    return 0 if status == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
