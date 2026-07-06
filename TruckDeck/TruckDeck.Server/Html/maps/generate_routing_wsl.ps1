# Build NAV routing sidecars only (graph + cities) using cached WSL parser output.
param(
    [Parameter(Mandatory)]
    [ValidateSet("ets2", "ats")]
    [string]$Game,

    [string]$HtmlRoot = "",
    [string]$Distro = ""
)

$ErrorActionPreference = "Stop"

function ConvertTo-WslPath([string]$WinPath) {
    $full = $WinPath.Trim().TrimEnd('\')
    if (Test-Path $full) { $full = (Resolve-Path $full).Path }
    if ($full -match '^([A-Za-z]):\\(.*)$') {
        $drive = $Matches[1].ToLower()
        $rest = $Matches[2] -replace '\\', '/'
        if ($rest) { return "/mnt/$drive/$rest" }
        return "/mnt/$drive"
    }
    throw "Cannot convert path to WSL: $WinPath"
}

if (-not $HtmlRoot) {
    $HtmlRoot = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) "server\Html"
}
if (-not (Test-Path $HtmlRoot)) {
    throw "Html root not found: $HtmlRoot"
}

if (-not $Distro) {
    . (Join-Path $PSScriptRoot "wsl\resolve_distro.ps1")
    $Distro = Resolve-WslDistro
}
if (-not $Distro) { throw "No WSL distro found" }

$htmlWsl = ConvertTo-WslPath $HtmlRoot
$remoteDir = "/tmp/truckdeck-wsl"
$wslDirWin = Join-Path $PSScriptRoot "wsl"

& wsl -d $Distro -e bash -lc "mkdir -p '$remoteDir'" | Out-Null
foreach ($name in @("common.sh", "generate_routing.sh", "build-graph.js")) {
    $src = Join-Path $wslDirWin $name
    if (-not (Test-Path $src)) { throw "Missing $src" }
    $srcWsl = ConvertTo-WslPath $src
    $dest = "$remoteDir/$name"
    & wsl -d $Distro -e bash -lc "sed 's/\r`$//' '$srcWsl' > '$dest' && chmod +x '$dest'" | Out-Null
}

Write-Host "Generating NAV routing data for $Game -> $HtmlRoot"
& wsl -d $Distro -e bash "$remoteDir/generate_routing.sh" --game $Game --html-root $htmlWsl
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$steam = "D:\SteamLibrary\steamapps\common\Euro Truck Simulator 2\Telemetry Server\Html"
if (Test-Path $steam) {
    foreach ($f in @("$Game-graph.json", "$Game-cities.json")) {
        $src = Join-Path $HtmlRoot $f
        if (Test-Path $src) {
            Copy-Item -Force $src (Join-Path $steam $f)
            $genDir = Join-Path $steam "maps\generated"
            New-Item -ItemType Directory -Force -Path $genDir | Out-Null
            Copy-Item -Force $src (Join-Path $genDir $f)
        }
    }
    Write-Host "Copied routing files to Steam Telemetry Server Html"
}

Write-Host "Done."
