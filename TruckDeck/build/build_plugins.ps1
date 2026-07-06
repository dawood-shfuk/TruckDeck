# Build RenCloud and TruckSim GPS telemetry plugins (Release x64)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dest = Join-Path $root "TruckDeck.Server\Plugins\win_x64\plugins"
New-Item -ItemType Directory -Force -Path $dest | Out-Null

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
    throw "MSBuild not found. Install Visual Studio 2022 with C++ desktop development."
}

$msbuild = Find-MSBuild
Write-Host "Using MSBuild: $msbuild"

$rencloudSln = Join-Path $root "..\scs-sdk-plugin\scs-telemetry\vs2012\scs-telemetry.sln"
$gpsSln = Join-Path $root "..\trucksim-gps-plugin\scs-telemetry\vs2012\scs-telemetry.sln"

if (-not (Test-Path $rencloudSln)) {
    $rencloudSln = "L:\FUNBIT TS4 src\scs-sdk-plugin\scs-telemetry\vs2012\scs-telemetry.sln"
}
if (-not (Test-Path $gpsSln)) {
    $gpsSln = "L:\FUNBIT TS4 src\trucksim-gps-plugin\scs-telemetry\vs2012\scs-telemetry.sln"
}

& $msbuild $rencloudSln /p:Configuration=Release /p:Platform=Win32 /v:m
& $msbuild $gpsSln /p:Configuration=Release /p:Platform=Win32 /v:m

$rencloudOut = Join-Path (Split-Path $rencloudSln) "x64\Release\scs-telemetry.dll"
if (-not (Test-Path $rencloudOut)) {
    $rencloudOut = Join-Path (Split-Path $rencloudSln) "Release\scs-telemetry.dll"
}
if (-not (Test-Path $rencloudOut)) {
    $rencloudOut = Join-Path (Split-Path $rencloudSln) "Win32\Release\scs-telemetry.dll"
}
$gpsOut = Join-Path (Split-Path $gpsSln) "x64\Release\trucksim-gps-telemetry.dll"
if (-not (Test-Path $gpsOut)) {
    $gpsOut = Join-Path (Split-Path $gpsSln) "Release\trucksim-gps-telemetry.dll"
}
if (-not (Test-Path $gpsOut)) {
    $gpsOut = Join-Path (Split-Path $gpsSln) "Win32\Release\trucksim-gps-telemetry.dll"
}

if (Test-Path $rencloudOut) {
    Copy-Item $rencloudOut (Join-Path $dest "scs-telemetry.dll") -Force
    Write-Host "Copied scs-telemetry.dll"
} else {
    Write-Warning "RenCloud plugin build output not found. Trying GitHub release download..."
    $zip = Join-Path $env:TEMP "release_v_1_12_1.zip"
    $extract = Join-Path $env:TEMP "rencloud_plugin_dl"
    try {
        Invoke-WebRequest -Uri "https://github.com/RenCloud/scs-sdk-plugin/releases/download/V.1.12.1/release_v_1_12_1.zip" -OutFile $zip
        Expand-Archive -Path $zip -DestinationPath $extract -Force
        $dll = Get-ChildItem $extract -Recurse -Filter "scs-telemetry.dll" |
            Where-Object { $_.DirectoryName -match 'Win64' } |
            Select-Object -First 1
        if (-not $dll) {
            $dll = Get-ChildItem $extract -Recurse -Filter "scs-telemetry.dll" | Select-Object -First 1
        }
        if ($dll) {
            Copy-Item $dll.FullName (Join-Path $dest "scs-telemetry.dll") -Force
            Write-Host "Downloaded scs-telemetry.dll from RenCloud V.1.12.1 release"
        } else {
            Write-Warning "Download succeeded but scs-telemetry.dll not found in archive."
        }
    } catch {
        Write-Warning "Could not download RenCloud plugin: $_"
    }
}

if (Test-Path $gpsOut) {
    Copy-Item $gpsOut (Join-Path $dest "trucksim-gps-telemetry.dll") -Force
    Write-Host "Copied trucksim-gps-telemetry.dll"
} else {
    Write-Warning "TruckSim GPS plugin build output not found. Downloading from trucksim-gps-server..."
    $zip = Join-Path $env:TEMP "trucksim-gps-server-master.zip"
    $extract = Join-Path $env:TEMP "trucksim_gps_server_src"
    try {
        Invoke-WebRequest -Uri "https://github.com/TruckSim-GPS/trucksim-gps-server/archive/refs/heads/master.zip" -OutFile $zip
        Expand-Archive -Path $zip -DestinationPath $extract -Force
        $dll = Get-ChildItem $extract -Recurse -Filter "trucksim-gps-telemetry.dll" |
            Where-Object { $_.FullName -match 'win_x64\\plugins' } |
            Select-Object -First 1
        if (-not $dll) {
            $dll = Get-ChildItem $extract -Recurse -Filter "trucksim-gps-telemetry.dll" | Select-Object -First 1
        }
        if ($dll) {
            Copy-Item $dll.FullName (Join-Path $dest "trucksim-gps-telemetry.dll") -Force
            Write-Host "Downloaded trucksim-gps-telemetry.dll from trucksim-gps-server"
        } else {
            Write-Warning "Download succeeded but trucksim-gps-telemetry.dll not found in archive."
        }
    } catch {
        Write-Warning "Could not download TruckSim GPS plugin: $_"
    }
}

Write-Host "Plugin output: $dest"
