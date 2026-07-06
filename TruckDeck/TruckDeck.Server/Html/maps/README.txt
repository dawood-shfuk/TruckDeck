TruckDeck map tiles — PMTiles
========================================

PMTiles (vector maps for NAV / MapLibre) — WSL workflow (recommended)
---------------------------------------------------------------------
Open the visual Map Generator while TruckDeck is running:

  http://127.0.0.1:25555/tools/map-generator.html

Step 0 — WSL (one-time)
  - Choose install folder (e.g. D:\WSL if C: is full)
  - Click "Install WSL (Administrator)" — may require reboot

Step 1 — Map tools (one-time, inside WSL)
  - Click "Install map tools (WSL)"
  - Installs Node.js, git, tippecanoe, truckermudgeon/maps into ~/.truckdeck/

Step 2 — Game paths
  - Set ETS2 / ATS install folders (Browse or Detect Steam)

Step 3 — Generate
  - Select game(s), click Generate map
  - Output: Html\maps\generated\ets2.pmtiles (or ats.pmtiles)
  - Also generates Html\maps\generated\{game}-graph.json (routing graph) and
    {game}-cities.json (city lookup) used by NAV to draw a road-following route line
  - Activate copies to Html\{ets2|ats}.pmtiles (+ -graph.json/-cities.json) for dashboards

PowerShell orchestrators (called by TruckDeck API):
  Html\maps\install_wsl.ps1          - elevated WSL + Ubuntu import
  Html\maps\setup_map_tools_wsl.ps1  - runs wsl/setup_map_tools.sh
  Html\maps\generate_pmtiles_wsl.ps1 - runs wsl/generate_pmtiles.sh

Bash scripts (run inside WSL):
  Html\maps\wsl\setup_map_tools.sh
  Html\maps\wsl\generate_pmtiles.sh

Native Windows fallback (only if tippecanoe.exe is on PATH):
  Html\maps\setup_map_tools.ps1
  Html\maps\fetch_nav_fonts.ps1  - download offline label fonts (optional)
  Html\maps\fonts\             - local MapLibre glyph PBF files
  Html\tools\map-health.html   - NAV map asset + render diagnostics

API (port 25555):
  GET  /api/maps/status
  POST /api/maps/install-wsl   { "installPath": "D:\\WSL" }
  POST /api/maps/setup-tools
  POST /api/maps/generate      { "games": ["ets2"], "activate": true }
  GET  /api/maps/jobs/{id}
  POST /api/maps/activate

Settings (%LocalAppData%\TruckDeck\Settings.json):
  WslInstallPath, MapGenerationBackend ("wsl" default)

Map data: truckermudgeon/maps (GPL-3.0). tippecanoe runs in WSL Ubuntu.
