#!/usr/bin/env bash
set -euo pipefail

PROJECT_VERSION_FILE="${PROJECT_VERSION_FILE:-ProjectSettings/ProjectVersion.txt}"
EXPECTED_UNITY_VERSION="${EXPECTED_UNITY_VERSION:-2022.3.16f1}"

if [[ ! -f "${PROJECT_VERSION_FILE}" ]]; then
  echo "::error::Unity version guard failed: missing ${PROJECT_VERSION_FILE}."
  exit 1
fi

actual_unity_version="$(awk -F': ' '/^m_EditorVersion:/ {print $2}' "${PROJECT_VERSION_FILE}" | tr -d '\r' | head -n 1)"

if [[ -z "${actual_unity_version}" ]]; then
  echo "::error::Unity version guard failed: could not parse m_EditorVersion from ${PROJECT_VERSION_FILE}."
  exit 1
fi

if [[ "${actual_unity_version}" != "${EXPECTED_UNITY_VERSION}" ]]; then
  echo "::error::Unity version guard failed: expected ${EXPECTED_UNITY_VERSION}, found ${actual_unity_version}. Update workflows/cache keys intentionally when upgrading Unity."
  exit 1
fi

echo "Unity version guard passed: ${actual_unity_version}"
echo "unity_version=${actual_unity_version}" >> "${GITHUB_OUTPUT:-/dev/null}"
