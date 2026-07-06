# Full TruckDeck build + deploy to local server output and Steam Telemetry Server mirror.
# TruckDeck\server\ is the canonical runtime; Steam path is kept in sync for live testing / server move.
param(
    [string]$SteamDir = "D:\SteamLibrary\steamapps\common\Euro Truck Simulator 2\Telemetry Server",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [switch]$SkipPlugins,
    [switch]$SkipBuild,
    [switch]$SkipSteam
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$server = Join-Path $root "server"
$sln = Join-Path $root "TruckDeck.sln"

function Find-MSBuild {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
        if ($msbuild) { return $msbuild }
    }
    foreach ($p in @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )) {
        if (Test-Path $p) { return $p }
    }
    throw "MSBuild not found. Install Visual Studio 2022 with .NET desktop development."
}

Write-Host "=== TruckDeck deploy ($Configuration) ===" -ForegroundColor Cyan
Write-Host "Source:  $root"
Write-Host "Output:  $server"
if (-not $SkipSteam) { Write-Host "Mirror:  $SteamDir" }

if (-not $SkipPlugins) {
    Write-Host "`n[1/4] Building telemetry plugins..." -ForegroundColor Yellow
    & (Join-Path $PSScriptRoot "build_plugins.ps1")
} else {
    Write-Host "`n[1/4] Skipping plugin build"
}

if (-not $SkipBuild) {
    Write-Host "`n[2/4] Building TruckDeck.sln..." -ForegroundColor Yellow
    $msbuild = Find-MSBuild
    Write-Host "Using MSBuild: $msbuild"

    $nuget = Get-Command nuget -ErrorAction SilentlyContinue
    if ($nuget) {
        & nuget restore $sln | Out-Host
    }

    & $msbuild $sln /p:Configuration=$Configuration /v:m /nologo
    if ($LASTEXITCODE -ne 0) {
        $builtExe = Join-Path $root "TruckDeck.Server\obj\$Configuration\TruckDeck.exe"
        if ((Test-Path $builtExe) -and (Get-Process TruckDeck -ErrorAction SilentlyContinue)) {
            Write-Warning "TruckDeck.exe is running - built output is in $builtExe (close TruckDeck and re-run deploy to update server exe)"
        } else {
            throw "MSBuild failed with exit $LASTEXITCODE"
        }
    }
} else {
    Write-Host "`n[2/4] Skipping solution build"
}

Write-Host "`n[3/4] Syncing Html from source..." -ForegroundColor Yellow
& (Join-Path $PSScriptRoot "sync_html.ps1") -SkipSteamMirror:$SkipSteam -SteamDir $SteamDir

$version = & (Join-Path $PSScriptRoot "Get-TruckDeckVersion.ps1") -Root $root
$builtAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss K"
$stamp = @"
TruckDeck $version
Configuration: $Configuration
Built: $builtAt
Source: $root
"@
Set-Content -Path (Join-Path $server "BUILD.txt") -Value $stamp -Encoding UTF8

if (-not $SkipSteam -and (Test-Path (Split-Path $SteamDir -Parent))) {
    Write-Host "`n[4/4] Deploying runtime to Steam Telemetry Server..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $SteamDir | Out-Null

    # Mirror exe, dlls, Plugins, Bridges, Resources. Html synced separately (preserves maps/generated, pmtiles).
    robocopy $server $SteamDir /MIR /XD Html .cursor `
        /XF *.log TruckDeck-crash.log Ets2Telemetry.log `
        /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy deploy failed with exit $LASTEXITCODE" }

    $builtExe = Join-Path $root "TruckDeck.Server\obj\$Configuration\TruckDeck.exe"
    if (Test-Path $builtExe) {
        Copy-Item $builtExe (Join-Path $SteamDir "TruckDeck.exe") -Force
        $pending = Join-Path $server "TruckDeck.exe.pending"
        Copy-Item $builtExe $pending -Force
        Write-Host "Fresh TruckDeck.exe copied to Steam mirror (and server\TruckDeck.exe.pending if server exe was locked)"
    }

    Copy-Item (Join-Path $server "BUILD.txt") (Join-Path $SteamDir "BUILD.txt") -Force
    Write-Host "Deployed to $SteamDir"
} elseif ($SkipSteam) {
    Write-Host "`n[4/4] Skipping Steam mirror"
} else {
    Write-Warning "Steam Telemetry Server parent not found - skipping mirror: $SteamDir"
}

Write-Host "`n=== Deploy complete ===" -ForegroundColor Green
Write-Host "Run: $(Join-Path $server 'TruckDeck.exe')"
if (-not $SkipSteam -and (Test-Path $SteamDir)) {
    Write-Host "Steam mirror: $(Join-Path $SteamDir 'TruckDeck.exe')"
}
