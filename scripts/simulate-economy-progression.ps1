param(
    [string]$ConfigPath = "ci/balance-sim-config.json",
    [string]$ReportJsonPath = "Artifacts/BalanceSim/balance_simulation_report.json",
    [string]$ReportMarkdownPath = "Artifacts/BalanceSim/balance_simulation_report.md",
    [switch]$FailOnWarnings
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

function Clamp {
    param(
        [double]$Value,
        [double]$Min,
        [double]$Max
    )

    if ($Value -lt $Min) { return $Min }
    if ($Value -gt $Max) { return $Max }
    return $Value
}

function Resolve-DistanceTier {
    param(
        [System.Random]$Random,
        [double[]]$Weights
    )

    if ($null -eq $Weights -or $Weights.Length -eq 0) {
        return 1
    }

    $roll = $Random.NextDouble()
    $cursor = 0.0
    for ($i = 0; $i -lt $Weights.Length; $i++) {
        $cursor += [double]$Weights[$i]
        if ($roll -le $cursor) {
            return ($i + 1)
        }
    }

    return $Weights.Length
}

function Calculate-CatchXp {
    param(
        [int]$DistanceTier,
        [double]$WeightKg,
        [double]$ValueCopecs
    )

    $xp = 10.0
    $xp += ([Math]::Max(1, $DistanceTier) - 1) * 5.0
    $xp += [Math]::Round($WeightKg * 3.0)
    $xp += [Math]::Round($ValueCopecs / 25.0)
    return [int](Clamp -Value $xp -Min 5 -Max 200)
}

if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
    throw "Balance simulation config not found: '$ConfigPath'."
}

$config = Get-Content -Raw -Path $ConfigPath | ConvertFrom-Json
if ($null -eq $config) {
    throw "Unable to parse config '$ConfigPath'."
}

$seed = [int]$config.seed
$hours = [double]$config.simulated_hours
$tripsPerHour = [int]$config.trips_per_hour
$catchesPerTrip = [int]$config.catches_per_trip
$failRate = [double]$config.catch_fail_rate
$baseValueMin = [double]$config.base_value_min
$baseValueMax = [double]$config.base_value_max
$weightMin = [double]$config.weight_min_kg
$weightMax = [double]$config.weight_max_kg
$distanceTierWeights = @($config.distance_tier_weights | ForEach-Object { [double]$_ })
$distanceTierStep = [double]$config.distance_tier_step
$levelThresholds = @($config.level_thresholds | ForEach-Object { [int]$_ })

$rng = [System.Random]::new($seed)

$tripCount = [Math]::Max(1, [int]([Math]::Round($hours * $tripsPerHour)))
$totalAttempts = 0
$totalLanded = 0
$totalFails = 0
$totalCurrency = 0.0
$totalXp = 0
$minutesElapsed = 0.0
$minutesPerTrip = if ($tripCount -gt 0) { ($hours * 60.0) / $tripCount } else { 0.0 }
$minutesToLevel = @{}

$currentLevel = 1
if ($levelThresholds.Length -eq 0) {
    $levelThresholds = @(0, 100, 250, 450, 700, 1000)
}

for ($trip = 0; $trip -lt $tripCount; $trip++) {
    for ($attempt = 0; $attempt -lt $catchesPerTrip; $attempt++) {
        $totalAttempts++
        $minutesElapsed += ($minutesPerTrip / [Math]::Max(1, $catchesPerTrip))
        if ($rng.NextDouble() -lt $failRate) {
            $totalFails++
            continue
        }

        $totalLanded++
        $distanceTier = Resolve-DistanceTier -Random $rng -Weights $distanceTierWeights
        $baseValue = $baseValueMin + ($rng.NextDouble() * ($baseValueMax - $baseValueMin))
        $distanceMultiplier = 1.0 + (([Math]::Max(1, $distanceTier) - 1) * $distanceTierStep)
        $value = [Math]::Round($baseValue * $distanceMultiplier)
        $weight = $weightMin + ($rng.NextDouble() * ($weightMax - $weightMin))

        $totalCurrency += $value
        $xpAward = Calculate-CatchXp -DistanceTier $distanceTier -WeightKg $weight -ValueCopecs $value
        $totalXp += $xpAward

        $resolvedLevel = 1
        for ($i = 0; $i -lt $levelThresholds.Length; $i++) {
            if ($totalXp -ge $levelThresholds[$i]) {
                $resolvedLevel = $i + 1
            }
        }

        if ($resolvedLevel -gt $currentLevel) {
            for ($lvl = $currentLevel + 1; $lvl -le $resolvedLevel; $lvl++) {
                $key = "level_$lvl"
                if (-not $minutesToLevel.ContainsKey($key)) {
                    $minutesToLevel[$key] = [Math]::Round($minutesElapsed, 2)
                }
            }

            $currentLevel = $resolvedLevel
        }
    }
}

$currencyPerHour = if ($hours -gt 0.0) { [Math]::Round(($totalCurrency / $hours), 2) } else { 0.0 }
$observedFailRate = if ($totalAttempts -gt 0) { [Math]::Round(($totalFails / $totalAttempts), 4) } else { 0.0 }
$minutesToLevel3 = if ($minutesToLevel.ContainsKey("level_3")) { [double]$minutesToLevel["level_3"] } else { [double]::PositiveInfinity }

$status = "passed"
$reason = "ok"
$warnings = New-Object System.Collections.Generic.List[string]
$failures = New-Object System.Collections.Generic.List[string]

$thresholds = $config.thresholds
if ($currencyPerHour -lt [double]$thresholds.fail_currency_per_hour_min) {
    $failures.Add("currency_per_hour_below_fail")
}
elseif ($currencyPerHour -lt [double]$thresholds.warn_currency_per_hour_min) {
    $warnings.Add("currency_per_hour_below_warn")
}

if ($observedFailRate -gt [double]$thresholds.fail_fail_rate_max) {
    $failures.Add("fail_rate_above_fail")
}
elseif ($observedFailRate -gt [double]$thresholds.warn_fail_rate_max) {
    $warnings.Add("fail_rate_above_warn")
}

if ([double]::IsInfinity($minutesToLevel3) -or $minutesToLevel3 -gt [double]$thresholds.fail_minutes_to_level3_max) {
    $failures.Add("minutes_to_level3_above_fail")
}
elseif ($minutesToLevel3 -gt [double]$thresholds.warn_minutes_to_level3_max) {
    $warnings.Add("minutes_to_level3_above_warn")
}

if ($failures.Count -gt 0) {
    $status = "failed"
    $reason = "threshold_fail"
}
elseif ($warnings.Count -gt 0) {
    $status = "warning"
    $reason = "threshold_warn"
}

$report = [ordered]@{
    status = $status
    reason = $reason
    generated_utc = (Get-Date).ToUniversalTime().ToString("o")
    config_path = $ConfigPath
    seed = $seed
    simulated_hours = $hours
    trips_simulated = $tripCount
    catch_attempts = $totalAttempts
    catches_landed = $totalLanded
    catches_failed = $totalFails
    currency_total = [Math]::Round($totalCurrency, 2)
    currency_per_hour = $currencyPerHour
    observed_fail_rate = $observedFailRate
    total_xp = $totalXp
    reached_level = $currentLevel
    minutes_to_level = $minutesToLevel
    minutes_to_level3 = if ([double]::IsInfinity($minutesToLevel3)) { "not_reached" } else { $minutesToLevel3 }
    thresholds = $thresholds
    warnings = $warnings
    failures = $failures
}

Ensure-ParentDirectory -Path $ReportJsonPath
($report | ConvertTo-Json -Depth 8) | Set-Content -Path $ReportJsonPath

$md = New-Object System.Collections.Generic.List[string]
$md.Add("# Economy/Progression Simulation Report")
$md.Add("")
$md.Add(("Status: **{0}**" -f $status.ToUpperInvariant()))
$md.Add(("Reason: {0}" -f $reason))
$md.Add(('Seed: `{0}`' -f $seed))
$md.Add("")
$md.Add("| Metric | Value |")
$md.Add("|---|---:|")
$md.Add(("Currency / hour | {0}" -f $currencyPerHour))
$md.Add(("Observed fail rate | {0}" -f $observedFailRate))
$md.Add(("Minutes to level 3 | {0}" -f $report.minutes_to_level3))
$md.Add(("Trips simulated | {0}" -f $tripCount))
$md.Add(("Catch attempts | {0}" -f $totalAttempts))
$md.Add(("Catches landed | {0}" -f $totalLanded))
$md.Add(("Total XP | {0}" -f $totalXp))
$md.Add("")
$md.Add("## Thresholds")
$md.Add(("- currency/hour warn>= {0}, fail>= {1}" -f $thresholds.warn_currency_per_hour_min, $thresholds.fail_currency_per_hour_min))
$md.Add(("- fail-rate warn<= {0}, fail<= {1}" -f $thresholds.warn_fail_rate_max, $thresholds.fail_fail_rate_max))
$md.Add(("- minutes-to-level3 warn<= {0}, fail<= {1}" -f $thresholds.warn_minutes_to_level3_max, $thresholds.fail_minutes_to_level3_max))
$md.Add("")
$md.Add("## Flags")
if ($failures.Count -eq 0 -and $warnings.Count -eq 0) {
    $md.Add("- none")
}
else {
    foreach ($failure in $failures) {
        $md.Add(("- FAIL: {0}" -f $failure))
    }
    foreach ($warning in $warnings) {
        $md.Add(("- WARN: {0}" -f $warning))
    }
}

Ensure-ParentDirectory -Path $ReportMarkdownPath
@($md) | Set-Content -Path $ReportMarkdownPath

if ($status -eq "failed") {
    Write-Host ("Balance simulation: FAILED ({0})" -f ([string]::Join(", ", $failures)))
    exit 1
}

if ($status -eq "warning" -and $FailOnWarnings) {
    Write-Host ("Balance simulation: WARNING promoted to failure ({0})" -f ([string]::Join(", ", $warnings)))
    exit 1
}

Write-Host ("Balance simulation: {0}" -f $status.ToUpperInvariant())
exit 0
