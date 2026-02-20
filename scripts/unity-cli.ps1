param(
    [ValidateSet('custom', 'build', 'validate', 'test-edit', 'test-play')]
    [string]$Task = 'custom',
    [string]$Method = '',
    [string]$UnityPath = '',
    [string]$ProjectPath = '',
    [string]$LogFile = 'unity_cli.log',
    [string[]]$ExtraArgs
)

$ErrorActionPreference = 'Stop'

function Get-ProjectRoot {
    if (-not [string]::IsNullOrWhiteSpace($ProjectPath)) {
        return (Resolve-Path $ProjectPath).Path
    }

    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Get-ProjectVersion {
    param([string]$Root)

    $versionFile = Join-Path $Root 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path $versionFile)) {
        return ''
    }

    $content = Get-Content $versionFile
    foreach ($line in $content) {
        if ($line -match '^m_EditorVersion:\s*(.+)$') {
            return $matches[1].Trim()
        }
    }

    return ''
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

    throw "Unity Editor executable not found. Set UNITY_EDITOR_PATH or pass -UnityPath to scripts/unity-cli.ps1."
}

$root = Get-ProjectRoot
$unityExe = Resolve-UnityEditorPath -Root $root

$args = New-Object System.Collections.Generic.List[string]
$args.Add('-batchmode')
$args.Add('-nographics')
$args.Add('-quit')
$args.Add('-projectPath')
$args.Add($root)
$args.Add('-logFile')
$args.Add($LogFile)

switch ($Task) {
    'build' {
        $args.Add('-executeMethod')
        $args.Add('RavenDevOps.Fishing.EditorTools.BuildCommandLine.BuildWindowsBatchMode')
    }
    'validate' {
        $args.Add('-executeMethod')
        $args.Add('RavenDevOps.Fishing.EditorTools.ContentValidatorRunner.ValidateCatalogBatchMode')
    }
    'test-edit' {
        $args.Add('-runTests')
        $args.Add('-testPlatform')
        $args.Add('editmode')
    }
    'test-play' {
        $args.Add('-runTests')
        $args.Add('-testPlatform')
        $args.Add('playmode')
    }
    'custom' {
        if (-not [string]::IsNullOrWhiteSpace($Method)) {
            $args.Add('-executeMethod')
            $args.Add($Method)
        }
    }
}

if ($ExtraArgs -ne $null) {
    foreach ($extra in $ExtraArgs) {
        if (-not [string]::IsNullOrWhiteSpace($extra)) {
            $args.Add($extra)
        }
    }
}

Write-Host "Unity CLI: $unityExe"
Write-Host "Project: $root"
Write-Host "Task: $Task"

& $unityExe @args
$exitCode = $LASTEXITCODE
if ($null -eq $exitCode) {
    $exitCode = 0
}

exit $exitCode
