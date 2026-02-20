#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <release_tag>"
  exit 1
fi

RELEASE_TAG="$1"
DRY_RUN="${DRY_RUN:-false}"
STEAMCMD_PATH="${STEAMCMD_PATH:-steamcmd}"
STEAM_BUILD_OUTPUT="${STEAM_BUILD_OUTPUT:-Builds/Windows}"
STEAM_BUILD_LOGS="${STEAM_BUILD_LOGS:-Builds/SteamPipeLogs}"
STEAM_BETA_BRANCH="${STEAM_BETA_BRANCH:-beta}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEMPLATE_DIR="${SCRIPT_DIR}/steampipe"
WORK_DIR="$(mktemp -d)"
trap 'rm -rf "${WORK_DIR}"' EXIT

required_vars=(
  STEAM_APP_ID
  STEAM_DEPOT_WINDOWS_ID
  STEAM_USERNAME
  STEAM_CONFIG_VDF
)

for key in "${required_vars[@]}"; do
  if [[ -z "${!key:-}" ]]; then
    echo "Missing required environment variable: ${key}"
    exit 1
  fi
done

if [[ ! -f "${TEMPLATE_DIR}/app_build_template.vdf" ]]; then
  echo "Missing template: ${TEMPLATE_DIR}/app_build_template.vdf"
  exit 1
fi

if [[ ! -f "${TEMPLATE_DIR}/depot_build_windows_template.vdf" ]]; then
  echo "Missing template: ${TEMPLATE_DIR}/depot_build_windows_template.vdf"
  exit 1
fi

if [[ ! -d "${STEAM_BUILD_OUTPUT}" ]]; then
  if [[ "${DRY_RUN}" == "true" ]]; then
    echo "Dry run: build output folder not found (${STEAM_BUILD_OUTPUT}); continuing with template validation only."
  else
    echo "Build output folder not found: ${STEAM_BUILD_OUTPUT}"
    exit 1
  fi
fi

mkdir -p "${STEAM_BUILD_LOGS}"

CONFIG_VDF_FILE="${WORK_DIR}/config.vdf"
if [[ -f "${STEAM_CONFIG_VDF}" ]]; then
  cp "${STEAM_CONFIG_VDF}" "${CONFIG_VDF_FILE}"
else
  printf '%s\n' "${STEAM_CONFIG_VDF}" > "${CONFIG_VDF_FILE}"
fi
chmod 600 "${CONFIG_VDF_FILE}"

DEPOT_VDF_FILE="${WORK_DIR}/depot_build_windows.vdf"
APP_VDF_FILE="${WORK_DIR}/app_build_${RELEASE_TAG}.vdf"

sed \
  -e "s|__DEPOT_ID__|${STEAM_DEPOT_WINDOWS_ID}|g" \
  -e "s|__CONTENT_ROOT__|${STEAM_BUILD_OUTPUT}|g" \
  "${TEMPLATE_DIR}/depot_build_windows_template.vdf" > "${DEPOT_VDF_FILE}"

sed \
  -e "s|__APP_ID__|${STEAM_APP_ID}|g" \
  -e "s|__DEPOT_ID__|${STEAM_DEPOT_WINDOWS_ID}|g" \
  -e "s|__CONTENT_ROOT__|${STEAM_BUILD_OUTPUT}|g" \
  -e "s|__BUILD_OUTPUT__|${STEAM_BUILD_LOGS}|g" \
  -e "s|__RELEASE_TAG__|${RELEASE_TAG}|g" \
  -e "s|__BRANCH__|${STEAM_BETA_BRANCH}|g" \
  "${TEMPLATE_DIR}/app_build_template.vdf" > "${APP_VDF_FILE}"

if [[ "${DRY_RUN}" == "true" ]]; then
  echo "Dry run: generated SteamPipe VDF files:"
  echo "  ${DEPOT_VDF_FILE}"
  echo "  ${APP_VDF_FILE}"
  echo "Dry run: would execute:"
  echo "  ${STEAMCMD_PATH} +@ShutdownOnFailedCommand 1 +@NoPromptForPassword 1 +login ${STEAM_USERNAME} +run_app_build ${APP_VDF_FILE} +quit"
  exit 0
fi

set +x
"${STEAMCMD_PATH}" \
  +@ShutdownOnFailedCommand 1 \
  +@NoPromptForPassword 1 \
  +login "${STEAM_USERNAME}" \
  +run_app_build "${APP_VDF_FILE}" \
  +quit

echo "SteamPipe upload complete for tag ${RELEASE_TAG} on branch ${STEAM_BETA_BRANCH}."
