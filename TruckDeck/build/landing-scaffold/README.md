# TruckDeck landing site scaffold

Minimal Flask app for **truckdeck.site** on `127.0.0.1:25855` behind nginx.

## Quick start (VPS)

```bash
cd landing
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
export TRUCKDECK_VERSION=1.6.2.5
export TRUCKDECK_STATIC_ROOT=/var/www/veggrowing_g_usr/data/www/truckdeck.site
python app.py
```

Production: `gunicorn -w 2 -b 127.0.0.1:25855 app:app`

## nginx

See `../nginx.conf`. Static downloads should live under `$root_path/downloads/` so nginx serves them via `try_files` without hitting Flask.

## Next agent tasks

1. Replace placeholder HTML with full trucker-themed landing (use `../landing-assets/previews/`)
2. Add secure download page with SHA-256 hashes for Setup/APK/mod
3. Add CSP, rate limiting, and optional signed download tokens
4. Do not expose map-generator web UI — mention desktop app only
