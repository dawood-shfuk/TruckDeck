# Truck Command Deck - Telemetry Server Html

This repository contains the web-based dashboard and Android wrapper for the Truck Command Deck, a telemetry server for truck simulators (Euro Truck Simulator 2 and American Truck Simulator).

## Project Structure

- **`/` (Root)**: The legacy/main web entry point.
  - `index.html`: A menu to select and launch different dashboard skins.
  - `dashboard-host.html`: The container that hosts the selected skin at runtime.
  - `cordova.js`: Support for Cordova-based mobile deployments.
- **`pwa/`**: A modern Progressive Web App (PWA) version of the dashboard. This is the recommended version for mobile devices as it can be installed and works offline.
- **`android_app/`**: A native Android wrapper (WebView) that loads the PWA version of the dashboard.
- **`skins/`**: A collection of dashboard skins. Each skin is a separate directory containing its own `dashboard.html`, `dashboard.js`, `dashboard.css`, and `config.json`.
- **`scripts/`**: Shared JavaScript libraries (jQuery, SignalR, doT.js) and core dashboard logic (`dashboard-core.js`, `app.js`).
- **`images/`**: Shared static assets and icons.

## Getting Started

### Web Version
1. Ensure the Telemetry Server is running on your PC.
2. Open `index.html` in a web browser.
3. Enter the Server IP and select a skin.

### PWA Version
1. Navigate to the `/pwa/` directory on your web server.
2. Follow the prompts to install the app on your mobile device.

### Android App
1. Build the APK using the instructions in `android_app/README.md`.
2. Install the APK on your Android device.
3. Ensure the app is configured with the correct Server IP in `MainActivity.java`.

## TruckDeck stack (Command Deck + in-cab NAV)

The **Trucker Command Deck** skin (`skins/truck_command_deck`) works with the **TruckDeck NAV** ETS2 mod (`mod/TruckDeck_NAV.scs`):

| Component | Role |
|-----------|------|
| `Ets2Telemetry.exe` (:25555) | Telemetry to the Command Deck (trip distance, ETA, speed limit) |
| Input Bridge (:25556) | Remote keys: in-cab nav, zoom, world map |
| `truck_command_deck` skin | TRUCKDECK NAV panel — mirror GPS canvas, trip readout, IN-CAB NAV / ZOOM / WORLD MAP |
| TruckDeck NAV mod | Factory in-cab GPS screen override (`bare_map.mat`) |
| Paper Sun GPS plugin | Renders the live map into the in-cab screen |

Build the NAV mod: `Html/mod/build_truckdeck_nav.bat`

**Road map (Command Deck mirror GPS / NAV):** generated via the PMTiles pipeline — see `Html/maps/README.txt` and the Map Generator UI at `http://127.0.0.1:25555/tools/map-generator.html`.

## Development

- **Adding Skins**: Create a new folder in `skins/` and follow the structure of existing skins.
- **Modifying Core Logic**: Core functionality is located in `scripts/dashboard-core.js` and `pwa/app.js`.
