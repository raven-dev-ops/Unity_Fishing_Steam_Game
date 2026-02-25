param(
    [string]$OutputRoot = "Artifacts/SteamMetadataRehearsal",
    [string]$SummaryJsonPath = "Artifacts/SteamMetadataRehearsal/steam_metadata_drift_rehearsal_summary.json",
    [string]$SummaryMarkdownPath = "Artifacts/SteamMetadataRehearsal/steam_metadata_drift_rehearsal_summary.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$bundleRoot = Join-Path $OutputRoot "evidence"
$bundleDir = Join-Path $bundleRoot "rc-drift-rehearsal"

if (Test-Path -Path $OutputRoot) {
    Remove-Item -Path $OutputRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $bundleDir -Force | Out-Null

$controllerPngPath = Join-Path $bundleDir "controller_support.png"
$steamInputPngPath = Join-Path $bundleDir "steam_input_settings.png"
$summaryPath = Join-Path $bundleDir "summary.md"
$manifestPath = Join-Path $bundleDir "manifest.json"

Set-Content -Path $controllerPngPath -Value "rehearsal-placeholder"
Set-Content -Path $steamInputPngPath -Value "rehearsal-placeholder"
Set-Content -Path $summaryPath -Value @(
    "# Steam Metadata Drift Rehearsal Summary"
    ""
    "- Scenario: simulated metadata mismatch"
    "- Expected action: release gate blocks until a pass bundle exists"
    "- Remediation issue: #245"
)

$manifestObject = [ordered]@{
    schema_version = 1
    rc_tag = "rc-drift-rehearsal"
    captured_at_utc = "2026-02-25T00:00:00Z"
    captured_by = "rehearsal-bot"
    verification_result = "fail"
    notes = "Simulated metadata mismatch for rehearsal."
    evidence_files = [ordered]@{
        controller_support = "controller_support.png"
        steam_input_settings = "steam_input_settings.png"
        summary = "summary.md"
    }
}

$manifestObject | ConvertTo-Json -Depth 6 | Set-Content -Path $manifestPath

$verificationSummaryJson = Join-Path $OutputRoot "steam_metadata_evidence_strict_summary.json"
$verificationSummaryMarkdown = Join-Path $OutputRoot "steam_metadata_evidence_strict_summary.md"

$expectedFailureObserved = $false
$failureMessage = ""

try {
    & "./scripts/ci/verify-steam-metadata-evidence.ps1" `
        -EvidenceRoot $bundleRoot `
        -RequireAtLeastOneBundle `
        -RequireAtLeastOnePassingBundle `
        -SummaryJsonPath $verificationSummaryJson `
        -SummaryMarkdownPath $verificationSummaryMarkdown
}
catch {
    $expectedFailureObserved = $true
    $failureMessage = [string]$_.Exception.Message
}

if (-not $expectedFailureObserved) {
    throw "Expected strict verification to fail for rehearsal mismatch scenario, but it passed."
}

$verificationSummary = Get-Content -Path $verificationSummaryJson -Raw | ConvertFrom-Json
$hasNoPassIssue = @($verificationSummary.issues | Where-Object { $_ -match "No bundle with verification_result='pass'" }).Count -gt 0
if (-not $hasNoPassIssue) {
    throw "Strict verification failed, but expected no-pass-bundle issue was not reported."
}

$summaryObject = [ordered]@{
    generated_utc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    output_root = $OutputRoot
    rehearsal_bundle = $bundleDir
    expected_failure_observed = $expectedFailureObserved
    failure_message = $failureMessage
    strict_summary_json = $verificationSummaryJson
    strict_summary_markdown = $verificationSummaryMarkdown
    strict_summary_overall_status = [string]$verificationSummary.overall_status
    strict_summary_issue_count = @($verificationSummary.issues).Count
}

$summaryJsonDirectory = Split-Path -Path $SummaryJsonPath -Parent
if (-not [string]::IsNullOrWhiteSpace($summaryJsonDirectory)) {
    New-Item -ItemType Directory -Path $summaryJsonDirectory -Force | Out-Null
}

$summaryMarkdownDirectory = Split-Path -Path $SummaryMarkdownPath -Parent
if (-not [string]::IsNullOrWhiteSpace($summaryMarkdownDirectory)) {
    New-Item -ItemType Directory -Path $summaryMarkdownDirectory -Force | Out-Null
}

$summaryObject | ConvertTo-Json -Depth 6 | Set-Content -Path $SummaryJsonPath

$markdown = @()
$markdown += "# Steam Metadata Drift Rehearsal Summary"
$markdown += ""
$markdown += "- Generated UTC: ``$($summaryObject.generated_utc)``"
$markdown += "- Expected strict failure observed: ``$($summaryObject.expected_failure_observed)``"
$markdown += "- Strict verifier overall status: ``$($summaryObject.strict_summary_overall_status)``"
$markdown += "- Strict verifier issue count: ``$($summaryObject.strict_summary_issue_count)``"
$markdown += "- Failure message: ``$($summaryObject.failure_message)``"
$markdown += "- Strict summary JSON: ``$($summaryObject.strict_summary_json)``"
$markdown += "- Strict summary Markdown: ``$($summaryObject.strict_summary_markdown)``"

$markdown -join "`n" | Set-Content -Path $SummaryMarkdownPath

Write-Output "Steam metadata drift rehearsal completed."
Write-Output "Summary JSON: $SummaryJsonPath"
Write-Output "Summary Markdown: $SummaryMarkdownPath"
