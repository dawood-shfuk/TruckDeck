# TruckDeck NAV — sync vanilla factory GPS UI and pack .scs for ETS2 testing.
param(
    [switch]$SyncOnly,
    [switch]$PackOnly
)

$ErrorActionPreference = 'Stop'
$modRoot = $PSScriptRoot
$htmlRoot = Split-Path $modRoot -Parent
$outScs = Join-Path $modRoot 'TruckDeck_NAV.scs'
$gameRoot = 'D:\SteamLibrary\steamapps\common\Euro Truck Simulator 2'
$extractor = Join-Path $modRoot '_tools\scs_extractor.exe'
$vanillaCache = Join-Path $htmlRoot '_vanilla_gps_cache'

function Ensure-VanillaGpsCache {
    # Force fresh extraction to ensure compatibility with newest game version
    if (Test-Path $vanillaCache) { 
        Write-Host "  cleaning old cache..."
        Remove-Item -LiteralPath $vanillaCache -Recurse -Force 
    }

    if (-not (Test-Path $extractor)) { throw "Missing SCS extractor: $extractor" }
    if (-not (Test-Path $gameRoot)) { throw "ETS2 install not found: $gameRoot" }

    Write-Host '  extracting vanilla GPS UI from base_vehicle.scs (one-time)...'
    New-Item -ItemType Directory -Force -Path $vanillaCache | Out-Null
    & $extractor (Join-Path $gameRoot 'base_vehicle.scs') $vanillaCache | Out-Null
}

function Sync-VanillaFactoryGpsUi {
    Ensure-VanillaGpsCache

    $uiRoot = Join-Path $modRoot 'ui'
    $dashDst = Join-Path $uiRoot 'dashboard'
    New-Item -ItemType Directory -Force -Path $dashDst | Out-Null

    # Factory GPS views and templates copied verbatim from game
    $copies = @(
        @{ Src = 'ui\gps.sii'; Dst = 'ui\gps.sii' }
        @{ Src = 'ui\dashboard\scania_2025_gps.sii'; Dst = 'ui\dashboard\scania_2025_gps.sii' }
        @{ Src = 'ui\dashboard\renault_t_2024_gps.sii'; Dst = 'ui\dashboard\renault_t_2024_gps.sii' }
    )

    # Sync all dashboard templates from vanilla to avoid dangling pointers
    $templateDst = Join-Path $uiRoot 'template'
    New-Item -ItemType Directory -Force -Path $templateDst | Out-Null
    $vanillaTemplates = Get-ChildItem (Join-Path $vanillaCache 'ui\template') -Filter 'dashboard_text*.sii'
    foreach ($vTemp in $vanillaTemplates) {
        $dstPath = Join-Path $templateDst $vTemp.Name
        # Don't overwrite our custom mod template if it exists in mod root (it shouldn't be in vanilla cache anyway)
        Copy-Item -LiteralPath $vTemp.FullName -Destination $dstPath -Force
        Write-Host "  + ui/template/$($vTemp.Name) (vanilla templates)"
    }

    foreach ($item in $copies) {
        $src = Join-Path $vanillaCache $item.Src
        $dst = Join-Path $modRoot $item.Dst
        if (-not (Test-Path $src)) { throw "Missing vanilla source: $src" }
        Copy-Item -LiteralPath $src -Destination $dst -Force
        Write-Host "  + $($item.Dst) (vanilla factory GPS)"
    }

    # Volvo FH 2024 from bundled game extract (DLC pack).
    $volvoSrc = Join-Path $modRoot '_game_extract\dlc_volvo\ui\dashboard'
    foreach ($name in @('volvo_fh_2024_gps.sii', 'volvo_fh_2024_mph_gps.sii')) {
        $src = Join-Path $volvoSrc $name
        if (-not (Test-Path $src)) { 
            Write-Host "  ! Warning: Missing Volvo GPS source: $src"
            continue
        }
        Copy-Item -LiteralPath $src -Destination (Join-Path $dashDst $name) -Force
        Write-Host "  + ui/dashboard/$name (vanilla factory GPS)"
    }
}

function Get-PackFiles {
    $roots = @(
        (Join-Path $modRoot 'ui'),
        (Join-Path $modRoot 'material'),
        (Join-Path $modRoot 'def'),
        (Join-Path $modRoot 'vehicle'),
        (Join-Path $modRoot 'automat')
    )
    foreach ($root in $roots) {
        if (Test-Path $root) {
            Get-ChildItem -LiteralPath $root -Recurse -File
        }
    }
    foreach ($name in @('manifest.sii', 'mod_description.txt', 'mod_icon.jpg')) {
        $path = Join-Path $modRoot $name
        if (Test-Path $path) { Get-Item -LiteralPath $path }
    }
}

if (-not $PackOnly) {
    Write-Host 'Syncing vanilla factory GPS UI...'
    Sync-VanillaFactoryGpsUi
}

if (-not $SyncOnly) {
    Write-Host "Packing $outScs ..."
    if (Test-Path $outScs) { Remove-Item -LiteralPath $outScs -Force }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $zip = [System.IO.Compression.ZipFile]::Open($outScs, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-PackFiles | ForEach-Object {
            $entryName = $_.FullName.Substring($modRoot.Length + 1).Replace('\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip, $_.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal
            ) | Out-Null
        }
    } finally {
        $zip.Dispose()
    }

    $sizeMb = [math]::Round((Get-Item $outScs).Length / 1MB, 2)
    Write-Host "Done: $outScs ($sizeMb MB)"
    Write-Host 'Copy to Documents\Euro Truck Simulator 2\mod and enable in Mod Manager.'
}
