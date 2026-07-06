# Pack TruckDeck + plugin sources for deployment (no pmtiles, no runtime output, no build artifacts).
# Output: sibling folder TruckDeck_build_{version} with self-contained source tree.
param(
    [string]$DestRoot = ""
)

$ErrorActionPreference = "Stop"
$funbitRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$truckDeckSrc = Join-Path $funbitRoot "TruckDeck"
if (-not $DestRoot) {
    $DestRoot = & (Join-Path (Join-Path $truckDeckSrc "build") "Get-TruckDeckBuildRoot.ps1") -Root $truckDeckSrc
}
$rencloudSrc = Join-Path $funbitRoot "scs-sdk-plugin"
$gpsSrc = Join-Path $funbitRoot "trucksim-gps-plugin"

$destTruckDeck = Join-Path $DestRoot "TruckDeck"
$destRencloud = Join-Path $DestRoot "scs-sdk-plugin"
$destGps = Join-Path $DestRoot "trucksim-gps-plugin"

$excludeDirs = @(
    "server",
    "packages",
    "obj",
    "bin",
    ".vs",
    ".git",
    ".cursor",
    "maps\generated",
    "_vanilla_gps_cache",
    "android_app\_gradle",
    "android_app\app\build",
    "node_modules",
    "x64",
    "Release",
    "Debug",
    "Win32"
)

$excludeFiles = @(
    "*.pmtiles",
    "*.log",
    "TruckDeck.exe",
    "TruckDeck.exe.pending",
    "TruckDeck-crash.log",
    "BUILD.txt",
    "err.txt",
    "out.txt",
    "tmp_err.txt",
    "tmp_out.txt",
    "*.user",
    "*.suo",
    "*.cache"
)

function Copy-SourceTree {
    param(
        [string]$Source,
        [string]$Destination,
        [string[]]$ExtraExcludeDirs = @()
    )

    if (-not (Test-Path $Source)) {
        Write-Warning "Source not found, skipping: $Source"
        return
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    $xd = ($excludeDirs + $ExtraExcludeDirs) | ForEach-Object { "/XD"; $_ }
    $xf = $excludeFiles | ForEach-Object { "/XF"; $_ }

    Write-Host "Copying $Source -> $Destination"
    robocopy $Source $Destination /E /PURGE @xd @xf /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed ($LASTEXITCODE) for $Source" }
}

Write-Host "=== TruckDeck source pack ===" -ForegroundColor Cyan
Write-Host "Destination: $DestRoot"

if (Test-Path $DestRoot) {
    $preserve = @(
        'README-DEPLOY.md', 'release', 'downloads', 'landing', 'landing-assets', 'zips',
        'AGENT_HANDOFF.md', 'nginx.conf', 'TruckDeck-Setup.exe', 'TruckDeck-Release.zip'
    )
    Write-Host "Clearing previous pack (preserving release/downloads/landing if present)..."
    Get-ChildItem $DestRoot -Force | Where-Object { $preserve -notcontains $_.Name } | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Force -Path $DestRoot | Out-Null
}

Copy-SourceTree -Source $truckDeckSrc -Destination $destTruckDeck
Copy-SourceTree -Source $rencloudSrc -Destination $destRencloud
Copy-SourceTree -Source $gpsSrc -Destination $destGps

# Remove personal generated map/routing files from Html (robocopy /XF may miss nested copies).
$personalPatterns = @(
    "ets2.pmtiles", "ats.pmtiles",
    "ets2-graph.json", "ats-graph.json",
    "ets2-cities.json", "ats-cities.json"
)
$htmlRoots = @(
    (Join-Path $destTruckDeck "TruckDeck.Server\Html"),
    (Join-Path $destTruckDeck "TruckDeck.Server\Html\maps\generated")
)
foreach ($html in $htmlRoots) {
    if (-not (Test-Path $html)) { continue }
    foreach ($name in $personalPatterns) {
        Get-ChildItem $html -Filter $name -File -Recurse -ErrorAction SilentlyContinue |
            ForEach-Object { Remove-Item $_.FullName -Force; Write-Host "Removed personal asset: $($_.FullName)" }
    }
}

$version = & (Join-Path $PSScriptRoot "Get-TruckDeckVersion.ps1") -Root (Split-Path $PSScriptRoot -Parent)

$manifest = @"
TruckDeck deployment source pack
Version: $version
Packed: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss K")
Source root: $funbitRoot

Contents:
  TruckDeck/           Main server, Html skins, build scripts
  scs-sdk-plugin/      RenCloud telemetry plugin (C++)
  trucksim-gps-plugin/   TruckSim GPS telemetry plugin (C++)

Excluded (generate on target machine):
  *.pmtiles, maps/generated/, *-graph.json, *-cities.json at Html root
  server/ runtime output, packages/, obj/, bin/, logs

Deploy steps:
  1. cd TruckDeck
  2. .\build\deploy.ps1
  3. Run server\TruckDeck.exe (or mirror path from deploy.ps1 -SteamDir)
"@
Set-Content -Path (Join-Path $DestRoot "MANIFEST.txt") -Value $manifest -Encoding UTF8

$readme = Join-Path $DestRoot "README-DEPLOY.md"
if (-not (Test-Path $readme)) {
    @"
# TruckDeck deployment source

Self-contained source bundle for building and deploying TruckDeck on a server.

## Layout

| Folder | Purpose |
|--------|---------|
| `TruckDeck/` | Main project — run `build\deploy.ps1` from here |
| `scs-sdk-plugin/` | RenCloud telemetry DLL source |
| `trucksim-gps-plugin/` | TruckSim GPS telemetry DLL source |

Personal game assets (`*.pmtiles`, generated routing JSON) are **not** included. Generate them on the server with the Map Generator UI or `Html\maps\generate_pmtiles_wsl.ps1`.

## Quick start

```powershell
cd TruckDeck
.\build\deploy.ps1 -SteamDir "C:\path\to\Telemetry Server"
```

See `TruckDeck\README.md` for full documentation.
"@ | Set-Content -Path $readme -Encoding UTF8
}

$packedSize = (Get-ChildItem $DestRoot -Recurse -File | Measure-Object Length -Sum).Sum
Write-Host "`n=== Pack complete ===" -ForegroundColor Green
Write-Host "Location: $DestRoot"
Write-Host "Size: $([math]::Round($packedSize / 1MB, 1)) MB"
Write-Host "Manifest: $(Join-Path $DestRoot 'MANIFEST.txt')"
