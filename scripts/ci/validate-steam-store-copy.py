#!/usr/bin/env python3
"""Validate Steam storefront copy package for compliance and media alignment."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Dict, List


URL_PATTERN = re.compile(r"(https?://|www\.)", re.IGNORECASE)
MARKDOWN_LINK_PATTERN = re.compile(r"\[[^\]]+\]\([^)]+\)")
IMAGE_TAG_PATTERN = re.compile(r"!\[[^\]]*\]\([^)]+\)")
FAUX_UI_PATTERNS = [
    re.compile(r"\bclick here\b", re.IGNORECASE),
    re.compile(r"\btap here\b", re.IGNORECASE),
    re.compile(r"\bwishlist now\b", re.IGNORECASE),
    re.compile(r"\badd to cart\b", re.IGNORECASE),
    re.compile(r"\bpress (the )?(start|enter|button)\b", re.IGNORECASE),
]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Validate Steam store copy package")
    parser.add_argument(
        "--copy-dir",
        default="marketing/steam/store_copy/rc-2026-02-25",
        help="Store copy package directory.",
    )
    parser.add_argument(
        "--summary-json",
        default="Artifacts/StoreCopy/store_copy_validation_summary.json",
        help="Summary JSON output path.",
    )
    parser.add_argument(
        "--summary-md",
        default="Artifacts/StoreCopy/store_copy_validation_summary.md",
        help="Summary markdown output path.",
    )
    return parser.parse_args()


def load_required_file(path: Path) -> str:
    if not path.exists():
        raise FileNotFoundError(f"Required file missing: {path}")
    return path.read_text(encoding="utf-8")


def run_copy_checks(short_text: str, long_text: str, media_payload: Dict) -> List[Dict]:
    checks: List[Dict] = []
    combined_text = f"{short_text}\n{long_text}"

    def add_check(name: str, passed: bool, details: str) -> None:
        checks.append({"name": name, "status": "PASS" if passed else "FAIL", "details": details})

    add_check(
        "short_description_length",
        len(short_text.strip()) <= 300,
        f"length={len(short_text.strip())} (limit=300)",
    )

    has_url_violation = bool(URL_PATTERN.search(combined_text) or MARKDOWN_LINK_PATTERN.search(combined_text))
    add_check("no_links_or_urls", not has_url_violation, "URL/link patterns detected" if has_url_violation else "no URL/link patterns found")

    has_image_tag = bool(IMAGE_TAG_PATTERN.search(combined_text))
    add_check("no_embedded_images_in_copy", not has_image_tag, "markdown image tags detected" if has_image_tag else "no embedded image tags")

    faux_ui_match = None
    for pattern in FAUX_UI_PATTERNS:
        match = pattern.search(combined_text)
        if match:
            faux_ui_match = match.group(0)
            break
    add_check("no_faux_ui_language", faux_ui_match is None, f"detected phrase: '{faux_ui_match}'" if faux_ui_match else "no faux UI phrases detected")

    screenshots = media_payload.get("screenshots", [])
    existing_screenshots = [path for path in screenshots if Path(path).exists()]
    screenshot_gate = len(existing_screenshots) >= 5 and len(existing_screenshots) == len(screenshots)
    add_check(
        "screenshot_payload_alignment",
        screenshot_gate,
        f"screenshots_listed={len(screenshots)} existing={len(existing_screenshots)}",
    )

    mentions_trailer_or_gif = bool(re.search(r"\btrailer\b|\bgif\b", combined_text, flags=re.IGNORECASE))
    trailer_expected = bool(media_payload.get("has_trailer", False) or media_payload.get("has_gif_media", False))
    if trailer_expected:
        add_check(
            "media_claim_alignment",
            mentions_trailer_or_gif,
            "media payload includes trailer/gif and text references it"
            if mentions_trailer_or_gif
            else "media payload includes trailer/gif but text does not mention it",
        )
    else:
        add_check(
            "media_claim_alignment",
            not mentions_trailer_or_gif,
            "no trailer/gif claim in copy and payload does not include it"
            if not mentions_trailer_or_gif
            else "text references trailer/gif while payload does not include it",
        )

    return checks


def write_summary(copy_dir: Path, checks: List[Dict], summary_json: Path, summary_md: Path) -> str:
    summary_json.parent.mkdir(parents=True, exist_ok=True)
    summary_md.parent.mkdir(parents=True, exist_ok=True)

    status = "PASS" if all(check["status"] == "PASS" for check in checks) else "FAIL"
    payload = {
        "status": status,
        "copy_dir": copy_dir.as_posix(),
        "checks": checks,
    }
    summary_json.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")

    lines: List[str] = []
    lines.append("# Steam Store Copy Validation Summary")
    lines.append("")
    lines.append(f"- Status: **{status}**")
    lines.append(f"- Copy package: `{copy_dir.as_posix()}`")
    lines.append("")
    lines.append("| Check | Status | Details |")
    lines.append("|---|---|---|")
    for check in checks:
        lines.append(f"| `{check['name']}` | {check['status']} | {check['details']} |")
    lines.append("")
    summary_md.write_text("\n".join(lines) + "\n", encoding="utf-8")

    return status


def main() -> int:
    args = parse_args()
    copy_dir = Path(args.copy_dir)

    short_description = load_required_file(copy_dir / "short_description.txt")
    long_description = load_required_file(copy_dir / "long_description.md")
    metadata = json.loads(load_required_file(copy_dir / "metadata_copy.json"))
    media_payload = json.loads(load_required_file(copy_dir / "media_payload.json"))

    _ = metadata  # metadata file presence is required even when current checks do not parse every field.
    checks = run_copy_checks(short_description, long_description, media_payload)
    status = write_summary(copy_dir, checks, Path(args.summary_json), Path(args.summary_md))

    print(f"steam_store_copy_validation_status={status}")
    print(f"steam_store_copy_validation_json={Path(args.summary_json).as_posix()}")
    print(f"steam_store_copy_validation_md={Path(args.summary_md).as_posix()}")
    return 0 if status == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
