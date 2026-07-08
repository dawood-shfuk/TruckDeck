# Pack a GitHub-ready source tree into ..\git source\
param(
    [string]$DestRoot = ""
)

$ErrorActionPreference = "Stop"
$funbitRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$truckDeckSrc = Join-Path $funbitRoot "TruckDeck"
if (-not $DestRoot) {
    $DestRoot = Join-Path $funbitRoot "git source"
}

$version = & (Join-Path $PSScriptRoot "Get-TruckDeckVersion.ps1") -Root $truckDeckSrc

$excludeDirs = @(
    "server", "packages", "obj", "bin", ".vs", ".git", ".cursor",
    "maps\generated", "_vanilla_gps_cache", "_game_extract", "mod\_tools",
    "android_app\_gradle", "android_app\app\build", "android_app\.gradle",
    "node_modules", "x64", "Release", "Debug", "Win32", "_backup_*"
)
$excludeFiles = @(
    "*.pmtiles", "*.log", "TruckDeck.exe", "TruckDeck.exe.pending", "TruckDeck-crash.log",
    "BUILD.txt", "err.txt", "out.txt", "*.user", "*.suo", "*.cache",
    "admin.json", "truckdeck.db"
)

function Copy-SourceTree {
    param([string]$Source, [string]$Destination, [string[]]$ExtraExcludeDirs = @())
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

Write-Host "=== TruckDeck GitHub source pack v$version ===" -ForegroundColor Cyan
Write-Host "Destination: $DestRoot"

if (Test-Path $DestRoot) {
    $preserve = @('.git', '.gitignore', 'README.md', 'CONTRIBUTORS.md', 'SUPPORT.md', 'LICENSE', 'THIRD_PARTY.md', 'CHANGELOG.md')
    Get-ChildItem $DestRoot -Force | Where-Object { $preserve -notcontains $_.Name } | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Force -Path $DestRoot | Out-Null
}

Copy-SourceTree -Source $truckDeckSrc -Destination (Join-Path $DestRoot "TruckDeck")
Copy-SourceTree -Source (Join-Path $funbitRoot "scs-sdk-plugin") -Destination (Join-Path $DestRoot "scs-sdk-plugin")
Copy-SourceTree -Source (Join-Path $funbitRoot "trucksim-gps-plugin") -Destination (Join-Path $DestRoot "trucksim-gps-plugin")

$modRef = Join-Path $funbitRoot "Mod source"
if (Test-Path $modRef) {
    Copy-SourceTree -Source $modRef -Destination (Join-Path $DestRoot "reference\paper-sun-gps-pc-mod")
}

Copy-Item (Join-Path $funbitRoot "nginx.conf") (Join-Path $DestRoot "nginx.conf") -Force -ErrorAction SilentlyContinue

$docs = Join-Path $DestRoot "docs"
$previews = Join-Path $docs "previews"
New-Item -ItemType Directory -Force -Path $previews | Out-Null

$htmlSkins = Join-Path $truckDeckSrc "TruckDeck.Server\Html\skins"
Get-ChildItem $htmlSkins -Directory | ForEach-Object {
    $jpg = Join-Path $_.FullName "dashboard.jpg"
    if (Test-Path $jpg) {
        Copy-Item $jpg (Join-Path $previews "$($_.Name).jpg") -Force
    }
}

$icon = Join-Path $truckDeckSrc "TruckDeck.Server\Html\images\app-icon.png"
if (Test-Path $icon) { Copy-Item $icon (Join-Path $previews "app-icon.png") -Force }

$handoff = Join-Path $truckDeckSrc "build\AGENT_HANDOFF_1.6.3.2.md"
if (Test-Path $handoff) { Copy-Item $handoff (Join-Path $docs "AGENT_HANDOFF_1.6.3.2.md") -Force }

$modChangelog = Join-Path $truckDeckSrc "TruckDeck.Server\Html\mod\MOD_CHANGELOG.md"
if (Test-Path $modChangelog) { Copy-Item $modChangelog (Join-Path $docs "MOD_CHANGELOG.md") -Force }

$packedSize = (Get-ChildItem $DestRoot -Recurse -File -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
Write-Host "`n=== GitHub pack complete ===" -ForegroundColor Green
Write-Host "Location: $DestRoot"
Write-Host "Size: $([math]::Round($packedSize / 1MB, 1)) MB"
Write-Host "Next: cd `"$DestRoot`"; git init; git add .; git commit -m `"TruckDeck $version source release`""
