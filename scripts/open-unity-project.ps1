param(
    [string]$UnityPath = "",
    [string]$ProjectPath = "",
    [switch]$PrintOnly
)

$ErrorActionPreference = "Stop"

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
$unityExe = Resolve-UnityEditorPath -Root $root

if ($PrintOnly) {
    Write-Host $unityExe
    exit 0
}

Write-Host "Opening Unity project..."
Write-Host "Unity:   $unityExe"
Write-Host "Project: $root"

Start-Process -FilePath $unityExe -ArgumentList @("-projectPath", $root) | Out-Null
