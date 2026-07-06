# TruckDeck deployment pack (v1.6.2.5)

Ready-to-upload bundle for **truckdeck.site** VPS and Windows end-user distribution.

## Layout

| Path | Purpose |
|------|---------|
| `AGENT_HANDOFF.md` | Full handoff for landing-page agent (nginx, :25855, downloads) |
| `nginx.conf` | Production vhost — proxy to Flask :25855, static try_files |
| `downloads/` | Public binaries → copy to nginx web root |
| `landing/` | Flask scaffold for https://truckdeck.site |
| `landing-assets/previews/` | Skin splash JPEGs for marketing |
| `release/` | Windows runtime (TruckDeck.exe + Html) |
| `zips/` | Pre-built zip archives |
| `TruckDeck-Setup.exe` | Windows installer |
| `TruckDeck/` | Source tree for rebuild on build machine |

## VPS quick deploy

1. Upload `downloads/`, `landing-assets/`, and `landing/` to `/var/www/veggrowing_g_usr/data/www/truckdeck.site/`
2. Run Flask/gunicorn on `127.0.0.1:25855` (see `landing/README.md`)
3. Ensure nginx matches `nginx.conf` (upstream → :25855)
4. Test `https://truckdeck.site/downloads/TruckDeck-Setup.exe`

## Rebuild on Windows

```powershell
cd "L:\FUNBIT TS4 src\TruckDeck"
.\build\pack_server_handover.ps1
```
