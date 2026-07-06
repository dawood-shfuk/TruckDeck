# Semi-automated WSL2 + Ubuntu install to a user-chosen drive (elevated).
param(
    [Parameter(Mandatory)]
    [string]$InstallPath,

    [string]$DistroName = "TruckDeckUbuntu",
    [string]$LogFile = ""
)

$ErrorActionPreference = "Continue"

function Write-Log([string]$Message) {
    if ([string]::IsNullOrWhiteSpace($Message)) { return }
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $Message"
    Write-Host $line
    if ($LogFile) { Add-Content -Path $LogFile -Value $line -Encoding UTF8 }
}

function Invoke-WslLogged([string[]]$WslArgs) {
    $output = & wsl @WslArgs 2>&1
    $code = $LASTEXITCODE
    foreach ($line in @($output)) {
        if ($null -ne $line -and "$line".Trim()) { Write-Log "$line" }
    }
    return $code
}

function Write-ProgressLine([int]$Percent, [string]$Message) {
    $progress = "TRUCKDECK_PROGRESS:$Percent $Message"
    Write-Host $progress
    if ($LogFile) {
        Add-Content -Path $LogFile -Value "[$(Get-Date -Format 'HH:mm:ss')] $progress" -Encoding UTF8
    }
}

# Require admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Log "ERROR: This script must run as Administrator."
    exit 1
}

$InstallPath = $InstallPath.Trim().TrimEnd('\')
$distroDir = Join-Path $InstallPath $DistroName
$tarPath = Join-Path $env:TEMP "ubuntu-24.04-wsl-rootfs.tar.gz"
$rootfsUrl = "https://cloud-images.ubuntu.com/wsl/releases/24.04/current/ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz"

Write-ProgressLine 2 "Preparing WSL install at $distroDir..."

if (-not (Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
}

Write-ProgressLine 10 "Enabling Windows features (WSL + Virtual Machine Platform)..."
$dism = @(
    "/online", "/enable-feature", "/featurename:Microsoft-Windows-Subsystem-Linux",
    "/all", "/norestart"
)
$null = Start-Process -FilePath "dism.exe" -ArgumentList $dism -Wait -PassThru -NoNewWindow

$dism2 = @(
    "/online", "/enable-feature", "/featurename:VirtualMachinePlatform",
    "/all", "/norestart"
)
$null = Start-Process -FilePath "dism.exe" -ArgumentList $dism2 -Wait -PassThru -NoNewWindow

Write-ProgressLine 25 "Setting WSL 2 as default..."
$null = Invoke-WslLogged @("--set-default-version", "2")

$existing = (wsl -l -v 2>&1 | Out-String)
if ($existing -match "(?m)^\s*\*?\s*$([regex]::Escape($DistroName))\s") {
    Write-Log "Distro $DistroName already registered."
} else {
    if (Test-Path $distroDir) {
        Write-Log "Unregistering stale distro (if any)..."
        $null = Invoke-WslLogged @("--unregister", $DistroName)
        if (Test-Path $distroDir) {
            Write-Log "Removing incomplete distro folder..."
            Remove-Item -Recurse -Force $distroDir -ErrorAction SilentlyContinue
        }
    }

    Write-ProgressLine 35 "Downloading Ubuntu 24.04 rootfs (~300 MB)..."
    if (-not (Test-Path $tarPath)) {
        try {
            Invoke-WebRequest -Uri $rootfsUrl -OutFile $tarPath -UseBasicParsing
        } catch {
            Write-Log "ERROR: Download failed - $_"
            exit 1
        }
    }

    Write-ProgressLine 60 "Importing $DistroName to $distroDir..."
    $importCode = Invoke-WslLogged @("--import", $DistroName, $distroDir, $tarPath, "--version", "2")
    if ($importCode -ne 0) {
        Write-Log "ERROR: wsl --import failed with code $importCode"
        exit $importCode
    }
}

Write-ProgressLine 80 "Setting default distro..."
$null = Invoke-WslLogged @("--set-default", $DistroName)

Write-ProgressLine 90 "First boot..."
$null = Invoke-WslLogged @("-d", $DistroName, "-e", "bash", "-lc", "echo TruckDeck WSL ready")

Write-ProgressLine 100 "WSL installed."
Write-Log "TRUCKDECK_DONE: $distroDir"
Write-Log "NOTE: A system reboot may be required before WSL works fully."

exit 0
