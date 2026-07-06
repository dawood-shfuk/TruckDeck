# TruckDeck

**Live ETS2 & ATS telemetry dashboards** for your phone, tablet, or second monitor — OEM-style clusters, **Trucker's Command Deck**, turn-by-turn **NAV**, and classic Funbit skins, all from one Windows server on your gaming PC.

[![Website](https://img.shields.io/badge/website-truckdeck.site-5ad11b)](https://truckdeck.site)
[![Downloads](https://img.shields.io/badge/downloads-Windows%20%7C%20APK%20%7C%20NAV-5ad11b)](https://truckdeck.site/downloads)

**Maintainer:** Dawood · **Version:** 1.6.3.2 · **Site:** [truckdeck.site](https://truckdeck.site)

---

## What is TruckDeck?

TruckDeck unifies:

- **Funbit Telemetry Server** foundation — REST `/api/ets2/telemetry`, browser menu, SignalR
- **RenCloud / TruckSim GPS** extended telemetry — hazard lights, diff lock, route distance, and more
- **Native C# input bridge** — map dashboard buttons to in-game keys (no Python at runtime)
- **9+ TruckDeck skins** + **17 legacy Funbit skins**
- **Map Generator** (desktop app) — offline PMTiles from your owned DLC
- **TruckDeck NAV** — web routing + ETS2 cabin GPS mirror mod (`TruckDeck_NAV.scs`)

```
ETS2/ATS  →  telemetry plugin  →  TruckDeck.exe (:25555)
                                      ├── HTML dashboards
                                      └── Input bridge (:25556)
```

---

## Quick start (users)

1. Download **[TruckDeck-Setup.exe](https://truckdeck.site/downloads/TruckDeck-Setup.exe)** from [truckdeck.site/downloads](https://truckdeck.site/downloads)
2. Run the installer as Administrator; point at your ETS2/ATS Steam folder (leave blank for a game you do not own)
3. Launch **TruckDeck**, start ETS2 or ATS, open `http://127.0.0.1:25555/` on your phone (use the IP shown in the app)

Optional: [Android APK](https://truckdeck.site/downloads/TruckDeck.apk) · [NAV mod](https://truckdeck.site/downloads/TruckDeck_NAV.scs)

---

## Build from source (developers)

**Requirements:** Windows 10/11 x64, .NET Framework 4.8, Visual Studio 2022 + MSBuild, optional VC++ for plugins

```powershell
cd TruckDeck
nuget restore TruckDeck.sln
.\build\build_plugins.ps1
.\build\deploy.ps1 -SkipSteam -SkipPlugins
.\build\build_installer.ps1
```

Output: `..\TruckDeck_build_1.6.3.2\TruckDeck-Setup.exe` (sibling to this repo layout on a full dev machine)

See [`TruckDeck/README.md`](TruckDeck/README.md) for API endpoints, Map Generator, and plugin details.

---

## Repository layout

| Path | Purpose |
|------|---------|
| [`TruckDeck/`](TruckDeck/) | Main server, Html skins, build scripts |
| [`scs-sdk-plugin/`](scs-sdk-plugin/) | RenCloud telemetry plugin (C++) |
| [`trucksim-gps-plugin/`](trucksim-gps-plugin/) | TruckSim GPS plugin (C++) |
| [`reference/paper-sun-gps-pc-mod/`](reference/paper-sun-gps-pc-mod/) | Original Paper Sun GPS/PC mod (attribution reference) |
| [`docs/previews/`](docs/previews/) | Dashboard splash images for docs & marketing |
| [`docs/MOD_CHANGELOG.md`](docs/MOD_CHANGELOG.md) | NAV mod fork changelog |
| [`CONTRIBUTORS.md`](CONTRIBUTORS.md) | **Full credits** — please read before redistributing |
| [`SUPPORT.md`](SUPPORT.md) | Support & donate links |
| [`nginx.conf`](nginx.conf) | Reference vhost for [truckdeck.site](https://truckdeck.site) |

---

## TruckDeck dashboards (preview)

Splash thumbnails in [`docs/previews/`](docs/previews/):

| Skin | Description |
|------|-------------|
| **TruckDeckDash** | Big Rig Cluster |
| **truck_command_deck** | Trucker’s Command Deck — full cab control grid |
| **truckdeck_nav** | Turn-by-turn NAV + map mirror |
| **truckdeck_volvo / scania / mercedes / man / daf / renault** | OEM-style brand dashboards |

Live mock (no game required): run TruckDeck and open  
`/skins/truck_command_deck/mock.html`

---

## Credits

TruckDeck builds on years of community work. **Funbit**, **MrMike**, **Paulo Cunha**, community skin authors, **RenCloud**, **SCS Software**, **Paper Sun** (NAV mod base), and many others — see **[CONTRIBUTORS.md](CONTRIBUTORS.md)**.

Skin authors are also credited in-app on the **Credits** tab and on each dashboard card.

---

## Support development

TruckDeck is free and community-driven. If it helps you drive, please see **[SUPPORT.md](SUPPORT.md)** for:

- [truckdeck.site](https://truckdeck.site)
- [Downloads](https://truckdeck.site/downloads)
- PayPal donation tiers (hosting, features, long-term maintenance)

---

## License

See [LICENSE](LICENSE). Third-party and upstream components retain their original terms — see [CONTRIBUTORS.md](CONTRIBUTORS.md) and [THIRD_PARTY.md](THIRD_PARTY.md).

---

*Built with care for the trucking community. Copy that, driver.*
