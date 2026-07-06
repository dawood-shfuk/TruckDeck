# TruckDeck %TRUCKDECK_VERSION%

Unified ETS2/ATS telemetry server and TruckDeck dashboard suite. Combines the TelemetryServer4 foundation, RenCloud `scs-sdk-plugin` telemetry, native C# input bridge, and all TruckDeck Html skins from the live install.

**No Python sidecars at runtime** — telemetry extras and input commands are handled inside `TruckDeck.exe`.

## Architecture

```
ETS2/ATS  →  scs-telemetry.dll  →  Local\SCSTelemetry (or Local\TSGPSTelemetry)
TruckDeck.exe
  ├── OWIN :25555  — REST /api/ets2/telemetry, SignalR, Html dashboards
  └── HttpListener :25556  — input bridge (/api/command/*, /health)
```

RenCloud fields (hazard lights, diff lock, lift axle, `fuelRange`, `wearBody`, route distance, MP offset, etc.) are merged **server-side** in C# via `ScsTelemetryDataReader` + `ScsTelemetryData`.

**Default in-game plugin:** `trucksim-gps-telemetry.dll` (TruckSim GPS, MMF `Local\TSGPSTelemetry`). RenCloud `scs-telemetry.dll` is bundled as an optional swap. Only one plugin should be active in-game.

## Requirements

- Windows 10/11 x64
- .NET Framework 4.8
- Visual Studio 2022 (or Build Tools) with MSBuild — for building the C# solution
- Visual Studio C++ desktop workload — **optional**, only if building plugins from source
- Euro Truck Simulator 2 or American Truck Simulator

## Build

```powershell
cd "L:\FUNBIT TS4 src\TruckDeck"

# Restore NuGet packages (first time)
nuget restore TruckDeck.sln

# Telemetry plugins (RenCloud + optional TruckSim GPS)
.\build\build_plugins.ps1

# Server + SCSSdkClient
msbuild TruckDeck.sln /p:Configuration=Release
```

Release output: `TruckDeck\server\` (`TruckDeck.exe`, `Html\`, `Plugins\`, `Bridges\`).

**Html source of truth:** edit files under `TruckDeck.Server\Html\` only. After Html changes, either rebuild the solution or run `build\sync_html.ps1` to copy into `server\Html\` and the Steam Telemetry Server mirror.

**Full build + Steam mirror (recommended before server move):**

```powershell
.\build\deploy.ps1
```

This runs plugin build, `msbuild` Release, Html sync, and mirrors `server\` to `D:\SteamLibrary\steamapps\common\Euro Truck Simulator 2\Telemetry Server\` (exe, DLLs, Plugins, Bridges — Html synced separately so maps/generated and `*.pmtiles` are preserved).

**Pack portable source for server deployment** (no pmtiles, no runtime output):

```powershell
.\build\pack_source.ps1
```

Creates `..\TruckDeck_build_1.6.3.2\` with `TruckDeck\`, `scs-sdk-plugin\`, and `trucksim-gps-plugin\` — ready to copy to a server and run `build\deploy.ps1` there.

**End-user installer** (ready-to-run package, no source — skins, server, plugins; no pmtiles):

```powershell
.\build\build_installer.ps1
```

Builds `..\TruckDeck_build_1.6.3.2\TruckDeck-Setup.exe` with [Inno Setup 6](https://jrsoftware.org/isdl.php), or `TruckDeck-Release.zip` if Inno is not installed. The installer asks where to install (default: `{ETS2}\Telemetry Server`), auto-detects Steam game folders, requires at least one of ETS2/ATS for the telemetry plugin, and can install the NAV mod + ship the Android APK in `Extras\`.

**NAV map diagnostics:** `http://127.0.0.1:25555/tools/map-health.html` — checks PMTiles, vendor scripts, sprites, fonts, and live map preview.

If C++ is not installed, `build_plugins.ps1` automatically downloads RenCloud **V.1.12.1** `scs-telemetry.dll` from GitHub releases.

### Plugin sources (sibling repos)

| Plugin | Source | MMF name | Output DLL |
|--------|--------|----------|------------|
| TruckSim GPS (default) | `..\trucksim-gps-plugin\` | `Local\TSGPSTelemetry` | `trucksim-gps-telemetry.dll` |
| RenCloud (alt) | `..\scs-sdk-plugin\` | `Local\SCSTelemetry` | `scs-telemetry.dll` |

**Only one telemetry plugin should be active in-game.**

## First run / setup

1. Run `server\TruckDeck.exe` **as Administrator** (first time) to create firewall and URL ACL rules for ports **25555** and **25556**.
2. Complete the setup wizard — point to your ETS2/ATS install folder. The wizard copies `trucksim-gps-telemetry.dll` to `{game}\bin\win_x64\plugins\`.
3. Launch ETS2/ATS and start driving — telemetry connects when the game is running with the plugin loaded.

Settings and logs: `%LocalAppData%\TruckDeck\` (log file: `TruckDeck.log` in the server working directory).

## API endpoints

| URL | Description |
|-----|-------------|
| `GET /api/ets2/telemetry` | Full telemetry JSON (Funbit-compatible schema + RenCloud extras) |
| `GET /api/rencloud/extras` | Backward-compat stub (`{"available":true}`) |
| `GET http://host:25556/health` | Input bridge health |
| `POST http://host:25556/api/command/{action}` | Send game key commands |

## Dashboard skins

Eight TruckDeck skins ship in `Html\skins\`:

- `truck_command_deck`, `TruckDeckDash`
- `truckdeck_volvo`, `truckdeck_scania`, `truckdeck_mercedes`, `truckdeck_man`, `truckdeck_daf`, `truckdeck_renault`

Open any skin's `mock.html` in a browser for offline preview, or browse `http://127.0.0.1:25555/` when the server is running.

Set `UseEts2TestTelemetryData` to `true` in `TruckDeck.exe.config` to serve static data from `Ets2TestTelemetry.json` (useful for skin development without the game).

## Input bridge

Key bindings live in `Bridges\InputBridgeConfig.json` (copied to output). Align bindings with your in-game ETS2 key settings. The bridge starts automatically with TruckDeck — no `bridge.py` required.

## Included dev assets (not started at runtime)

| Path | Purpose |
|------|---------|
| `Html\mod\` | TruckDeck NAV mod source |
| `Html\maps\` | PMTiles map generation pipeline (WSL/tippecanoe/truckermudgeon) for Command Deck GPS / NAV |
| `Html\pwa\`, `Html\android_app\` | PWA and Android wrapper source |
| `Html\input_bridge\` | Legacy Python bridge (reference only) |

## Project layout

```
TruckDeck/
├── TruckDeck.sln
├── TruckDeck.Server/          # WinExe (assembly name: TruckDeck)
├── libs/SCSSdkClient/         # RenCloud C# client
├── build/build_plugins.ps1
├── server/                    # Release build output
└── packages/                  # NuGet restore target
```

## Verification checklist

1. `msbuild TruckDeck.sln /p:Configuration=Release` — no errors
2. Setup installs `scs-telemetry.dll` to ETS2 `bin\win_x64\plugins\`
3. TruckDeck running → `GET :25555/api/ets2/telemetry` and `GET :25556/health` respond
4. In-game → telemetry returns `connected: true` with hazard/lift axle fields
5. Truck Command Deck buttons send commands via `:25556`
6. All eight `mock.html` skins render
7. No Python processes required at runtime
