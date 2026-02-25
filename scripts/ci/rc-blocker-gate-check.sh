#!/usr/bin/env bash
set -euo pipefail

REPO="${REPO:-}"
if [[ -z "${REPO}" ]]; then
  echo "::error::REPO env var is required (format owner/repo)." >&2
  exit 2
fi

RC_BLOCKER_LABEL="${RC_BLOCKER_LABEL:-P0-blocker}"
RC_SCOPE_LABEL="${RC_SCOPE_LABEL:-scope:1.0}"
RC_BLOCKER_MILESTONE="${RC_BLOCKER_MILESTONE:-M9.1 - 1.0 Launch Remediation}"
RC_BLOCKER_OVERRIDE="${RC_BLOCKER_OVERRIDE:-false}"
RC_BLOCKER_OVERRIDE_REASON="${RC_BLOCKER_OVERRIDE_REASON:-}"
RC_BLOCKER_QUERY_OVERRIDE="${RC_BLOCKER_QUERY_OVERRIDE:-}"
RC_BLOCKER_SUMMARY_JSON="${RC_BLOCKER_SUMMARY_JSON:-Artifacts/RCValidation/rc_blocker_gate_summary.json}"
RC_BLOCKER_SUMMARY_MD="${RC_BLOCKER_SUMMARY_MD:-Artifacts/RCValidation/rc_blocker_gate_summary.md}"

mkdir -p "$(dirname "${RC_BLOCKER_SUMMARY_JSON}")"
mkdir -p "$(dirname "${RC_BLOCKER_SUMMARY_MD}")"

if [[ -n "${RC_BLOCKER_QUERY_OVERRIDE}" ]]; then
  query="${RC_BLOCKER_QUERY_OVERRIDE}"
else
  query="repo:${REPO} is:issue is:open label:\"${RC_BLOCKER_LABEL}\" label:\"${RC_SCOPE_LABEL}\" milestone:\"${RC_BLOCKER_MILESTONE}\""
fi

encoded_query="$(printf '%s' "${query}" | jq -sRr '@uri')"
api_url="https://api.github.com/search/issues?q=${encoded_query}&per_page=100"

curl_headers=(
  -H "Accept: application/vnd.github+json"
)
if [[ -n "${GH_TOKEN:-}" ]]; then
  curl_headers+=(-H "Authorization: Bearer ${GH_TOKEN}")
fi

response="$(curl -fsSL "${curl_headers[@]}" "${api_url}")"
blocker_count="$(echo "${response}" | jq -r '.total_count // 0')"
blockers_json="$(echo "${response}" | jq '[.items[] | {number, title, html_url, milestone: (.milestone.title // "")}]')"

status="passed"
reason="no_open_blockers"
override_used="false"
error_message=""

if [[ "${blocker_count}" -gt 0 ]]; then
  status="failed"
  reason="open_rc_blockers"

  if [[ "${RC_BLOCKER_OVERRIDE}" == "true" ]]; then
    if [[ -z "${RC_BLOCKER_OVERRIDE_REASON//[[:space:]]/}" ]]; then
      reason="override_reason_missing"
      error_message="RC blocker override requested but RC_BLOCKER_OVERRIDE_REASON is empty."
    else
      status="overridden"
      reason="override_emergency_approved"
      override_used="true"
    fi
  fi
fi

generated_utc="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"

jq -n \
  --arg generated_utc "${generated_utc}" \
  --arg repository "${REPO}" \
  --arg query "${query}" \
  --arg blocker_label "${RC_BLOCKER_LABEL}" \
  --arg scope_label "${RC_SCOPE_LABEL}" \
  --arg milestone "${RC_BLOCKER_MILESTONE}" \
  --arg status "${status}" \
  --arg reason "${reason}" \
  --arg override_requested "${RC_BLOCKER_OVERRIDE}" \
  --arg override_used "${override_used}" \
  --arg override_reason "${RC_BLOCKER_OVERRIDE_REASON}" \
  --arg error_message "${error_message}" \
  --argjson blocker_count "${blocker_count}" \
  --argjson blockers "${blockers_json}" \
  '{
    generated_utc: $generated_utc,
    repository: $repository,
    query: $query,
    labels: {
      blocker: $blocker_label,
      scope: $scope_label
    },
    milestone: $milestone,
    status: $status,
    reason: $reason,
    blocker_count: $blocker_count,
    override: {
      requested: ($override_requested == "true"),
      used: ($override_used == "true"),
      reason: $override_reason
    },
    error_message: (if $error_message == "" then null else $error_message end),
    blockers: $blockers
  }' \
  > "${RC_BLOCKER_SUMMARY_JSON}"

{
  echo "# RC Blocker Gate Summary"
  echo
  echo "- Generated UTC: \`${generated_utc}\`"
  echo "- Repository: \`${REPO}\`"
  echo "- Milestone: \`${RC_BLOCKER_MILESTONE}\`"
  echo "- Query: \`${query}\`"
  echo "- Status: \`${status}\`"
  echo "- Reason: \`${reason}\`"
  echo "- Override requested: \`${RC_BLOCKER_OVERRIDE}\`"
  echo "- Override used: \`${override_used}\`"
  if [[ -n "${RC_BLOCKER_OVERRIDE_REASON}" ]]; then
    echo "- Override reason: ${RC_BLOCKER_OVERRIDE_REASON}"
  fi
  echo "- Blocker count: \`${blocker_count}\`"
  echo
  echo "| Issue | Title | Milestone |"
  echo "|---|---|---|"
  if [[ "${blocker_count}" -eq 0 ]]; then
    echo "| _none_ | _none_ | _none_ |"
  else
    echo "${blockers_json}" | jq -r '.[] | "| [#\(.number)](\(.html_url)) | \(.title) | " + (if .milestone == "" then "_none_" else .milestone end) + " |"'
  fi
  if [[ -n "${error_message}" ]]; then
    echo
    echo "Error: ${error_message}"
  fi
} > "${RC_BLOCKER_SUMMARY_MD}"

if [[ "${status}" == "failed" ]]; then
  echo "::error::RC blocker gate failed. Open P0 blockers were found for the active 1.0 scope."
  exit 1
fi

if [[ "${reason}" == "override_reason_missing" ]]; then
  echo "::error::${error_message}"
  exit 1
fi

if [[ "${status}" == "overridden" ]]; then
  echo "::warning::RC blocker gate override applied. Proceed only for emergency-approved release handling."
fi

echo "RC blocker gate status: ${status}"
