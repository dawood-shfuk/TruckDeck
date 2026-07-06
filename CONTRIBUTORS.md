# Contributors & credits

TruckDeck is **community-built software**. It combines original work, forks, and preserved skins from the ETS2/ATS telemetry dashboard ecosystem. Thank you to everyone below.

---

## TruckDeck (this project)

| Person | Role |
|--------|------|
| **Dawood** | TruckDeck maintainer — server, NAV maps, input bridge, installer, website integration, TruckDeckDash, **Trucker's Command Deck**, **TruckDeck NAV**, OEM-style skins (Volvo, Scania, Mercedes, MAN, DAF, Renault), NAV mod fork packaging |

**Website:** [https://truckdeck.site](https://truckdeck.site)  
**Downloads:** [https://truckdeck.site/downloads](https://truckdeck.site/downloads)

---

## Original telemetry server (Funbit era)

| Person | Contribution |
|--------|----------------|
| **Funbit** | ETS2/ATS telemetry server, mobile dashboard framework, default skins |
| **MrMike** | Original telemetry server co-author |
| **Paulo Cunha** | Job Monitor skins and ongoing community contributions |
| **Mike** | Early telemetry server contributions (acknowledged in Job Monitor skins) |

---

## Community dashboard skins (preserved under `TruckDeck.Server/Html/skins/FUNBITskins/`)

| Author | Skins |
|--------|--------|
| **Klauzzy** | DAF XF, Volvo FH, MAN TGX, Scania, Mercedes Atego |
| **Nino Scholz** | MAN TGX MPH dashboard (with Klauzzy) |
| **Jianqun Z.** | T Dashboard 4.x |
| **Trinity / Trinity4u** | 18-Speed helper, Kenworth K100E |
| **Argiano** | RenaultDash-Info (RD-Info) |
| **NightstalkerPL & Lisek Chytrusek** | Peterbilt 579 (WEBX.PL) |

---

## Telemetry plugins & SDK

| Name | Contribution |
|------|----------------|
| **SCS Software** | Official ETS2/ATS telemetry SDK |
| **RenCloud** | Extended `scs-sdk-plugin` (shared-memory telemetry) |
| **nlhans** | Original ets2-sdk-plugin foundation |
| **TruckSim GPS** | `trucksim-gps-telemetry` plugin and GPS telemetry path |

---

## TruckDeck NAV in-game mod

| Author | Contribution |
|--------|----------------|
| **Paper Sun** | Original **GPS/PC** cabin mod (v1.21) — 3D models, materials, accessory definitions |
| **Dawood** | TruckDeck NAV fork — rebrand, build script, dashboard UI sync, packaging as `TruckDeck_NAV.scs` |

See [`docs/MOD_CHANGELOG.md`](docs/MOD_CHANGELOG.md) and [`reference/paper-sun-gps-pc-mod/`](reference/paper-sun-gps-pc-mod/) for the upstream comparison.

### Community truck packs (accessory targets in NAV mod — not TruckDeck authorship)

XBS, Koral, and other mod authors listed in `MOD_CHANGELOG.md` — truck models remain their work; TruckDeck only maintains GPS/PC accessory hooks.

---

## Open-source libraries

jQuery, SignalR/OWIN, MapLibre GL, PMTiles, Newtonsoft.Json, log4net, Inno Setup, Playwright (dev splash capture), and other dependencies shipped with or used to build TruckDeck. See individual package licenses in `TruckDeck/` solution and `Html/scripts/`.

---

## Preview images

Dashboard splash thumbnails shipped in [`docs/previews/`](docs/previews/) for documentation and [truckdeck.site](https://truckdeck.site). Each skin’s author is credited on the in-app menu card and in this file.

---

*If you contributed and are missing from this list, please open an issue or pull request.*
