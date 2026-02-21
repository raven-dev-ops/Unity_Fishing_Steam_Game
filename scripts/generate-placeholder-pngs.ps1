param(
    [string]$OutputRoot = "Assets/Art/Placeholders",
    [string]$ManifestPath = "Assets/Art/Placeholders/placeholder_manifest.json",
    [int]$IconWidth = 512,
    [int]$IconHeight = 512,
    [int]$SceneWidth = 1920,
    [int]$SceneHeight = 1080
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

function Get-TextSourceFiles {
    $roots = @("Assets", "docs", "mods", "scripts")
    $files = New-Object System.Collections.Generic.List[System.IO.FileInfo]

    foreach ($root in $roots) {
        if (-not (Test-Path $root)) {
            continue
        }

        Get-ChildItem -Path $root -Recurse -File -Include *.cs, *.md, *.json, *.txt | ForEach-Object {
            if ($_.FullName -match "[\\/]Assets[\\/]Art[\\/]Placeholders[\\/]") {
                return
            }

            $files.Add($_)
        }
    }

    return $files
}

function Get-KnownIds {
    $pattern = "\b(?:fish|ship|hook)_[a-z0-9_]+\b"
    $idSet = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($file in (Get-TextSourceFiles)) {
        $content = Get-Content -Raw -Path $file.FullName
        $matches = [regex]::Matches($content, $pattern)
        foreach ($match in $matches) {
            [void]$idSet.Add($match.Value.ToLowerInvariant())
        }
    }

    return @($idSet | Sort-Object)
}

function Get-ReferencedPngIds {
    $pattern = "(?<![A-Za-z0-9_<>])([A-Za-z0-9_./-]+\.png)\b"
    $idSet = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($file in (Get-TextSourceFiles)) {
        $content = Get-Content -Raw -Path $file.FullName
        $matches = [regex]::Matches($content, $pattern)
        foreach ($match in $matches) {
            $value = $match.Groups[1].Value
            if ([string]::IsNullOrWhiteSpace($value)) {
                continue
            }

            if ($value.Contains("<") -or $value.Contains(">") -or $value.Contains("*")) {
                continue
            }

            $fileName = [System.IO.Path]::GetFileNameWithoutExtension($value)
            if ([string]::IsNullOrWhiteSpace($fileName)) {
                continue
            }

            [void]$idSet.Add($fileName.ToLowerInvariant())
        }
    }

    return @($idSet | Sort-Object)
}

function Get-SceneIds {
    $sceneSet = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)

    $sceneRoot = "Assets/Scenes"
    if (-not (Test-Path $sceneRoot)) {
        return @()
    }

    Get-ChildItem -Path $sceneRoot -File -Filter *.unity | ForEach-Object {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($_.Name).ToLowerInvariant()
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            [void]$sceneSet.Add(("scene_{0}" -f $name))
        }
    }

    return @($sceneSet | Sort-Object)
}

function Get-IconCategory {
    param([string]$Id)

    if ($Id.StartsWith("fish_")) {
        return "Fish"
    }

    if ($Id.StartsWith("ship_")) {
        return "Ships"
    }

    if ($Id.StartsWith("hook_")) {
        return "Hooks"
    }

    if ($Id.StartsWith("ui_") -or $Id.Contains("icon")) {
        return "UI"
    }

    return "Misc"
}

function Build-FileName {
    param([string]$Id)

    $segments = $Id.Split("_")
    if ($segments.Length -ge 2) {
        $typePrefix = $segments[0]
        $idPart = ($segments[1..($segments.Length - 1)] -join "_")
        return "{0}_{1}_placeholder_v01.png" -f $typePrefix, $idPart
    }

    return "{0}_placeholder_v01.png" -f $Id
}

function Get-RelativePathSafe {
    param(
        [string]$BasePath,
        [string]$TargetPath
    )

    $base = (Resolve-Path $BasePath).Path
    $target = (Resolve-Path $TargetPath).Path

    if (-not $base.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $base = $base + [System.IO.Path]::DirectorySeparatorChar
    }

    $baseUri = New-Object System.Uri($base)
    $targetUri = New-Object System.Uri($target)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', '\')
}

function New-PlaceholderPng {
    param(
        [string]$Path,
        [string]$Id,
        [int]$ImageWidth,
        [int]$ImageHeight
    )

    $bitmap = New-Object System.Drawing.Bitmap($ImageWidth, $ImageHeight)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $titleSize = [Math]::Max(16, [int]($ImageHeight * 0.055))
    $bodySize = [Math]::Max(12, [int]($ImageHeight * 0.04))
    $fontTitle = New-Object System.Drawing.Font("Segoe UI", $titleSize, [System.Drawing.FontStyle]::Bold)
    $fontBody = New-Object System.Drawing.Font("Segoe UI", $bodySize, [System.Drawing.FontStyle]::Regular)
    $brush = [System.Drawing.Brushes]::Black
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::Black, 3)

    try {
        $graphics.Clear([System.Drawing.Color]::White)
        $graphics.DrawRectangle($pen, 2, 2, $ImageWidth - 4, $ImageHeight - 4)

        $name = [System.IO.Path]::GetFileNameWithoutExtension($Path)
        $textLines = @(
            "NAME: $name",
            "DIMENSIONS: ${ImageWidth}x${ImageHeight}",
            "ID: $Id"
        )

        $y = [int]($ImageHeight * 0.28)
        $lineSpacing = [int]($bodySize * 1.8)

        for ($i = 0; $i -lt $textLines.Count; $i++) {
            $line = $textLines[$i]
            $font = if ($i -eq 0) { $fontTitle } else { $fontBody }
            $lineSize = $graphics.MeasureString($line, $font)
            $x = [int](($ImageWidth - $lineSize.Width) / 2)
            $graphics.DrawString($line, $font, $brush, $x, $y + ($i * $lineSpacing))
        }

        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $pen.Dispose()
        $fontTitle.Dispose()
        $fontBody.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$iconOutputRoot = Join-Path $OutputRoot "Icons"
$sceneOutputRoot = Join-Path $OutputRoot "Scenes"

$iconIdSet = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($id in (Get-KnownIds)) {
    [void]$iconIdSet.Add($id)
}
foreach ($id in (Get-ReferencedPngIds)) {
    [void]$iconIdSet.Add($id)
}
$iconIds = @($iconIdSet | Sort-Object)
$sceneIds = Get-SceneIds

if ($iconIds.Count -eq 0 -and $sceneIds.Count -eq 0) {
    throw "No placeholder targets were discovered."
}

$manifestEntries = New-Object System.Collections.Generic.List[object]
$iconCount = 0
$sceneCount = 0

foreach ($id in $iconIds) {
    if ([string]::IsNullOrWhiteSpace($id)) {
        continue
    }

    $category = Get-IconCategory -Id $id
    $fileName = Build-FileName -Id $id
    $outputDir = Join-Path $iconOutputRoot $category
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    $filePath = Join-Path $outputDir $fileName

    New-PlaceholderPng -Path $filePath -Id $id -ImageWidth $IconWidth -ImageHeight $IconHeight
    $iconCount++

    $relativePath = (Get-RelativePathSafe -BasePath (Get-Location).Path -TargetPath $filePath).Replace('\', '/')
    $manifestEntries.Add([ordered]@{
            id = $id
            category = $category
            type = "icon"
            name = [System.IO.Path]::GetFileNameWithoutExtension($filePath)
            dimensions = "${IconWidth}x${IconHeight}"
            path = $relativePath
        })
}

foreach ($sceneId in $sceneIds) {
    $sceneName = $sceneId.Substring("scene_".Length)
    $fileName = "{0}_placeholder_v01.png" -f $sceneName
    New-Item -ItemType Directory -Path $sceneOutputRoot -Force | Out-Null
    $filePath = Join-Path $sceneOutputRoot $fileName

    New-PlaceholderPng -Path $filePath -Id $sceneId -ImageWidth $SceneWidth -ImageHeight $SceneHeight
    $sceneCount++

    $relativePath = (Get-RelativePathSafe -BasePath (Get-Location).Path -TargetPath $filePath).Replace('\', '/')
    $manifestEntries.Add([ordered]@{
            id = $sceneId
            category = "Scenes"
            type = "scene"
            name = [System.IO.Path]::GetFileNameWithoutExtension($filePath)
            dimensions = "${SceneWidth}x${SceneHeight}"
            path = $relativePath
        })
}

$manifestDir = Split-Path -Parent $ManifestPath
if (-not [string]::IsNullOrWhiteSpace($manifestDir)) {
    New-Item -ItemType Directory -Path $manifestDir -Force | Out-Null
}

$manifestPayload = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString("o")
    background = "white"
    text = @("name", "dimensions", "id")
    iconCount = $iconCount
    sceneCount = $sceneCount
    count = $manifestEntries.Count
    entries = $manifestEntries
}

$manifestPayload | ConvertTo-Json -Depth 6 | Set-Content -Path $ManifestPath

Write-Host ("Generated {0} placeholder PNG(s): {1} icon(s), {2} scene placeholder(s)." -f $manifestEntries.Count, $iconCount, $sceneCount)
