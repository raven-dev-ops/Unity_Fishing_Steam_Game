param(
    [string]$ExplicitReportPath = "",
    [string[]]$SearchRoots = @("Artifacts/Addressables", "PerfLogs"),
    [string[]]$NamePatterns = @("*addressables*dup*.json", "*duplication*.json", "*buildlayout*.json"),
    [string]$BaselineConfigPath = "ci/addressables-duplication-baseline.json",
    [switch]$FailOnWarnings,
    [string]$SummaryJsonPath = "Artifacts/Addressables/duplication_summary.json",
    [string]$SummaryMarkdownPath = "Artifacts/Addressables/duplication_summary.md"
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

function Add-CandidateFile {
    param(
        [string]$CandidatePath,
        [hashtable]$CandidateMap
    )

    if ([string]::IsNullOrWhiteSpace($CandidatePath)) {
        return
    }

    if (-not (Test-Path -LiteralPath $CandidatePath -PathType Leaf)) {
        return
    }

    $fullPath = [System.IO.Path]::GetFullPath($CandidatePath)
    $key = $fullPath.ToLowerInvariant()
    $CandidateMap[$key] = $fullPath
}

function Discover-Files {
    $candidates = @{}

    if (-not [string]::IsNullOrWhiteSpace($ExplicitReportPath)) {
        if (Test-Path -LiteralPath $ExplicitReportPath -PathType Leaf) {
            Add-CandidateFile -CandidatePath $ExplicitReportPath -CandidateMap $candidates
        }
        elseif (Test-Path -LiteralPath $ExplicitReportPath -PathType Container) {
            foreach ($pattern in $NamePatterns) {
                Get-ChildItem -Path $ExplicitReportPath -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
                    Add-CandidateFile -CandidatePath $_.FullName -CandidateMap $candidates
                }
            }
        }
        else {
            throw "Explicit report path '$ExplicitReportPath' was not found."
        }
    }
    else {
        foreach ($root in $SearchRoots) {
            if ([string]::IsNullOrWhiteSpace($root) -or -not (Test-Path -LiteralPath $root -PathType Container)) {
                continue
            }

            foreach ($pattern in $NamePatterns) {
                Get-ChildItem -Path $root -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
                    Add-CandidateFile -CandidatePath $_.FullName -CandidateMap $candidates
                }
            }
        }
    }

    return @($candidates.Values | Sort-Object)
}

function Resolve-DuplicationTotals {
    param($Payload)

    if ($null -eq $Payload) {
        return [ordered]@{
            duplicate_total_mb = 0.0
            duplicate_asset_count = 0
        }
    }

    if ($null -ne $Payload.duplicate_total_bytes) {
        return [ordered]@{
            duplicate_total_mb = [Math]::Round(([double]$Payload.duplicate_total_bytes / 1MB), 2)
            duplicate_asset_count = if ($null -ne $Payload.duplicate_asset_count) { [int]$Payload.duplicate_asset_count } else { 0 }
        }
    }

    if ($null -ne $Payload.duplicate_total_mb) {
        return [ordered]@{
            duplicate_total_mb = [Math]::Round([double]$Payload.duplicate_total_mb, 2)
            duplicate_asset_count = if ($null -ne $Payload.duplicate_asset_count) { [int]$Payload.duplicate_asset_count } else { 0 }
        }
    }

    if ($null -ne $Payload.bundles) {
        $totalBytes = 0.0
        $assetCount = 0
        foreach ($bundle in $Payload.bundles) {
            if ($null -eq $bundle) {
                continue
            }

            if ($null -ne $bundle.duplicate_bytes) {
                $totalBytes += [double]$bundle.duplicate_bytes
            }

            if ($null -ne $bundle.duplicate_asset_count) {
                $assetCount += [int]$bundle.duplicate_asset_count
            }
        }

        return [ordered]@{
            duplicate_total_mb = [Math]::Round(($totalBytes / 1MB), 2)
            duplicate_asset_count = $assetCount
        }
    }

    return [ordered]@{
        duplicate_total_mb = 0.0
        duplicate_asset_count = 0
    }
}

if (-not (Test-Path -LiteralPath $BaselineConfigPath -PathType Leaf)) {
    throw "Baseline config not found: '$BaselineConfigPath'."
}

$baseline = Get-Content -Raw -Path $BaselineConfigPath | ConvertFrom-Json
if ($null -eq $baseline) {
    throw "Unable to parse baseline config '$BaselineConfigPath'."
}

$warnTotalMb = [double]$baseline.warn_duplicate_total_mb
$failTotalMb = [double]$baseline.fail_duplicate_total_mb
$warnAssetCount = [int]$baseline.warn_duplicate_asset_count
$failAssetCount = [int]$baseline.fail_duplicate_asset_count

$files = @(Discover-Files)
$summary = [ordered]@{
    status = "skipped"
    reason = "no_duplication_reports_found"
    generated_utc = (Get-Date).ToUniversalTime().ToString("o")
    baseline_config_path = $BaselineConfigPath
    discovered_files = $files.Count
    warning_count = 0
    failure_count = 0
    entries = @()
}

if ($files.Count -eq 0) {
    Ensure-ParentDirectory -Path $SummaryJsonPath
    ($summary | ConvertTo-Json -Depth 8) | Set-Content -Path $SummaryJsonPath

    Ensure-ParentDirectory -Path $SummaryMarkdownPath
    @(
        "# Addressables Duplication Summary",
        "",
        "Status: **SKIPPED**",
        "Reason: no_duplication_reports_found",
        "",
        "No duplication report files were discovered."
    ) | Set-Content -Path $SummaryMarkdownPath

    Write-Warning "Addressables duplication check skipped: no report files discovered."
    exit 0
}

$entries = New-Object System.Collections.Generic.List[object]
$warningCount = 0
$failureCount = 0

foreach ($file in $files) {
    $payload = $null
    try {
        $payload = Get-Content -Raw -Path $file | ConvertFrom-Json
    }
    catch {
        $failureCount++
        $entries.Add([ordered]@{
            file = $file
            duplicate_total_mb = 0.0
            duplicate_asset_count = 0
            status = "failed"
            reason = "invalid_json"
        })
        continue
    }

    $totals = Resolve-DuplicationTotals -Payload $payload
    $status = "passed"
    $reason = "ok"

    if ($totals.duplicate_total_mb -gt $failTotalMb -or $totals.duplicate_asset_count -gt $failAssetCount) {
        $status = "failed"
        $reason = "duplication_above_fail"
        $failureCount++
    }
    elseif ($totals.duplicate_total_mb -gt $warnTotalMb -or $totals.duplicate_asset_count -gt $warnAssetCount) {
        $status = "warning"
        $reason = "duplication_above_warn"
        $warningCount++
    }

    $entries.Add([ordered]@{
        file = $file
        duplicate_total_mb = $totals.duplicate_total_mb
        duplicate_asset_count = $totals.duplicate_asset_count
        warn_duplicate_total_mb = $warnTotalMb
        fail_duplicate_total_mb = $failTotalMb
        warn_duplicate_asset_count = $warnAssetCount
        fail_duplicate_asset_count = $failAssetCount
        status = $status
        reason = $reason
    })
}

$summary.warning_count = $warningCount
$summary.failure_count = $failureCount
$summary.entries = $entries

if ($failureCount -gt 0) {
    $summary.status = "failed"
    $summary.reason = "one_or_more_reports_failed"
}
elseif ($warningCount -gt 0) {
    $summary.status = "warning"
    $summary.reason = "one_or_more_reports_warn"
}
else {
    $summary.status = "passed"
    $summary.reason = "ok"
}

Ensure-ParentDirectory -Path $SummaryJsonPath
($summary | ConvertTo-Json -Depth 8) | Set-Content -Path $SummaryJsonPath

$markdown = New-Object System.Collections.Generic.List[string]
$markdown.Add("# Addressables Duplication Summary")
$markdown.Add("")
$markdown.Add(("Status: **{0}**" -f $summary.status.ToUpperInvariant()))
$markdown.Add(("Reason: {0}" -f $summary.reason))
$markdown.Add("")
$markdown.Add("| File | Duplicate MB | Duplicate Asset Count | Warn MB | Fail MB | Warn Assets | Fail Assets | Status | Reason |")
$markdown.Add("|---|---:|---:|---:|---:|---:|---:|---|---|")
foreach ($entry in $entries) {
    $markdown.Add(("{0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8}" -f
            $entry.file.Replace("|", "\|"),
            $entry.duplicate_total_mb,
            $entry.duplicate_asset_count,
            $entry.warn_duplicate_total_mb,
            $entry.fail_duplicate_total_mb,
            $entry.warn_duplicate_asset_count,
            $entry.fail_duplicate_asset_count,
            $entry.status.ToUpperInvariant(),
            $entry.reason.Replace("|", "\|")))
}

Ensure-ParentDirectory -Path $SummaryMarkdownPath
@($markdown) | Set-Content -Path $SummaryMarkdownPath

if ($summary.status -eq "failed") {
    Write-Host ("Addressables duplication check: FAILED ({0} failing report(s))." -f $failureCount)
    exit 1
}

if ($summary.status -eq "warning" -and $FailOnWarnings) {
    Write-Host ("Addressables duplication check: WARNING promoted to failure ({0} warning report(s))." -f $warningCount)
    exit 1
}

Write-Host ("Addressables duplication check: {0} ({1} report(s))." -f $summary.status.ToUpperInvariant(), $files.Count)
exit 0
