param(
    [string]$ExplicitLogFile = "",
    [string[]]$SearchRoots = @("PerfLogs", "Artifacts/Perf/Captured"),
    [string[]]$NamePatterns = @("perf*.log", "*perf*.log", "*sanity*.log"),
    [double]$MinAverageFps = 60.0,
    [double]$MaxP95FrameMs = 25.0,
    [double]$MaxGcDeltaKb = 64.0,
    [int]$MinSamples = 1,
    [string]$SummaryJsonPath = "Artifacts/Perf/perf_ingestion_summary.json",
    [string]$SummaryMarkdownPath = "Artifacts/Perf/perf_ingestion_summary.md",
    [string]$PerLogOutputDirectory = "Artifacts/Perf/Ingested"
)

$ErrorActionPreference = "Stop"

function Ensure-Directory {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Ensure-ParentDirectory {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        Ensure-Directory -Path $directory
    }
}

function Escape-MarkdownCell {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return ($Value.Replace("|", "\|").Replace("`r", " ").Replace("`n", " "))
}

function Add-CandidateLog {
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

function Discover-PerfLogs {
    $candidates = @{}

    if (-not [string]::IsNullOrWhiteSpace($ExplicitLogFile)) {
        if (Test-Path -LiteralPath $ExplicitLogFile -PathType Leaf) {
            Add-CandidateLog -CandidatePath $ExplicitLogFile -CandidateMap $candidates
        }
        elseif (Test-Path -LiteralPath $ExplicitLogFile -PathType Container) {
            foreach ($pattern in $NamePatterns) {
                Get-ChildItem -Path $ExplicitLogFile -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
                    Add-CandidateLog -CandidatePath $_.FullName -CandidateMap $candidates
                }
            }
        }
        else {
            throw "Explicit perf log path '$ExplicitLogFile' was not found."
        }
    }
    else {
        foreach ($root in $SearchRoots) {
            if ([string]::IsNullOrWhiteSpace($root) -or -not (Test-Path -LiteralPath $root -PathType Container)) {
                continue
            }

            foreach ($pattern in $NamePatterns) {
                Get-ChildItem -Path $root -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
                    Add-CandidateLog -CandidatePath $_.FullName -CandidateMap $candidates
                }
            }
        }
    }

    return @($candidates.Values | Sort-Object)
}

function Write-IngestionArtifacts {
    param(
        [hashtable]$Summary,
        [string[]]$MarkdownLines
    )

    Ensure-ParentDirectory -Path $SummaryJsonPath
    ($Summary | ConvertTo-Json -Depth 8) | Set-Content -Path $SummaryJsonPath

    Ensure-ParentDirectory -Path $SummaryMarkdownPath
    $MarkdownLines | Set-Content -Path $SummaryMarkdownPath
}

$parserScriptPath = Join-Path $PSScriptRoot "perf-budget-check.ps1"
if (-not (Test-Path -LiteralPath $parserScriptPath -PathType Leaf)) {
    throw "Missing parser script '$parserScriptPath'."
}

$discoveredLogs = @(Discover-PerfLogs)
if ($discoveredLogs.Count -eq 0) {
    $summary = [ordered]@{
        status = "skipped"
        reason = "no_logs_found"
        generated_utc = (Get-Date).ToUniversalTime().ToString("o")
        explicit_log_file = $ExplicitLogFile
        discovered_count = 0
        passed_count = 0
        failed_count = 0
        thresholds = [ordered]@{
            min_average_fps = $MinAverageFps
            max_p95_frame_ms = $MaxP95FrameMs
            max_gc_delta_kb = $MaxGcDeltaKb
            min_samples = $MinSamples
        }
        entries = @()
    }

    $markdown = @(
        "# Perf Ingestion Summary",
        "",
        "Status: **SKIPPED**",
        "Reason: no_logs_found",
        "",
        "No matching perf logs were discovered."
    )

    Write-IngestionArtifacts -Summary $summary -MarkdownLines $markdown
    Write-Warning "Perf ingestion skipped: no matching logs discovered."
    exit 0
}

Ensure-Directory -Path $PerLogOutputDirectory

$entries = New-Object System.Collections.Generic.List[object]
$passedCount = 0
$failedCount = 0

for ($index = 0; $index -lt $discoveredLogs.Count; $index++) {
    $logFile = $discoveredLogs[$index]
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($logFile)
    if ([string]::IsNullOrWhiteSpace($baseName)) {
        $baseName = "log_$($index + 1)"
    }

    $safeName = [System.Text.RegularExpressions.Regex]::Replace($baseName, "[^a-zA-Z0-9_.-]", "_")
    $entryDirectory = Join-Path $PerLogOutputDirectory ("{0:D2}_{1}" -f ($index + 1), $safeName)
    Ensure-Directory -Path $entryDirectory

    $entrySummaryJson = Join-Path $entryDirectory "perf_budget_summary.json"
    $entrySummaryText = Join-Path $entryDirectory "perf_budget_summary.txt"

    $parserExitCode = 1
    $parserException = $null
    try {
        $parserExitCode = & $parserScriptPath `
            -LogFile $logFile `
            -MinAverageFps $MinAverageFps `
            -MaxP95FrameMs $MaxP95FrameMs `
            -MaxGcDeltaKb $MaxGcDeltaKb `
            -MinSamples $MinSamples `
            -SummaryJsonPath $entrySummaryJson `
            -SummaryTextPath $entrySummaryText `
            -NoExit

        if ($null -eq $parserExitCode) {
            $parserExitCode = 0
        }
    }
    catch {
        $parserException = $_.Exception.Message
        Write-Warning ("Perf ingestion: parser failed for '{0}' ({1})." -f $logFile, $parserException)
    }

    $status = "failed"
    $reason = if ([string]::IsNullOrWhiteSpace($parserException)) { "parser_failure" } else { $parserException }
    $sampleCount = 0
    $failureCount = 0

    if (Test-Path -LiteralPath $entrySummaryJson -PathType Leaf) {
        $entrySummary = Get-Content -Raw $entrySummaryJson | ConvertFrom-Json
        if (-not [string]::IsNullOrWhiteSpace($entrySummary.status)) {
            $status = [string]$entrySummary.status
        }

        if (-not [string]::IsNullOrWhiteSpace($entrySummary.reason)) {
            $reason = [string]$entrySummary.reason
        }

        if ($null -ne $entrySummary.sample_count) {
            $sampleCount = [int]$entrySummary.sample_count
        }

        if ($null -ne $entrySummary.failure_count) {
            $failureCount = [int]$entrySummary.failure_count
        }
    }
    elseif ($parserExitCode -eq 0) {
        $status = "passed"
        $reason = "ok"
    }

    if ($status -eq "passed") {
        $passedCount++
    }
    else {
        $failedCount++
    }

    $entries.Add([ordered]@{
        log_file = $logFile
        status = $status
        reason = $reason
        sample_count = $sampleCount
        failure_count = $failureCount
        summary_json = $entrySummaryJson
        summary_text = $entrySummaryText
    })
}

$overallStatus = if ($failedCount -gt 0) { "failed" } else { "passed" }
$overallReason = if ($failedCount -gt 0) { "one_or_more_logs_failed_budget" } else { "ok" }

$finalSummary = [ordered]@{
    status = $overallStatus
    reason = $overallReason
    generated_utc = (Get-Date).ToUniversalTime().ToString("o")
    explicit_log_file = $ExplicitLogFile
    discovered_count = $discoveredLogs.Count
    passed_count = $passedCount
    failed_count = $failedCount
    thresholds = [ordered]@{
        min_average_fps = $MinAverageFps
        max_p95_frame_ms = $MaxP95FrameMs
        max_gc_delta_kb = $MaxGcDeltaKb
        min_samples = $MinSamples
    }
    entries = $entries
}

$markdownLines = New-Object System.Collections.Generic.List[string]
$markdownLines.Add("# Perf Ingestion Summary")
$markdownLines.Add("")
$markdownLines.Add(("Status: **{0}**" -f $overallStatus.ToUpperInvariant()))
$markdownLines.Add(('Generated (UTC): `{0}`' -f $finalSummary.generated_utc))
$markdownLines.Add(("Discovered logs: {0}" -f $finalSummary.discovered_count))
$markdownLines.Add(("Thresholds: avg_fps>={0}, p95_frame_ms<={1}, gc_delta_kb<={2}, min_samples={3}" -f $MinAverageFps, $MaxP95FrameMs, $MaxGcDeltaKb, $MinSamples))
$markdownLines.Add("")
$markdownLines.Add("| Log File | Status | Samples | Failures | Reason |")
$markdownLines.Add("|---|---|---:|---:|---|")

foreach ($entry in $entries) {
    $markdownLines.Add(("{0} | {1} | {2} | {3} | {4}" -f
            (Escape-MarkdownCell -Value $entry.log_file),
            (Escape-MarkdownCell -Value ([string]$entry.status).ToUpperInvariant()),
            $entry.sample_count,
            $entry.failure_count,
            (Escape-MarkdownCell -Value $entry.reason)))
}

Write-IngestionArtifacts -Summary $finalSummary -MarkdownLines @($markdownLines)

if ($failedCount -gt 0) {
    Write-Host ("Perf ingestion: FAILED ({0}/{1} log(s) failed budgets)." -f $failedCount, $discoveredLogs.Count)
    exit 1
}

Write-Host ("Perf ingestion: PASSED ({0} log(s) processed)." -f $discoveredLogs.Count)
exit 0
