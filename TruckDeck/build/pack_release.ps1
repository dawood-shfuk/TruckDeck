# Build a ready-to-ship TruckDeck runtime package (no source).
param(
    [string]$OutRoot = "",
    [switch]$SkipBuild,
    [switch]$SkipMod,
    [switch]$SkipApk
)

$ErrorActionPreference = "Stop"
$truckDeckRoot = Split-Path $PSScriptRoot -Parent
if (-not $OutRoot) {
    $OutRoot = Join-Path (& (Join-Path $PSScriptRoot "Get-TruckDeckBuildRoot.ps1") -Root $truckDeckRoot) "release"
}
$server = Join-Path $truckDeckRoot "server"
$staging = Join-Path $OutRoot "TruckDeck"
$extras = Join-Path $OutRoot "Extras"

Write-Host "=== TruckDeck release pack ===" -ForegroundColor Cyan

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot "deploy.ps1") -SkipSteam
}

if (-not (Test-Path (Join-Path $server "TruckDeck.exe"))) {
    throw "TruckDeck.exe not found in $server - run deploy.ps1 first"
}

if (Test-Path $OutRoot) {
    Write-Host "Clearing $OutRoot ..."
    Remove-Item $OutRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $staging, $extras | Out-Null

Write-Host "Staging runtime -> $staging"
robocopy $server $staging /E /XD Html /XF *.log TruckDeck-crash.log BUILD.txt *.pdb /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy server failed" }

$htmlSrc = Join-Path $truckDeckRoot "TruckDeck.Server\Html"
$htmlDst = Join-Path $staging "Html"
Write-Host "Staging Html (skins, tools, pwa - no dev/build artifacts)..."
$htmlExcludeDirs = @(
    "android_app\_gradle", "android_app\app\build", "android_app\.gradle",
    "maps\generated", "maps\node_modules", "maps\wsl", "mod\_tools", "mod\_game_extract",
    "_vanilla_gps_cache", "input_bridge"
)
$xd = $htmlExcludeDirs | ForEach-Object { "/XD"; $_ }
robocopy $htmlSrc $htmlDst /E @xd /XF *.pmtiles ets2-graph.json ats-graph.json ets2-cities.json ats-cities.json /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy Html failed" }

# Optional mod pack
if (-not $SkipMod) {
    $modScript = Join-Path $htmlSrc "mod\build_truckdeck_nav.ps1"
    $modScs = Join-Path $htmlSrc "mod\TruckDeck_NAV.scs"
    if ((Test-Path $modScript) -and -not (Test-Path $modScs)) {
        Write-Host "Building TruckDeck NAV mod..."
        try { & $modScript -PackOnly 2>&1 | Out-Host } catch { Write-Warning "Mod build skipped: $_" }
    }
    if (Test-Path $modScs) {
        Copy-Item $modScs (Join-Path $extras "TruckDeck_NAV.scs") -Force
        Write-Host "Included TruckDeck_NAV.scs"
    }
}

# Optional APK
if (-not $SkipApk) {
    $apk = Get-ChildItem (Join-Path $htmlSrc "android_app\app\build\outputs\apk") -Recurse -Filter "*.apk" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($apk) {
        Copy-Item $apk.FullName (Join-Path $extras "TruckDeck.apk") -Force
        Write-Host "Included TruckDeck.apk"
    } else {
        Write-Host "APK not built - run Html\android_app\build_apk.bat to include"
    }
}

$version = & (Join-Path $PSScriptRoot "Get-TruckDeckVersion.ps1") -Root $truckDeckRoot

@"
TruckDeck $version
Packed: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss K")

Run TruckDeck-Setup.exe or open INSTALL.txt in this folder.
"@ | Set-Content (Join-Path $OutRoot "VERSION.txt") -Encoding UTF8

Copy-Item (Join-Path $truckDeckRoot "VERSION.txt") (Join-Path $OutRoot "VERSION.txt") -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $PSScriptRoot "INSTALL.txt") (Join-Path $OutRoot "INSTALL.txt") -Force -ErrorAction SilentlyContinue

$size = (Get-ChildItem $OutRoot -Recurse -File | Measure-Object Length -Sum).Sum
Write-Host "`nRelease ready: $OutRoot ($([math]::Round($size/1MB,1)) MB)" -ForegroundColor Green
