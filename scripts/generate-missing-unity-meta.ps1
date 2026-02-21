param(
    [string]$RootPath = "Assets"
)

$ErrorActionPreference = "Stop"

function New-UnityGuid {
    param([System.Collections.Generic.HashSet[string]]$UsedGuids)

    while ($true) {
        $value = [guid]::NewGuid().ToString("N")
        if (-not $UsedGuids.Contains($value)) {
            [void]$UsedGuids.Add($value)
            return $value
        }
    }
}

function Build-FolderMeta {
    param([string]$Guid)

    return @"
fileFormatVersion: 2
guid: $Guid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
}

function Build-FileMeta {
    param([string]$Guid)

    return @"
fileFormatVersion: 2
guid: $Guid
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
}

if (-not (Test-Path -LiteralPath $RootPath)) {
    throw "Root path '$RootPath' was not found."
}

$usedGuids = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)
Get-ChildItem -Path Assets -Recurse -File -Filter *.meta -ErrorAction SilentlyContinue | ForEach-Object {
    $line = Get-Content -Path $_.FullName | Select-Object -First 2 | Where-Object { $_ -like "guid:*" }
    if (-not [string]::IsNullOrWhiteSpace($line)) {
        $guid = $line.Substring(5).Trim()
        if (-not [string]::IsNullOrWhiteSpace($guid)) {
            [void]$usedGuids.Add($guid)
        }
    }
}

$created = 0
$items = New-Object System.Collections.Generic.List[object]

Get-Item -LiteralPath $RootPath | ForEach-Object { $items.Add($_) }
Get-ChildItem -Path $RootPath -Recurse -Force | ForEach-Object { $items.Add($_) }

$ordered = $items |
    Where-Object { $_.Extension -ne ".meta" } |
    Sort-Object FullName

foreach ($item in $ordered) {
    $metaPath = "$($item.FullName).meta"
    if (Test-Path -LiteralPath $metaPath) {
        continue
    }

    $guid = New-UnityGuid -UsedGuids $usedGuids
    $content = if ($item.PSIsContainer) {
        Build-FolderMeta -Guid $guid
    }
    else {
        Build-FileMeta -Guid $guid
    }

    Set-Content -Path $metaPath -Value $content -Encoding ascii
    $created++
}

Write-Host ("Created {0} missing .meta file(s) under '{1}'." -f $created, $RootPath)
