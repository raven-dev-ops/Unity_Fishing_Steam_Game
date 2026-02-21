param(
    [string]$Tier = "reference",
    [string]$OutputPath = "Artifacts/Hardware/hardware_fingerprint.json",
    [string]$CaptureId = ""
)

$ErrorActionPreference = "Stop"

function Ensure-ParentDirectory {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
}

function Get-WindowsHardwareFingerprint {
    $cpu = Get-CimInstance Win32_Processor | Select-Object -First 1 Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed
    $gpu = Get-CimInstance Win32_VideoController | Select-Object -First 1 Name, DriverVersion
    $computer = Get-CimInstance Win32_ComputerSystem | Select-Object -First 1 TotalPhysicalMemory
    $os = Get-CimInstance Win32_OperatingSystem | Select-Object -First 1 Caption, Version

    return [ordered]@{
        os = ("{0} {1}" -f $os.Caption, $os.Version).Trim()
        cpu = [ordered]@{
            name = [string]$cpu.Name
            cores = [int]$cpu.NumberOfCores
            logical_processors = [int]$cpu.NumberOfLogicalProcessors
            max_clock_mhz = [int]$cpu.MaxClockSpeed
        }
        gpu = [ordered]@{
            name = [string]$gpu.Name
            driver_version = [string]$gpu.DriverVersion
        }
        ram_gb = [Math]::Round(([double]$computer.TotalPhysicalMemory / 1GB), 2)
    }
}

function Get-PortableHardwareFingerprint {
    return [ordered]@{
        os = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        cpu = [ordered]@{
            name = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
            cores = [Environment]::ProcessorCount
            logical_processors = [Environment]::ProcessorCount
            max_clock_mhz = 0
        }
        gpu = [ordered]@{
            name = "unknown"
            driver_version = "unknown"
        }
        ram_gb = 0
    }
}

$capturedUtc = (Get-Date).ToUniversalTime()
if ([string]::IsNullOrWhiteSpace($CaptureId)) {
    $CaptureId = ("{0}-{1}" -f $Tier.ToLowerInvariant(), $capturedUtc.ToString("yyyyMMddTHHmmssZ"))
}

$fingerprint = $null
if ($IsWindows) {
    $fingerprint = Get-WindowsHardwareFingerprint
}
else {
    $fingerprint = Get-PortableHardwareFingerprint
}

$result = [ordered]@{
    capture_id = $CaptureId
    captured_utc = $capturedUtc.ToString("o")
    tier = $Tier.ToLowerInvariant()
    source = "hardware-fingerprint-script"
    machine = $fingerprint
}

Ensure-ParentDirectory -Path $OutputPath
($result | ConvertTo-Json -Depth 8) | Set-Content -Path $OutputPath

Write-Host ("Hardware fingerprint captured: {0}" -f $OutputPath)
Write-Host ("Capture ID: {0}" -f $CaptureId)
exit 0
