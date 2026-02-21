#!/usr/bin/env bash
set -euo pipefail

PROJECT_VERSION_FILE="${PROJECT_VERSION_FILE:-ProjectSettings/ProjectVersion.txt}"
EXPECTED_UNITY_VERSION="${EXPECTED_UNITY_VERSION:-6000.3.9f1}"

if [[ ! -f "${PROJECT_VERSION_FILE}" ]]; then
  echo "::error::Unity version guard failed: missing ${PROJECT_VERSION_FILE}."
  exit 1
fi

actual_line="$(grep -m 1 'm_EditorVersion:' "${PROJECT_VERSION_FILE}" || true)"
actual_unity_version="$(
  printf '%s' "${actual_line}" \
    | sed -e 's/^\xEF\xBB\xBF//' -e 's/^m_EditorVersion:[[:space:]]*//' -e 's/\r$//'
)"

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

