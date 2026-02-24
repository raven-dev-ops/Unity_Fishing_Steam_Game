param(
    [ValidateSet('pack', 'pack-rows')]
    [string]$Command = 'pack',
    [string]$PythonPath = 'python',
    [string]$InputPath = '',
    [string[]]$Rows,
    [string]$Output = '',
    [string]$Json = '',
    [ValidateRange(1, 8192)]
    [int]$Columns = 8,
    [ValidateRange(0, 2048)]
    [int]$Margin = 0,
    [ValidateRange(0, 2048)]
    [int]$Spacing = 0
)

$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Resolve-ToolPath {
    param([string]$Root)

    $path = Join-Path $Root 'fishing_sprite_assets_placeholders_and_tools\tools\spritesheet_packer.py'
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Sprite sheet packer tool was not found at '$path'."
    }

    return $path
}

function Assert-RequiredParameter {
    param(
        [string]$Value,
        [string]$Name
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Missing required parameter: -$Name"
    }
}

$repoRoot = Get-RepoRoot
$toolPath = Resolve-ToolPath -Root $repoRoot

$arguments = New-Object System.Collections.Generic.List[string]
$arguments.Add($toolPath)
$arguments.Add($Command)

switch ($Command) {
    'pack' {
        Assert-RequiredParameter -Value $InputPath -Name 'InputPath'
        Assert-RequiredParameter -Value $Output -Name 'Output'
        $arguments.Add('--input')
        $arguments.Add($InputPath)
        $arguments.Add('--output')
        $arguments.Add($Output)
    }
    'pack-rows' {
        Assert-RequiredParameter -Value $Output -Name 'Output'
        if ($Rows -eq $null -or $Rows.Count -eq 0) {
            throw "Missing required parameter: -Rows (example: swim=./swim,caught=./caught,escape=./escape)"
        }

        $arguments.Add('--rows')
        foreach ($row in $Rows) {
            if ([string]::IsNullOrWhiteSpace($row)) {
                continue
            }

            $arguments.Add($row)
        }

        $arguments.Add('--output')
        $arguments.Add($Output)
    }
}

if (-not [string]::IsNullOrWhiteSpace($Json)) {
    $arguments.Add('--json')
    $arguments.Add($Json)
}

if ($Columns -gt 0) {
    $arguments.Add('--columns')
    $arguments.Add($Columns.ToString())
}

if ($Margin -ge 0) {
    $arguments.Add('--margin')
    $arguments.Add($Margin.ToString())
}

if ($Spacing -ge 0) {
    $arguments.Add('--spacing')
    $arguments.Add($Spacing.ToString())
}

Write-Host "Sprite sheet packer: $Command"
Write-Host "Tool: $toolPath"

& $PythonPath @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Sprite sheet packer command failed with exit code $LASTEXITCODE."
}
