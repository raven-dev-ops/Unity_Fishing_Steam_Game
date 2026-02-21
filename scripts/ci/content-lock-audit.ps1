param(
    [string]$PlaceholderManifestPath = "Assets/Art/Placeholders/placeholder_manifest.json",
    [string]$ReplacementPlanPath = "ci/content-lock-replacements.json",
    [switch]$FailOnFindings,
    [string]$SummaryJsonPath = "Artifacts/ContentLock/content_lock_summary.json",
    [string]$SummaryMarkdownPath = "Artifacts/ContentLock/content_lock_summary.md"
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

function Get-MetaGuid {
    param([string]$AssetPath)

    $metaPath = "$AssetPath.meta"
    if (-not (Test-Path -LiteralPath $metaPath -PathType Leaf)) {
        return ""
    }

    $guidLine = Select-String -Path $metaPath -Pattern "^guid:\s*(?<guid>[a-f0-9]+)\s*$" | Select-Object -First 1
    if ($null -eq $guidLine) {
        return ""
    }

    return [string]$guidLine.Matches[0].Groups["guid"].Value
}

function Get-ExternalReferences {
    param(
        [string]$GuidValue,
        [string]$PlaceholderRoot = "Assets/Art/Placeholders"
    )

    if ([string]::IsNullOrWhiteSpace($GuidValue)) {
        return @()
    }

    $args = @(
        "--files-with-matches",
        "--fixed-strings",
        "--glob", "!$PlaceholderRoot/**",
        "--glob", "!**/*.meta",
        $GuidValue,
        "Assets"
    )

    $results = & rg @args 2>$null
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 1) {
        throw "rg failed while scanning references for guid '$GuidValue'."
    }

    if ($null -eq $results) {
        return @()
    }

    return @($results | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

if (-not (Test-Path -LiteralPath $PlaceholderManifestPath -PathType Leaf)) {
    throw "Placeholder manifest not found: '$PlaceholderManifestPath'."
}

if (-not (Test-Path -LiteralPath $ReplacementPlanPath -PathType Leaf)) {
    throw "Replacement plan not found: '$ReplacementPlanPath'."
}

$manifest = Get-Content -Raw -Path $PlaceholderManifestPath | ConvertFrom-Json
$plan = Get-Content -Raw -Path $ReplacementPlanPath | ConvertFrom-Json

if ($null -eq $manifest -or $null -eq $manifest.entries) {
    throw "Placeholder manifest '$PlaceholderManifestPath' is missing 'entries'."
}

if ($null -eq $plan) {
    throw "Replacement plan '$ReplacementPlanPath' is empty."
}

$waiverRequiredFields = @("id", "owner", "reason", "expires_on", "ticket")
$maxWaiverDays = 30
if ($null -ne $plan.waiver_policy) {
    if ($null -ne $plan.waiver_policy.required_fields) {
        $waiverRequiredFields = @($plan.waiver_policy.required_fields | ForEach-Object { [string]$_ })
    }
    if ($null -ne $plan.waiver_policy.max_waiver_days) {
        $maxWaiverDays = [int]$plan.waiver_policy.max_waiver_days
    }
}

$replacements = @()
if ($null -ne $plan.replacements) {
    $replacements = @($plan.replacements)
}

$waivers = @()
if ($null -ne $plan.waivers) {
    $waivers = @($plan.waivers)
}

$nowUtc = (Get-Date).ToUniversalTime()
$entries = New-Object System.Collections.Generic.List[object]
$warnings = New-Object System.Collections.Generic.List[object]
$failures = New-Object System.Collections.Generic.List[object]

# Validate waiver definitions first.
foreach ($waiver in $waivers) {
    if ($null -eq $waiver) {
        $failures.Add([ordered]@{
            scope = "waiver"
            id = "unknown"
            reason = "null_waiver_entry"
        })
        continue
    }

    foreach ($field in $waiverRequiredFields) {
        $value = $waiver.$field
        if ($null -eq $value -or ([string]$value).Trim().Length -eq 0) {
            $failures.Add([ordered]@{
                scope = "waiver"
                id = [string]$waiver.id
                reason = "waiver_field_missing:$field"
            })
        }
    }

    $expiryUtc = Parse-DateUtc -Value ([string]$waiver.expires_on)
    if ($null -eq $expiryUtc) {
        $failures.Add([ordered]@{
            scope = "waiver"
            id = [string]$waiver.id
            reason = "waiver_expiry_invalid"
        })
    }
    else {
        if ($expiryUtc -lt $nowUtc.Date) {
            $failures.Add([ordered]@{
                scope = "waiver"
                id = [string]$waiver.id
                reason = "waiver_expired"
            })
        }

        $daysUntilExpiry = [Math]::Ceiling(($expiryUtc - $nowUtc.Date).TotalDays)
        if ($daysUntilExpiry -gt $maxWaiverDays) {
            $warnings.Add([ordered]@{
                scope = "waiver"
                id = [string]$waiver.id
                reason = "waiver_exceeds_policy_window"
                days_until_expiry = $daysUntilExpiry
            })
        }
    }
}

foreach ($manifestEntry in $manifest.entries) {
    if ($null -eq $manifestEntry) {
        continue
    }

    $id = [string]$manifestEntry.id
    $category = [string]$manifestEntry.category
    $path = [string]$manifestEntry.path
    $assetExists = Test-Path -LiteralPath $path -PathType Leaf

    $replacement = $replacements | Where-Object { $_ -ne $null -and [string]$_.id -eq $id } | Select-Object -First 1
    $replacementStatus = "missing"
    $replacementPath = ""
    $replacementOwner = ""
    if ($null -ne $replacement) {
        $replacementStatus = if ([string]::IsNullOrWhiteSpace([string]$replacement.status)) { "pending" } else { [string]$replacement.status }
        $replacementPath = [string]$replacement.replacementPath
        $replacementOwner = [string]$replacement.owner
    }

    if ($replacementStatus -eq "complete") {
        if ([string]::IsNullOrWhiteSpace($replacementPath) -or -not (Test-Path -LiteralPath $replacementPath -PathType Leaf)) {
            $failures.Add([ordered]@{
                scope = "replacement"
                id = $id
                reason = "replacement_marked_complete_but_path_missing"
            })
        }
    }

    $guid = if ($assetExists) { Get-MetaGuid -AssetPath $path } else { "" }
    if ($assetExists -and [string]::IsNullOrWhiteSpace($guid)) {
        $failures.Add([ordered]@{
            scope = "placeholder_asset"
            id = $id
            reason = "meta_guid_missing"
        })
    }

    $externalReferences = @()
    if ($assetExists -and -not [string]::IsNullOrWhiteSpace($guid)) {
        $externalReferences = Get-ExternalReferences -GuidValue $guid
    }

    $waiver = $waivers | Where-Object {
        $_ -ne $null -and (
            [string]$_.id -eq "*" -or [string]$_.id -eq $id
        )
    } | Sort-Object { [string]$_.id -eq "*" } | Select-Object -First 1

    $hasActiveWaiver = $false
    if ($null -ne $waiver) {
        $expiryUtc = Parse-DateUtc -Value ([string]$waiver.expires_on)
        if ($null -ne $expiryUtc -and $expiryUtc -ge $nowUtc.Date) {
            $hasActiveWaiver = $true
        }
    }

    $entryStatus = "ok"
    $entryReason = "replacement_complete_or_not_referenced"

    if ($externalReferences.Count -gt 0 -and $replacementStatus -ne "complete") {
        if ($hasActiveWaiver) {
            $entryStatus = "waived"
            $entryReason = "placeholder_referenced_with_active_waiver"
        }
        else {
            $entryStatus = "warning"
            $entryReason = "placeholder_referenced_without_replacement"
            $warnings.Add([ordered]@{
                scope = "placeholder_reference"
                id = $id
                reason = $entryReason
                reference_count = $externalReferences.Count
            })
        }
    }
    elseif ($replacementStatus -ne "complete") {
        $entryStatus = "pending"
        $entryReason = "replacement_not_complete"
    }

    $entries.Add([ordered]@{
        id = $id
        category = $category
        placeholder_path = $path
        replacement_status = $replacementStatus
        replacement_path = $replacementPath
        replacement_owner = $replacementOwner
        reference_count = $externalReferences.Count
        has_active_waiver = $hasActiveWaiver
        status = $entryStatus
        reason = $entryReason
    })
}

$summary = [ordered]@{
    status = "passed"
    reason = "ok"
    generated_utc = $nowUtc.ToString("o")
    placeholder_manifest_path = $PlaceholderManifestPath
    replacement_plan_path = $ReplacementPlanPath
    placeholder_count = $entries.Count
    warning_count = $warnings.Count
    failure_count = $failures.Count
    replacements_complete_count = @($entries | Where-Object { $_.replacement_status -eq "complete" }).Count
    referenced_placeholder_count = @($entries | Where-Object { $_.reference_count -gt 0 }).Count
    entries = $entries
    warnings = $warnings
    failures = $failures
}

if ($failures.Count -gt 0) {
    $summary.status = "failed"
    $summary.reason = "policy_failures"
}
elseif ($warnings.Count -gt 0) {
    $summary.status = "warning"
    $summary.reason = "open_findings"
}

if ($summary.status -eq "warning" -and $FailOnFindings) {
    $summary.status = "failed"
    $summary.reason = "open_findings_fail_on_findings"
}

Ensure-ParentDirectory -Path $SummaryJsonPath
($summary | ConvertTo-Json -Depth 12) | Set-Content -Path $SummaryJsonPath

$md = New-Object System.Collections.Generic.List[string]
$md.Add("# Content Lock Audit Summary")
$md.Add("")
$md.Add(("Status: **{0}**" -f $summary.status.ToUpperInvariant()))
$md.Add(("Reason: {0}" -f $summary.reason))
$md.Add(("Placeholder entries: {0}" -f $summary.placeholder_count))
$md.Add(("Referenced placeholders: {0}" -f $summary.referenced_placeholder_count))
$md.Add(("Replacements complete: {0}" -f $summary.replacements_complete_count))
$md.Add("")
$md.Add("| ID | Category | Replacement Status | References | Waiver | Status |")
$md.Add("|---|---|---|---:|---|---|")
foreach ($entry in $entries) {
    $md.Add(("{0} | {1} | {2} | {3} | {4} | {5}" -f $entry.id, $entry.category, $entry.replacement_status, $entry.reference_count, $entry.has_active_waiver, $entry.status))
}
$md.Add("")

if ($failures.Count -gt 0) {
    $md.Add("## Failures")
    foreach ($failure in $failures) {
        $md.Add(("- scope={0} id={1} reason={2}" -f $failure.scope, $failure.id, $failure.reason))
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
        if ($null -ne $warning.reference_count) {
            $extra = (" reference_count={0}" -f $warning.reference_count)
        }
        elseif ($null -ne $warning.days_until_expiry) {
            $extra = (" days_until_expiry={0}" -f $warning.days_until_expiry)
        }
        $md.Add(("- scope={0} id={1} reason={2}{3}" -f $warning.scope, $warning.id, $warning.reason, $extra))
    }
}
else {
    $md.Add("## Warnings")
    $md.Add("- none")
}

Ensure-ParentDirectory -Path $SummaryMarkdownPath
@($md) | Set-Content -Path $SummaryMarkdownPath

if ($summary.status -eq "failed") {
    Write-Host "Content lock audit: FAILED"
    exit 1
}

Write-Host ("Content lock audit: {0}" -f $summary.status.ToUpperInvariant())
exit 0
