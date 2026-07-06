# Installs truckermudgeon/maps (GPL-3.0) for PMTiles generation.
# Requires: Git, Node.js LTS, tippecanoe on PATH.
param(
    [string]$MapToolsRoot = (Join-Path $env:LOCALAPPDATA "TruckDeck\map-tools\maps"),
    [string]$LogFile = ""
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

function Test-Command([string]$Name) {
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

Write-ProgressLine 2 "Checking prerequisites..."

$missing = @()
if (-not (Test-Command node)) { $missing += "Node.js (https://nodejs.org/)" }
if (-not (Test-Command npm)) { $missing += "npm (bundled with Node.js)" }
if (-not (Test-Command git)) { $missing += "Git (https://git-scm.com/)" }
if (-not (Test-Command tippecanoe)) { $missing += "tippecanoe (https://github.com/felt/tippecanoe)" }

if ($missing.Count -gt 0) {
    Write-Log "ERROR: Missing prerequisites:"
    $missing | ForEach-Object { Write-Log "  - $_" }
    exit 1
}

Write-Log "Node: $(node --version)"
Write-Log "Git: $(git --version)"
Write-Log "tippecanoe: $(tippecanoe --version 2>&1 | Select-Object -First 1)"

$parent = Split-Path $MapToolsRoot -Parent
if (-not (Test-Path $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }

Write-ProgressLine 10 "Preparing map tools directory..."

if (-not (Test-Path (Join-Path $MapToolsRoot ".git"))) {
    if (Test-Path $MapToolsRoot) {
        Write-Log "Removing incomplete map-tools folder..."
        Remove-Item -Recurse -Force $MapToolsRoot
    }
    Write-ProgressLine 15 "Cloning truckermudgeon/maps (may take a few minutes)..."
    git clone --recurse-submodules "https://github.com/truckermudgeon/maps.git" $MapToolsRoot
} else {
    Write-Log "Map tools repo already present at $MapToolsRoot"
}

Push-Location $MapToolsRoot
try {
    Write-ProgressLine 40 "Installing npm packages..."
    npm install --no-fund --no-audit 2>&1 | ForEach-Object { Write-Log $_ }

    Write-ProgressLine 70 "Building native parser addon..."
    npm run build -w packages/clis/parser 2>&1 | ForEach-Object { Write-Log $_ }

    Write-ProgressLine 95 "Verifying installation..."
    if (-not (Test-Path "node_modules")) { throw "npm install did not create node_modules" }
    if (-not (Test-Path "packages\clis\parser")) { throw "parser package missing" }

    Write-ProgressLine 100 "Map tools ready."
    Write-Log "TRUCKDECK_DONE: Map tools installed at $MapToolsRoot"
}
finally {
    Pop-Location
}
