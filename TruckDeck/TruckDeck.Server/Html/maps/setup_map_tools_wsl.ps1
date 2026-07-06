# Windows orchestrator: run setup_map_tools.sh inside WSL.
param(
    [string]$LogFile = "",
    [string]$Distro = ""
)

$ErrorActionPreference = "Continue"
. (Join-Path $PSScriptRoot "wsl\resolve_distro.ps1")

function Write-Log([string]$Message) {
    if ([string]::IsNullOrWhiteSpace($Message)) { return }
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $Message"
    Write-Host $line
    if ($LogFile) { Add-Content -Path $LogFile -Value $line -Encoding UTF8 }
}

function Write-ProgressLine([int]$Percent, [string]$Message) {
    $progress = "TRUCKDECK_PROGRESS:$Percent $Message"
    Write-Host $progress
    if ($LogFile) {
        Add-Content -Path $LogFile -Value "[$(Get-Date -Format 'HH:mm:ss')] $progress" -Encoding UTF8
    }
}

function ConvertTo-WslPath([string]$WinPath) {
    $full = $WinPath.Trim().TrimEnd('\')
    if (Test-Path -LiteralPath $full) { $full = (Resolve-Path -LiteralPath $full).Path }
    if ($full -match '^([A-Za-z]):\\(.*)$') {
        $drive = $Matches[1].ToLower()
        $rest = $Matches[2] -replace '\\', '/'
        if ($rest) { return "/mnt/$drive/$rest" }
        return "/mnt/$drive"
    }
    if ($full -match '^([A-Za-z]):$') {
        return "/mnt/$($Matches[1].ToLower())"
    }
    throw "Cannot convert path to WSL: $WinPath"
}

$Distro = Resolve-WslDistro -Requested $Distro
if (-not $Distro) {
    Write-Log "ERROR: No WSL distro found. Install WSL first."
    exit 1
}

Write-Log "Using WSL distro: $Distro"
Write-ProgressLine 5 "Using WSL distro $Distro..."

$scriptWin = Join-Path $PSScriptRoot "wsl\setup_map_tools.sh"
if (-not (Test-Path -LiteralPath $scriptWin)) {
    Write-Log "ERROR: Script not found: $scriptWin"
    exit 1
}

$scriptWsl = ConvertTo-WslPath $scriptWin
$logWsl = ""
if ($LogFile) { $logWsl = ConvertTo-WslPath $LogFile }

# Copy all bash helpers to a native WSL path (DrvFs CRLF breaks sourcing common.sh).
$remoteDir = "/tmp/truckdeck-wsl"
$wslDirWin = Join-Path $PSScriptRoot "wsl"
$null = & wsl -d $Distro -e bash -lc "mkdir -p '$remoteDir'"
if ($LASTEXITCODE -ne 0) {
    Write-Log "ERROR: Could not create WSL script directory."
    exit 1
}
foreach ($sh in Get-ChildItem -LiteralPath $wslDirWin -Filter *.sh) {
    $srcWsl = ConvertTo-WslPath $sh.FullName
    $dest = "$remoteDir/$($sh.Name)"
    $null = & wsl -d $Distro -e bash -lc "sed 's/\r`$//' '$srcWsl' > '$dest' && chmod +x '$dest'"
    if ($LASTEXITCODE -ne 0) {
        Write-Log "ERROR: Could not prepare $($sh.Name) in WSL."
        exit 1
    }
}

$remoteScript = "$remoteDir/setup_map_tools.sh"

$runArgs = @($remoteScript)
if ($logWsl) { $runArgs += @("--log-file", $logWsl) }

Write-Log "Running setup_map_tools.sh in WSL..."
Write-ProgressLine 10 "Installing packages in WSL (may take several minutes)..."

$hadError = $false
& wsl -d $Distro -e bash @runArgs 2>&1 | ForEach-Object {
    $line = "$_"
    if ($line -match 'TRUCKDECK_PROGRESS:(\d+)\s+(.*)') {
        Write-Host "TRUCKDECK_PROGRESS:$($Matches[1]) $($Matches[2])"
        if ($LogFile) {
            Add-Content -Path $LogFile -Value "[$(Get-Date -Format 'HH:mm:ss')] TRUCKDECK_PROGRESS:$($Matches[1]) $($Matches[2])" -Encoding UTF8
        }
    } else {
        Write-Log $line
    }
    if ($line -match 'ERROR:') { $script:hadError = $true }
}

$exitCode = $LASTEXITCODE
if ($exitCode -ne 0 -or $hadError) {
    Write-Log "ERROR: WSL setup failed (exit $exitCode)."
    exit $(if ($exitCode -ne 0) { $exitCode } else { 1 })
}

Write-ProgressLine 100 "Map tools installed."
Write-Log "TRUCKDECK_DONE: map-tools"
exit 0
