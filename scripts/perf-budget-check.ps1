param(
    [string]$LogFile = "perf_sanity.log",
    [string]$Tier = "",
    [string]$TierBudgetConfigPath = "ci/perf-tier-budgets.json",
    [double]$MinAverageFps = 60.0,
    [double]$MaxP95FrameMs = 25.0,
    [double]$MaxGcDeltaKb = 64.0,
    [int]$MinSamples = 1,
    [switch]$FailOnWarnings,
    [string]$SummaryJsonPath = "",
    [string]$SummaryTextPath = "",
    [switch]$NoExit
)

$ErrorActionPreference = "Stop"

function Normalize-Tier {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Trim().ToLowerInvariant()
}

function Write-SummaryArtifacts {
    param([hashtable]$Data)

    if (-not [string]::IsNullOrWhiteSpace($SummaryJsonPath)) {
        $jsonDir = Split-Path -Parent $SummaryJsonPath
        if (-not [string]::IsNullOrWhiteSpace($jsonDir)) {
            New-Item -ItemType Directory -Path $jsonDir -Force | Out-Null
        }

        ($Data | ConvertTo-Json -Depth 8) | Set-Content -Path $SummaryJsonPath
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
            "tier: $($Data.tier)"
            "generated_utc: $($Data.generated_utc)"
            "sample_count: $($Data.sample_count)"
            "failure_count: $($Data.failure_count)"
            "warning_count: $($Data.warning_count)"
            "thresholds_warn: avg_fps>=$($Data.thresholds.warn.min_average_fps), p95_frame_ms<=$($Data.thresholds.warn.max_p95_frame_ms), gc_delta_kb<=$($Data.thresholds.warn.max_gc_delta_kb), min_samples=$($Data.thresholds.min_samples)"
            "thresholds_fail: avg_fps>=$($Data.thresholds.fail.min_average_fps), p95_frame_ms<=$($Data.thresholds.fail.max_p95_frame_ms), gc_delta_kb<=$($Data.thresholds.fail.max_gc_delta_kb)"
            ""
        )

        if ($Data.failure_count -gt 0) {
            $lines += "failures:"
            foreach ($failure in $Data.failures) {
                $lines += ("- Scene={0} Tier={1} avg_fps={2} p95_frame_ms={3} gc_delta_kb={4} reason={5}" -f $failure.scene, $failure.tier, $failure.avg_fps, $failure.p95_frame_ms, $failure.gc_delta_kb, $failure.reason)
            }
        }
        else {
            $lines += "failures: none"
        }

        if ($Data.warning_count -gt 0) {
            $lines += ""
            $lines += "warnings:"
            foreach ($warning in $Data.warnings) {
                $lines += ("- Scene={0} Tier={1} avg_fps={2} p95_frame_ms={3} gc_delta_kb={4} reason={5}" -f $warning.scene, $warning.tier, $warning.avg_fps, $warning.p95_frame_ms, $warning.gc_delta_kb, $warning.reason)
            }
        }
        else {
            $lines += ""
            $lines += "warnings: none"
        }

        $lines | Set-Content -Path $SummaryTextPath
    }
}

function Resolve-TierThresholds {
    param(
        [string]$EffectiveTier,
        [string]$ConfigPath,
        [double]$FallbackMinAverageFps,
        [double]$FallbackMaxP95FrameMs,
        [double]$FallbackMaxGcDeltaKb,
        [int]$FallbackMinSamples
    )

    $warnThresholds = [ordered]@{
        min_average_fps = $FallbackMinAverageFps
        max_p95_frame_ms = $FallbackMaxP95FrameMs
        max_gc_delta_kb = $FallbackMaxGcDeltaKb
    }

    $failThresholds = [ordered]@{
        min_average_fps = $FallbackMinAverageFps
        max_p95_frame_ms = $FallbackMaxP95FrameMs
        max_gc_delta_kb = $FallbackMaxGcDeltaKb
    }

    $result = [ordered]@{
        source = "defaults"
        min_samples = $FallbackMinSamples
        warn = $warnThresholds
        fail = $failThresholds
    }

    if ([string]::IsNullOrWhiteSpace($ConfigPath) -or -not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
        return $result
    }

    try {
        $config = Get-Content -Raw -Path $ConfigPath | ConvertFrom-Json
    }
    catch {
        return $result
    }

    if ($null -eq $config -or $null -eq $config.tiers) {
        return $result
    }

    $tierProperty = $config.tiers.PSObject.Properties | Where-Object { $_.Name -ieq $EffectiveTier } | Select-Object -First 1
    if ($null -eq $tierProperty) {
        return $result
    }

    $tierConfig = $tierProperty.Value
    if ($null -eq $tierConfig) {
        return $result
    }

    if ($null -ne $tierConfig.min_average_fps_warn) {
        $warnThresholds.min_average_fps = [double]$tierConfig.min_average_fps_warn
    }
    if ($null -ne $tierConfig.max_p95_frame_ms_warn) {
        $warnThresholds.max_p95_frame_ms = [double]$tierConfig.max_p95_frame_ms_warn
    }
    if ($null -ne $tierConfig.max_gc_delta_kb_warn) {
        $warnThresholds.max_gc_delta_kb = [double]$tierConfig.max_gc_delta_kb_warn
    }

    if ($null -ne $tierConfig.min_average_fps_fail) {
        $failThresholds.min_average_fps = [double]$tierConfig.min_average_fps_fail
    }
    else {
        $failThresholds.min_average_fps = $warnThresholds.min_average_fps
    }

    if ($null -ne $tierConfig.max_p95_frame_ms_fail) {
        $failThresholds.max_p95_frame_ms = [double]$tierConfig.max_p95_frame_ms_fail
    }
    else {
        $failThresholds.max_p95_frame_ms = $warnThresholds.max_p95_frame_ms
    }

    if ($null -ne $tierConfig.max_gc_delta_kb_fail) {
        $failThresholds.max_gc_delta_kb = [double]$tierConfig.max_gc_delta_kb_fail
    }
    else {
        $failThresholds.max_gc_delta_kb = $warnThresholds.max_gc_delta_kb
    }

    if ($null -ne $tierConfig.min_samples) {
        $result.min_samples = [int]$tierConfig.min_samples
    }

    $result.source = "tier-config"
    return $result
}

$summary = [ordered]@{
    status = "failed"
    reason = ""
    log_file = $LogFile
    tier = ""
    generated_utc = (Get-Date).ToUniversalTime().ToString("o")
    thresholds = [ordered]@{
        source = "defaults"
        min_samples = $MinSamples
        warn = [ordered]@{
            min_average_fps = $MinAverageFps
            max_p95_frame_ms = $MaxP95FrameMs
            max_gc_delta_kb = $MaxGcDeltaKb
        }
        fail = [ordered]@{
            min_average_fps = $MinAverageFps
            max_p95_frame_ms = $MaxP95FrameMs
            max_gc_delta_kb = $MaxGcDeltaKb
        }
    }
    sample_count = 0
    failure_count = 0
    warning_count = 0
    failures = @()
    warnings = @()
}

if (-not (Test-Path $LogFile)) {
    $summary.reason = "log_file_not_found"
    Write-SummaryArtifacts -Data $summary
    throw "Perf budget check failed: log file not found at '$LogFile'."
}

$pattern = "PERF_SANITY scene=(?<scene>\S+)(?: tier=(?<tier>\S+))? frames=(?<frames>\d+) avg_fps=(?<avg>[0-9.]+) min_fps=(?<min>[0-9.]+) max_fps=(?<max>[0-9.]+) avg_frame_ms=(?<avgms>[0-9.]+) p95_frame_ms=(?<p95>[0-9.]+) gc_delta_kb=(?<gc>[0-9.]+)"
$samples = New-Object System.Collections.Generic.List[object]

Get-Content $LogFile | ForEach-Object {
    $line = $_
    if ($line -match $pattern) {
        $samples.Add([PSCustomObject]@{
                Scene      = $Matches["scene"]
                Tier       = Normalize-Tier $Matches["tier"]
                Frames     = [int]$Matches["frames"]
                AverageFps = [double]$Matches["avg"]
                P95FrameMs = [double]$Matches["p95"]
                GcDeltaKb  = [double]$Matches["gc"]
            })
    }
}

$summary.sample_count = $samples.Count
if ($samples.Count -eq 0) {
    $summary.reason = "no_perf_sanity_samples"
    Write-SummaryArtifacts -Data $summary
    throw "Perf budget check failed: no PERF_SANITY samples found in '$LogFile'."
}

$logTier = ""
for ($i = 0; $i -lt $samples.Count; $i++) {
    if (-not [string]::IsNullOrWhiteSpace($samples[$i].Tier)) {
        $logTier = $samples[$i].Tier
        break
    }
}

$requestedTier = Normalize-Tier $Tier
$effectiveTier = if (-not [string]::IsNullOrWhiteSpace($requestedTier)) { $requestedTier } elseif (-not [string]::IsNullOrWhiteSpace($logTier)) { $logTier } else { "minimum" }
$summary.tier = $effectiveTier

$resolvedThresholds = Resolve-TierThresholds `
    -EffectiveTier $effectiveTier `
    -ConfigPath $TierBudgetConfigPath `
    -FallbackMinAverageFps $MinAverageFps `
    -FallbackMaxP95FrameMs $MaxP95FrameMs `
    -FallbackMaxGcDeltaKb $MaxGcDeltaKb `
    -FallbackMinSamples $MinSamples

$summary.thresholds = $resolvedThresholds
$requiredSamples = [int]$resolvedThresholds.min_samples
if ($samples.Count -lt $requiredSamples) {
    $summary.reason = "insufficient_samples"
    Write-SummaryArtifacts -Data $summary
    throw "Perf budget check failed: expected at least $requiredSamples PERF_SANITY samples, found $($samples.Count)."
}

$failures = @()
$warnings = @()

foreach ($sample in $samples) {
    $effectiveSampleTier = if (-not [string]::IsNullOrWhiteSpace($sample.Tier)) { $sample.Tier } else { $effectiveTier }
    $failureReasons = New-Object System.Collections.Generic.List[string]
    $warningReasons = New-Object System.Collections.Generic.List[string]

    if ($sample.AverageFps -lt [double]$resolvedThresholds.fail.min_average_fps) {
        $failureReasons.Add("avg_fps_below_fail")
    }
    elseif ($sample.AverageFps -lt [double]$resolvedThresholds.warn.min_average_fps) {
        $warningReasons.Add("avg_fps_below_warn")
    }

    if ($sample.P95FrameMs -gt [double]$resolvedThresholds.fail.max_p95_frame_ms) {
        $failureReasons.Add("p95_frame_ms_above_fail")
    }
    elseif ($sample.P95FrameMs -gt [double]$resolvedThresholds.warn.max_p95_frame_ms) {
        $warningReasons.Add("p95_frame_ms_above_warn")
    }

    if ($sample.GcDeltaKb -gt [double]$resolvedThresholds.fail.max_gc_delta_kb) {
        $failureReasons.Add("gc_delta_kb_above_fail")
    }
    elseif ($sample.GcDeltaKb -gt [double]$resolvedThresholds.warn.max_gc_delta_kb) {
        $warningReasons.Add("gc_delta_kb_above_warn")
    }

    if ($failureReasons.Count -gt 0) {
        $failures += [ordered]@{
            scene = $sample.Scene
            tier = $effectiveSampleTier
            avg_fps = $sample.AverageFps
            p95_frame_ms = $sample.P95FrameMs
            gc_delta_kb = $sample.GcDeltaKb
            reason = [string]::Join(",", $failureReasons)
        }
        continue
    }

    if ($warningReasons.Count -gt 0) {
        $warnings += [ordered]@{
            scene = $sample.Scene
            tier = $effectiveSampleTier
            avg_fps = $sample.AverageFps
            p95_frame_ms = $sample.P95FrameMs
            gc_delta_kb = $sample.GcDeltaKb
            reason = [string]::Join(",", $warningReasons)
        }
    }
}

$summary.failure_count = $failures.Count
$summary.warning_count = $warnings.Count
$summary.failures = $failures
$summary.warnings = $warnings

if ($failures.Count -gt 0) {
    $summary.status = "failed"
    $summary.reason = "budget_violation_fail"
    Write-SummaryArtifacts -Data $summary

    Write-Host "Perf budget check: FAILED"
    Write-Host "Tier: $effectiveTier"
    Write-Host ("Thresholds fail: avg_fps>={0}, p95_frame_ms<={1}, gc_delta_kb<={2}" -f $resolvedThresholds.fail.min_average_fps, $resolvedThresholds.fail.max_p95_frame_ms, $resolvedThresholds.fail.max_gc_delta_kb)
    $failures | ForEach-Object {
        Write-Host ("- Scene={0} Tier={1} avg_fps={2} p95_frame_ms={3} gc_delta_kb={4} reason={5}" -f $_.scene, $_.tier, $_.avg_fps, $_.p95_frame_ms, $_.gc_delta_kb, $_.reason)
    }

    if ($NoExit) {
        return 1
    }

    exit 1
}

if ($warnings.Count -gt 0) {
    $summary.status = "warning"
    $summary.reason = "budget_violation_warn"
    Write-SummaryArtifacts -Data $summary

    Write-Host "Perf budget check: WARNING"
    Write-Host "Tier: $effectiveTier"
    Write-Host ("Thresholds warn: avg_fps>={0}, p95_frame_ms<={1}, gc_delta_kb<={2}" -f $resolvedThresholds.warn.min_average_fps, $resolvedThresholds.warn.max_p95_frame_ms, $resolvedThresholds.warn.max_gc_delta_kb)
    $warnings | ForEach-Object {
        Write-Host ("- Scene={0} Tier={1} avg_fps={2} p95_frame_ms={3} gc_delta_kb={4} reason={5}" -f $_.scene, $_.tier, $_.avg_fps, $_.p95_frame_ms, $_.gc_delta_kb, $_.reason)
    }

    if ($FailOnWarnings) {
        if ($NoExit) {
            return 1
        }

        exit 1
    }

    if ($NoExit) {
        return 0
    }

    exit 0
}

$summary.status = "passed"
$summary.reason = "ok"
Write-SummaryArtifacts -Data $summary

Write-Host "Perf budget check: PASSED"
Write-Host "Tier: $effectiveTier"
Write-Host "Samples parsed: $($samples.Count)"
if ($NoExit) {
    return 0
}

exit 0
