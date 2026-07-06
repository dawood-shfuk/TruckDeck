# Sync Html from source (TruckDeck.Server/Html) to runtime output and Steam mirror.
# Run after editing Html without a full rebuild, or use: build\deploy.ps1
param(
    [switch]$SkipSteamMirror,
    [string]$SteamDir = "D:\SteamLibrary\steamapps\common\Euro Truck Simulator 2\Telemetry Server\Html"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$src = Join-Path $root "TruckDeck.Server\Html"
$out = Join-Path $root "server\Html"
$steam = $SteamDir
if (-not $steam.EndsWith("Html")) {
    $steam = Join-Path $SteamDir "Html"
}

if (-not (Test-Path $src)) {
    throw "Source Html not found: $src"
}

function Inject-TruckDeckVersion {
    param([string]$HtmlDir, [string]$Version)
    if (-not (Test-Path $HtmlDir)) { return }
    Get-ChildItem $HtmlDir -Recurse -Filter *.html -File | ForEach-Object {
        $content = [System.IO.File]::ReadAllText($_.FullName)
        if ($content -notlike '*%TRUCKDECK_VERSION%*') { return }
        $updated = $content.Replace('%TRUCKDECK_VERSION%', $Version)
        if ($updated -ne $content) {
            [System.IO.File]::WriteAllText($_.FullName, $updated, (New-Object System.Text.UTF8Encoding $false))
        }
    }
}

$version = & (Join-Path $PSScriptRoot "Get-TruckDeckVersion.ps1") -Root $root
Write-Host "TruckDeck version (from source): $version"

Write-Host "Syncing Html -> $out (preserving maps/generated and *.pmtiles)"
robocopy $src $out /E /XD maps\generated android_app\_gradle android_app\app\build _vanilla_gps_cache /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit $LASTEXITCODE" }
Inject-TruckDeckVersion -HtmlDir $out -Version $version

if (-not $SkipSteamMirror -and (Test-Path (Split-Path $steam -Parent))) {
    Write-Host "Syncing Html -> $steam (preserving maps/generated and *.pmtiles)"
    robocopy $src $steam /E /XD maps\generated android_app\_gradle android_app\app\build _vanilla_gps_cache /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy steam mirror failed with exit $LASTEXITCODE" }
    Inject-TruckDeckVersion -HtmlDir $steam -Version $version
}

Write-Host "Html sync complete."
