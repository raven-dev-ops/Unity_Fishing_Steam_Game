param(
    [string]$UnityPath = "",
    [string]$ProjectPath = ""
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

function Resolve-UnityEditorPath {
    param([string]$Root)

    if (-not [string]::IsNullOrWhiteSpace($UnityPath) -and (Test-Path $UnityPath)) {
        return (Resolve-Path $UnityPath).Path
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EDITOR_PATH) -and (Test-Path $env:UNITY_EDITOR_PATH)) {
        return (Resolve-Path $env:UNITY_EDITOR_PATH).Path
    }

    $projectVersion = Get-ProjectVersion -Root $Root
    $candidates = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($projectVersion)) {
        $candidates.Add("$env:ProgramFiles\Unity\Hub\Editor\$projectVersion\Editor\Unity.exe")
    }

    $candidates.Add("$env:ProgramFiles\Unity\Hub\Editor\Unity.exe")
    $candidates.Add("$env:ProgramFiles\Unity\Editor\Unity.exe")

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    $hubEditorRoot = "$env:ProgramFiles\Unity\Hub\Editor"
    if (Test-Path $hubEditorRoot) {
        $found = Get-ChildItem -Path $hubEditorRoot -Recurse -Filter Unity.exe -ErrorAction SilentlyContinue |
            Sort-Object FullName |
            Select-Object -First 1

        if ($found -ne $null) {
            return $found.FullName
        }
    }

    throw "Unity Editor executable not found. Install Unity Hub editor $projectVersion or set UNITY_EDITOR_PATH."
}

$root = Get-ProjectRoot
$unityExe = Resolve-UnityEditorPath -Root $root

Write-Host "Opening Unity project..."
Write-Host "Unity:   $unityExe"
Write-Host "Project: $root"

Start-Process -FilePath $unityExe -ArgumentList @("-projectPath", $root) | Out-Null
