# TruckDeck — Agent Handoff (v1.6.3.2)

**For:** Next agent continuing TruckDeck development or deployment  
**Version:** **1.6.3.2** (single source of truth: `TruckDeck.Server\Properties\AssemblyInfo.cs`)  
**Prepared:** 2026-07-06  
**Source root:** `L:\FUNBIT TS4 src\TruckDeck`

This document summarizes **functional and UX changes in 1.6.3.2**. Donation / PayPal / support-board UI is intentionally omitted here (present in the product but out of scope for this handoff).

---

## 1. Version bump

| Location | Value |
|----------|--------|
| `TruckDeck.Server\Properties\AssemblyInfo.cs` | `1.6.3.2` |
| `TruckDeck.Server\Funbit.Ets.Telemetry.Server.csproj` | `ApplicationVersion` → `1.6.3.2` |
| `VERSION.txt` | `1.6.3.2` |
| `build\TruckDeckSetup.iss` | `#define MyAppVersion "1.6.3.2"` |
| `TruckDeck.Server\Html\android_app\app\build.gradle` | `versionName "1.6.3.2"` |
| Build output folder | `L:\FUNBIT TS4 src\TruckDeck_build_1.6.3.2\` (via `Get-TruckDeckBuildRoot.ps1`) |

Read version in scripts: `.\build\Get-TruckDeckVersion.ps1`

---

## 2. What changed in 1.6.3.2 (excluding support UI)

### 2.1 Browser downloads page (`TruckDeck.Server\Html\downloads.html` + `downloads.js`)

- **Official download fallback:** If installer, APK, or NAV mod are not on the local PC server, buttons link to `https://truckdeck.site/downloads/` (HTTPS).
- **Windows installer card** added alongside APK and NAV mod.
- **Smart probe:** `downloads.js` HEAD-checks local paths first (`TruckDeck-Setup.exe`, `Extras/`, `mod/`); buttons are never disabled.
- **Layout:** `downloads-wrap` uses flex column with `gap: 14px` between hero, installer card, and APK/NAV grid.
- **Menu links:** `index.html` footer and Home tab point to downloads page and `truckdeck.site`.

### 2.2 Windows app main panel (`MainForm.cs` + `MainForm.Designer.cs`)

- **Auto network IP (DHCP):** Removed manual adapter dropdown. `NetworkHelper.GetPreferredNetworkInterface()` scores adapters (private LAN, DHCP, Ethernet/Wi‑Fi; penalizes virtual/WSL). Refreshes every 8s via `networkRefreshTimer`.
- **CAB LINKS buttons:**
  - **OPEN DASHBOARD** moved from bottom action bar into CAB LINKS card.
  - **OPEN REST API** new button opens `/api/ets2/telemetry`.
  - Raw URL link labels removed; URLs stored internally (`_dashboardUrl`, `_apiUrl`).
- **Removed COPY IP** button (IP shown automatically under NETWORK).
- **Bottom bar** slimmed to minimize / uninstall links only.

### 2.3 Uninstall (TruckDeck + Windows Apps)

- **Windows uninstaller (Inno):** `[UninstallRun]` calls `TruckDeck.exe -uninstall` which now runs **silent cleanup** via `UninstallBootstrap.RunSilent()` — no extra “Uninstall” click, no main window.
- **In-app Uninstall link:** Launches registered Windows uninstaller (`WindowsUninstallHelper` reads Inno registry `AppId`). Portable installs fall back to `-uninstall -interactive` wizard.
- **Cleanup steps:** TruckSim GPS plugin backup (`.bak`), firewall rules (25555/25556), URL ACL rules, `%LocalAppData%\TruckDeck\` settings.
- **Fixes:** `FirewallSetup.Uninstall` and `UrlReservationSetup.Uninstall` now set `_status` correctly.

**New files:** `UninstallBootstrap.cs`, `Helpers\WindowsUninstallHelper.cs`

### 2.4 Installer (`build\TruckDeckSetup.iss`) — carried from 1.6.2.x, still relevant

- ETS2/ATS path pages allow **blank** if user does not own a game.
- Steam library autodetect via `libraryfolders.vdf` + drive scan.
- Game paths passed via `{app}\.truckdeck-install.ini` + `TruckDeck.exe -install -frominstaller` (avoids Inno quoting bugs and `eurotrucks2.exe` error 740).
- `[UninstallRun]` includes `RunOnceId: "TruckDeckCleanup"`.

### 2.5 NAV mod changelog

- `TruckDeck.Server\Html\mod\MOD_CHANGELOG.md` — fork vs original Paper Sun mod (not a runtime feature; docs only).

### 2.6 Trucker’s Command Deck (TCD) button padding

- **Skin:** `Html/skins/truck_command_deck/dashboard.css`
- **Change:** All grid `control-btn` buttons (lights, brakes, cameras, radio, system, NAV, PC mouse keys, etc.) use **`--control-btn-pad: 5px`** internal padding (was ~2–4px clamp).
- **Mock preview (live demo):** `http://<server>:25555/skins/truck_command_deck/mock.html` — fake telemetry, no game required. Use for landing “Dashboard Live Demo” iframe or marketing screenshots.
- **Splash thumbnail:** Regenerate `dashboard.jpg` from mock if updating `landing-assets/previews/truck_command_deck.jpg` on VPS.

---

## 3. Key paths

| Area | Path |
|------|------|
| C# server source | `TruckDeck\TruckDeck.Server\` |
| Html (skins, downloads, tools) | `TruckDeck.Server\Html\` → synced to `TruckDeck\server\Html\` on deploy |
| Runtime after deploy | `TruckDeck\server\TruckDeck.exe` |
| Installer script | `TruckDeck\build\TruckDeckSetup.iss` |
| Build output | `TruckDeck_build_1.6.3.2\` |
| VPS landing handoff (separate) | `build\AGENT_HANDOFF_LANDING.md` |

---

## 4. Build commands (Windows)

```powershell
cd "L:\FUNBIT TS4 src\TruckDeck"
.\build\deploy.ps1 -SkipSteam -SkipPlugins
.\build\build_installer.ps1
.\build\pack_server_handover.ps1   # optional VPS pack
```

Outputs:

- `TruckDeck_build_1.6.3.2\TruckDeck-Setup.exe`
- `TruckDeck_build_1.6.3.2\release\TruckDeck\` (staged runtime)

---

## 5. Runtime architecture (unchanged)

| Component | Host | Port |
|-----------|------|------|
| Telemetry + dashboards | User PC | **25555** |
| Input bridge | User PC | **25556** |
| Public site | VPS `truckdeck.site` | **443** → Flask **25855** |

Users install from `https://truckdeck.site/downloads` or local `downloads.html`; telemetry server runs only on their gaming PC.

---

## 6. Testing checklist for next agent

- [ ] `downloads.html` — local files served when present; otherwise `truckdeck.site` links work.
- [ ] Main panel — adapter line shows `Name · DHCP`; IP updates without manual selection.
- [ ] **OPEN DASHBOARD** / **OPEN REST API** open correct URLs.
- [ ] **Windows → Apps → Uninstall** — silent cleanup, then files removed; close ETS2/ATS first.
- [ ] In-app **Uninstall** — launches Windows uninstaller (Inno install).
- [ ] Fresh install with only ETS2 or only ATS (blank other game path).
- [ ] Version **1.6.3.2** in exe, menu footer, downloads page (`%TRUCKDECK_VERSION%` after deploy).

---

## 7. Out of scope (do not document as product changes here)

- Donation board on `downloads.html` (`donations.js`, PayPal SDK, QR images under `images/donate/`).
- SUPPORT card on WinForms main panel (PayPal tier buttons).
- VPS landing page polish (see `AGENT_HANDOFF_LANDING.md`).

---

## 8. What NOT to break

- Version must stay in sync: `AssemblyInfo.cs` → deploy substitutes `%TRUCKDECK_VERSION%` in Html.
- Inno `AppId` must match `WindowsUninstallHelper.InnoAppId` for in-app uninstall.
- `-uninstall` without `-interactive` must remain silent for Windows uninstaller.
- Map Generator remains **desktop app only** (not public browser tool on VPS).

---

*End of handoff — v1.6.3.2*
