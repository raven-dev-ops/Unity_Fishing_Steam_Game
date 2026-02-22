param(
    [string]$UnityPath = "",
    [string]$ProjectPath = "",
    [switch]$PrintOnly,
    [switch]$AllowVersionMismatch
)

$ErrorActionPreference = "Stop"
$LauncherApiVersion = 2

function Get-ProjectRoot {
    if (-not [string]::IsNullOrWhiteSpace($ProjectPath)) {
        return (Resolve-Path $ProjectPath).Path
    }

    return (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function Get-ProjectVersion {
    param([string]$Root)

    $versionFile = Join-Path $Root "ProjectSettings\ProjectVersion.txt"
    if (-not (Test-Path $versionFile)) {
        return ""
    }

    $content = Get-Content $versionFile
    foreach ($line in $content) {
        if ($line -match "^m_EditorVersion:\s*(.+)$") {
            return $matches[1].Trim()
        }
    }

    return ""
}

function Resolve-UnityExeFromHint {
    param([string]$PathHint)

    if ([string]::IsNullOrWhiteSpace($PathHint)) {
        return ""
    }

    $hint = [Environment]::ExpandEnvironmentVariables($PathHint.Trim().Trim('"'))
    $hint = $hint -replace '/', '\'

    $candidates = New-Object System.Collections.Generic.List[string]
    $candidates.Add($hint)

    if (Test-Path -LiteralPath $hint -PathType Container) {
        $candidates.Add((Join-Path $hint "Unity.exe"))
        $candidates.Add((Join-Path $hint "Editor\Unity.exe"))
    }

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            continue
        }

        if ([string]::Equals([System.IO.Path]::GetFileName($candidate), "Unity.exe", [System.StringComparison]::OrdinalIgnoreCase)) {
            return (Resolve-Path $candidate).Path
        }
    }

    return ""
}

function Get-UnityHubEditors {
    $hubEditorsPath = Join-Path $env:AppData "UnityHub\editors-v2.json"
    if (-not (Test-Path -LiteralPath $hubEditorsPath -PathType Leaf)) {
        return @()
    }

    try {
        $data = Get-Content -Raw -Path $hubEditorsPath | ConvertFrom-Json
        if ($null -eq $data -or $null -eq $data.data) {
            return @()
        }

        return @($data.data)
    }
    catch {
        return @()
    }
}

function Get-UnityHubEditorVersionForExe {
    param(
        [string]$UnityExePath,
        [object[]]$HubEditors
    )

    if ([string]::IsNullOrWhiteSpace($UnityExePath) -or $HubEditors -eq $null) {
        return ""
    }

    $normalizedTarget = ""
    try {
        $normalizedTarget = (Resolve-Path -LiteralPath $UnityExePath).Path
    }
    catch {
        return ""
    }

    foreach ($entry in $HubEditors) {
        if ($null -eq $entry) {
            continue
        }

        $version = [string]$entry.version
        if ([string]::IsNullOrWhiteSpace($version)) {
            continue
        }

        $locations = @()
        if ($entry.location -is [System.Array]) {
            $locations = @($entry.location)
        }
        elseif (-not [string]::IsNullOrWhiteSpace([string]$entry.location)) {
            $locations = @([string]$entry.location)
        }

        foreach ($location in $locations) {
            $candidateExe = Resolve-UnityExeFromHint -PathHint ([string]$location)
            if ([string]::IsNullOrWhiteSpace($candidateExe)) {
                continue
            }

            try {
                $normalizedCandidate = (Resolve-Path -LiteralPath $candidateExe).Path
            }
            catch {
                continue
            }

            if ([string]::Equals($normalizedCandidate, $normalizedTarget, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $version
            }
        }
    }

    return ""
}

function Get-VersionTokenFromPath {
    param([string]$PathValue)

    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return ""
    }

    if ($PathValue -match "(?<version>\d+\.\d+\.\d+f\d+)") {
        return $matches["version"]
    }

    return ""
}

function Get-UnityEditorVersion {
    param(
        [string]$UnityExePath,
        [object[]]$HubEditors
    )

    $hubVersion = Get-UnityHubEditorVersionForExe -UnityExePath $UnityExePath -HubEditors $HubEditors
    if (-not [string]::IsNullOrWhiteSpace($hubVersion)) {
        return $hubVersion
    }

    $pathVersion = Get-VersionTokenFromPath -PathValue $UnityExePath
    if (-not [string]::IsNullOrWhiteSpace($pathVersion)) {
        return $pathVersion
    }

    try {
        $fileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($UnityExePath).ProductVersion
        $fileToken = Get-VersionTokenFromPath -PathValue $fileVersion
        if (-not [string]::IsNullOrWhiteSpace($fileToken)) {
            return $fileToken
        }
    }
    catch {
    }

    return ""
}

function Assert-UnityEditorVersion {
    param(
        [string]$UnityExePath,
        [string]$ExpectedVersion,
        [object[]]$HubEditors,
        [bool]$AllowMismatch
    )

    if ([string]::IsNullOrWhiteSpace($ExpectedVersion)) {
        return
    }

    $actualVersion = Get-UnityEditorVersion -UnityExePath $UnityExePath -HubEditors $HubEditors
    if ([string]::IsNullOrWhiteSpace($actualVersion)) {
        if ($AllowMismatch) {
            Write-Warning "Unity launcher: unable to verify editor version for '$UnityExePath'. Continuing because -AllowVersionMismatch is set."
            return
        }

        throw "Unity launcher: unable to verify editor version for '$UnityExePath'. Expected project version '$ExpectedVersion'. Use a Unity Hub-managed editor path/version, or pass -AllowVersionMismatch."
    }

    if (-not [string]::Equals($actualVersion, $ExpectedVersion, [System.StringComparison]::OrdinalIgnoreCase)) {
        if ($AllowMismatch) {
            Write-Warning "Unity launcher: using Unity '$actualVersion' while project expects '$ExpectedVersion'. Continuing because -AllowVersionMismatch is set."
            return
        }

        throw "Unity launcher: Unity version mismatch. Expected '$ExpectedVersion', resolved '$actualVersion' at '$UnityExePath'. Install/use the pinned editor version or pass -AllowVersionMismatch."
    }
}

function Get-UnityHubSecondaryInstallPath {
    $secondaryPathFile = Join-Path $env:AppData "UnityHub\secondaryInstallPath.json"
    if (-not (Test-Path -LiteralPath $secondaryPathFile -PathType Leaf)) {
        return ""
    }

    try {
        $secondaryPath = Get-Content -Raw -Path $secondaryPathFile | ConvertFrom-Json
        if ($secondaryPath -is [string]) {
            return $secondaryPath
        }
    }
    catch {
    }

    return ""
}

function Resolve-UnityEditorPath {
    param([string]$Root)

    $preferredCandidates = New-Object System.Collections.Generic.List[string]
    $fallbackCandidates = New-Object System.Collections.Generic.List[string]

    function Add-Candidate {
        param(
            [string]$Hint,
            [bool]$Preferred
        )

        $resolved = Resolve-UnityExeFromHint -PathHint $Hint
        if ([string]::IsNullOrWhiteSpace($resolved)) {
            return
        }

        if ($preferredCandidates.Contains($resolved) -or $fallbackCandidates.Contains($resolved)) {
            return
        }

        if ($Preferred) {
            $preferredCandidates.Add($resolved)
            return
        }

        $fallbackCandidates.Add($resolved)
    }

    $projectVersion = Get-ProjectVersion -Root $Root
    $hubEditors = Get-UnityHubEditors
    $hubEditorVersions = New-Object System.Collections.Generic.List[string]

    Add-Candidate -Hint $UnityPath -Preferred $true
    Add-Candidate -Hint $env:UNITY_EDITOR_PATH -Preferred $true
    Add-Candidate -Hint $env:UNITY_PATH -Preferred $true

    $hubEditorRoots = New-Object System.Collections.Generic.List[string]
    foreach ($rootCandidate in @(
        "$env:ProgramFiles\Unity\Hub\Editor",
        "$env:ProgramFiles(x86)\Unity\Hub\Editor",
        "$env:LocalAppData\Programs\Unity\Hub\Editor",
        "$env:LocalAppData\Unity\Hub\Editor",
        (Get-UnityHubSecondaryInstallPath)
    )) {
        if ([string]::IsNullOrWhiteSpace($rootCandidate)) {
            continue
        }

        $expanded = [Environment]::ExpandEnvironmentVariables($rootCandidate)
        if ([string]::IsNullOrWhiteSpace($expanded) -or $hubEditorRoots.Contains($expanded)) {
            continue
        }

        $hubEditorRoots.Add($expanded)
    }

    foreach ($hubRoot in $hubEditorRoots) {
        if (-not (Test-Path -LiteralPath $hubRoot -PathType Container)) {
            continue
        }

        if (-not [string]::IsNullOrWhiteSpace($projectVersion)) {
            Add-Candidate -Hint (Join-Path $hubRoot $projectVersion) -Preferred $true
            Add-Candidate -Hint (Join-Path $hubRoot "$projectVersion\Editor\Unity.exe") -Preferred $true
        }

        Add-Candidate -Hint (Join-Path $hubRoot "Unity.exe") -Preferred $false
        Add-Candidate -Hint (Join-Path $hubRoot "Editor\Unity.exe") -Preferred $false

        $installedVersionDirs = Get-ChildItem -Path $hubRoot -Directory -ErrorAction SilentlyContinue
        foreach ($versionDir in $installedVersionDirs) {
            Add-Candidate -Hint (Join-Path $versionDir.FullName "Editor\Unity.exe") -Preferred $false
        }
    }

    foreach ($entry in $hubEditors) {
        if ($null -eq $entry) {
            continue
        }

        $entryVersion = [string]$entry.version
        if (-not [string]::IsNullOrWhiteSpace($entryVersion) -and -not $hubEditorVersions.Contains($entryVersion)) {
            $hubEditorVersions.Add($entryVersion)
        }

        $locations = @()
        if ($entry.location -is [System.Array]) {
            $locations = @($entry.location)
        }
        elseif (-not [string]::IsNullOrWhiteSpace([string]$entry.location)) {
            $locations = @([string]$entry.location)
        }

        $isExactVersionMatch = -not [string]::IsNullOrWhiteSpace($projectVersion) -and [string]::Equals($entryVersion, $projectVersion, [System.StringComparison]::OrdinalIgnoreCase)
        foreach ($location in $locations) {
            Add-Candidate -Hint ([string]$location) -Preferred $isExactVersionMatch
        }
    }

    Add-Candidate -Hint "$env:ProgramFiles\Unity\Editor\Unity.exe" -Preferred $false
    Add-Candidate -Hint "$env:ProgramFiles(x86)\Unity\Editor\Unity.exe" -Preferred $false

    foreach ($resolvedPath in @($preferredCandidates + $fallbackCandidates)) {
        if (-not [string]::IsNullOrWhiteSpace($resolvedPath)) {
            return $resolvedPath
        }
    }

    $detectedVersions = if ($hubEditorVersions.Count -gt 0) {
        [string]::Join(", ", @($hubEditorVersions | Sort-Object))
    }
    else {
        "none"
    }

    throw "Unity Editor executable not found. Project version: '$projectVersion'. Unity Hub detected versions: $detectedVersions. Set UNITY_EDITOR_PATH to Unity.exe or install the required editor in Unity Hub."
}

$root = Get-ProjectRoot
$projectVersion = Get-ProjectVersion -Root $root
$hubEditors = Get-UnityHubEditors
$unityExe = Resolve-UnityEditorPath -Root $root
Assert-UnityEditorVersion -UnityExePath $unityExe -ExpectedVersion $projectVersion -HubEditors $hubEditors -AllowMismatch $AllowVersionMismatch.IsPresent

if ($PrintOnly) {
    Write-Host $unityExe
    exit 0
}

Write-Host "Opening Unity project..."
Write-Host "Unity:   $unityExe"
Write-Host "Project: $root"
if (-not [string]::IsNullOrWhiteSpace($projectVersion)) {
    Write-Host "EditorVersion: $projectVersion"
}

Start-Process -FilePath $unityExe -ArgumentList @("-projectPath", $root) | Out-Null
