"""
TruckDeck landing site — binds 127.0.0.1:25855 for nginx proxy_pass.
Expand with secure downloads, skin previews, and trucker-themed layout.
"""
import os
import json
import hashlib
import secrets
from datetime import timedelta
from pathlib import Path
from typing import Optional

from flask import Flask, abort, render_template, send_file, url_for, redirect, send_from_directory, request, make_response

from admin_bp import admin_bp
from api_bp import api_bp
from crash_bp import crash_bp
from db import init_db
from reviews_bp import reviews_bp
from traffic import record_hit

APP_VERSION = os.environ.get("TRUCKDECK_VERSION", "1.6.5.0")
STATIC_ROOT = Path(os.environ.get(
    "TRUCKDECK_STATIC_ROOT",
    "/var/www/veggrowing_g_usr/data/www/truckdeck.site",
))
DOWNLOAD_DIR = STATIC_ROOT / "downloads"
PREVIEW_DIR = STATIC_ROOT / "landing-assets" / "previews"


def resolve_html_root() -> Path:
    env_root = os.environ.get("TRUCKDECK_HTML_ROOT")
    if env_root:
        return Path(env_root)
    return STATIC_ROOT / "demo-html"


HTML_ROOT = resolve_html_root()
SKINS_ROOT = HTML_ROOT / "skins"
COUNTS_FILE = Path(__file__).parent / "data" / "counts.json"

DEMO_SKIN_ORDER = [
    "TruckDeckDash",
    "truck_command_deck",
    "truckdeck_nav",
    "truckdeck_volvo",
    "truckdeck_scania",
    "truckdeck_mercedes",
    "truckdeck_man",
    "truckdeck_daf",
    "truckdeck_renault",
]

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
        "description": "ETS2 cabin GPS mirror mod for TruckDeck NAV. Subscribe on Steam Workshop (recommended) or download the .scs file directly.",
        "steam_url": "https://steamcommunity.com/sharedfiles/filedetails/?id=3759641869",
        "steam_label": "Get on Steam Workshop",
    },
]

CONTRIBUTORS = [
    {
        "group": "TruckDeck dashboards",
        "members": [
            {"name": "Dawood", "github": "https://github.com/dawood-shfuk"}
        ]
    },
    {
        "group": "Original server",
        "members": [
            {"name": "Funbit", "github": "https://github.com/Funbit"},
            {"name": "MrMike", "github": "https://github.com/mrmike"},
            {"name": "Paulo Cunha", "github": "https://github.com/paulocunha"},
            {"name": "Mike", "github": "https://github.com/mike"}
        ]
    },
    {
        "group": "Community skins",
        "members": [
            {"name": "Klauzzy", "github": "https://github.com/Klauzzy"},
            {"name": "Nino Scholz", "github": "https://github.com/ninoscholz"},
            {"name": "Jianqun Z.", "github": "#"},
            {"name": "Trinity", "github": "#"},
            {"name": "Argiano", "github": "#"},
            {"name": "NightstalkerPL", "github": "#"}
        ]
    },
    {
        "group": "TruckDeck NAV mod",
        "members": [
            {"name": "Paper Sun", "github": "#", "role": "Original GPS/PC mod author"},
            {"name": "Dawood", "github": "https://github.com/dawood-shfuk", "role": "TruckDeck fork, packaging & ETS2 1.50+ updates"},
            {"name": "XBS", "github": "#", "role": "Community truck packs (DAF, MAN, Mercedes, Volvo, Sisu)"},
            {"name": "Koral", "github": "#", "role": "Community truck packs (KamAZ)"},
            {"name": "RJL", "github": "https://steamcommunity.com/workshop/filedetails/?id=2466514780", "role": "Scania G/R/Streamline/T series"},
        ]
    },
    {
        "group": "Plugins",
        "members": [
            {"name": "SCS Software", "github": "https://scssoft.com"},
            {"name": "RenCloud", "github": "https://github.com/RenCloud"},
            {"name": "nlhans", "github": "https://github.com/nlhans"},
            {"name": "TruckSim GPS", "github": "https://github.com/trucksim-gps"}
        ]
    }
]

app = Flask(__name__)
app.config["TRUCKDECK_VERSION"] = APP_VERSION
app.config["SECRET_KEY"] = os.environ.get("TRUCKDECK_SECRET_KEY") or secrets.token_hex(32)
app.config["PERMANENT_SESSION_LIFETIME"] = timedelta(hours=12)
app.config["SESSION_COOKIE_HTTPONLY"] = True
app.config["SESSION_COOKIE_SAMESITE"] = "Lax"
app.config["SESSION_COOKIE_SECURE"] = os.environ.get("TRUCKDECK_HTTPS", "1") == "1"
app.register_blueprint(admin_bp)
app.register_blueprint(api_bp)
app.register_blueprint(reviews_bp)
app.register_blueprint(crash_bp)
init_db()


def file_sha256(path: Path) -> Optional[str]:
    if not path.is_file():
        return None
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def get_counts():
    if not COUNTS_FILE.exists():
        return {}
    try:
        with COUNTS_FILE.open("r") as f:
            return json.load(f)
    except (json.JSONDecodeError, IOError):
        return {}


def increment_count(file_id):
    counts = get_counts()
    counts[file_id] = counts.get(file_id, 0) + 1
    try:
        with COUNTS_FILE.open("w") as f:
            json.dump(counts, f)
    except IOError:
        pass


def find_mock_demos():
    """Collect skin mock.html previews from the TruckDeck Html/skins tree."""
    demos = []
    if not SKINS_ROOT.is_dir():
        return demos

    for mock_path in SKINS_ROOT.rglob("mock.html"):
        skin_dir = mock_path.parent
        rel_skin = skin_dir.relative_to(SKINS_ROOT).as_posix()
        title = rel_skin
        author = ""
        config_path = skin_dir / "config.json"
        if config_path.is_file():
            try:
                with config_path.open("r", encoding="utf-8") as f:
                    cfg = json.load(f).get("config", {})
                title = cfg.get("title", title)
                author = cfg.get("author", "")
            except (json.JSONDecodeError, OSError):
                pass

        preview_url = None
        if (skin_dir / "dashboard.jpg").is_file():
            preview_url = f"/demo/skins/{rel_skin}/dashboard.jpg"

        has_gear_cycle = False
        dash_html = skin_dir / "dashboard.html"
        if dash_html.is_file():
            try:
                has_gear_cycle = "gauge-rpm" in dash_html.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                pass

        demos.append({
            "id": rel_skin,
            "title": title,
            "author": author,
            "url": f"/demo/skins/{rel_skin}/mock.html",
            "preview": preview_url,
            "has_gear_cycle": has_gear_cycle,
        })

    order = {name: idx for idx, name in enumerate(DEMO_SKIN_ORDER)}
    demos.sort(key=lambda d: (order.get(d["id"], 999), d["title"].lower()))
    return demos


def download_meta():
    items = []
    counts = get_counts()
    for d in DOWNLOADS:
        path = DOWNLOAD_DIR / d["file"]
        items.append({
            **d,
            "available": path.is_file(),
            "size_mb": round(path.stat().st_size / (1024 * 1024), 1) if path.is_file() else None,
            "sha256": file_sha256(path),
            "url": url_for("download_waiting", file_id=d["id"]),
            "count": counts.get(d["id"], 0),
        })
    return items


def is_embeddable_asset(path: str) -> bool:
    """Public images/CSS that may be embedded on Steam Workshop etc."""
    path = path.lower()
    if path.startswith("/demo/") or path.startswith("/landing-assets/"):
        return True
    return path.endswith((".jpg", ".jpeg", ".png", ".webp", ".gif", ".css"))


@app.before_request
def track_visit():
    if request.method == "OPTIONS":
        return None
    record_hit(request)


@app.before_request
def cors_preflight():
    if request.method == "OPTIONS" and is_embeddable_asset(request.path):
        response = make_response("", 204)
        response.headers["Access-Control-Allow-Origin"] = "*"
        response.headers["Access-Control-Allow-Methods"] = "GET, HEAD, OPTIONS"
        response.headers["Access-Control-Allow-Headers"] = "*"
        return response


@app.after_request
def security_headers(response):
    response.headers["X-Content-Type-Options"] = "nosniff"
    response.headers["X-Frame-Options"] = "SAMEORIGIN"
    response.headers["Referrer-Policy"] = "strict-origin-when-cross-origin"
    if is_embeddable_asset(request.path):
        response.headers["Access-Control-Allow-Origin"] = "*"
        response.headers["Access-Control-Allow-Methods"] = "GET, HEAD, OPTIONS"
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


@app.route("/contributors")
def contributors_page():
    return render_template("contributors.html", version=APP_VERSION, contributors=CONTRIBUTORS)


@app.route("/live-demo")
def live_demo_page():
    return render_template(
        "live_demo.html",
        version=APP_VERSION,
        demos=find_mock_demos(),
    )


@app.route("/demo/<path:filepath>")
def serve_demo_static(filepath):
    """Serve TruckDeck Html assets (mock pages, scripts, skin CSS/JS)."""
    base = HTML_ROOT.resolve()
    target = (HTML_ROOT / filepath).resolve()
    if not str(target).startswith(str(base)):
        abort(404)
    if not target.is_file():
        abort(404)
    return send_from_directory(target.parent, target.name)


@app.route("/dl/<file_id>")
def download_waiting(file_id):
    file_info = next((d for d in DOWNLOADS if d["id"] == file_id), None)
    if not file_info:
        abort(404)
    
    path = DOWNLOAD_DIR / file_info["file"]
    if not path.is_file():
        abort(404)
    
    # Get metadata for the waiting page
    meta = {
        **file_info,
        "size_mb": round(path.stat().st_size / (1024 * 1024), 1),
        "sha256": file_sha256(path),
    }
    
    return render_template("download_waiting.html", version=APP_VERSION, file=meta)


@app.route("/dl/start/<file_id>")
def download_file(file_id):
    file_info = next((d for d in DOWNLOADS if d["id"] == file_id), None)
    if not file_info:
        abort(404)
    
    path = DOWNLOAD_DIR / file_info["file"]
    if not path.is_file():
        abort(404)
    
    increment_count(file_id)
    return redirect(f"/downloads/{file_info['file']}")


@app.route("/favicon.ico")
def favicon():
    return send_from_directory(app.static_folder, "favicon.png", mimetype="image/png")


@app.route("/health")
def health():
    return {"status": "ok", "version": APP_VERSION}


@app.route("/reviews")
def reviews_page():
    from reviews_bp import _public_stats, list_approved_reviews

    stats = _public_stats()
    approved = [dict(r) for r in list_approved_reviews(100)]
    return render_template(
        "reviews.html",
        version=APP_VERSION,
        stats=stats,
        reviews=approved,
    )


@app.route("/review")
def review_form():
    from reviews_bp import already_reviewed, mint_review_token, verify_review_link

    install_id = request.args.get("install_id", "").strip()
    ts = request.args.get("ts", "").strip()
    sig = request.args.get("sig", "").strip()
    key_b64 = request.args.get("key", "").strip()

    if not install_id:
        abort(400)

    ok, reason = verify_review_link(install_id, ts, sig, key_b64)
    if not ok:
        return render_template("review_error.html", version=APP_VERSION, reason=reason), 403

    if already_reviewed(install_id):
        return render_template("review_error.html", version=APP_VERSION, reason="already_reviewed")

    token = mint_review_token(install_id)
    return render_template("review.html", version=APP_VERSION, token=token)


@app.route("/downloads/<path:filename>")
def serve_static_download(filename):
    return send_from_directory(DOWNLOAD_DIR, filename, as_attachment=True)


if __name__ == "__main__":
    app.run(host="127.0.0.1", port=25855, debug=False)
