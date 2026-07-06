# Windows orchestrator: run generate_pmtiles.sh inside WSL.
param(
    [Parameter(Mandatory)]
    [ValidateSet("ets2", "ats")]
    [string]$Game,

    [Parameter(Mandatory)]
    [string]$GamePath,

    [Parameter(Mandatory)]
    [string]$HtmlRoot,

    [string]$LogFile = "",
    [string]$Distro = "",
    [switch]$Activate
)

$ErrorActionPreference = "Continue"

function Write-Log([string]$Message) {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $Message"
    Write-Host $line
    if ($LogFile) { Add-Content -Path $LogFile -Value $line -Encoding UTF8 }
}

function ConvertTo-WslPath([string]$WinPath) {
    $full = $WinPath.Trim().TrimEnd('\')
    if (Test-Path $full) { $full = (Resolve-Path $full).Path }
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

if (-not $Distro) {
    . (Join-Path $PSScriptRoot "wsl\resolve_distro.ps1")
    $Distro = Resolve-WslDistro
}

if (-not $Distro) {
    Write-Log "ERROR: No WSL distro found."
    exit 1
}

$scriptWin = Join-Path $PSScriptRoot "wsl\generate_pmtiles.sh"
if (-not (Test-Path $scriptWin)) {
    Write-Log "ERROR: Script not found: $scriptWin"
    exit 1
}

$gameWsl = ConvertTo-WslPath $GamePath
$htmlWsl = ConvertTo-WslPath $HtmlRoot

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
$buildGraphWin = Join-Path $wslDirWin "build-graph.js"
if (Test-Path -LiteralPath $buildGraphWin) {
    $srcWsl = ConvertTo-WslPath $buildGraphWin
    $dest = "$remoteDir/build-graph.js"
    $null = & wsl -d $Distro -e bash -lc "sed 's/\r`$//' '$srcWsl' > '$dest'"
    if ($LASTEXITCODE -ne 0) {
        Write-Log "ERROR: Could not prepare build-graph.js in WSL."
        exit 1
    }
}

$bashArgs = @(
    "$remoteDir/generate_pmtiles.sh",
    "--game", $Game,
    "--game-path", $gameWsl,
    "--html-root", $htmlWsl
)
if ($LogFile) {
    $bashArgs += @("--log-file", (ConvertTo-WslPath $LogFile))
}
if ($Activate) { $bashArgs += "--activate" }

Write-Log "Using WSL distro: $Distro"
Write-Log "Game (WSL): $gameWsl"
Write-Log "Html root (WSL): $htmlWsl"

& wsl -d $Distro -e bash @bashArgs 2>&1 | ForEach-Object { Write-Log $_ }

if ($LASTEXITCODE -ne 0) {
    Write-Log "ERROR: WSL generate exited with code $LASTEXITCODE"
    exit $LASTEXITCODE
}

exit 0
