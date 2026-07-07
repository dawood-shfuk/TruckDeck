# Full server handover pack for truckdeck.site (landing :25855 + static downloads).
# Output: TruckDeck_build_{version}\
param(
    [string]$OutRoot = "",
    [switch]$SkipBuild,
    [switch]$SkipInstaller,
    [switch]$SkipSourcePack
)

$ErrorActionPreference = "Stop"
$buildDir = $PSScriptRoot
$truckDeckRoot = Split-Path $buildDir -Parent
$funbitRoot = Split-Path $truckDeckRoot -Parent
if (-not $OutRoot) {
    $OutRoot = & (Join-Path $buildDir "Get-TruckDeckBuildRoot.ps1") -Root $truckDeckRoot
}
$version = & (Join-Path $buildDir "Get-TruckDeckVersion.ps1") -Root $truckDeckRoot

Write-Host "=== TruckDeck server handover pack v$version ===" -ForegroundColor Cyan
Write-Host "Output: $OutRoot"

if (-not $SkipBuild) {
    & (Join-Path $buildDir "deploy.ps1") -SkipSteam -SkipPlugins
}

if (-not $SkipSourcePack) {
    & (Join-Path $buildDir "pack_source.ps1") -DestRoot $OutRoot
}

if (-not $SkipBuild) {
    & (Join-Path $buildDir "pack_release.ps1") -OutRoot (Join-Path $OutRoot "release") -SkipBuild
}

if (-not $SkipInstaller) {
    try {
        & (Join-Path $buildDir "build_installer.ps1") -SkipReleasePack
    } catch {
        Write-Warning "Installer build skipped: $_"
    }
}

# (source pack moved before release pack above)

# Public downloads for static serving (web root /downloads/)
$downloads = Join-Path $OutRoot "downloads"
New-Item -ItemType Directory -Force -Path $downloads | Out-Null

$setup = Join-Path $OutRoot "TruckDeck-Setup.exe"
if (Test-Path $setup) {
    Copy-Item $setup (Join-Path $downloads "TruckDeck-Setup.exe") -Force
}

$extras = Join-Path $OutRoot "release\Extras"
if (Test-Path $extras) {
    Get-ChildItem $extras -File | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $downloads $_.Name) -Force
    }
}

# Html download page assets (splash thumbnails for landing previews)
$previews = Join-Path $OutRoot "landing-assets\previews"
New-Item -ItemType Directory -Force -Path $previews | Out-Null
$htmlSkins = Join-Path $truckDeckRoot "TruckDeck.Server\Html\skins"
$previewSkins = @("TruckDeckDash", "truck_command_deck", "truckdeck_nav", "truckdeck_scania", "truckdeck_volvo", "truckdeck_daf")
foreach ($skin in $previewSkins) {
    $jpg = Join-Path $htmlSkins "$skin\dashboard.jpg"
    if (Test-Path $jpg) {
        Copy-Item $jpg (Join-Path $previews "$skin.jpg") -Force
    }
}
$icon = Join-Path $htmlSkins "..\images\app-icon.png"
if (Test-Path $icon) { Copy-Item $icon (Join-Path $previews "app-icon.png") -Force }

# Landing scaffold
$landingSrc = Join-Path $buildDir "landing-scaffold"
$landingDst = Join-Path $OutRoot "landing"
if (Test-Path $landingSrc) {
    if (Test-Path $landingDst) { Remove-Item $landingDst -Recurse -Force }
    Copy-Item $landingSrc $landingDst -Recurse -Force
}

if (-not (Test-Path (Join-Path $OutRoot "README-DEPLOY.md"))) {
    @"
# TruckDeck deployment pack (v$version)
Set TRUCKDECK_STATIC_ROOT to your web root. Run landing/ on 127.0.0.1:25855 behind your reverse proxy.
"@ | Set-Content (Join-Path $OutRoot "README-DEPLOY.md") -Encoding UTF8
}

@"
TruckDeck handover pack
Version: $version
Packed: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss K")
Source: $truckDeckRoot

Folders:
  release/          Windows runtime (TruckDeck.exe + Html) for end-user zip/installer
  downloads/        Public files for static serving (Setup, APK, NAV mod)
  landing/          Flask scaffold for truckdeck.site :25855
  landing-assets/   Skin preview JPEGs + app icon for landing page
  TruckDeck/        Source tree (deploy on build server if needed)

Deploy:
  Set TRUCKDECK_STATIC_ROOT to your web root
  Landing app: 127.0.0.1:25855 behind HTTPS reverse proxy
"@ | Set-Content (Join-Path $OutRoot "MANIFEST.txt") -Encoding UTF8

# Zip packages
$zips = Join-Path $OutRoot "zips"
New-Item -ItemType Directory -Force -Path $zips | Out-Null

$releaseZip = Join-Path $zips "TruckDeck-$version-release.zip"
if (Test-Path $releaseZip) { Remove-Item $releaseZip -Force }
$releaseDir = Join-Path $OutRoot "release"
if (Test-Path $releaseDir) {
    Compress-Archive -Path (Join-Path $releaseDir "*") -DestinationPath $releaseZip -Force
}

$dlZip = Join-Path $zips "TruckDeck-$version-downloads.zip"
if (Test-Path $dlZip) { Remove-Item $dlZip -Force }
Compress-Archive -Path (Join-Path $downloads "*") -DestinationPath $dlZip -Force -ErrorAction SilentlyContinue

$handoverZip = Join-Path $zips "TruckDeck-$version-handover.zip"
if (Test-Path $handoverZip) { Remove-Item $handoverZip -Force }
$zipItems = @(
    (Join-Path $OutRoot "MANIFEST.txt"),
    (Join-Path $OutRoot "README-DEPLOY.md"),
    (Join-Path $OutRoot "landing"),
    (Join-Path $OutRoot "landing-assets"),
    (Join-Path $OutRoot "downloads")
) | Where-Object { Test-Path $_ }
Compress-Archive -Path $zipItems -DestinationPath $handoverZip -Force

Write-Host "`nHandover pack ready: $OutRoot" -ForegroundColor Green
Write-Host "  $releaseZip"
Write-Host "  $dlZip"
Write-Host "  $handoverZip"
