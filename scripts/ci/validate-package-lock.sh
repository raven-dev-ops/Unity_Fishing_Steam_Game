#!/usr/bin/env bash
set -euo pipefail

LOCK_FILE="${LOCK_FILE:-Packages/packages-lock.json}"
MANIFEST_FILE="${MANIFEST_FILE:-Packages/manifest.json}"

if [[ ! -f "${MANIFEST_FILE}" ]]; then
  echo "::error::Package lock guard failed: missing ${MANIFEST_FILE}."
  exit 1
fi

if [[ ! -s "${LOCK_FILE}" ]]; then
  echo "::error::Package lock guard failed: ${LOCK_FILE} is missing or empty."
  exit 1
fi

python3 - "${LOCK_FILE}" "${MANIFEST_FILE}" <<'PY'
import json
import sys
from pathlib import Path

lock_path = Path(sys.argv[1])
manifest_path = Path(sys.argv[2])

errors = []

def load_json(path: Path):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append(f"{path} is not valid JSON: {exc}")
        return None

lock_data = load_json(lock_path)
manifest_data = load_json(manifest_path)

if lock_data is None or manifest_data is None:
    for err in errors:
        print(f"::error::{err}")
    sys.exit(1)

if not isinstance(lock_data, dict):
    errors.append(f"{lock_path} root must be an object.")

if not isinstance(manifest_data, dict):
    errors.append(f"{manifest_path} root must be an object.")

lock_dependencies = lock_data.get("dependencies") if isinstance(lock_data, dict) else None
manifest_dependencies = manifest_data.get("dependencies") if isinstance(manifest_data, dict) else None

if not isinstance(lock_dependencies, dict):
    errors.append(f"{lock_path} must contain a 'dependencies' object.")
elif not lock_dependencies:
    errors.append(f"{lock_path} dependencies object is empty.")

if not isinstance(manifest_dependencies, dict):
    errors.append(f"{manifest_path} must contain a 'dependencies' object.")
    manifest_dependencies = {}

missing_from_lock = sorted(set(manifest_dependencies.keys()) - set(lock_dependencies.keys() if isinstance(lock_dependencies, dict) else []))
for package_name in missing_from_lock:
    errors.append(f"{lock_path} is missing manifest dependency: {package_name}")

non_exact_prefixes = ("file:", "git", "http:", "https:", "ssh:", "../", "./")

if isinstance(lock_dependencies, dict):
    for package_name, manifest_version in manifest_dependencies.items():
        lock_entry = lock_dependencies.get(package_name)
        if lock_entry is None:
            continue

        if not isinstance(lock_entry, dict):
            errors.append(f"{lock_path} dependency '{package_name}' must be an object.")
            continue

        lock_version = str(lock_entry.get("version", "")).strip()
        if not lock_version:
            errors.append(f"{lock_path} dependency '{package_name}' is missing required 'version'.")
            continue

        manifest_version_str = str(manifest_version).strip()
        if manifest_version_str and not manifest_version_str.startswith(non_exact_prefixes):
            if lock_version != manifest_version_str:
                errors.append(
                    f"{lock_path} dependency '{package_name}' version mismatch: "
                    f"manifest={manifest_version_str}, lock={lock_version}"
                )

if errors:
    for err in errors:
        print(f"::error::{err}")
    sys.exit(1)

print(
    f"Package lock guard passed: {lock_path} has "
    f"{len(lock_dependencies)} dependencies and covers all manifest entries."
)
PY

