# Build Steam Workshop upload folder for TruckDeck NAV.
# Output: workshop\upload\  (browse to this folder in SCS Workshop Uploader)
param(
    [switch]$SkipNavBuild
)

$ErrorActionPreference = 'Stop'
$modRoot = Split-Path $PSScriptRoot -Parent
$workshopRoot = $PSScriptRoot
$uploadRoot = Join-Path $workshopRoot 'upload'
$packageRoot = Join-Path $uploadRoot 'universal'

$contentDirs = @('ui', 'material', 'def', 'vehicle', 'automat')
$metaFiles = @('manifest.sii', 'mod_description.txt', 'mod_icon.jpg')
$excludeNames = @(
    '_tools', '_game_extract', 'workshop',
    'TruckDeck_NAV.scs', 'MOD_CHANGELOG.md',
    'build_truckdeck_nav.ps1', 'build_truckdeck_nav.bat',
    'build_truckdeck_nav.ps1_new_func.txt'
)

function Copy-ModTree {
    param([string]$Source, [string]$Destination)
    if (-not (Test-Path $Source)) { return }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {
        if ($excludeNames -contains $_.Name) { return }
        if ($_.PSIsContainer) {
            Copy-ModTree -Source $_.FullName -Destination (Join-Path $Destination $_.Name)
        } else {
            $ext = $_.Extension.ToLowerInvariant()
            if ($ext -in @('.ps1', '.bat', '.md', '.scs', '.txt') -and $_.Name -notin $metaFiles) { return }
            Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $Destination $_.Name) -Force
        }
    }
}

function Ensure-PreviewImage {
    $preview = Join-Path $workshopRoot 'preview_640x360.jpg'
    $iconSrc = Join-Path $modRoot 'mod_icon.jpg'
    if (-not (Test-Path $iconSrc)) {
        throw "Missing mod_icon.jpg in mod root (original mod preview icon)."
    }

    Add-Type -AssemblyName System.Drawing
    $img = [System.Drawing.Image]::FromFile($iconSrc)
    try {
        $bmp = New-Object System.Drawing.Bitmap 640, 360
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

        # Fill frame with scaled mod icon (cover crop)
        $scale = [Math]::Max(640 / $img.Width, 360 / $img.Height)
        $w = [int]($img.Width * $scale)
        $h = [int]($img.Height * $scale)
        $x = (640 - $w) / 2
        $y = (360 - $h) / 2
        $g.DrawImage($img, $x, $y, $w, $h)

        # Bottom bar + branding
        $barHeight = 52
        $bar = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 12, 18, 12))
        $g.FillRectangle($bar, 0, (360 - $barHeight), 640, $barHeight)
        $bar.Dispose()

        $font = New-Object System.Drawing.Font 'Segoe UI', 22, ([System.Drawing.FontStyle]::Bold)
        $shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 0, 0, 0))
        $textBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 90, 209, 27))
        $caption = 'TruckDeck by Dawood'
        $size = $g.MeasureString($caption, $font)
        $tx = (640 - $size.Width) / 2
        $ty = 360 - $barHeight + (($barHeight - $size.Height) / 2)
        $g.DrawString($caption, $font, $shadow, ($tx + 1), ($ty + 1))
        $g.DrawString($caption, $font, $textBrush, $tx, $ty)
        $font.Dispose()
        $shadow.Dispose()
        $textBrush.Dispose()
        $g.Dispose()

        if (Test-Path $preview) { Remove-Item -LiteralPath $preview -Force }
        $bmp.Save($preview, [System.Drawing.Imaging.ImageFormat]::Jpeg)
        $bmp.Dispose()
        Write-Host "Created preview from mod_icon.jpg: $preview"
    } finally {
        $img.Dispose()
    }
}

function Repair-WorkshopMaterials {
    param([string]$Root)
    Get-ChildItem -LiteralPath $Root -Recurse -Filter '*.mat' -File | ForEach-Object {
        if ($_.FullName -match '[\\/]automat[\\/]') { return }
        $text = [System.IO.File]::ReadAllText($_.FullName)
        if ($text -match 'mip_filter\s*:\s*linear') {
            $patched = $text -replace 'mip_filter\s*:\s*linear', 'mip_filter : none'
            [System.IO.File]::WriteAllText($_.FullName, $patched)
            Write-Host "  patched $($_.FullName.Substring($Root.Length + 1))"
        }
    }
}

function Write-Utf8NoBom {
    param([string]$Path, [string]$Content)
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Content, $utf8)
}

if (-not $SkipNavBuild) {
    $navBuild = Join-Path $modRoot 'build_truckdeck_nav.ps1'
    if (Test-Path $navBuild) {
        Write-Host 'Building TruckDeck_NAV.scs (PackOnly)...'
        & $navBuild -PackOnly
    }
}

Write-Host 'Fixing mod assets (white.dds, ps_gps_mod.sii, stray files)...'
$fixAssets = Join-Path $workshopRoot 'fix_mod_assets.py'
if (Get-Command python -ErrorAction SilentlyContinue) {
    & python $fixAssets $modRoot
}

Write-Host "Building Workshop upload folder..."
if (Test-Path $uploadRoot) { Remove-Item -LiteralPath $uploadRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null

Copy-Item (Join-Path $workshopRoot 'versions.sii') (Join-Path $uploadRoot 'versions.sii') -Force
$versionsText = [System.IO.File]::ReadAllText((Join-Path $workshopRoot 'versions.sii'))
Write-Utf8NoBom -Path (Join-Path $uploadRoot 'versions.sii') -Content $versionsText

foreach ($dir in $contentDirs) {
    $src = Join-Path $modRoot $dir
    if (Test-Path $src) {
        Copy-ModTree -Source $src -Destination (Join-Path $packageRoot $dir)
        Write-Host "  + $dir/"
    }
}

foreach ($file in $metaFiles) {
    if ($file -eq 'manifest.sii') {
        Copy-Item (Join-Path $workshopRoot 'manifest_workshop.sii') (Join-Path $packageRoot 'manifest.sii') -Force
        Write-Host '  + manifest.sii (workshop)'
        continue
    }
    $src = Join-Path $modRoot $file
    if (-not (Test-Path $src)) {
        if ($file -eq 'mod_icon.jpg') { throw "Missing mod_icon.jpg in mod root (required for Workshop)." }
        throw "Missing required file: $src"
    }
    if ($file -eq 'mod_description.txt') {
        $desc = [System.IO.File]::ReadAllText($src)
        Write-Utf8NoBom -Path (Join-Path $packageRoot $file) -Content $desc
    } else {
        Copy-Item -LiteralPath $src -Destination (Join-Path $packageRoot $file) -Force
    }
    Write-Host "  + $file"
}

Write-Host 'Patching Workshop materials...'
Repair-WorkshopMaterials -Root $packageRoot

Write-Host 'Fixing NPOT UI textures (Workshop validation)...'
$fixScript = Join-Path $workshopRoot 'fix_workshop_textures.py'
if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    Write-Warning 'Python not found - skip texture fix; Workshop may reject NPOT UI textures.'
} else {
    & python $fixScript $packageRoot
    if ($LASTEXITCODE -ne 0) { throw "fix_workshop_textures.py failed ($LASTEXITCODE)" }
    & python $fixAssets $packageRoot --workshop
}

Ensure-PreviewImage

# Desktop copy for easy browsing in SCS Workshop Uploader
$desktopExport = Join-Path ([Environment]::GetFolderPath('Desktop')) 'TruckDeck_NAV_Workshop'
if (Test-Path $desktopExport) { Remove-Item -LiteralPath $desktopExport -Recurse -Force }
Copy-Item -LiteralPath $uploadRoot -Destination (Join-Path $desktopExport 'upload') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $workshopRoot 'preview_640x360.jpg') (Join-Path $desktopExport 'preview_640x360.jpg') -Force
Copy-Item -LiteralPath (Join-Path $workshopRoot 'workshop_description.txt') (Join-Path $desktopExport 'workshop_description.txt') -Force
@"
TRUCKDECK NAV - STEAM WORKSHOP UPLOAD
=====================================

In SCS Workshop Uploader:

  Mod data folder (browse HERE):
    $desktopExport\upload

  Preview image (separate field):
    $desktopExport\preview_640x360.jpg

  Mod name: TruckDeck NAV
  Description: paste from workshop_description.txt

DO NOT use:
  - TruckDeck source folder
  - TruckDeck_Server folder
  - workshop\universal only (missing versions.sii)
  - Any folder with .scs, .ps1, .html, apk.txt, job_list files

The upload folder must contain ONLY:
  upload\versions.sii
  upload\universal\  (mod files)
"@ | Set-Content (Join-Path $desktopExport 'READ_ME_FIRST.txt') -Encoding UTF8

Write-Host ""
Write-Host "Workshop pack ready:" -ForegroundColor Green
Write-Host "  Mod data folder (browse HERE in uploader):"
Write-Host "    $uploadRoot"
Write-Host "  Desktop copy: $desktopExport"
Write-Host "    upload\           <- Mod data folder"
Write-Host "    preview_640x360.jpg"
Write-Host "    READ_ME_FIRST.txt"
Write-Host ""
Write-Host 'IMPORTANT: Mod data must be the upload folder that contains versions.sii'
Write-Host '           and the universal subfolder - not universal itself.'
Write-Host ""
Write-Host "Description paste: $workshopRoot\workshop_description.txt"
Write-Host "Guide: $workshopRoot\WORKSHOP_UPLOAD.md"
