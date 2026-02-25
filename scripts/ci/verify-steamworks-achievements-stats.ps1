param(
    [string]$ContractPath = "release/steamworks/achievements_stats/backend_contract.json",
    [string]$SteamStatsServicePath = "Assets/Scripts/Steam/SteamStatsService.cs",
    [string]$SteamBootstrapPath = "Assets/Scripts/Steam/SteamBootstrap.cs",
    [string]$ReleaseWorkflowPath = ".github/workflows/release-steampipe.yml",
    [string]$SummaryJsonPath = "Artifacts/Steamworks/steamworks_achievements_stats_contract_summary.json",
    [string]$SummaryMarkdownPath = "Artifacts/Steamworks/steamworks_achievements_stats_contract_summary.md",
    [switch]$RequirePublishedMetadata
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Require-File {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing required file: $Path"
    }
}

function Get-SerializedStringKeys {
    param(
        [string]$Source,
        [string]$FieldPrefix
    )

    $pattern = [string]::Format(
        '\[SerializeField\]\s+private\s+string\s+({0}[A-Za-z0-9_]+)\s*=\s*"([^"]+)"',
        [regex]::Escape($FieldPrefix))
    $matches = [regex]::Matches($Source, $pattern)
    $rows = @()

    foreach ($match in $matches) {
        $rows += [PSCustomObject]@{
            field = $match.Groups[1].Value
            key = $match.Groups[2].Value
        }
    }

    return $rows
}

function Compare-KeySets {
    param(
        [string[]]$Expected,
        [string[]]$Actual
    )

    $missing = @($Expected | Where-Object { $_ -and ($_ -notin $Actual) })
    $extra = @($Actual | Where-Object { $_ -and ($_ -notin $Expected) })

    return [PSCustomObject]@{
        missing = $missing
        extra = $extra
    }
}

function Get-DuplicateValues {
    param([string[]]$Values)

    if ($null -eq $Values) {
        return @()
    }

    $dupes = @()
    foreach ($group in ($Values | Group-Object | Where-Object { $_.Count -gt 1 })) {
        $dupes += [string]$group.Name
    }

    return @($dupes)
}

Require-File -Path $ContractPath
Require-File -Path $SteamStatsServicePath
Require-File -Path $SteamBootstrapPath
Require-File -Path $ReleaseWorkflowPath

$contract = Get-Content -Raw -Path $ContractPath | ConvertFrom-Json
$steamStatsSource = Get-Content -Raw -Path $SteamStatsServicePath
$steamBootstrapSource = Get-Content -Raw -Path $SteamBootstrapPath
$releaseWorkflowSource = Get-Content -Raw -Path $ReleaseWorkflowPath

$runtimeStatRows = Get-SerializedStringKeys -Source $steamStatsSource -FieldPrefix "_stat"
$runtimeAchievementRows = Get-SerializedStringKeys -Source $steamStatsSource -FieldPrefix "_achievement"

$runtimeStatKeys = @($runtimeStatRows | ForEach-Object { [string]$_.key })
$runtimeAchievementKeys = @($runtimeAchievementRows | ForEach-Object { [string]$_.key })
$contractStatKeys = @($contract.stats | ForEach-Object { [string]$_.key })
$contractAchievementKeys = @($contract.achievements | ForEach-Object { [string]$_.key })

$statParity = Compare-KeySets -Expected $contractStatKeys -Actual $runtimeStatKeys
$achievementParity = Compare-KeySets -Expected $contractAchievementKeys -Actual $runtimeAchievementKeys

$contractStatTypeViolations = @($contract.stats | Where-Object { [string]$_.type -ne "INT" } | ForEach-Object { [string]$_.key })
$contractStatDuplicateKeys = @(Get-DuplicateValues -Values $contractStatKeys)
$runtimeStatDuplicateKeys = @(Get-DuplicateValues -Values $runtimeStatKeys)
$contractAchievementDuplicateKeys = @(Get-DuplicateValues -Values $contractAchievementKeys)
$runtimeAchievementDuplicateKeys = @(Get-DuplicateValues -Values $runtimeAchievementKeys)

$bootstrapAppIdMatch = [regex]::Match($steamBootstrapSource, '_steamAppId\s*=\s*([0-9]+)\s*;')
$runtimeBootstrapDefaultAppId = if ($bootstrapAppIdMatch.Success) { [int]$bootstrapAppIdMatch.Groups[1].Value } else { $null }
$contractBootstrapDefaultAppId = [int]$contract.app_mapping.bootstrap_default_app_id

$appIdSecretName = [string]$contract.app_mapping.steam_app_id.secret_name
$depotSecretName = [string]$contract.app_mapping.windows_depot_id.secret_name
$workflowHasAppSecret = -not [string]::IsNullOrWhiteSpace($appIdSecretName) -and $releaseWorkflowSource.Contains($appIdSecretName)
$workflowHasDepotSecret = -not [string]::IsNullOrWhiteSpace($depotSecretName) -and $releaseWorkflowSource.Contains($depotSecretName)

$publishMetadata = $contract.backend_publish
$publishArtifactsCount = if ($null -ne $publishMetadata.verification_artifacts) { @($publishMetadata.verification_artifacts).Count } else { 0 }
$publishMetadataComplete =
    -not [string]::IsNullOrWhiteSpace([string]$publishMetadata.steamworks_change_number) -and
    -not [string]::IsNullOrWhiteSpace([string]$publishMetadata.published_at_utc) -and
    -not [string]::IsNullOrWhiteSpace([string]$publishMetadata.verified_by) -and
    ($publishArtifactsCount -gt 0)

$failures = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

if ($statParity.missing.Count -gt 0) {
    $failures.Add("Contract stats missing in runtime source: $($statParity.missing -join ', ')") | Out-Null
}

if ($statParity.extra.Count -gt 0) {
    $failures.Add("Runtime stats missing in contract: $($statParity.extra -join ', ')") | Out-Null
}

if ($achievementParity.missing.Count -gt 0) {
    $failures.Add("Contract achievements missing in runtime source: $($achievementParity.missing -join ', ')") | Out-Null
}

if ($achievementParity.extra.Count -gt 0) {
    $failures.Add("Runtime achievements missing in contract: $($achievementParity.extra -join ', ')") | Out-Null
}

if ($contractStatTypeViolations.Count -gt 0) {
    $failures.Add("Contract stats with non-INT types: $($contractStatTypeViolations -join ', ')") | Out-Null
}

if ($contractStatDuplicateKeys.Count -gt 0) {
    $failures.Add("Duplicate stat keys in contract: $($contractStatDuplicateKeys -join ', ')") | Out-Null
}

if ($runtimeStatDuplicateKeys.Count -gt 0) {
    $failures.Add("Duplicate stat keys in runtime source: $($runtimeStatDuplicateKeys -join ', ')") | Out-Null
}

if ($contractAchievementDuplicateKeys.Count -gt 0) {
    $failures.Add("Duplicate achievement keys in contract: $($contractAchievementDuplicateKeys -join ', ')") | Out-Null
}

if ($runtimeAchievementDuplicateKeys.Count -gt 0) {
    $failures.Add("Duplicate achievement keys in runtime source: $($runtimeAchievementDuplicateKeys -join ', ')") | Out-Null
}

if ($null -eq $runtimeBootstrapDefaultAppId) {
    $failures.Add("Could not resolve _steamAppId default in SteamBootstrap source.") | Out-Null
}
elseif ($runtimeBootstrapDefaultAppId -ne $contractBootstrapDefaultAppId) {
    $failures.Add("SteamBootstrap default app ID mismatch. Runtime=$runtimeBootstrapDefaultAppId Contract=$contractBootstrapDefaultAppId") | Out-Null
}

if (-not $workflowHasAppSecret) {
    $failures.Add("Release workflow does not reference app ID secret '$appIdSecretName'.") | Out-Null
}

if (-not $workflowHasDepotSecret) {
    $failures.Add("Release workflow does not reference depot secret '$depotSecretName'.") | Out-Null
}

if (-not $publishMetadataComplete) {
    $warnings.Add("Backend publish metadata is incomplete in $ContractPath.") | Out-Null
    if ($RequirePublishedMetadata) {
        $failures.Add("Publish metadata is required but incomplete.") | Out-Null
    }
}

$status = if ($failures.Count -gt 0) { "failed" } elseif ($warnings.Count -gt 0) { "warning" } else { "passed" }

$summary = [ordered]@{
    generated_utc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    contract_path = $ContractPath
    steam_stats_service_path = $SteamStatsServicePath
    steam_bootstrap_path = $SteamBootstrapPath
    release_workflow_path = $ReleaseWorkflowPath
    status = $status
    checks = [ordered]@{
        stat_key_parity = [ordered]@{
            expected_count = $contractStatKeys.Count
            runtime_count = $runtimeStatKeys.Count
            missing_in_runtime = $statParity.missing
            extra_in_runtime = $statParity.extra
        }
        achievement_key_parity = [ordered]@{
            expected_count = $contractAchievementKeys.Count
            runtime_count = $runtimeAchievementKeys.Count
            missing_in_runtime = $achievementParity.missing
            extra_in_runtime = $achievementParity.extra
        }
        stat_type_contract = [ordered]@{
            required_type = "INT"
            violating_keys = $contractStatTypeViolations
        }
        app_mapping = [ordered]@{
            bootstrap_default_app_id_contract = $contractBootstrapDefaultAppId
            bootstrap_default_app_id_runtime = $runtimeBootstrapDefaultAppId
            release_workflow_has_app_secret = $workflowHasAppSecret
            release_workflow_has_depot_secret = $workflowHasDepotSecret
            app_secret = $appIdSecretName
            depot_secret = $depotSecretName
        }
        backend_publish_metadata = [ordered]@{
            complete = $publishMetadataComplete
            steamworks_change_number = [string]$publishMetadata.steamworks_change_number
            published_at_utc = [string]$publishMetadata.published_at_utc
            verified_by = [string]$publishMetadata.verified_by
            verification_artifact_count = $publishArtifactsCount
        }
    }
    failures = @($failures)
    warnings = @($warnings)
}

$summaryJsonDirectory = Split-Path -Parent $SummaryJsonPath
if (-not [string]::IsNullOrWhiteSpace($summaryJsonDirectory)) {
    New-Item -ItemType Directory -Force -Path $summaryJsonDirectory | Out-Null
}

$summaryMarkdownDirectory = Split-Path -Parent $SummaryMarkdownPath
if (-not [string]::IsNullOrWhiteSpace($summaryMarkdownDirectory)) {
    New-Item -ItemType Directory -Force -Path $summaryMarkdownDirectory | Out-Null
}

$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $SummaryJsonPath

$markdown = @()
$markdown += "# Steamworks Achievements/Stats Contract Summary"
$markdown += ""
$markdown += "- Generated UTC: ``$($summary.generated_utc)``"
$markdown += "- Status: ``$status``"
$markdown += "- Contract: ``$ContractPath``"
$markdown += ""
$markdown += "| Check | Result |"
$markdown += "|---|---|"
$markdown += "| Stat key parity | missing=$($statParity.missing.Count), extra=$($statParity.extra.Count) |"
$markdown += "| Achievement key parity | missing=$($achievementParity.missing.Count), extra=$($achievementParity.extra.Count) |"
$markdown += "| Contract stat type violations | $($contractStatTypeViolations.Count) |"
$markdown += "| Bootstrap app ID parity | contract=$contractBootstrapDefaultAppId runtime=$runtimeBootstrapDefaultAppId |"
$markdown += "| Release workflow app secret | $workflowHasAppSecret ($appIdSecretName) |"
$markdown += "| Release workflow depot secret | $workflowHasDepotSecret ($depotSecretName) |"
$markdown += "| Backend publish metadata complete | $publishMetadataComplete |"

if ($failures.Count -gt 0) {
    $markdown += ""
    $markdown += "## Failures"
    foreach ($failure in $failures) {
        $markdown += "- $failure"
    }
}

if ($warnings.Count -gt 0) {
    $markdown += ""
    $markdown += "## Warnings"
    foreach ($warning in $warnings) {
        $markdown += "- $warning"
    }
}

$markdown -join "`n" | Set-Content -Path $SummaryMarkdownPath

if ($failures.Count -gt 0) {
    throw "Steamworks achievements/stats contract verification failed. See $SummaryJsonPath"
}

Write-Output "Steamworks achievements/stats contract verification status: $status"
Write-Output "Summary JSON: $SummaryJsonPath"
Write-Output "Summary Markdown: $SummaryMarkdownPath"
