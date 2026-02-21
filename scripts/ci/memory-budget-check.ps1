param(
    [string]$ExplicitInputPath = "",
    [string[]]$SearchRoots = @("PerfLogs", "Artifacts/Memory"),
    [string[]]$NamePatterns = @("*memory*.json", "*mem*.json"),
    [string]$BaselineConfigPath = "ci/memory-budget-baseline.json",
    [switch]$FailOnWarnings,
    [string]$SummaryJsonPath = "Artifacts/Memory/memory_budget_summary.json",
    [string]$SummaryMarkdownPath = "Artifacts/Memory/memory_budget_summary.md"
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

    if (-not [string]::IsNullOrWhiteSpace($ExplicitInputPath)) {
        if (Test-Path -LiteralPath $ExplicitInputPath -PathType Leaf) {
            Add-CandidateFile -CandidatePath $ExplicitInputPath -CandidateMap $candidates
        }
        elseif (Test-Path -LiteralPath $ExplicitInputPath -PathType Container) {
            foreach ($pattern in $NamePatterns) {
                Get-ChildItem -Path $ExplicitInputPath -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
                    Add-CandidateFile -CandidatePath $_.FullName -CandidateMap $candidates
                }
            }
        }
        else {
            throw "Explicit input path '$ExplicitInputPath' was not found."
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

function Get-Thresholds {
    param(
        $Config,
        [string]$Scene
    )

    $defaultWarn = [double]$Config.default.warn_total_mb
    $defaultFail = [double]$Config.default.fail_total_mb
    $warn = $defaultWarn
    $fail = $defaultFail

    if (-not [string]::IsNullOrWhiteSpace($Scene) -and $null -ne $Config.scenes) {
        $property = $Config.scenes.PSObject.Properties | Where-Object { $_.Name -ieq $Scene } | Select-Object -First 1
        if ($null -ne $property -and $null -ne $property.Value) {
            if ($null -ne $property.Value.warn_total_mb) {
                $warn = [double]$property.Value.warn_total_mb
            }

            if ($null -ne $property.Value.fail_total_mb) {
                $fail = [double]$property.Value.fail_total_mb
            }
        }
    }

    return [ordered]@{
        warn_total_mb = $warn
        fail_total_mb = $fail
    }
}

if (-not (Test-Path -LiteralPath $BaselineConfigPath -PathType Leaf)) {
    throw "Baseline config not found: '$BaselineConfigPath'."
}

$baselineConfig = Get-Content -Raw -Path $BaselineConfigPath | ConvertFrom-Json
if ($null -eq $baselineConfig -or $null -eq $baselineConfig.default) {
    throw "Baseline config '$BaselineConfigPath' is missing required 'default' thresholds."
}

$files = @(Discover-Files)
$summary = [ordered]@{
    status = "skipped"
    reason = "no_memory_samples_found"
    generated_utc = (Get-Date).ToUniversalTime().ToString("o")
    baseline_config_path = $BaselineConfigPath
    discovered_files = $files.Count
    sample_count = 0
    warning_count = 0
    failure_count = 0
    entries = @()
}

if ($files.Count -eq 0) {
    Ensure-ParentDirectory -Path $SummaryJsonPath
    ($summary | ConvertTo-Json -Depth 8) | Set-Content -Path $SummaryJsonPath

    Ensure-ParentDirectory -Path $SummaryMarkdownPath
    @(
        "# Memory Budget Summary",
        "",
        "Status: **SKIPPED**",
        "Reason: no_memory_samples_found",
        "",
        "No memory sample JSON files were discovered."
    ) | Set-Content -Path $SummaryMarkdownPath

    Write-Warning "Memory budget check skipped: no memory samples discovered."
    exit 0
}

$entries = New-Object System.Collections.Generic.List[object]
$warningCount = 0
$failureCount = 0
$sampleCount = 0

foreach ($file in $files) {
    $payload = $null
    try {
        $payload = Get-Content -Raw -Path $file | ConvertFrom-Json
    }
    catch {
        $failureCount++
        $entries.Add([ordered]@{
            file = $file
            scene = "unknown"
            tier = "unknown"
            total_mb = 0
            warn_total_mb = 0
            fail_total_mb = 0
            status = "failed"
            reason = "invalid_json"
        })
        continue
    }

    $samples = @()
    if ($payload -is [System.Array]) {
        $samples = @($payload)
    }
    elseif ($null -ne $payload.samples) {
        $samples = @($payload.samples)
    }
    else {
        $samples = @($payload)
    }

    foreach ($sample in $samples) {
        $sampleCount++
        $scene = if ($null -ne $sample.scene) { [string]$sample.scene } else { "unknown" }
        $tier = if ($null -ne $sample.tier -and -not [string]::IsNullOrWhiteSpace($sample.tier)) { [string]$sample.tier } else { "minimum" }
        $totalMb = if ($null -ne $sample.total_mb) { [double]$sample.total_mb } elseif ($null -ne $sample.totalMemoryMb) { [double]$sample.totalMemoryMb } else { 0.0 }
        $thresholds = Get-Thresholds -Config $baselineConfig -Scene $scene

        $status = "passed"
        $reason = "ok"
        if ($totalMb -gt $thresholds.fail_total_mb) {
            $status = "failed"
            $reason = "total_mb_above_fail"
            $failureCount++
        }
        elseif ($totalMb -gt $thresholds.warn_total_mb) {
            $status = "warning"
            $reason = "total_mb_above_warn"
            $warningCount++
        }

        $entries.Add([ordered]@{
            file = $file
            scene = $scene
            tier = $tier
            total_mb = [Math]::Round($totalMb, 2)
            warn_total_mb = $thresholds.warn_total_mb
            fail_total_mb = $thresholds.fail_total_mb
            status = $status
            reason = $reason
        })
    }
}

$summary.sample_count = $sampleCount
$summary.warning_count = $warningCount
$summary.failure_count = $failureCount
$summary.entries = $entries

if ($failureCount -gt 0) {
    $summary.status = "failed"
    $summary.reason = "one_or_more_samples_failed"
}
elseif ($warningCount -gt 0) {
    $summary.status = "warning"
    $summary.reason = "one_or_more_samples_warn"
}
else {
    $summary.status = "passed"
    $summary.reason = "ok"
}

Ensure-ParentDirectory -Path $SummaryJsonPath
($summary | ConvertTo-Json -Depth 8) | Set-Content -Path $SummaryJsonPath

$markdown = New-Object System.Collections.Generic.List[string]
$markdown.Add("# Memory Budget Summary")
$markdown.Add("")
$markdown.Add(("Status: **{0}**" -f $summary.status.ToUpperInvariant()))
$markdown.Add(("Reason: {0}" -f $summary.reason))
$markdown.Add(("Samples: {0}" -f $summary.sample_count))
$markdown.Add("")
$markdown.Add("| File | Scene | Tier | Total MB | Warn MB | Fail MB | Status | Reason |")
$markdown.Add("|---|---|---|---:|---:|---:|---|---|")
foreach ($entry in $entries) {
    $markdown.Add(("{0} | {1} | {2} | {3} | {4} | {5} | {6} | {7}" -f
            $entry.file.Replace("|", "\|"),
            $entry.scene.Replace("|", "\|"),
            $entry.tier.Replace("|", "\|"),
            $entry.total_mb,
            $entry.warn_total_mb,
            $entry.fail_total_mb,
            $entry.status.ToUpperInvariant(),
            $entry.reason.Replace("|", "\|")))
}

Ensure-ParentDirectory -Path $SummaryMarkdownPath
@($markdown) | Set-Content -Path $SummaryMarkdownPath

if ($summary.status -eq "failed") {
    Write-Host ("Memory budget check: FAILED ({0} failing sample(s))." -f $failureCount)
    exit 1
}

if ($summary.status -eq "warning" -and $FailOnWarnings) {
    Write-Host ("Memory budget check: WARNING promoted to failure ({0} warning sample(s))." -f $warningCount)
    exit 1
}

Write-Host ("Memory budget check: {0} ({1} sample(s))." -f $summary.status.ToUpperInvariant(), $sampleCount)
exit 0
