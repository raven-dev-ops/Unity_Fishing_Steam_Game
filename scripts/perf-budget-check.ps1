param(
    [string]$LogFile = "perf_sanity.log",
    [double]$MinAverageFps = 60.0,
    [double]$MaxP95FrameMs = 25.0,
    [double]$MaxGcDeltaKb = 64.0,
    [int]$MinSamples = 1
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $LogFile)) {
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

if ($samples.Count -lt $MinSamples) {
    throw "Perf budget check failed: expected at least $MinSamples PERF_SANITY samples, found $($samples.Count)."
}

$failures = $samples | Where-Object {
    $_.AverageFps -lt $MinAverageFps -or
    $_.P95FrameMs -gt $MaxP95FrameMs -or
    $_.GcDeltaKb -gt $MaxGcDeltaKb
}

if ($failures.Count -gt 0) {
    Write-Host "Perf budget check: FAILED"
    Write-Host "Thresholds: avg_fps>=$MinAverageFps, p95_frame_ms<=$MaxP95FrameMs, gc_delta_kb<=$MaxGcDeltaKb"
    $failures | ForEach-Object {
        Write-Host ("- Scene={0} avg_fps={1} p95_frame_ms={2} gc_delta_kb={3}" -f $_.Scene, $_.AverageFps, $_.P95FrameMs, $_.GcDeltaKb)
    }
    exit 1
}

Write-Host "Perf budget check: PASSED"
Write-Host "Samples parsed: $($samples.Count)"
exit 0
