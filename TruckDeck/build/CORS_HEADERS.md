# CORS headers for truckdeck.site (Steam Workshop images)

Steam Workshop descriptions that embed external images with `[img]…[/img]` load those URLs from **`https://steamcommunity.com`**. If the image host does not return CORS headers, the browser blocks the request and previews fail.

## Symptom

Browser console on a Workshop item page:

```
Access to image at 'https://truckdeck.site/demo/skins/truckdeck_nav/dashboard.jpg'
from origin 'https://steamcommunity.com' has been blocked by CORS policy:
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

Affected URLs (Workshop description in `mod/workshop/workshop_description.txt`):

- `https://truckdeck.site/demo/skins/*/dashboard.jpg`
- Any other `jpg` / `png` / `webp` used in Steam BBCode from truckdeck.site

Normal browsing on truckdeck.site is unaffected — CORS only matters when another site (Steam) embeds your assets.

## Required response headers

For public embeddable images, add at minimum:

```http
Access-Control-Allow-Origin: *
```

Optional (only if something sends `OPTIONS` preflight):

```http
Access-Control-Allow-Methods: GET, HEAD, OPTIONS
Access-Control-Allow-Headers: *
```

`Access-Control-Allow-Origin: *` is appropriate here: dashboard preview JPEGs are already public. Do **not** use `*` on authenticated or private API routes.

## nginx (production — preferred)

On the VPS, static files under the web root are usually served by nginx **`try_files`** before Flask (`:25855`). CORS must be set on nginx for those static paths, not only on Flask.

Web root:

```
/var/www/veggrowing_g_usr/data/www/truckdeck.site/
```

### Option A — demo preview images only (narrowest)

Inside the `server { … }` block for `truckdeck.site`:

```nginx
location ^~ /demo/ {
    root /var/www/veggrowing_g_usr/data/www/truckdeck.site;
    try_files $uri $uri/ @fallback;

    add_header Access-Control-Allow-Origin "*" always;
    add_header Access-Control-Allow-Methods "GET, HEAD, OPTIONS" always;

    if ($request_method = OPTIONS) {
        return 204;
    }
}
```

### Option B — all public images (broader)

If Workshop or other embeds use paths under `/landing-assets/` etc.:

```nginx
location ~* \.(jpg|jpeg|png|gif|webp)$ {
    add_header Access-Control-Allow-Origin "*" always;
    add_header Access-Control-Allow-Methods "GET, HEAD, OPTIONS" always;

    if ($request_method = OPTIONS) {
        return 204;
    }

    try_files $uri $uri/ @fallback;
}
```

Place this **before** or **instead of** duplicating rules — match your existing `try_files` / `@fallback` layout from `nginx.conf`. Use `always` so headers are sent on success **and** error responses.

### Apply

```bash
sudo nginx -t
sudo systemctl reload nginx
```

## Flask fallback (if nginx proxies to :25855)

If a file is missing on disk and nginx falls through to `@fallback` → Flask, add CORS in the landing app for image responses:

```python
@app.after_request
def cors_for_embeddable_assets(response):
    path = request.path.lower()
    if path.startswith("/demo/") or path.endswith((".jpg", ".jpeg", ".png", ".webp", ".gif")):
        response.headers["Access-Control-Allow-Origin"] = "*"
        response.headers["Access-Control-Allow-Methods"] = "GET, HEAD, OPTIONS"
    return response
```

Restart gunicorn/Flask after changing `landing/app.py`.

## Verify

From any machine:

```bash
curl -sI "https://truckdeck.site/demo/skins/truckdeck_nav/dashboard.jpg" | grep -i access-control
```

Expected:

```
Access-Control-Allow-Origin: *
```

Then reload the Steam Workshop item page (hard refresh). Images in the description should render.

## Workshop description

No change to BBCode URLs is required once CORS is fixed. Image sources stay:

```bbcode
[img]https://truckdeck.site/demo/skins/truckdeck_nav/dashboard.jpg[/img]
```

## Related files

| File | Role |
|------|------|
| `nginx.conf` (repo root / handover pack) | Production vhost reference |
| `build/AGENT_HANDOFF_LANDING.md` | VPS layout, ports, static vs proxy |
| `mod/workshop/workshop_description.txt` | Steam description using demo JPEGs |

## Security note

Restrict CORS `*` to **public static assets** (images, optional public CSS). Keep API routes (`/api/…`), downloads you want to track, and admin paths on default same-origin policy unless you have a specific cross-origin need.
