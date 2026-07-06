# TruckDeck — Agent Handoff (Landing Site & Downloads)

**For:** Agent building the secure public landing page at **https://truckdeck.site**  
**Version:** 1.6.2.5 (from `AssemblyInfo.cs` / `Get-TruckDeckVersion.ps1`)  
**Packed:** 2026-07-06  
**Prepared from:** `L:\FUNBIT TS4 src\TruckDeck\TruckDeck.Server` (source of truth)

---

## 1. What you are building

A **marketing + downloads landing site** on the VPS. This is **not** the in-game telemetry server users run on their gaming PC.

| Component | Where it runs | Port |
|-----------|---------------|------|
| **Landing site (your work)** | VPS `truckdeck.site` | **25855** (backend), **443** (public HTTPS via nginx) |
| **TruckDeck telemetry server** | User's Windows PC | **25555** (HTTP dashboards), **25556** (input bridge) |

Users download **TruckDeck-Setup.exe**, optional **TruckDeck.apk**, and **TruckDeck_NAV.scs** from the landing site, then install and run TruckDeck locally while playing ETS2/ATS.

---

## 2. Production nginx (must follow)

Copy of production config is in this pack: **`nginx.conf`**

### Architecture

```
Browser → https://truckdeck.site (87.106.99.188:443, TLS)
       → nginx root: /var/www/veggrowing_g_usr/data/www/truckdeck.site
       → Static files (*.zip, *.apk, *.scs, *.jpg, *.css, *.js) via try_files
       → Everything else → proxy_pass http://127.0.0.1:25855 (upstream truckdeck.site7)
```

### Key nginx rules (do not break)

1. **`upstream truckdeck.site7`** → `127.0.0.1:25855`
2. **`location /`** → `proxy_pass http://truckdeck.site7` (Flask landing app)
3. **Static extensions** (`zip`, `apk`, `scs`, `jpg`, `png`, `css`, `js`, …) → `try_files $uri $uri/ @fallback` (serve from web root **before** proxy)
4. **`@fallback`** → proxy to `:25855` if static file missing
5. **HTTP → HTTPS** redirect on port 80
6. **HSTS** + **HTTP/2** + **QUIC** already configured — keep them

### Deploy static downloads

Place public binaries in the **nginx web root**:

```
/var/www/veggrowing_g_usr/data/www/truckdeck.site/
  downloads/TruckDeck-Setup.exe
  downloads/TruckDeck.apk          (optional)
  downloads/TruckDeck_NAV.scs      (optional)
  landing-assets/previews/*.jpg    (skin thumbnails)
  landing-assets/previews/app-icon.png
```

URLs become: `https://truckdeck.site/downloads/TruckDeck-Setup.exe` — nginx serves them directly (no Flask hop).

---

## 3. Files in this handover pack

```
TruckDeck_build_1.6.2.5/
├── AGENT_HANDOFF.md          ← this file
├── MANIFEST.txt              ← version + folder map
├── nginx.conf                ← production vhost (reference)
├── README-DEPLOY.md          ← Windows deploy notes
├── downloads/                ← copy to nginx web root
├── landing/                  ← Flask scaffold (start here)
├── landing-assets/previews/  ← dashboard splash JPEGs for marketing
├── release/                  ← full Windows runtime (zip/installer source)
│   ├── TruckDeck/            ← TruckDeck.exe + Html + Plugins
│   └── Extras/               ← APK + NAV mod (if built)
├── zips/
│   ├── TruckDeck-1.6.2.5-release.zip
│   ├── TruckDeck-1.6.2.5-downloads.zip
│   └── TruckDeck-1.6.2.5-handover.zip
├── TruckDeck-Setup.exe       ← Inno Setup installer (if built)
└── TruckDeck/                ← C# + Html source (optional on VPS)
```

---

## 4. Landing page requirements

### Brand & tone
- **Trucker / dispatch themed** — CB radio, “copy that driver”, dark dashboard aesthetic
- Reuse **Rajdhani** font and lime-on-dark palette from `TruckDeck.Server/Html/index-theme.css`
- Show **skin preview images** from `landing-assets/previews/`:
  - TruckDeckDash, Command Deck, NAV, Scania, Volvo, DAF

### Pages / sections
1. **Hero** — What TruckDeck is (ETS2/ATS telemetry → phone/tablet dashboards)
2. **Features** — Live telemetry, OEM skins, NAV + PMTiles, input bridge
3. **Map Generator** — **Desktop app only** (TruckDeck.exe → Map Generator form). Do **not** link to browser `tools/map-generator.html` on the public site.
4. **Downloads** — Secure links to:
   - `TruckDeck-Setup.exe` (primary Windows installer)
   - `TruckDeck.apk` (Android companion)
   - `TruckDeck_NAV.scs` (ETS2 cabin GPS mod)
5. **Version** — Display **1.6.2.5** (read from `VERSION.txt` or env)

### Security (downloads)
Implement at least:
- **HTTPS only** (nginx already terminates TLS)
- **`Content-Disposition: attachment`** on download routes
- **`X-Content-Type-Options: nosniff`**
- **CSP** restricting scripts to self + known CDNs
- Optional: signed download tokens, rate limiting per IP, file hash (SHA-256) displayed on download page
- Never expose server paths, `.env`, or source tree

### Flask app (scaffold in `landing/`)
```bash
cd landing
python3 -m venv .venv
source .venv/bin/activate   # Linux VPS
pip install -r requirements.txt
export TRUCKDECK_VERSION=1.6.2.5
export TRUCKDECK_DOWNLOAD_ROOT=/var/www/veggrowing_g_usr/data/www/truckdeck.site/downloads
python app.py               # binds 127.0.0.1:25855
```

Use **gunicorn** in production:
```bash
gunicorn -w 2 -b 127.0.0.1:25855 app:app
```

---

## 5. TruckDeck product summary (for copy)

**TruckDeck 1.6.2.5** unifies:
- Funbit **TelemetryServer4** foundation (REST `/api/ets2/telemetry`, skin menu)
- **RenCloud** `scs-sdk-plugin` extended fields (hazard, diff lock, route distance, …)
- **TruckSim GPS** plugin path (default `trucksim-gps-telemetry.dll`)
- Native C# **input bridge** (no Python at runtime)
- **9+ TruckDeck skins** + **17 Funbit legacy skins** under `Html/skins/FUNBITskins/`
- **Map Generator** in the Windows app (PMTiles from owned DLC — local only)

### Browser menu (runs on user's PC at `:25555`)
- Tabs: **Home**, **TruckDeck skins**, **Funbit skins**, **Credits**, **Downloads** (`downloads.html`)
- Map Generator removed from browser — desktop app only

---

## 6. Version source of truth

Edit once, rebuild:
```
TruckDeck/TruckDeck.Server/Properties/AssemblyInfo.cs
  AssemblyInformationalVersion("1.6.2.5")

TruckDeck/TruckDeck.Server/Funbit.Ets.Telemetry.Server.csproj
  ApplicationVersion 1.6.2.5
```

Read version in scripts: `TruckDeck/build/Get-TruckDeckVersion.ps1`

---

## 7. Rebuild on Windows (maintainer)

From `FUNBIT TS4 src\TruckDeck`:

```powershell
.\build\deploy.ps1 -SkipSteam -SkipPlugins
.\build\pack_server_handover.ps1
```

Outputs refreshed pack under `TruckDeck_build_1.6.2.5\`.

---

## 8. VPS deployment checklist

- [ ] Upload `downloads/` + `landing-assets/` to nginx web root
- [ ] Deploy `landing/` app, systemd unit on `127.0.0.1:25855`
- [ ] Verify nginx `proxy_pass` to `:25855` for `/`
- [ ] Verify `https://truckdeck.site/downloads/TruckDeck-Setup.exe` returns file (static)
- [ ] Verify landing page loads over HTTPS with correct version
- [ ] Test APK + mod links (404 OK if not built — hide buttons if missing)
- [ ] Do **not** open ports 25555/25556 on the VPS firewall (those are end-user PC only)

---

## 9. Skin authors (Credits tab content)

| Group | Names |
|-------|-------|
| TruckDeck dashboards | Dawood |
| Original server | Funbit, MrMike, Paulo Cunha, Mike |
| Community skins | Klauzzy, Nino Scholz, Jianqun Z., Trinity, Argiano, NightstalkerPL |
| Plugins | SCS Software, RenCloud, nlhans, TruckSim GPS |

---

## 10. Contact / ownership

- **Site:** https://truckdeck.site  
- **Copyright:** TruckDeck 2026  
- **Built on:** Funbit ETS2 Telemetry Server & community skins

---

## 11. What NOT to do

- Do not proxy user telemetry through the VPS landing server
- Do not expose `tools/map-generator.html` on the public site (desktop app only)
- Do not change nginx upstream port from **25855** without updating systemd/Flask bind
- Do not commit SSL private keys or FastPanel paths to public repos

---

*End of handoff — good luck on the landing build.*
