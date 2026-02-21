#!/usr/bin/env python3
"""Compare captured scene screenshots against approved baselines."""

from __future__ import annotations

import argparse
import json
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List

from PIL import Image, ImageChops, ImageStat


@dataclass
class DiffResult:
    scene: str
    status: str
    diff_ratio: float
    changed_pixel_ratio: float
    baseline_path: str
    capture_path: str
    diff_image_path: str
    note: str


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Compare scene captures to baseline PNG files.")
    parser.add_argument("--baseline-dir", required=True, help="Directory containing approved baseline PNG files.")
    parser.add_argument("--capture-dir", required=True, help="Directory containing newly captured PNG files.")
    parser.add_argument("--output-dir", required=True, help="Output directory for diff artifacts and summary files.")
    parser.add_argument("--warn-threshold", type=float, default=0.015, help="Warn threshold for mean absolute pixel delta ratio.")
    parser.add_argument("--fail-threshold", type=float, default=0.03, help="Fail threshold for mean absolute pixel delta ratio.")
    parser.add_argument("--enforce", default="false", help="If true, fail process on severe regressions.")
    parser.add_argument("--summary-json", default="", help="Optional path for summary JSON.")
    parser.add_argument("--summary-md", default="", help="Optional path for summary markdown.")
    return parser.parse_args()


def collect_pngs(directory: Path) -> Dict[str, Path]:
    if not directory.exists():
        return {}
    return {path.name: path for path in sorted(directory.glob("*.png"))}


def bool_from_string(raw: str) -> bool:
    value = (raw or "").strip().lower()
    return value in {"1", "true", "yes", "on"}


def clamp_ratio(value: float) -> float:
    return max(0.0, min(1.0, value))


def build_visual_diff(baseline_rgb: Image.Image, capture_rgb: Image.Image) -> Image.Image:
    delta = ImageChops.difference(baseline_rgb, capture_rgb)
    mask = delta.convert("L").point(lambda p: 255 if p > 0 else 0)
    overlay = Image.new("RGB", baseline_rgb.size, (255, 0, 0))
    highlighted = Image.composite(overlay, baseline_rgb, mask)

    width, height = baseline_rgb.size
    panel = Image.new("RGB", (width * 3, height), (255, 255, 255))
    panel.paste(baseline_rgb, (0, 0))
    panel.paste(capture_rgb, (width, 0))
    panel.paste(highlighted, (width * 2, 0))
    return panel


def analyze_pair(
    scene_name: str,
    baseline_path: Path | None,
    capture_path: Path | None,
    output_diff_path: Path,
    warn_threshold: float,
    fail_threshold: float,
) -> DiffResult:
    if baseline_path is None:
        return DiffResult(
            scene=scene_name,
            status="missing_baseline",
            diff_ratio=1.0,
            changed_pixel_ratio=1.0,
            baseline_path="",
            capture_path=str(capture_path) if capture_path else "",
            diff_image_path="",
            note="Capture exists without baseline file.",
        )

    if capture_path is None:
        return DiffResult(
            scene=scene_name,
            status="missing_capture",
            diff_ratio=1.0,
            changed_pixel_ratio=1.0,
            baseline_path=str(baseline_path),
            capture_path="",
            diff_image_path="",
            note="Baseline exists but capture output is missing.",
        )

    with Image.open(baseline_path) as baseline_raw:
        baseline_rgb = baseline_raw.convert("RGB")
    with Image.open(capture_path) as capture_raw:
        capture_rgb = capture_raw.convert("RGB")

    if baseline_rgb.size != capture_rgb.size:
        panel = Image.new(
            "RGB",
            (max(baseline_rgb.width, capture_rgb.width) * 2, max(baseline_rgb.height, capture_rgb.height)),
            (255, 255, 255),
        )
        panel.paste(baseline_rgb, (0, 0))
        panel.paste(capture_rgb, (panel.width // 2, 0))
        panel.save(output_diff_path)
        return DiffResult(
            scene=scene_name,
            status="dimension_mismatch",
            diff_ratio=1.0,
            changed_pixel_ratio=1.0,
            baseline_path=str(baseline_path),
            capture_path=str(capture_path),
            diff_image_path=str(output_diff_path),
            note=f"Baseline size={baseline_rgb.size}, capture size={capture_rgb.size}.",
        )

    delta = ImageChops.difference(baseline_rgb, capture_rgb)
    stats = ImageStat.Stat(delta)
    channel_mean = sum(stats.mean) / max(1, len(stats.mean))
    diff_ratio = clamp_ratio(channel_mean / 255.0)

    mask = delta.convert("L").point(lambda p: 255 if p > 0 else 0)
    non_zero = sum(1 for px in mask.getdata() if px)
    total_pixels = baseline_rgb.width * baseline_rgb.height
    changed_pixel_ratio = clamp_ratio(non_zero / max(1, total_pixels))

    if diff_ratio >= fail_threshold:
        status = "fail"
    elif diff_ratio >= warn_threshold:
        status = "warn"
    else:
        status = "pass"

    panel = build_visual_diff(baseline_rgb, capture_rgb)
    panel.save(output_diff_path)

    return DiffResult(
        scene=scene_name,
        status=status,
        diff_ratio=diff_ratio,
        changed_pixel_ratio=changed_pixel_ratio,
        baseline_path=str(baseline_path),
        capture_path=str(capture_path),
        diff_image_path=str(output_diff_path),
        note="",
    )


def to_markdown(results: List[DiffResult], warn_threshold: float, fail_threshold: float, enforce: bool) -> str:
    counts = {}
    for result in results:
        counts[result.status] = counts.get(result.status, 0) + 1

    lines = [
        "## Scene Capture Visual Diff",
        "",
        f"- Warn threshold: `{warn_threshold:.4f}`",
        f"- Fail threshold: `{fail_threshold:.4f}`",
        f"- Enforce fail mode: `{str(enforce).lower()}`",
        "",
        "| Scene | Status | Mean Diff Ratio | Changed Pixel Ratio | Notes |",
        "|---|---|---:|---:|---|",
    ]

    for result in results:
        note = result.note or "-"
        lines.append(
            f"| `{result.scene}` | `{result.status}` | `{result.diff_ratio:.6f}` | `{result.changed_pixel_ratio:.6f}` | {note} |"
        )

    lines.extend(
        [
            "",
            "Summary:",
            f"- pass: {counts.get('pass', 0)}",
            f"- warn: {counts.get('warn', 0)}",
            f"- fail: {counts.get('fail', 0)}",
            f"- missing_baseline: {counts.get('missing_baseline', 0)}",
            f"- missing_capture: {counts.get('missing_capture', 0)}",
            f"- dimension_mismatch: {counts.get('dimension_mismatch', 0)}",
        ]
    )

    return "\n".join(lines) + "\n"


def main() -> int:
    args = parse_args()

    baseline_dir = Path(args.baseline_dir)
    capture_dir = Path(args.capture_dir)
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    diff_dir = output_dir / "diff-images"
    diff_dir.mkdir(parents=True, exist_ok=True)

    summary_json_path = Path(args.summary_json) if args.summary_json else output_dir / "scene_capture_diff_summary.json"
    summary_md_path = Path(args.summary_md) if args.summary_md else output_dir / "scene_capture_diff_summary.md"

    warn_threshold = max(0.0, float(args.warn_threshold))
    fail_threshold = max(warn_threshold, float(args.fail_threshold))
    enforce = bool_from_string(args.enforce)

    baseline_map = collect_pngs(baseline_dir)
    capture_map = collect_pngs(capture_dir)
    scene_names = sorted(set(baseline_map.keys()) | set(capture_map.keys()))

    results: List[DiffResult] = []
    for scene_name in scene_names:
        safe_stem = scene_name.rsplit(".", 1)[0]
        diff_path = diff_dir / f"{safe_stem}_diff.png"
        result = analyze_pair(
            scene_name=scene_name,
            baseline_path=baseline_map.get(scene_name),
            capture_path=capture_map.get(scene_name),
            output_diff_path=diff_path,
            warn_threshold=warn_threshold,
            fail_threshold=fail_threshold,
        )
        results.append(result)

    counts = {}
    for result in results:
        counts[result.status] = counts.get(result.status, 0) + 1

    should_fail = False
    severe_count = counts.get("fail", 0) + counts.get("missing_capture", 0) + counts.get("missing_baseline", 0) + counts.get("dimension_mismatch", 0)
    if enforce and severe_count > 0:
        should_fail = True

    summary_payload = {
        "baseline_dir": str(baseline_dir),
        "capture_dir": str(capture_dir),
        "warn_threshold": warn_threshold,
        "fail_threshold": fail_threshold,
        "enforce": enforce,
        "counts": counts,
        "severe_count": severe_count,
        "results": [result.__dict__ for result in results],
    }

    summary_json_path.parent.mkdir(parents=True, exist_ok=True)
    summary_json_path.write_text(json.dumps(summary_payload, indent=2), encoding="utf-8")

    markdown = to_markdown(results, warn_threshold, fail_threshold, enforce)
    summary_md_path.parent.mkdir(parents=True, exist_ok=True)
    summary_md_path.write_text(markdown, encoding="utf-8")

    print(markdown)
    if should_fail:
        print("Scene capture diff enforcement failed due to severe regressions.", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
