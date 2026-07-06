# Downloads Noto Sans Regular MapLibre glyph ranges for offline NAV city labels.
param(
    [string]$OutDir = (Join-Path $PSScriptRoot "fonts")
)

$ErrorActionPreference = "Stop"
$baseUrl = "https://tiles.openfreemap.org/fonts/Noto%20Sans%20Regular"
$fontDir = Join-Path $OutDir "Noto Sans Regular"
New-Item -ItemType Directory -Force -Path $fontDir | Out-Null

# Common ranges for Latin + extended European city names
$ranges = @(
    "0-255", "256-511", "512-767", "768-1023", "1024-1279", "1280-1535",
    "1536-1791", "1792-2047", "2048-2303", "2304-2559", "2560-2815", "2816-3071",
    "3072-3327", "3328-3583", "3584-3839", "3840-4095", "4096-4351", "4352-4607",
    "4608-4863", "4864-5119", "5120-5375", "5376-5631", "5632-5887", "5888-6143",
    "6144-6399", "6400-6655", "6656-6911", "6912-7167", "7168-7423", "7424-7679"
)

$ok = 0
foreach ($range in $ranges) {
    $dest = Join-Path $fontDir "$range.pbf"
    if ((Test-Path -LiteralPath $dest -PathType Leaf) -and (Get-Item -LiteralPath $dest).Length -gt 0) {
        Write-Host "Skip (exists): $range"
        $ok++
        continue
    }
    $url = "$baseUrl/$range.pbf"
    Write-Host "Fetching $range ..."
    try {
        Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing -TimeoutSec 30
        $ok++
    } catch {
        Write-Warning "Failed $range : $_"
    }
}

Write-Host "Done. $ok / $($ranges.Count) glyph ranges in $fontDir"
