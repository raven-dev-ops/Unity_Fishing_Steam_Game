#!/usr/bin/env bash
set -euo pipefail

TRUSTED_CONTEXT="${TRUSTED_CONTEXT:-false}"
AUTOMATION_WRITE_TOKEN="${AUTOMATION_WRITE_TOKEN:-}"
OUTPUT_FILE="${GITHUB_OUTPUT:-}"

if [[ -z "${OUTPUT_FILE}" ]]; then
  OUTPUT_FILE="$(mktemp)"
fi

trusted_normalized="$(echo "${TRUSTED_CONTEXT}" | tr '[:upper:]' '[:lower:]')"

write_output() {
  local key="$1"
  local value="$2"
  echo "${key}=${value}" >> "${OUTPUT_FILE}"
}

if [[ "${trusted_normalized}" == "true" && -n "${AUTOMATION_WRITE_TOKEN}" ]]; then
  echo "::add-mask::${AUTOMATION_WRITE_TOKEN}"
  write_output "write_enabled" "true"
  write_output "token" "${AUTOMATION_WRITE_TOKEN}"
  write_output "token_source" "AUTOMATION_WRITE_TOKEN"
  write_output "reason" "trusted_context_with_automation_token"
  echo "Write-capable automation enabled (trusted context + AUTOMATION_WRITE_TOKEN)."
  exit 0
fi

write_output "write_enabled" "false"
write_output "token" ""
write_output "token_source" "none"

if [[ "${trusted_normalized}" != "true" ]]; then
  write_output "reason" "untrusted_context"
  echo "::warning::Write-capable automation disabled: untrusted context."
else
  write_output "reason" "missing_automation_token"
  echo "::warning::Write-capable automation disabled: AUTOMATION_WRITE_TOKEN is not configured for trusted context."
fi

echo "::notice::Manual fallback: rely on uploaded workflow artifacts/logs and add PR annotations or comments manually when needed."

