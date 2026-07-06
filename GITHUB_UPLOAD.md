# GitHub upload — manual steps

**Profile:** [github.com/dawood-shfuk](https://github.com/dawood-shfuk)  
**Repo URL (after create):** [github.com/dawood-shfuk/TruckDeck](https://github.com/dawood-shfuk/TruckDeck)

Local folder is ready: commit exists, remote is set. You only create the empty repo on GitHub and push.

---

## Quick upload

1. **Create repo** at [github.com/new](https://github.com/new) → name `TruckDeck` → **empty** (no README / .gitignore / license)
2. **Double-click** [`MANUAL_UPLOAD.bat`](MANUAL_UPLOAD.bat) in this folder, **or** run:

```powershell
cd "L:\FUNBIT TS4 src\git source"
git push -u origin main
```

Full troubleshooting and post-upload settings: **[MANUAL_UPLOAD.txt](MANUAL_UPLOAD.txt)**

---

## After first push

| Setting | Value |
|--------|--------|
| Website | `https://truckdeck.site` |
| Issues | On |
| Topics | `ets2`, `ats`, `telemetry`, `euro-truck-simulator` |

Optional profile README: copy [`docs/PROFILE_README.md`](docs/PROFILE_README.md) into a repo named `dawood-shfuk` (same as your username).

---

## Re-pack after local changes

```powershell
cd "L:\FUNBIT TS4 src\TruckDeck"
.\build\pack_github.ps1

cd "L:\FUNBIT TS4 src\git source"
git add .
git commit -m "Your change summary"
git push
```

Preserves root `README.md`, `CONTRIBUTORS.md`, `SUPPORT.md`, `LICENSE`, `.gitignore` unless you delete them first.

---

## Included / excluded

**Included:** `TruckDeck/` source, plugin sources, reference mod, preview JPEGs, docs, support links, `nginx.conf`

**Not included:** `TruckDeck-Setup.exe` ([truckdeck.site/downloads](https://truckdeck.site/downloads)), `*.pmtiles`, `TruckDeck_build_*` folders
