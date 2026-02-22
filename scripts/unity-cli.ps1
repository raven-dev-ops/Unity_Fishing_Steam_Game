param(
    [ValidateSet('custom', 'build', 'validate', 'rebuild-sheets', 'test-edit', 'test-play')]
    [string]$Task = 'custom',
    [ValidateSet('Dev', 'QA', 'Release')]
    [string]$BuildProfile = 'QA',
    [string]$Method = '',
    [string]$UnityPath = '',
    [string]$ProjectPath = '',
    [string]$LogFile = 'unity_cli.log',
    [string[]]$ExtraArgs,
    [switch]$AllowVersionMismatch,
    [switch]$KeepGeneratedProjectFiles
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

function Resolve-UnityExeFromHint {
    param([string]$PathHint)

    if ([string]::IsNullOrWhiteSpace($PathHint)) {
        return ''
    }

    $hint = [Environment]::ExpandEnvironmentVariables($PathHint.Trim().Trim('"'))
    $hint = $hint -replace '/', '\'

    $candidates = New-Object System.Collections.Generic.List[string]
    $candidates.Add($hint)

    if (Test-Path -LiteralPath $hint -PathType Container) {
        $candidates.Add((Join-Path $hint 'Unity.exe'))
        $candidates.Add((Join-Path $hint 'Editor\Unity.exe'))
    }

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            continue
        }

        if ([string]::Equals([System.IO.Path]::GetFileName($candidate), 'Unity.exe', [System.StringComparison]::OrdinalIgnoreCase)) {
            return (Resolve-Path $candidate).Path
        }
    }

    return ''
}

function Get-UnityHubEditors {
    $hubEditorsPath = Join-Path $env:AppData 'UnityHub\editors-v2.json'
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
        return ''
    }

    $normalizedTarget = ''
    try {
        $normalizedTarget = (Resolve-Path -LiteralPath $UnityExePath).Path
    }
    catch {
        return ''
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

    return ''
}

function Get-VersionTokenFromPath {
    param([string]$PathValue)

    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return ''
    }

    if ($PathValue -match '(?<version>\d+\.\d+\.\d+f\d+)') {
        return $matches['version']
    }

    return ''
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

    return ''
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
            Write-Warning "Unity CLI: unable to verify editor version for '$UnityExePath'. Continuing because -AllowVersionMismatch is set."
            return
        }

        throw "Unity CLI: unable to verify editor version for '$UnityExePath'. Expected project version '$ExpectedVersion'. Use a Unity Hub-managed editor path/version, or pass -AllowVersionMismatch."
    }

    if (-not [string]::Equals($actualVersion, $ExpectedVersion, [System.StringComparison]::OrdinalIgnoreCase)) {
        if ($AllowMismatch) {
            Write-Warning "Unity CLI: using Unity '$actualVersion' while project expects '$ExpectedVersion'. Continuing because -AllowVersionMismatch is set."
            return
        }

        throw "Unity CLI: Unity version mismatch. Expected '$ExpectedVersion', resolved '$actualVersion' at '$UnityExePath'. Install/use the pinned editor version or pass -AllowVersionMismatch."
    }
}

function Get-UnityHubSecondaryInstallPath {
    $secondaryPathFile = Join-Path $env:AppData 'UnityHub\secondaryInstallPath.json'
    if (-not (Test-Path -LiteralPath $secondaryPathFile -PathType Leaf)) {
        return ''
    }

    try {
        $secondaryPath = Get-Content -Raw -Path $secondaryPathFile | ConvertFrom-Json
        if ($secondaryPath -is [string]) {
            return $secondaryPath
        }
    }
    catch {
    }

    return ''
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

        Add-Candidate -Hint (Join-Path $hubRoot 'Unity.exe') -Preferred $false
        Add-Candidate -Hint (Join-Path $hubRoot 'Editor\Unity.exe') -Preferred $false

        $installedVersionDirs = Get-ChildItem -Path $hubRoot -Directory -ErrorAction SilentlyContinue
        foreach ($versionDir in $installedVersionDirs) {
            Add-Candidate -Hint (Join-Path $versionDir.FullName 'Editor\Unity.exe') -Preferred $false
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
        [string]::Join(', ', @($hubEditorVersions | Sort-Object))
    }
    else {
        'none'
    }

    throw "Unity Editor executable not found. Project version: '$projectVersion'. Unity Hub detected versions: $detectedVersions. Set UNITY_EDITOR_PATH to Unity.exe or pass -UnityPath."
}

function Get-GitPathStatus {
    param(
        [string]$Root,
        [string]$RelativePath
    )

    try {
        $status = & git -C $Root status --porcelain -- $RelativePath 2>$null
        if ($LASTEXITCODE -ne 0) {
            return $null
        }

        return (($status -join "`n").Trim())
    }
    catch {
        return $null
    }
}

function New-GeneratedFileCleanupPlan {
    param(
        [string]$Root,
        [bool]$Enabled
    )

    $plan = [ordered]@{
        trackedBackups = New-Object System.Collections.Generic.List[object]
        deleteIfCreated = New-Object System.Collections.Generic.List[string]
    }

    if (-not $Enabled) {
        return [pscustomobject]$plan
    }

    $trackedFiles = @(
        'Packages/packages-lock.json',
        'ProjectSettings/ProjectSettings.asset'
    )

    foreach ($relativePath in $trackedFiles) {
        $absolutePath = Join-Path $Root $relativePath
        if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
            continue
        }

        $status = Get-GitPathStatus -Root $Root -RelativePath $relativePath
        if ($status -ne $null -and -not [string]::IsNullOrWhiteSpace($status)) {
            continue
        }

        $backupPath = Join-Path ([System.IO.Path]::GetTempPath()) ("unitycli_backup_{0}_{1}.tmp" -f ([System.IO.Path]::GetFileName($relativePath)), [Guid]::NewGuid().ToString('N'))
        Copy-Item -LiteralPath $absolutePath -Destination $backupPath -Force
        $plan.trackedBackups.Add([pscustomobject]@{
                RelativePath = $relativePath
                AbsolutePath = $absolutePath
                BackupPath   = $backupPath
            })
    }

    $deleteIfMissingBeforeRun = @(
        'ProjectSettings/SceneTemplateSettings.json'
    )

    foreach ($relativePath in $deleteIfMissingBeforeRun) {
        $absolutePath = Join-Path $Root $relativePath
        if (-not (Test-Path -LiteralPath $absolutePath)) {
            $plan.deleteIfCreated.Add($absolutePath)
        }
    }

    return [pscustomobject]$plan
}

function Invoke-GeneratedFileCleanup {
    param([object]$Plan)

    if ($null -eq $Plan) {
        return
    }

    if ($Plan.trackedBackups -ne $null) {
        foreach ($entry in $Plan.trackedBackups) {
            $backupPath = [string]$entry.BackupPath
            $absolutePath = [string]$entry.AbsolutePath
            $relativePath = [string]$entry.RelativePath

            try {
                if ((Test-Path -LiteralPath $backupPath -PathType Leaf) -and (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
                    $backupHash = (Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash
                    $currentHash = (Get-FileHash -LiteralPath $absolutePath -Algorithm SHA256).Hash
                    if (-not [string]::Equals($backupHash, $currentHash, [System.StringComparison]::OrdinalIgnoreCase)) {
                        Copy-Item -LiteralPath $backupPath -Destination $absolutePath -Force
                        Write-Host "Unity CLI: restored generated file '$relativePath' to pre-run state."
                    }
                }
                elseif ((Test-Path -LiteralPath $backupPath -PathType Leaf) -and -not (Test-Path -LiteralPath $absolutePath)) {
                    Copy-Item -LiteralPath $backupPath -Destination $absolutePath -Force
                    Write-Host "Unity CLI: restored missing file '$relativePath' to pre-run state."
                }
            }
            catch {
                Write-Warning "Unity CLI: failed to restore generated file '$relativePath' ($($_.Exception.Message))."
            }
            finally {
                if (Test-Path -LiteralPath $backupPath -PathType Leaf) {
                    Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue
                }
            }
        }
    }

    if ($Plan.deleteIfCreated -ne $null) {
        foreach ($absolutePath in $Plan.deleteIfCreated) {
            if (Test-Path -LiteralPath $absolutePath) {
                try {
                    Remove-Item -LiteralPath $absolutePath -Force
                    Write-Host "Unity CLI: removed generated file '$absolutePath'."
                }
                catch {
                    Write-Warning "Unity CLI: failed to remove generated file '$absolutePath' ($($_.Exception.Message))."
                }
            }
        }
    }
}

function Test-UnityEditorProcessRunning {
    try {
        $processes = Get-Process -Name 'Unity' -ErrorAction SilentlyContinue
        return $processes -ne $null -and $processes.Count -gt 0
    }
    catch {
        return $false
    }
}

function Remove-StaleProjectLockFiles {
    param([string]$Root)

    if (Test-UnityEditorProcessRunning) {
        Write-Warning 'Unity CLI: Unity editor process detected; skipping stale lock cleanup.'
        return
    }

    $lockFiles = @(
        (Join-Path $Root 'Library/ArtifactDB-lock'),
        (Join-Path $Root 'Library/SourceAssetDB-lock'),
        (Join-Path $Root 'Temp/UnityLockfile')
    )

    foreach ($lockFile in $lockFiles) {
        if (-not (Test-Path -LiteralPath $lockFile -PathType Leaf)) {
            continue
        }

        try {
            Remove-Item -LiteralPath $lockFile -Force
            Write-Host "Unity CLI: removed stale lock '$lockFile'."
        }
        catch {
            Write-Warning "Unity CLI: failed to remove lock '$lockFile' ($($_.Exception.Message))."
        }
    }
}

function Resolve-TestResultsPathFromArguments {
    param(
        [string]$Root,
        [System.Collections.Generic.List[string]]$Arguments
    )

    for ($index = 0; $index -lt $Arguments.Count; $index++) {
        $value = $Arguments[$index]
        if ([string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        if ([string]::Equals($value, '-testResults', [System.StringComparison]::OrdinalIgnoreCase) -or
            [string]::Equals($value, '/testResults', [System.StringComparison]::OrdinalIgnoreCase)) {
            if (($index + 1) -lt $Arguments.Count) {
                $rawPath = $Arguments[$index + 1]
                if ([System.IO.Path]::IsPathRooted($rawPath)) {
                    return $rawPath
                }

                return (Join-Path $Root $rawPath)
            }
        }

        if ($value.StartsWith('-testResults=', [System.StringComparison]::OrdinalIgnoreCase) -or
            $value.StartsWith('/testResults=', [System.StringComparison]::OrdinalIgnoreCase)) {
            $parts = $value.Split('=', 2)
            if ($parts.Length -eq 2 -and -not [string]::IsNullOrWhiteSpace($parts[1])) {
                if ([System.IO.Path]::IsPathRooted($parts[1])) {
                    return $parts[1]
                }

                return (Join-Path $Root $parts[1])
            }
        }
    }

    return $null
}

function Get-FileStateSnapshot {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [pscustomobject]@{
            Exists         = $false
            LastWriteUtc   = [DateTime]::MinValue
            LengthInBytes  = 0
        }
    }

    $item = Get-Item -LiteralPath $Path
    return [pscustomobject]@{
        Exists         = $true
        LastWriteUtc   = $item.LastWriteTimeUtc
        LengthInBytes  = $item.Length
    }
}

function Assert-TestResultsWereUpdated {
    param(
        [string]$TestResultsPath,
        [object]$BeforeSnapshot
    )

    if ([string]::IsNullOrWhiteSpace($TestResultsPath)) {
        throw 'Unity CLI: unable to resolve test results path for test task.'
    }

    if (-not (Test-Path -LiteralPath $TestResultsPath -PathType Leaf)) {
        throw "Unity CLI: test results file not found at '$TestResultsPath'."
    }

    $afterSnapshot = Get-FileStateSnapshot -Path $TestResultsPath
    $wasUpdated = -not $BeforeSnapshot.Exists -or
        $afterSnapshot.LastWriteUtc -gt $BeforeSnapshot.LastWriteUtc -or
        $afterSnapshot.LengthInBytes -ne $BeforeSnapshot.LengthInBytes
    if (-not $wasUpdated) {
        throw "Unity CLI: test results were not updated at '$TestResultsPath'. Unity likely failed before tests executed (for example stale project lock files)."
    }
}

function Get-TestRunSummary {
    param([string]$TestResultsPath)

    try {
        [xml]$xml = Get-Content -LiteralPath $TestResultsPath -Raw
        $run = $xml.'test-run'
        return [pscustomobject]@{
            Result = [string]$run.result
            Total = [int]$run.total
            Passed = [int]$run.passed
            Failed = [int]$run.failed
            Skipped = [int]$run.skipped
            Inconclusive = [int]$run.inconclusive
        }
    }
    catch {
        throw "Unity CLI: failed to parse test results from '$TestResultsPath' ($($_.Exception.Message))."
    }
}

function Test-ArgumentPresent {
    param(
        [System.Collections.Generic.List[string]]$Arguments,
        [string[]]$ArgumentNames
    )

    if ($null -eq $Arguments -or $null -eq $ArgumentNames) {
        return $false
    }

    foreach ($name in $ArgumentNames) {
        if ([string]::IsNullOrWhiteSpace($name)) {
            continue
        }

        for ($index = 0; $index -lt $Arguments.Count; $index++) {
            $value = $Arguments[$index]
            if ([string]::IsNullOrWhiteSpace($value)) {
                continue
            }

            if ([string]::Equals($value, $name, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }

            if ($value.StartsWith("$name=", [System.StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }
        }
    }

    return $false
}

function Convert-ToProcessArgumentString {
    param([string]$Value)

    if ($null -eq $Value) {
        return '""'
    }

    $escaped = $Value.Replace('"', '\"')
    if ($escaped.Length -eq 0 -or $escaped -match '\s') {
        return '"' + $escaped + '"'
    }

    return $escaped
}

function Get-DefaultTestResultsPath {
    param(
        [string]$Root,
        [string]$TaskName
    )

    $outputDir = Join-Path $Root 'Artifacts\TestResults'
    if (-not (Test-Path -LiteralPath $outputDir -PathType Container)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    $fileName = if ($TaskName -eq 'test-play') { 'playmode-results.xml' } else { 'editmode-results.xml' }
    return (Join-Path $outputDir $fileName)
}

$root = Get-ProjectRoot
$projectVersion = Get-ProjectVersion -Root $root
$hubEditors = Get-UnityHubEditors
$unityExe = Resolve-UnityEditorPath -Root $root
Assert-UnityEditorVersion -UnityExePath $unityExe -ExpectedVersion $projectVersion -HubEditors $hubEditors -AllowMismatch $AllowVersionMismatch.IsPresent

$args = New-Object System.Collections.Generic.List[string]
$args.Add('-batchmode')
$args.Add('-nographics')
$isTestTask = $Task -eq 'test-edit' -or $Task -eq 'test-play'
if (-not $isTestTask) {
    $args.Add('-quit')
}
$args.Add('-projectPath')
$args.Add($root)
$args.Add('-logFile')
$args.Add($LogFile)

switch ($Task) {
    'build' {
        $args.Add('-executeMethod')
        $args.Add('RavenDevOps.Fishing.EditorTools.BuildCommandLine.BuildWindowsBatchMode')
        $args.Add("-buildProfile=$BuildProfile")
    }
    'validate' {
        $args.Add('-executeMethod')
        $args.Add('RavenDevOps.Fishing.EditorTools.ContentValidatorRunner.ValidateCatalogBatchMode')
    }
    'rebuild-sheets' {
        $args.Add('-executeMethod')
        $args.Add('RavenDevOps.Fishing.EditorTools.SpriteSheetAtlasWorkflow.RebuildSheetsAndAtlasesBatchMode')
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

if ($isTestTask -and -not (Test-ArgumentPresent -Arguments $args -ArgumentNames @('-testResults', '/testResults'))) {
    $defaultTestResults = Get-DefaultTestResultsPath -Root $root -TaskName $Task
    $args.Add('-testResults')
    $args.Add($defaultTestResults)
}

$testResultsPath = $null
$testResultsBeforeSnapshot = $null
if ($isTestTask) {
    $testResultsPath = Resolve-TestResultsPathFromArguments -Root $root -Arguments $args
    $testResultsBeforeSnapshot = Get-FileStateSnapshot -Path $testResultsPath
}

Write-Host "Unity CLI: $unityExe"
Write-Host "Project: $root"
Write-Host "Task: $Task"
if (-not [string]::IsNullOrWhiteSpace($projectVersion)) {
    Write-Host "EditorVersion: $projectVersion"
}

$cleanupEnabled = -not $KeepGeneratedProjectFiles.IsPresent
$cleanupPlan = New-GeneratedFileCleanupPlan -Root $root -Enabled $cleanupEnabled
if ($cleanupEnabled) {
    Write-Host 'GeneratedFileCleanup: enabled'
}
else {
    Write-Host 'GeneratedFileCleanup: disabled'
}

Remove-StaleProjectLockFiles -Root $root

$unityExitCode = 0
$argumentString = (($args | ForEach-Object { Convert-ToProcessArgumentString -Value $_ }) -join ' ')

try {
    $unityProcess = Start-Process -FilePath $unityExe -ArgumentList $argumentString -PassThru -Wait
    if ($null -eq $unityProcess) {
        throw 'Unity CLI: failed to launch Unity process.'
    }

    $unityExitCode = $unityProcess.ExitCode
}
finally {
    if ($cleanupEnabled) {
        Invoke-GeneratedFileCleanup -Plan $cleanupPlan
    }
}

$exitCode = $unityExitCode

if ($isTestTask) {
    Assert-TestResultsWereUpdated -TestResultsPath $testResultsPath -BeforeSnapshot $testResultsBeforeSnapshot
    $testSummary = Get-TestRunSummary -TestResultsPath $testResultsPath
    Write-Host ("Unity CLI: test summary => total={0}, passed={1}, failed={2}, skipped={3}, inconclusive={4}, result={5}" -f `
            $testSummary.Total, $testSummary.Passed, $testSummary.Failed, $testSummary.Skipped, $testSummary.Inconclusive, $testSummary.Result)
    if ($testSummary.Failed -gt 0 -and $exitCode -eq 0) {
        $exitCode = 1
    }
}

exit $exitCode
