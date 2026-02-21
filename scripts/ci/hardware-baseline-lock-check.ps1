param(
    [string]$MatrixPath = "ci/hardware-baseline-matrix.json",
    [switch]$RequireAllTiersValidated,
    [switch]$FailOnWarnings,
    [string]$SummaryJsonPath = "Artifacts/Hardware/hardware_baseline_lock_summary.json",
    [string]$SummaryMarkdownPath = "Artifacts/Hardware/hardware_baseline_lock_summary.md"
)

$ErrorActionPreference = "Stop"

function Ensure-ParentDirectory {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
}

function Normalize-Tier {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Trim().ToLowerInvariant()
}

function Parse-DateUtc {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    try {
        return ([DateTime]::Parse($Value)).ToUniversalTime()
    }
    catch {
        return $null
    }
}

if (-not (Test-Path -LiteralPath $MatrixPath -PathType Leaf)) {
    throw "Hardware baseline matrix not found: '$MatrixPath'."
}

$matrix = Get-Content -Raw -Path $MatrixPath | ConvertFrom-Json
if ($null -eq $matrix -or $null -eq $matrix.tiers) {
    throw "Hardware baseline matrix '$MatrixPath' is missing 'tiers'."
}

$requiredTiers = @("minimum", "recommended", "reference")
if ($null -ne $matrix.capture_policy -and $null -ne $matrix.capture_policy.required_tiers) {
    $requiredTiers = @($matrix.capture_policy.required_tiers | ForEach-Object { Normalize-Tier $_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

$maxCaptureAgeDays = 30
if ($null -ne $matrix.capture_policy -and $null -ne $matrix.capture_policy.max_capture_age_days) {
    $maxCaptureAgeDays = [int]$matrix.capture_policy.max_capture_age_days
}

$waiverRequiredFields = @("tier", "owner", "reason", "expires_on", "ticket")
$maxWaiverDays = 14
if ($null -ne $matrix.waiver_policy) {
    if ($null -ne $matrix.waiver_policy.required_fields) {
        $waiverRequiredFields = @($matrix.waiver_policy.required_fields | ForEach-Object { [string]$_ })
    }
    if ($null -ne $matrix.waiver_policy.max_waiver_days) {
        $maxWaiverDays = [int]$matrix.waiver_policy.max_waiver_days
    }
}

$nowUtc = (Get-Date).ToUniversalTime()
$entries = New-Object System.Collections.Generic.List[object]
$warnings = New-Object System.Collections.Generic.List[object]
$failures = New-Object System.Collections.Generic.List[object]
$waivers = @()
if ($null -ne $matrix.waivers) {
    $waivers = @($matrix.waivers)
}

$activeWaiverTiers = @{}
foreach ($waiver in $waivers) {
    if ($null -eq $waiver) {
        continue
    }

    $waiverTier = Normalize-Tier -Value ([string]$waiver.tier)
    if ([string]::IsNullOrWhiteSpace($waiverTier)) {
        continue
    }

    $waiverExpiry = Parse-DateUtc -Value ([string]$waiver.expires_on)
    if ($null -eq $waiverExpiry -or $waiverExpiry -lt $nowUtc.Date) {
        continue
    }

    if (-not $activeWaiverTiers.ContainsKey($waiverTier)) {
        $activeWaiverTiers[$waiverTier] = $true
    }
}

function Test-ActiveTierWaiver {
    param([string]$Tier)

    $normalizedTier = Normalize-Tier -Value $Tier
    if ($activeWaiverTiers.ContainsKey($normalizedTier)) {
        return $true
    }

    return $activeWaiverTiers.ContainsKey("*")
}

foreach ($tier in $requiredTiers) {
    $tierHasActiveWaiver = Test-ActiveTierWaiver -Tier $tier

    $tierProperty = $matrix.tiers.PSObject.Properties | Where-Object { $_.Name -ieq $tier } | Select-Object -First 1
    if ($null -eq $tierProperty -or $null -eq $tierProperty.Value) {
        $failures.Add([ordered]@{
            scope = "tier"
            tier = $tier
            reason = "tier_missing"
        })
        continue
    }

    $tierConfig = $tierProperty.Value
    $targetSpec = $tierConfig.target_spec
    if ($null -eq $targetSpec) {
        $failures.Add([ordered]@{
            scope = "tier"
            tier = $tier
            reason = "target_spec_missing"
        })
    }
    else {
        foreach ($requiredField in @("os", "cpu", "gpu", "ram_gb_min")) {
            $value = $targetSpec.$requiredField
            if ($null -eq $value -or ([string]$value).Trim().Length -eq 0) {
                $failures.Add([ordered]@{
                    scope = "tier"
                    tier = $tier
                    reason = "target_spec_field_missing:$requiredField"
                })
            }
        }
    }

    $captures = @()
    if ($null -ne $tierConfig.captures) {
        $captures = @($tierConfig.captures)
    }

    if ($captures.Count -eq 0) {
        if ($RequireAllTiersValidated -and -not $tierHasActiveWaiver) {
            $failures.Add([ordered]@{
                scope = "tier"
                tier = $tier
                reason = "no_capture_entries"
            })
        }
        else {
            $warningReason = if ($tierHasActiveWaiver) { "tier_validation_waived_no_capture" } else { "no_capture_entries" }
            $warnings.Add([ordered]@{
                scope = "tier"
                tier = $tier
                reason = $warningReason
            })
        }
        $entries.Add([ordered]@{
            tier = $tier
            capture_count = 0
            validated_capture_count = 0
            newest_capture_utc = ""
            status = if ($tierHasActiveWaiver) { "waived" } elseif ($RequireAllTiersValidated) { "failed" } else { "warning" }
            reason = if ($tierHasActiveWaiver) { "tier_validation_waived_no_capture" } else { "no_capture_entries" }
        })
        continue
    }

    $validatedCount = 0
    $newestCaptureUtc = $null
    foreach ($capture in $captures) {
        if ($null -eq $capture) {
            $failures.Add([ordered]@{
                scope = "capture"
                tier = $tier
                reason = "null_capture_entry"
            })
            continue
        }

        $captureId = [string]$capture.capture_id
        if ([string]::IsNullOrWhiteSpace($captureId)) {
            $failures.Add([ordered]@{
                scope = "capture"
                tier = $tier
                reason = "capture_id_missing"
            })
        }

        $capturedUtc = Parse-DateUtc -Value ([string]$capture.captured_utc)
        if ($null -eq $capturedUtc) {
            $failures.Add([ordered]@{
                scope = "capture"
                tier = $tier
                reason = "captured_utc_invalid"
            })
        }
        else {
            if ($null -eq $newestCaptureUtc -or $capturedUtc -gt $newestCaptureUtc) {
                $newestCaptureUtc = $capturedUtc
            }
        }

        if ([string]::IsNullOrWhiteSpace([string]$capture.source)) {
            $failures.Add([ordered]@{
                scope = "capture"
                tier = $tier
                reason = "source_missing"
            })
        }

        if ($null -eq $capture.machine) {
            $failures.Add([ordered]@{
                scope = "capture"
                tier = $tier
                reason = "machine_block_missing"
            })
        }
        else {
            foreach ($machineField in @("os", "cpu", "gpu", "ram_gb")) {
                $machineValue = $capture.machine.$machineField
                if ($null -eq $machineValue -or ([string]$machineValue).Trim().Length -eq 0) {
                    $failures.Add([ordered]@{
                        scope = "capture"
                        tier = $tier
                        reason = "machine_field_missing:$machineField"
                    })
                }
            }
        }

        $isValidated = $false
        if ($null -ne $capture.validated) {
            $isValidated = [bool]$capture.validated
        }

        if ($isValidated) {
            $validatedCount++
        }
    }

    if ($null -ne $newestCaptureUtc) {
        $captureAgeDays = [Math]::Floor(($nowUtc - $newestCaptureUtc).TotalDays)
        if ($captureAgeDays -gt $maxCaptureAgeDays) {
            $warnings.Add([ordered]@{
                scope = "tier"
                tier = $tier
                reason = "capture_too_old"
                age_days = $captureAgeDays
            })
        }
    }

    if ($validatedCount -eq 0) {
        $message = "no_validated_capture"
        if ($RequireAllTiersValidated -and -not $tierHasActiveWaiver) {
            $failures.Add([ordered]@{
                scope = "tier"
                tier = $tier
                reason = $message
            })
        }
        else {
            if ($tierHasActiveWaiver) {
                $message = "tier_validation_waived_no_validated_capture"
            }
            $warnings.Add([ordered]@{
                scope = "tier"
                tier = $tier
                reason = $message
            })
        }
    }

    $entries.Add([ordered]@{
        tier = $tier
        capture_count = $captures.Count
        validated_capture_count = $validatedCount
        newest_capture_utc = if ($null -eq $newestCaptureUtc) { "" } else { $newestCaptureUtc.ToString("o") }
        status = if ($validatedCount -gt 0) { "ok" } elseif ($tierHasActiveWaiver) { "waived" } else { "pending_validation" }
        reason = if ($validatedCount -gt 0) { "ok" } elseif ($tierHasActiveWaiver) { "tier_validation_waived_pending_capture" } else { "awaiting_validated_capture" }
    })
}

$waiverFailures = 0
foreach ($waiver in $waivers) {
    if ($null -eq $waiver) {
        $waiverFailures++
        $failures.Add([ordered]@{
            scope = "waiver"
            tier = "unknown"
            reason = "null_waiver_entry"
        })
        continue
    }

    foreach ($field in $waiverRequiredFields) {
        $value = $waiver.$field
        if ($null -eq $value -or ([string]$value).Trim().Length -eq 0) {
            $waiverFailures++
            $failures.Add([ordered]@{
                scope = "waiver"
                tier = [string]$waiver.tier
                reason = "waiver_field_missing:$field"
            })
        }
    }

    $expiry = Parse-DateUtc -Value ([string]$waiver.expires_on)
    if ($null -eq $expiry) {
        $waiverFailures++
        $failures.Add([ordered]@{
            scope = "waiver"
            tier = [string]$waiver.tier
            reason = "waiver_expiry_invalid"
        })
    }
    else {
        if ($expiry -lt $nowUtc.Date) {
            $waiverFailures++
            $failures.Add([ordered]@{
                scope = "waiver"
                tier = [string]$waiver.tier
                reason = "waiver_expired"
            })
        }

        $daysUntilExpiry = [Math]::Ceiling(($expiry - $nowUtc.Date).TotalDays)
        if ($daysUntilExpiry -gt $maxWaiverDays) {
            $warnings.Add([ordered]@{
                scope = "waiver"
                tier = [string]$waiver.tier
                reason = "waiver_exceeds_policy_window"
                days_until_expiry = $daysUntilExpiry
            })
        }
    }
}

$summary = [ordered]@{
    status = "passed"
    reason = "ok"
    generated_utc = $nowUtc.ToString("o")
    matrix_path = $MatrixPath
    require_all_tiers_validated = [bool]$RequireAllTiersValidated
    required_tier_count = $requiredTiers.Count
    tiers_evaluated = $entries.Count
    warning_count = $warnings.Count
    failure_count = $failures.Count
    entries = $entries
    warnings = $warnings
    failures = $failures
}

if ($failures.Count -gt 0) {
    $summary.status = "failed"
    $summary.reason = "lock_policy_failures"
}
elseif ($warnings.Count -gt 0) {
    $summary.status = "warning"
    $summary.reason = "lock_policy_warnings"
}

Ensure-ParentDirectory -Path $SummaryJsonPath
($summary | ConvertTo-Json -Depth 10) | Set-Content -Path $SummaryJsonPath

$md = New-Object System.Collections.Generic.List[string]
$md.Add("# Hardware Baseline Lock Summary")
$md.Add("")
$md.Add(("Status: **{0}**" -f $summary.status.ToUpperInvariant()))
$md.Add(("Reason: {0}" -f $summary.reason))
$md.Add(("Matrix: {0}" -f $MatrixPath))
$md.Add(("Require all tiers validated: {0}" -f ([bool]$RequireAllTiersValidated)))
$md.Add("")
$md.Add("| Tier | Captures | Validated | Newest Capture UTC | Status |")
$md.Add("|---|---:|---:|---|---|")
foreach ($entry in $entries) {
    $md.Add(("{0} | {1} | {2} | {3} | {4}" -f $entry.tier, $entry.capture_count, $entry.validated_capture_count, $entry.newest_capture_utc, $entry.status))
}
$md.Add("")
if ($failures.Count -gt 0) {
    $md.Add("## Failures")
    foreach ($failure in $failures) {
        $md.Add(("- scope={0} tier={1} reason={2}" -f $failure.scope, $failure.tier, $failure.reason))
    }
}
else {
    $md.Add("## Failures")
    $md.Add("- none")
}

$md.Add("")
if ($warnings.Count -gt 0) {
    $md.Add("## Warnings")
    foreach ($warning in $warnings) {
        $extra = ""
        if ($null -ne $warning.age_days) {
            $extra = (" age_days={0}" -f $warning.age_days)
        }
        elseif ($null -ne $warning.days_until_expiry) {
            $extra = (" days_until_expiry={0}" -f $warning.days_until_expiry)
        }
        $md.Add(("- scope={0} tier={1} reason={2}{3}" -f $warning.scope, $warning.tier, $warning.reason, $extra))
    }
}
else {
    $md.Add("## Warnings")
    $md.Add("- none")
}

Ensure-ParentDirectory -Path $SummaryMarkdownPath
@($md) | Set-Content -Path $SummaryMarkdownPath

if ($summary.status -eq "failed") {
    Write-Host "Hardware baseline lock check: FAILED"
    exit 1
}

if ($summary.status -eq "warning" -and $FailOnWarnings) {
    Write-Host "Hardware baseline lock check: WARNING promoted to failure"
    exit 1
}

Write-Host ("Hardware baseline lock check: {0}" -f $summary.status.ToUpperInvariant())
exit 0
