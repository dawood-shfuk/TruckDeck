# Build TruckDeck-Setup.exe (Inno Setup) or release zip fallback.
param(
    [switch]$SkipReleasePack
)

$ErrorActionPreference = "Stop"
$buildDir = $PSScriptRoot
$truckDeckRoot = Split-Path $buildDir -Parent
$outDir = & (Join-Path $buildDir "Get-TruckDeckBuildRoot.ps1") -Root $truckDeckRoot
$version = & (Join-Path $buildDir "Get-TruckDeckVersion.ps1") -Root $truckDeckRoot
$iss = Join-Path $buildDir "TruckDeckSetup.iss"

if (-not $SkipReleasePack) {
    & (Join-Path $buildDir "pack_release.ps1")
}

Copy-Item (Join-Path $buildDir "INSTALL.txt") (Join-Path $outDir "release\INSTALL.txt") -Force

function Find-InnoCompiler {
    $paths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe"
    )
    foreach ($p in $paths) {
        if (Test-Path $p) { return $p }
    }
    return $null
}

$iscc = Find-InnoCompiler
if ($iscc) {
    Write-Host "Compiling installer with $iscc" -ForegroundColor Yellow
    $releaseDir = Join-Path $outDir "release"
    $absRelease = (Resolve-Path (Join-Path $releaseDir "TruckDeck")).Path
    Push-Location $buildDir
    try {
        & $iscc "/DReleaseDir=$releaseDir" "/DOutputDir=$outDir" $iss
        if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit $LASTEXITCODE" }
    } finally {
        Pop-Location
    }
    $setup = Join-Path $outDir "TruckDeck-Setup.exe"
    Write-Host "`nInstaller ready: $setup" -ForegroundColor Green
} else {
    Write-Warning "Inno Setup not found. Creating TruckDeck-Release.zip instead."
    $zip = Join-Path $outDir "TruckDeck-Release.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path (Join-Path $outDir "release\*") -DestinationPath $zip -Force
    Write-Host "Zip ready: $zip" -ForegroundColor Green
    Write-Host "Install Inno Setup 6 to build TruckDeck-Setup.exe: https://jrsoftware.org/isdl.php"
}
