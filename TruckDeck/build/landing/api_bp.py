"""Public API routes for TruckDeck desktop clients."""
from __future__ import annotations

from datetime import datetime, timezone

from flask import Blueprint, jsonify

api_bp = Blueprint("api", __name__, url_prefix="/api")


@api_bp.route("/version")
def api_version():
    from app import APP_VERSION

    return jsonify({
        "version": APP_VERSION,
        "download_url": "https://truckdeck.site/downloads/TruckDeck-Setup.exe",
        "release_notes_url": "https://truckdeck.site/downloads",
        "published_at": datetime.now(timezone.utc).isoformat(),
    })
