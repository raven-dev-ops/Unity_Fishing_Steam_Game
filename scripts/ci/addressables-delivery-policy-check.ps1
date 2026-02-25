param(
    [string]$PolicyConfigPath = "ci/addressables-delivery-policy.json",
    [string]$ManifestPath = "Packages/manifest.json",
    [string]$LoaderScriptPath = "Assets/Scripts/Data/AddressablesPilotCatalogLoader.cs",
    [string]$SummaryJsonPath = "Artifacts/AddressablesPolicy/addressables_delivery_policy_summary.json",
    [string]$SummaryMarkdownPath = "Artifacts/AddressablesPolicy/addressables_delivery_policy_summary.md"
)

$ErrorActionPreference = "Stop"

function Ensure-ParentDirectory {
    param([string]$Path)

    $parent = Split-Path -Path $Path -Parent
    if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
}

function Add-Check {
    param(
        [System.Collections.Generic.List[object]]$Checks,
        [string]$Name,
        [string]$Status,
        [string]$Detail
    )

    $Checks.Add([ordered]@{
        name = $Name
        status = $Status
        detail = $Detail
    }) | Out-Null
}

function Add-Failure {
    param(
        [System.Collections.Generic.List[object]]$Failures,
        [string]$Reason,
        [string]$Detail
    )

    $Failures.Add([ordered]@{
        reason = $Reason
        detail = $Detail
    }) | Out-Null
}

if (-not (Test-Path -LiteralPath $PolicyConfigPath -PathType Leaf)) {
    throw "Addressables policy check: policy file not found: $PolicyConfigPath"
}

if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "Addressables policy check: manifest file not found: $ManifestPath"
}

if (-not (Test-Path -LiteralPath $LoaderScriptPath -PathType Leaf)) {
    throw "Addressables policy check: loader script not found: $LoaderScriptPath"
}

$policy = Get-Content -Raw -Path $PolicyConfigPath | ConvertFrom-Json
$manifest = Get-Content -Raw -Path $ManifestPath | ConvertFrom-Json
$loaderScriptContent = Get-Content -Raw -Path $LoaderScriptPath

$checks = New-Object System.Collections.Generic.List[object]
$failures = New-Object System.Collections.Generic.List[object]

$launchMode = [string]$policy.launch_mode
$manifestDependency = if ($null -ne $policy.unsafe_patterns -and $null -ne $policy.unsafe_patterns.manifest_dependency) {
    [string]$policy.unsafe_patterns.manifest_dependency
} else {
    "com.unity.addressables"
}

$addressablesAssetRoot = if ($null -ne $policy.unsafe_patterns -and $null -ne $policy.unsafe_patterns.addressables_asset_root) {
    [string]$policy.unsafe_patterns.addressables_asset_root
} else {
    "Assets/AddressableAssetsData"
}

$dependencies = @()
if ($null -ne $manifest.dependencies) {
    $dependencies = @($manifest.dependencies.PSObject.Properties.Name)
}

$hasAddressablesPackage = $dependencies -contains $manifestDependency
$packageAllowed = [bool]$policy.addressables_package_allowed
if (-not $packageAllowed -and $hasAddressablesPackage) {
    Add-Check -Checks $checks -Name "manifest_dependency_guard" -Status "failed" -Detail "Found disallowed dependency '$manifestDependency'."
    Add-Failure -Failures $failures -Reason "addressables_package_disallowed" -Detail "Remove '$manifestDependency' for launch mode '$launchMode'."
}
else {
    Add-Check -Checks $checks -Name "manifest_dependency_guard" -Status "passed" -Detail "Manifest dependency rule satisfied."
}

$hasAddressablesAssetRoot = Test-Path -LiteralPath $addressablesAssetRoot
$assetsAllowed = [bool]$policy.addressables_assets_allowed
if (-not $assetsAllowed -and $hasAddressablesAssetRoot) {
    Add-Check -Checks $checks -Name "addressables_asset_root_guard" -Status "failed" -Detail "Found disallowed Addressables asset root '$addressablesAssetRoot'."
    Add-Failure -Failures $failures -Reason "addressables_assets_disallowed" -Detail "Remove Addressables asset root '$addressablesAssetRoot' for launch mode '$launchMode'."
}
else {
    Add-Check -Checks $checks -Name "addressables_asset_root_guard" -Status "passed" -Detail "Addressables asset-root rule satisfied."
}

$toggleMustBeFalse = [bool]$policy.runtime_toggle_required_false
$toggleMatch = [regex]::Match($loaderScriptContent, "_useAddressablesWhenAvailable\s*=\s*(true|false)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if (-not $toggleMatch.Success) {
    Add-Check -Checks $checks -Name "runtime_toggle_guard" -Status "failed" -Detail "Could not resolve '_useAddressablesWhenAvailable' default in loader script."
    Add-Failure -Failures $failures -Reason "runtime_toggle_unresolved" -Detail "Loader default must be explicit for policy enforcement."
}
else {
    $toggleValue = [string]$toggleMatch.Groups[1].Value
    $toggleIsTrue = [string]::Equals($toggleValue, "true", [System.StringComparison]::OrdinalIgnoreCase)
    if ($toggleMustBeFalse -and $toggleIsTrue) {
        Add-Check -Checks $checks -Name "runtime_toggle_guard" -Status "failed" -Detail "Loader default sets '_useAddressablesWhenAvailable=true'."
        Add-Failure -Failures $failures -Reason "runtime_toggle_must_be_false" -Detail "Set '_useAddressablesWhenAvailable=false' for launch policy."
    }
    else {
        Add-Check -Checks $checks -Name "runtime_toggle_guard" -Status "passed" -Detail "Loader runtime toggle default is policy-compliant ($toggleValue)."
    }
}

$summaryStatus = if ($failures.Count -gt 0) { "failed" } else { "passed" }
$summaryReason = if ($failures.Count -gt 0) { "policy_violations_detected" } else { "policy_compliant" }

$summary = @{
    status = $summaryStatus
    reason = $summaryReason
    generated_utc = (Get-Date).ToUniversalTime().ToString("o")
    policy_path = $PolicyConfigPath
    manifest_path = $ManifestPath
    loader_script_path = $LoaderScriptPath
    launch_mode = $launchMode
    check_count = $checks.Count
    failure_count = $failures.Count
    checks = @($checks | ForEach-Object { $_ })
    failures = @($failures | ForEach-Object { $_ })
}

Ensure-ParentDirectory -Path $SummaryJsonPath
($summary | ConvertTo-Json -Depth 10) | Set-Content -Path $SummaryJsonPath

$md = New-Object System.Collections.Generic.List[string]
$md.Add("# Addressables Delivery Policy Summary")
$md.Add("")
$md.Add(("Status: **{0}**" -f $summary.status.ToUpperInvariant()))
$md.Add(("Reason: {0}" -f $summary.reason))
$md.Add(("Launch mode: {0}" -f $summary.launch_mode))
$md.Add("")
$md.Add("| Check | Status | Detail |")
$md.Add("|---|---|---|")
foreach ($check in $checks) {
    $md.Add(("{0} | {1} | {2}" -f $check.name, $check.status, $check.detail))
}

$md.Add("")
$md.Add("## Failures")
if ($failures.Count -eq 0) {
    $md.Add("- none")
}
else {
    foreach ($failure in $failures) {
        $md.Add(("- reason={0} detail={1}" -f $failure.reason, $failure.detail))
    }
}

Ensure-ParentDirectory -Path $SummaryMarkdownPath
$md | Set-Content -Path $SummaryMarkdownPath

if ($summaryStatus -eq "failed") {
    Write-Host ("Addressables delivery policy check: FAILED ({0} failure(s))." -f $failures.Count)
    exit 1
}

Write-Host "Addressables delivery policy check: PASSED."
exit 0
