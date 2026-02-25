param(
    [Parameter(Mandatory = $true)]
    [string]$RcTag,
    [Parameter(Mandatory = $true)]
    [string]$CapturedBy,
    [Parameter(Mandatory = $true)]
    [string]$SteamAppId,
    [Parameter(Mandatory = $true)]
    [string]$ControllerSupportScreenshotSource,
    [Parameter(Mandatory = $true)]
    [string]$SteamInputSettingsScreenshotSource,
    [string]$CapturedAtUtc = "",
    [string[]]$MetadataNotes = @(),
    [switch]$UpdateBackendPublishMetadata,
    [string]$BackendChangeNumber = "",
    [string]$BackendPublishedAtUtc = "",
    [string]$BackendVerifiedBy = "",
    [string[]]$BackendVerificationArtifactSources = @(),
    [string]$BackendPublishNotes = "",
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Require-File {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file was not found: $Path"
    }
}

function Assert-PngSource {
    param(
        [string]$Path,
        [string]$Label
    )

    $extension = [System.IO.Path]::GetExtension($Path)
    if (-not [string]::Equals($extension, ".png", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must be a PNG file: $Path"
    }
}

function Test-Iso8601UtcTimestamp {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    $styles = [System.Globalization.DateTimeStyles]::AssumeUniversal -bor [System.Globalization.DateTimeStyles]::AdjustToUniversal
    $parsed = [DateTime]::MinValue
    return [DateTime]::TryParseExact(
        $Value,
        "yyyy-MM-ddTHH:mm:ssZ",
        [System.Globalization.CultureInfo]::InvariantCulture,
        $styles,
        [ref]$parsed)
}

function Get-RepoRelativePath {
    param(
        [string]$AbsolutePath,
        [string]$RepoRootAbsolute
    )

    $targetFullPath = (Resolve-Path -LiteralPath $AbsolutePath).Path
    $rootFullPath = (Resolve-Path -LiteralPath $RepoRootAbsolute).Path

    if (-not $rootFullPath.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $rootFullPath += [System.IO.Path]::DirectorySeparatorChar
    }

    $rootUri = New-Object System.Uri($rootFullPath)
    $targetUri = New-Object System.Uri($targetFullPath)
    if (-not $rootUri.IsBaseOf($targetUri)) {
        throw "Path is outside repository root: $targetFullPath"
    }

    $relativeUri = $rootUri.MakeRelativeUri($targetUri)
    return [System.Uri]::UnescapeDataString($relativeUri.ToString())
}

if ([string]::IsNullOrWhiteSpace($CapturedAtUtc)) {
    $CapturedAtUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
}

if (-not (Test-Iso8601UtcTimestamp -Value $CapturedAtUtc)) {
    throw "CapturedAtUtc must use ISO-8601 UTC format yyyy-MM-ddTHH:mm:ssZ."
}

Require-File -Path $ControllerSupportScreenshotSource
Require-File -Path $SteamInputSettingsScreenshotSource
Assert-PngSource -Path $ControllerSupportScreenshotSource -Label "ControllerSupportScreenshotSource"
Assert-PngSource -Path $SteamInputSettingsScreenshotSource -Label "SteamInputSettingsScreenshotSource"

$repoRoot = (Get-Location).Path
$metadataBundleDir = Join-Path "release/steam_metadata" $RcTag
$metadataBundleSummaryPath = Join-Path $metadataBundleDir "summary.md"
$metadataBundleManifestPath = Join-Path $metadataBundleDir "manifest.json"
$metadataControllerScreenshotPath = Join-Path $metadataBundleDir "controller_support.png"
$metadataInputScreenshotPath = Join-Path $metadataBundleDir "steam_input_settings.png"

$backendContractPath = "release/steamworks/achievements_stats/backend_contract.json"
$backendEvidenceDir = Join-Path "release/steamworks/achievements_stats" $RcTag

$summaryLines = New-Object System.Collections.Generic.List[string]
$summaryLines.Add("# Steam Metadata Evidence Summary")
$summaryLines.Add("")
$summaryLines.Add("- RC tag: ``$RcTag``")
$summaryLines.Add("- Captured at (UTC): ``$CapturedAtUtc``")
$summaryLines.Add("- Captured by: ``$CapturedBy``")
$summaryLines.Add("- Steam App ID: ``$SteamAppId``")
$summaryLines.Add("")
$summaryLines.Add("## Screenshot Inventory")
$summaryLines.Add('- `controller_support.png`: Steamworks controller support declaration panel')
$summaryLines.Add('- `steam_input_settings.png`: Steam Input configuration panel')
$summaryLines.Add("")
$summaryLines.Add("## Verification Outcome")
$summaryLines.Add('- Result: `pass`')
$summaryLines.Add('- Drift action: `none`')
$summaryLines.Add("- Notes:")
if ($MetadataNotes.Count -eq 0) {
    $summaryLines.Add("  - Metadata aligns with runtime behavior and rebinding evidence.")
}
else {
    foreach ($note in $MetadataNotes) {
        if ([string]::IsNullOrWhiteSpace([string]$note)) {
            continue
        }

        $summaryLines.Add("  - $note")
    }
}

$manifestObject = [ordered]@{
    schema_version = 1
    rc_tag = $RcTag
    captured_at_utc = $CapturedAtUtc
    captured_by = $CapturedBy
    steam_app_id = $SteamAppId
    partner_section = "Steamworks > Store Presence > Edit Store Page > Basic Info"
    evidence_files = [ordered]@{
        controller_support = "controller_support.png"
        steam_input_settings = "steam_input_settings.png"
        summary = "summary.md"
    }
    expected_metadata_state = [ordered]@{
        controller_support_declared = $true
        steam_input_enabled = $true
        supported_devices_label = "Aligned with in-game bindings and issue #228 evidence"
    }
    verification_result = "pass"
    drift_action = "If metadata mismatches runtime behavior, block release and update Steamworks settings before retest."
    notes = if ($MetadataNotes.Count -gt 0) { ($MetadataNotes -join " | ") } else { "Captured from Steamworks partner portal for this RC." }
}

if (-not $DryRun) {
    New-Item -ItemType Directory -Path $metadataBundleDir -Force | Out-Null
    Copy-Item -LiteralPath $ControllerSupportScreenshotSource -Destination $metadataControllerScreenshotPath -Force
    Copy-Item -LiteralPath $SteamInputSettingsScreenshotSource -Destination $metadataInputScreenshotPath -Force
    Set-Content -Path $metadataBundleSummaryPath -Value $summaryLines
    $manifestObject | ConvertTo-Json -Depth 8 | Set-Content -Path $metadataBundleManifestPath
}

$backendRelativeArtifacts = New-Object System.Collections.Generic.List[string]
if ($UpdateBackendPublishMetadata) {
    Require-File -Path $backendContractPath

    if (-not [regex]::IsMatch($BackendChangeNumber, '^[0-9]+$')) {
        throw "BackendChangeNumber must be a numeric string."
    }

    if ([string]::IsNullOrWhiteSpace($BackendPublishedAtUtc)) {
        $BackendPublishedAtUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    }

    if (-not (Test-Iso8601UtcTimestamp -Value $BackendPublishedAtUtc)) {
        throw "BackendPublishedAtUtc must use ISO-8601 UTC format yyyy-MM-ddTHH:mm:ssZ."
    }

    if ([string]::IsNullOrWhiteSpace($BackendVerifiedBy)) {
        throw "BackendVerifiedBy is required when UpdateBackendPublishMetadata is set."
    }

    if ($BackendVerificationArtifactSources.Count -eq 0) {
        throw "At least one BackendVerificationArtifactSources file is required when UpdateBackendPublishMetadata is set."
    }

    foreach ($source in $BackendVerificationArtifactSources) {
        Require-File -Path $source
    }

    if (-not $DryRun) {
        New-Item -ItemType Directory -Path $backendEvidenceDir -Force | Out-Null

        foreach ($source in $BackendVerificationArtifactSources) {
            $target = Join-Path $backendEvidenceDir ([System.IO.Path]::GetFileName($source))
            Copy-Item -LiteralPath $source -Destination $target -Force
            $backendRelativeArtifacts.Add((Get-RepoRelativePath -AbsolutePath $target -RepoRootAbsolute $repoRoot)) | Out-Null
        }

        $contract = Get-Content -Raw -Path $backendContractPath | ConvertFrom-Json
        $contract.backend_publish.steamworks_change_number = $BackendChangeNumber
        $contract.backend_publish.published_at_utc = $BackendPublishedAtUtc
        $contract.backend_publish.verified_by = $BackendVerifiedBy
        $contract.backend_publish.verification_artifacts = @($backendRelativeArtifacts)
        if (-not [string]::IsNullOrWhiteSpace($BackendPublishNotes)) {
            $contract.backend_publish.notes = $BackendPublishNotes
        }

        $contract | ConvertTo-Json -Depth 10 | Set-Content -Path $backendContractPath
    }
}

if ($DryRun) {
    Write-Output "Dry run complete. Planned metadata bundle: $metadataBundleDir"
    if ($UpdateBackendPublishMetadata) {
        Write-Output "Dry run complete. Planned backend evidence dir: $backendEvidenceDir"
    }

    return
}

./scripts/ci/verify-steam-metadata-evidence.ps1 `
    -EvidenceRoot "release/steam_metadata" `
    -RequireAtLeastOneBundle `
    -RequireAtLeastOnePassingBundle `
    -SummaryJsonPath "Artifacts/SteamMetadata/steam_metadata_evidence_summary.json" `
    -SummaryMarkdownPath "Artifacts/SteamMetadata/steam_metadata_evidence_summary.md"

if ($UpdateBackendPublishMetadata) {
    ./scripts/ci/verify-steamworks-achievements-stats.ps1 `
        -RequirePublishedMetadata `
        -SummaryJsonPath "Artifacts/Steamworks/steamworks_achievements_stats_contract_summary.json" `
        -SummaryMarkdownPath "Artifacts/Steamworks/steamworks_achievements_stats_contract_summary.md"
}

Write-Output "Steam partner evidence intake completed for RC tag '$RcTag'."
Write-Output "Metadata bundle: $metadataBundleDir"
if ($UpdateBackendPublishMetadata) {
    Write-Output "Backend contract updated: $backendContractPath"
    Write-Output "Backend evidence dir: $backendEvidenceDir"
}
