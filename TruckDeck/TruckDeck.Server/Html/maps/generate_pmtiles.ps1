# Parse ETS2/ATS install and generate PMTiles via truckermudgeon/maps.
# Requires setup_map_tools.ps1 to have been run once.
param(
    [Parameter(Mandatory)]
    [ValidateSet("ets2", "ats")]
    [string]$Game,

    [Parameter(Mandatory)]
    [string]$GamePath,

    [string]$MapToolsRoot = (Join-Path $env:LOCALAPPDATA "TruckDeck\map-tools\maps"),
    [string]$HtmlRoot = "",
    [string]$LogFile = "",
    [switch]$Activate
)

$ErrorActionPreference = "Stop"

function Write-Log([string]$Message) {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $Message"
    Write-Host $line
    if ($LogFile) { Add-Content -Path $LogFile -Value $line -Encoding UTF8 }
}

function Write-ProgressLine([int]$Percent, [string]$Message) {
    Write-Log "TRUCKDECK_PROGRESS:$Percent $Message"
}

function Resolve-HtmlRoot {
    if ($HtmlRoot -and (Test-Path $HtmlRoot)) { return (Resolve-Path $HtmlRoot).Path }
    $fromScript = Split-Path $PSScriptRoot -Parent
    if (Test-Path $fromScript) { return (Resolve-Path $fromScript).Path }
    throw "Html root not found. Pass -HtmlRoot explicitly."
}

$mapMode = if ($Game -eq "ats") { "usa" } else { "europe" }
$pmtilesName = if ($Game -eq "ats") { "ats.pmtiles" } else { "ets2.pmtiles" }

Write-ProgressLine 3 "Validating game install..."

$gamePathResolved = $GamePath.Trim().TrimEnd('\')
if (-not (Test-Path (Join-Path $gamePathResolved "base.scs"))) {
    Write-Log "ERROR: base.scs not found in $gamePathResolved"
    exit 1
}
Write-Log "Game path: $gamePathResolved"

if (-not (Test-Path $MapToolsRoot)) {
    Write-Log "ERROR: Map tools not installed. Run setup_map_tools.ps1 first."
    exit 1
}

$htmlRootResolved = Resolve-HtmlRoot
$workRoot = Join-Path $env:LOCALAPPDATA "TruckDeck\map-work\$Game"
$parserOut = Join-Path $workRoot "parser"
$genOut = Join-Path $workRoot "generated"
$generatedDir = Join-Path $htmlRootResolved "maps\generated"

foreach ($d in @($workRoot, $parserOut, $genOut, $generatedDir)) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}

Push-Location $MapToolsRoot
try {
    Write-ProgressLine 8 "Parsing $($Game.ToUpper()) map data (this can take several minutes)..."
    if (Test-Path $parserOut) { Remove-Item -Recurse -Force $parserOut }
    New-Item -ItemType Directory -Path $parserOut -Force | Out-Null

    npx parser -i $gamePathResolved -o $parserOut 2>&1 | ForEach-Object { Write-Log $_ }
    if ($LASTEXITCODE -ne 0) { throw "parser exited with code $LASTEXITCODE" }

    Write-ProgressLine 55 "Generating PMTiles ($mapMode)..."
    if (Test-Path $genOut) { Remove-Item -Recurse -Force $genOut }
    New-Item -ItemType Directory -Path $genOut -Force | Out-Null

    $overrides = Join-Path $MapToolsRoot "packages\clis\generator\resources\trucksim-overrides.json"
    $genArgs = @("generator", "map", "-m", $mapMode, "-i", $parserOut, "-o", $genOut)
    if (Test-Path $overrides) {
        $genArgs += @("--dataOverridesPath", $overrides)
    }

    npx @genArgs 2>&1 | ForEach-Object { Write-Log $_ }
    if ($LASTEXITCODE -ne 0) { throw "generator exited with code $LASTEXITCODE" }

    $builtFile = Join-Path $genOut $pmtilesName
    if (-not (Test-Path $builtFile)) {
        throw "Expected output not found: $builtFile"
    }

    Write-ProgressLine 88 "Copying to TruckDeck maps folder..."
    $destGenerated = Join-Path $generatedDir $pmtilesName
    Copy-Item -Path $builtFile -Destination $destGenerated -Force
    Write-Log "Wrote $destGenerated"

    if ($Activate) {
        $destActive = Join-Path $htmlRootResolved $pmtilesName
        Copy-Item -Path $builtFile -Destination $destActive -Force
        Write-Log "Activated map: $destActive"
    }

    Write-ProgressLine 90 "Generating routing graph for NAV..."
    try {
        $graphOut = Join-Path $workRoot "graph"
        if (Test-Path $graphOut) { Remove-Item -Recurse -Force $graphOut }
        New-Item -ItemType Directory -Path $graphOut -Force | Out-Null

        npx generator graph -m $mapMode -i $parserOut -o $graphOut 2>&1 | ForEach-Object { Write-Log $_ }
        if ($LASTEXITCODE -ne 0) { throw "generator graph exited with code $LASTEXITCODE" }

        $rawGraph = Join-Path $graphOut "$mapMode-graph.json"
        $rawNodes = Join-Path $parserOut "$mapMode-nodes.json"
        $rawCities = Join-Path $parserOut "$mapMode-cities.json"
        if ((Test-Path $rawGraph) -and (Test-Path $rawNodes) -and (Test-Path $rawCities)) {
            $buildGraphScript = Join-Path $PSScriptRoot "wsl\build-graph.js"
            node $buildGraphScript --game $Game --nodes $rawNodes --graph $rawGraph --cities $rawCities --mapToolsRoot $MapToolsRoot --out $generatedDir 2>&1 | ForEach-Object { Write-Log $_ }
            if ($LASTEXITCODE -ne 0) { throw "build-graph.js exited with code $LASTEXITCODE" }

            if ($Activate) {
                Copy-Item -Path (Join-Path $generatedDir "$Game-graph.json") -Destination (Join-Path $htmlRootResolved "$Game-graph.json") -Force -ErrorAction SilentlyContinue
                Copy-Item -Path (Join-Path $generatedDir "$Game-cities.json") -Destination (Join-Path $htmlRootResolved "$Game-cities.json") -Force -ErrorAction SilentlyContinue
            }
            Write-Log "Wrote routing graph + city lookup for NAV route line"
        } else {
            Write-Log "WARN: routing graph inputs missing, skipping NAV route data (map will still work without a route line)"
        }
    }
    catch {
        Write-Log "WARN: routing graph generation failed ($($_.Exception.Message)), skipping NAV route data (map will still work without a route line)"
    }

    $sizeMb = [math]::Round((Get-Item $destGenerated).Length / 1MB, 1)
    Write-ProgressLine 100 "Done. $pmtilesName ($sizeMb MB)"
    Write-Log "TRUCKDECK_DONE: $destGenerated"
}
catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    exit 1
}
finally {
    Pop-Location
}
