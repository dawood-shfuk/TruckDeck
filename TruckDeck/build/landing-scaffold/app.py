"""
TruckDeck landing site — binds 127.0.0.1:25855 for nginx proxy_pass.
Expand with secure downloads, skin previews, and trucker-themed layout.
"""
from typing import Optional

from flask import Flask, abort, render_template, send_file, url_for

APP_VERSION = os.environ.get("TRUCKDECK_VERSION", "1.6.3.2")
STATIC_ROOT = Path(os.environ.get(
    "TRUCKDECK_STATIC_ROOT",
    "/var/www/veggrowing_g_usr/data/www/truckdeck.site",
))
DOWNLOAD_DIR = STATIC_ROOT / "downloads"
PREVIEW_DIR = STATIC_ROOT / "landing-assets" / "previews"

DOWNLOADS = [
    {
        "id": "setup",
        "title": "TruckDeck Windows Installer",
        "file": "TruckDeck-Setup.exe",
        "description": "Full telemetry server, dashboards, Map Generator, and setup wizard.",
    },
    {
        "id": "apk",
        "title": "TruckDeck Android APK",
        "file": "TruckDeck.apk",
        "description": "Mobile dashboard client — point at your PC server IP.",
    },
    {
        "id": "mod",
        "title": "TruckDeck NAV mod",
        "file": "TruckDeck_NAV.scs",
        "description": "ETS2 cabin GPS mirror mod for TruckDeck NAV.",
    },
]

app = Flask(__name__)
app.config["TRUCKDECK_VERSION"] = APP_VERSION


def file_sha256(path: Path) -> Optional[str]:
    if not path.is_file():
        return None
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def download_meta():
    items = []
    for d in DOWNLOADS:
        path = DOWNLOAD_DIR / d["file"]
        items.append({
            **d,
            "available": path.is_file(),
            "size_mb": round(path.stat().st_size / (1024 * 1024), 1) if path.is_file() else None,
            "sha256": file_sha256(path),
            "url": f"/downloads/{d['file']}",
        })
    return items


@app.after_request
def security_headers(response):
    response.headers["X-Content-Type-Options"] = "nosniff"
    response.headers["X-Frame-Options"] = "SAMEORIGIN"
    response.headers["Referrer-Policy"] = "strict-origin-when-cross-origin"
    return response


@app.route("/")
def index():
    previews = []
    if PREVIEW_DIR.is_dir():
        for jpg in sorted(PREVIEW_DIR.glob("*.jpg")):
            previews.append({
                "name": jpg.stem,
                "url": f"/landing-assets/previews/{jpg.name}",
            })
    return render_template(
        "index.html",
        version=APP_VERSION,
        downloads=download_meta(),
        previews=previews,
    )


@app.route("/downloads")
def downloads_page():
    return render_template("downloads.html", version=APP_VERSION, downloads=download_meta())


@app.route("/health")
def health():
    return {"status": "ok", "version": APP_VERSION}


if __name__ == "__main__":
    app.run(host="127.0.0.1", port=25855, debug=False)
