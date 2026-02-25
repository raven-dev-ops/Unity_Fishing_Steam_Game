[CmdletBinding()]
param(
    [string]$EvidenceRoot = "release/steam_metadata",
    [bool]$RequireAtLeastOneBundle = $false,
    [string]$SummaryJsonPath = "",
    [string]$SummaryMarkdownPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Add-ResultIssue {
    param(
        [System.Collections.Generic.List[string]]$Issues,
        [string]$Message
    )

    if ($null -eq $Issues) {
        return
    }

    $Issues.Add($Message)
}

if (-not (Test-Path -Path $EvidenceRoot -PathType Container)) {
    throw "Steam metadata evidence root not found: $EvidenceRoot"
}

$bundleDirs = @(Get-ChildItem -Path $EvidenceRoot -Directory |
        Where-Object { $_.Name -ne "rc-template" } |
        Sort-Object Name)

$bundleSummaries = New-Object System.Collections.Generic.List[object]
$overallIssues = New-Object System.Collections.Generic.List[string]

if ($bundleDirs.Count -eq 0 -and $RequireAtLeastOneBundle) {
    Add-ResultIssue -Issues $overallIssues -Message "No RC metadata bundles found under '$EvidenceRoot' (template excluded)."
}

foreach ($bundleDir in $bundleDirs) {
    $issues = New-Object System.Collections.Generic.List[string]
    $manifestPath = Join-Path $bundleDir.FullName "manifest.json"

    if (-not (Test-Path -Path $manifestPath -PathType Leaf)) {
        Add-ResultIssue -Issues $issues -Message "Missing manifest.json"
        $bundleSummaries.Add([pscustomobject]@{
                bundle = $bundleDir.Name
                status = "fail"
                issues = $issues
            })
        Add-ResultIssue -Issues $overallIssues -Message "Bundle '$($bundleDir.Name)' missing manifest.json."
        continue
    }

    $manifestRaw = Get-Content -Path $manifestPath -Raw
    $manifest = $null
    try {
        $manifest = $manifestRaw | ConvertFrom-Json
    }
    catch {
        Add-ResultIssue -Issues $issues -Message "manifest.json is not valid JSON: $($_.Exception.Message)"
    }

    if ($null -ne $manifest) {
        if ($manifest.schema_version -ne 1) {
            Add-ResultIssue -Issues $issues -Message "schema_version must be 1."
        }

        if ([string]::IsNullOrWhiteSpace($manifest.rc_tag)) {
            Add-ResultIssue -Issues $issues -Message "rc_tag is required."
        }
        elseif (-not [string]::Equals($manifest.rc_tag, $bundleDir.Name, [System.StringComparison]::Ordinal)) {
            Add-ResultIssue -Issues $issues -Message "rc_tag '$($manifest.rc_tag)' must match bundle folder '$($bundleDir.Name)'."
        }

        if ([string]::IsNullOrWhiteSpace($manifest.captured_at_utc)) {
            Add-ResultIssue -Issues $issues -Message "captured_at_utc is required."
        }
        else {
            $parsedCapturedAt = [DateTime]::MinValue
            if (-not [DateTime]::TryParse($manifest.captured_at_utc, [ref]$parsedCapturedAt)) {
                Add-ResultIssue -Issues $issues -Message "captured_at_utc must be parseable datetime."
            }
        }

        if ([string]::IsNullOrWhiteSpace($manifest.captured_by)) {
            Add-ResultIssue -Issues $issues -Message "captured_by is required."
        }

        if ([string]::IsNullOrWhiteSpace($manifest.verification_result)) {
            Add-ResultIssue -Issues $issues -Message "verification_result is required."
        }
        else {
            $result = $manifest.verification_result.ToString().Trim().ToLowerInvariant()
            if ($result -ne "pass" -and $result -ne "fail") {
                Add-ResultIssue -Issues $issues -Message "verification_result must be 'pass' or 'fail'."
            }
        }

        if ($null -eq $manifest.evidence_files) {
            Add-ResultIssue -Issues $issues -Message "evidence_files object is required."
        }
        else {
            foreach ($key in @("controller_support", "steam_input_settings", "summary")) {
                $relative = $manifest.evidence_files.$key
                if ([string]::IsNullOrWhiteSpace($relative)) {
                    Add-ResultIssue -Issues $issues -Message "evidence_files.$key is required."
                    continue
                }

                $resolved = Join-Path $bundleDir.FullName $relative
                if (-not (Test-Path -Path $resolved -PathType Leaf)) {
                    Add-ResultIssue -Issues $issues -Message "Missing evidence file '$relative'."
                }
            }
        }
    }

    $status = if ($issues.Count -eq 0) { "pass" } else { "fail" }
    if ($status -eq "fail") {
        foreach ($issue in $issues) {
            Add-ResultIssue -Issues $overallIssues -Message "Bundle '$($bundleDir.Name)': $issue"
        }
    }

    $bundleSummaries.Add([pscustomobject]@{
            bundle = $bundleDir.Name
            status = $status
            issues = $issues
        })
}

$overallStatus = if ($overallIssues.Count -eq 0) { "pass" } else { "fail" }
$summaryObject = [pscustomobject]@{
    evidence_root = (Resolve-Path $EvidenceRoot).Path
    bundle_count = $bundleDirs.Count
    require_at_least_one_bundle = $RequireAtLeastOneBundle
    overall_status = $overallStatus
    bundles = $bundleSummaries
    issues = $overallIssues
}

if (-not [string]::IsNullOrWhiteSpace($SummaryJsonPath)) {
    $summaryDir = Split-Path -Path $SummaryJsonPath -Parent
    if (-not [string]::IsNullOrWhiteSpace($summaryDir)) {
        New-Item -ItemType Directory -Path $summaryDir -Force | Out-Null
    }

    $summaryObject | ConvertTo-Json -Depth 8 | Set-Content -Path $SummaryJsonPath -Encoding utf8
}

if (-not [string]::IsNullOrWhiteSpace($SummaryMarkdownPath)) {
    $summaryDir = Split-Path -Path $SummaryMarkdownPath -Parent
    if (-not [string]::IsNullOrWhiteSpace($summaryDir)) {
        New-Item -ItemType Directory -Path $summaryDir -Force | Out-Null
    }

    $mdLines = New-Object System.Collections.Generic.List[string]
    $mdLines.Add("# Steam Metadata Evidence Verification")
    $mdLines.Add("")
    $mdLines.Add("- Evidence root: $($summaryObject.evidence_root)")
    $mdLines.Add("- Bundle count: $($summaryObject.bundle_count)")
    $mdLines.Add("- Overall status: $($summaryObject.overall_status)")
    $mdLines.Add("")
    $mdLines.Add("| Bundle | Status | Issues |")
    $mdLines.Add("|---|---|---|")
    foreach ($bundle in $bundleSummaries) {
        $issueText = if ($bundle.issues.Count -eq 0) { "none" } else { ($bundle.issues -join "; ") }
        $mdLines.Add("| $($bundle.bundle) | $($bundle.status) | $issueText |")
    }

    Set-Content -Path $SummaryMarkdownPath -Value $mdLines -Encoding utf8
}

if ($overallStatus -ne "pass") {
    foreach ($issue in $overallIssues) {
        Write-Error "Steam metadata evidence verification failed: $issue"
    }

    throw "Steam metadata evidence verification failed."
}

Write-Host "Steam metadata evidence verification passed ($($bundleDirs.Count) bundle(s))."
