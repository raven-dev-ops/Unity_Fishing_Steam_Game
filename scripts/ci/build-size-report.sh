#!/usr/bin/env bash
set -euo pipefail

BUILD_ROOT="${BUILD_ROOT:-Builds/Windows}"
BASELINE_FILE="${BASELINE_FILE:-ci/build-size-baseline.json}"
REPORT_JSON="${REPORT_JSON:-Artifacts/BuildSize/build_size_report.json}"
REPORT_MD="${REPORT_MD:-Artifacts/BuildSize/build_size_report.md}"
ENFORCEMENT_MODE="${ENFORCEMENT_MODE:-warn}" # off | warn | fail
WARN_DELTA_PERCENT="${BUILD_SIZE_WARN_DELTA_PERCENT:-8}"
FAIL_DELTA_PERCENT="${BUILD_SIZE_FAIL_DELTA_PERCENT:-15}"

if [[ ! -d "${BUILD_ROOT}" ]]; then
  echo "::error::Build size report failed: build root not found at '${BUILD_ROOT}'."
  exit 1
fi

mkdir -p "$(dirname "${REPORT_JSON}")"
mkdir -p "$(dirname "${REPORT_MD}")"

generated_utc="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
total_bytes="$(du -sb "${BUILD_ROOT}" | awk '{print $1}')"
file_count="$(find "${BUILD_ROOT}" -type f | wc -l | tr -d ' ')"
tmp_entries="$(mktemp)"
trap 'rm -f "${tmp_entries}"' EXIT

while IFS= read -r entry; do
  size_bytes="$(du -sb "${entry}" | awk '{print $1}')"
  rel_name="${entry#${BUILD_ROOT}/}"
  rel_name="${rel_name#${BUILD_ROOT}}"
  echo "${rel_name}"$'\t'"${size_bytes}" >> "${tmp_entries}"
done < <(find "${BUILD_ROOT}" -mindepth 1 -maxdepth 1 | sort)

baseline_total=""
if [[ -f "${BASELINE_FILE}" ]]; then
  baseline_total="$(python3 - "${BASELINE_FILE}" <<'PY'
import json,sys
path=sys.argv[1]
try:
    with open(path, 'r', encoding='utf-8') as fh:
        payload=json.load(fh)
    value=payload.get("total_bytes")
    if isinstance(value, int) and value > 0:
        print(value)
except Exception:
    pass
PY
)"
fi

baseline_status="missing"
delta_bytes=0
delta_percent=0
threshold_status="no_baseline"

if [[ -n "${baseline_total}" ]]; then
  baseline_status="present"
  delta_bytes=$((total_bytes - baseline_total))
  delta_percent="$(python3 - "${delta_bytes}" "${baseline_total}" <<'PY'
import sys
delta=int(sys.argv[1])
baseline=int(sys.argv[2])
pct=(delta / baseline * 100.0) if baseline > 0 else 0.0
print(f"{pct:.2f}")
PY
)"

  exceeds_warn="$(python3 - "${delta_percent}" "${WARN_DELTA_PERCENT}" <<'PY'
import sys
delta=float(sys.argv[1]); warn=float(sys.argv[2])
print("1" if delta > warn else "0")
PY
)"
  exceeds_fail="$(python3 - "${delta_percent}" "${FAIL_DELTA_PERCENT}" <<'PY'
import sys
delta=float(sys.argv[1]); fail=float(sys.argv[2])
print("1" if delta > fail else "0")
PY
)"

  if [[ "${exceeds_fail}" == "1" ]]; then
    threshold_status="fail"
  elif [[ "${exceeds_warn}" == "1" ]]; then
    threshold_status="warn"
  else
    threshold_status="ok"
  fi
fi

python3 - "${REPORT_JSON}" "${generated_utc}" "${BUILD_ROOT}" "${total_bytes}" "${file_count}" "${BASELINE_FILE}" "${baseline_status}" "${baseline_total}" "${delta_bytes}" "${delta_percent}" "${threshold_status}" "${WARN_DELTA_PERCENT}" "${FAIL_DELTA_PERCENT}" "${tmp_entries}" <<'PY'
import json
import os
import sys

(
    report_path,
    generated_utc,
    build_root,
    total_bytes,
    file_count,
    baseline_file,
    baseline_status,
    baseline_total,
    delta_bytes,
    delta_percent,
    threshold_status,
    warn_delta_percent,
    fail_delta_percent,
    entries_file,
) = sys.argv[1:]

entries = []
if os.path.exists(entries_file):
    with open(entries_file, "r", encoding="utf-8") as handle:
        for line in handle:
            line = line.rstrip("\n")
            if not line:
                continue
            name, size = line.split("\t", 1)
            entries.append({"name": name, "bytes": int(size)})

payload = {
    "schema_version": 1,
    "generated_utc": generated_utc,
    "build_root": build_root,
    "total_bytes": int(total_bytes),
    "file_count": int(file_count),
    "baseline": {
        "file": baseline_file,
        "status": baseline_status,
        "total_bytes": int(baseline_total) if baseline_total else None,
    },
    "delta": {
        "bytes": int(delta_bytes),
        "percent": float(delta_percent),
    },
    "thresholds": {
        "warn_delta_percent": float(warn_delta_percent),
        "fail_delta_percent": float(fail_delta_percent),
        "status": threshold_status,
    },
    "top_level_entries": entries,
}

with open(report_path, "w", encoding="utf-8") as handle:
    json.dump(payload, handle, indent=2)
    handle.write("\n")
PY

{
  echo "# Build Size Report"
  echo
  echo "Generated (UTC): \`${generated_utc}\`"
  echo
  echo "| Metric | Value |"
  echo "|---|---:|"
  echo "| Build root | \`${BUILD_ROOT}\` |"
  echo "| Total bytes | ${total_bytes} |"
  echo "| File count | ${file_count} |"
  echo "| Baseline status | ${baseline_status} |"
  if [[ -n "${baseline_total}" ]]; then
    echo "| Baseline total bytes | ${baseline_total} |"
    echo "| Delta bytes | ${delta_bytes} |"
    echo "| Delta percent | ${delta_percent}% |"
    echo "| Threshold status | ${threshold_status} |"
    echo "| Warn threshold | ${WARN_DELTA_PERCENT}% |"
    echo "| Fail threshold | ${FAIL_DELTA_PERCENT}% |"
  else
    echo "| Baseline file | \`${BASELINE_FILE}\` |"
    echo "| Threshold status | no_baseline |"
  fi
  echo
  echo "## Top-level entries"
  echo
  echo "| Entry | Bytes |"
  echo "|---|---:|"
  if [[ -s "${tmp_entries}" ]]; then
    while IFS=$'\t' read -r name size; do
      echo "| \`${name}\` | ${size} |"
    done < "${tmp_entries}"
  else
    echo "| _none_ | 0 |"
  fi
} > "${REPORT_MD}"

if [[ "${ENFORCEMENT_MODE}" == "off" ]]; then
  echo "Build size report generated (enforcement disabled)."
  exit 0
fi

if [[ "${threshold_status}" == "warn" ]]; then
  echo "::warning::Build size delta exceeded warning threshold (${delta_percent}% > ${WARN_DELTA_PERCENT}%)."
fi

if [[ "${threshold_status}" == "fail" && "${ENFORCEMENT_MODE}" == "fail" ]]; then
  echo "::error::Build size delta exceeded fail threshold (${delta_percent}% > ${FAIL_DELTA_PERCENT}%)."
  exit 1
fi

echo "Build size report generated: ${REPORT_JSON}"
