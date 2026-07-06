# GitHub upload checklist

1. Create a new repository on GitHub (e.g. `TruckDeck` or `truckdeck-telemetry`)
2. From this folder:

```powershell
cd "L:\FUNBIT TS4 src\git source"
git init
git add .
git commit -m "TruckDeck 1.6.3.2 — open source release"
git branch -M main
git remote add origin https://github.com/YOUR_USER/YOUR_REPO.git
git push -u origin main
```

3. In GitHub repo settings, add website URL: **https://truckdeck.site**
4. Pin **README.md** — it links downloads and [SUPPORT.md](SUPPORT.md)
5. Enable Issues for bug reports

## Re-pack after local changes

```powershell
cd "L:\FUNBIT TS4 src\TruckDeck"
.\build\pack_github.ps1
```

Preserves root `README.md`, `CONTRIBUTORS.md`, `SUPPORT.md`, `LICENSE`, `.gitignore` unless you delete them first.

## What is included

- Full `TruckDeck/` source (no `server/` build output, no `*.pmtiles`)
- Plugin sources (`scs-sdk-plugin`, `trucksim-gps-plugin`)
- Paper Sun reference mod (`reference/paper-sun-gps-pc-mod/`)
- Dashboard preview JPEGs (`docs/previews/`)
- Credits, support links, nginx reference

## What is NOT included (by design)

- `TruckDeck-Setup.exe` — distribute via [truckdeck.site/downloads](https://truckdeck.site/downloads)
- Generated map tiles (`*.pmtiles`) — create on your PC with Map Generator
- `TruckDeck_build_*` release folders
