param(
    [string]$LogFile = "perf_sanity.log",
    [double]$MinAverageFps = 60.0,
    [double]$MaxP95FrameMs = 25.0,
    [double]$MaxGcDeltaKb = 64.0,
    [int]$MinSamples = 1,
    [string]$SummaryJsonPath = "",
    [string]$SummaryTextPath = ""
)

$ErrorActionPreference = "Stop"

$summary = [ordered]@{
    status          = "failed"
    reason          = ""
    log_file        = $LogFile
    generated_utc   = (Get-Date).ToUniversalTime().ToString("o")
    thresholds      = [ordered]@{
        min_average_fps = $MinAverageFps
        max_p95_frame_ms = $MaxP95FrameMs
        max_gc_delta_kb = $MaxGcDeltaKb
        min_samples = $MinSamples
    }
    sample_count    = 0
    failure_count   = 0
    failures        = @()
}

function Write-SummaryArtifacts {
    param([hashtable]$Data)

    if (-not [string]::IsNullOrWhiteSpace($SummaryJsonPath)) {
        $jsonDir = Split-Path -Parent $SummaryJsonPath
        if (-not [string]::IsNullOrWhiteSpace($jsonDir)) {
            New-Item -ItemType Directory -Path $jsonDir -Force | Out-Null
        }

        ($Data | ConvertTo-Json -Depth 6) | Set-Content -Path $SummaryJsonPath
    }

    if (-not [string]::IsNullOrWhiteSpace($SummaryTextPath)) {
        $textDir = Split-Path -Parent $SummaryTextPath
        if (-not [string]::IsNullOrWhiteSpace($textDir)) {
            New-Item -ItemType Directory -Path $textDir -Force | Out-Null
        }

        $lines = @(
            "Perf budget check summary"
            "status: $($Data.status)"
            "reason: $($Data.reason)"
            "log_file: $($Data.log_file)"
            "generated_utc: $($Data.generated_utc)"
            "sample_count: $($Data.sample_count)"
            "failure_count: $($Data.failure_count)"
            "thresholds: avg_fps>=$($Data.thresholds.min_average_fps), p95_frame_ms<=$($Data.thresholds.max_p95_frame_ms), gc_delta_kb<=$($Data.thresholds.max_gc_delta_kb), min_samples=$($Data.thresholds.min_samples)"
            ""
        )

        if ($Data.failure_count -gt 0) {
            $lines += "failures:"
            foreach ($failure in $Data.failures) {
                $lines += ("- Scene={0} avg_fps={1} p95_frame_ms={2} gc_delta_kb={3}" -f $failure.scene, $failure.avg_fps, $failure.p95_frame_ms, $failure.gc_delta_kb)
            }
        } else {
            $lines += "failures: none"
        }

        $lines | Set-Content -Path $SummaryTextPath
    }
}

if (-not (Test-Path $LogFile)) {
    $summary.reason = "log_file_not_found"
    Write-SummaryArtifacts -Data $summary
    throw "Perf budget check failed: log file not found at '$LogFile'."
}

$pattern = "PERF_SANITY scene=(?<scene>\S+) frames=(?<frames>\d+) avg_fps=(?<avg>[0-9.]+) min_fps=(?<min>[0-9.]+) max_fps=(?<max>[0-9.]+) avg_frame_ms=(?<avgms>[0-9.]+) p95_frame_ms=(?<p95>[0-9.]+) gc_delta_kb=(?<gc>[0-9.]+)"
$samples = New-Object System.Collections.Generic.List[object]

Get-Content $LogFile | ForEach-Object {
    $line = $_
    if ($line -match $pattern) {
        $samples.Add([PSCustomObject]@{
            Scene      = $Matches["scene"]
            Frames     = [int]$Matches["frames"]
            AverageFps = [double]$Matches["avg"]
            P95FrameMs = [double]$Matches["p95"]
            GcDeltaKb  = [double]$Matches["gc"]
        })
    }
}

$summary.sample_count = $samples.Count

if ($samples.Count -lt $MinSamples) {
    $summary.reason = "insufficient_samples"
    Write-SummaryArtifacts -Data $summary
    throw "Perf budget check failed: expected at least $MinSamples PERF_SANITY samples, found $($samples.Count)."
}

$failures = $samples | Where-Object {
    $_.AverageFps -lt $MinAverageFps -or
    $_.P95FrameMs -gt $MaxP95FrameMs -or
    $_.GcDeltaKb -gt $MaxGcDeltaKb
}

$summary.failure_count = $failures.Count
if ($failures.Count -gt 0) {
    $summary.failures = @($failures | ForEach-Object {
        [ordered]@{
            scene = $_.Scene
            avg_fps = $_.AverageFps
            p95_frame_ms = $_.P95FrameMs
            gc_delta_kb = $_.GcDeltaKb
        }
    })
}

if ($failures.Count -gt 0) {
    $summary.status = "failed"
    $summary.reason = "budget_violation"
    Write-SummaryArtifacts -Data $summary

    Write-Host "Perf budget check: FAILED"
    Write-Host "Thresholds: avg_fps>=$MinAverageFps, p95_frame_ms<=$MaxP95FrameMs, gc_delta_kb<=$MaxGcDeltaKb"
    $failures | ForEach-Object {
        Write-Host ("- Scene={0} avg_fps={1} p95_frame_ms={2} gc_delta_kb={3}" -f $_.Scene, $_.AverageFps, $_.P95FrameMs, $_.GcDeltaKb)
    }
    exit 1
}

$summary.status = "passed"
$summary.reason = "ok"
Write-SummaryArtifacts -Data $summary

Write-Host "Perf budget check: PASSED"
Write-Host "Samples parsed: $($samples.Count)"
exit 0
