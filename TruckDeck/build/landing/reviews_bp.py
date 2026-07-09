"""Review registration, review-link verification, submit, and public list."""
from __future__ import annotations

import base64
import hashlib
import hmac
import re
import secrets
import time
from datetime import datetime, timezone

from flask import Blueprint, abort, jsonify, render_template, request

from db import get_db, init_db
from signing import hash_install_key

reviews_bp = Blueprint("reviews_api", __name__, url_prefix="/api/v1/reviews")

TOKEN_TTL_SEC = 900
MAX_COMMENT_LEN = 500
# Feedback links (?install_id=&ts=&sig=&key=) are self-contained proof of a
# registered install — no usage-threshold gate. Still time-boxed so an old
# screenshot/shared link can't be replayed indefinitely.
MAX_LINK_SKEW_SEC = 1800


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _clean_comment(text: str | None) -> str:
    if not text:
        return ""
    text = text.strip()[:MAX_COMMENT_LEN]
    try:
        import bleach

        return bleach.clean(text, tags=[], strip=True)
    except ImportError:
        return re.sub(r"<[^>]+>", "", text)


def _get_install(install_id: str):
    with get_db() as conn:
        return conn.execute(
            "SELECT * FROM installs WHERE install_id = ?",
            (install_id,),
        ).fetchone()


def _public_stats():
    with get_db() as conn:
        rows = conn.execute(
            "SELECT stars FROM reviews WHERE status = 'approved'"
        ).fetchall()
    if not rows:
        return {"avg": 0.0, "count": 0, "histogram": {str(i): 0 for i in range(1, 6)}}
    stars = [r["stars"] for r in rows]
    hist = {str(i): 0 for i in range(1, 6)}
    for s in stars:
        if 1 <= s <= 5:
            hist[str(s)] = hist.get(str(s), 0) + 1
    return {
        "avg": round(sum(stars) / len(stars), 1),
        "count": len(stars),
        "histogram": hist,
    }


@reviews_bp.record_once
def _init(state):
    init_db()


@reviews_bp.route("/register", methods=["POST"])
def register_install():
    data = request.get_json(silent=True) or {}
    install_id = str(data.get("install_id", "")).strip()
    install_key = str(data.get("install_key", "")).strip()
    platform = str(data.get("platform", "windows")).strip() or "windows"
    app_version = str(data.get("app_version", "")).strip()

    if not install_id or len(install_id) < 8 or not install_key or len(install_key) < 16:
        abort(400, "invalid install identity")

    now = _utc_now()
    key_hash = hash_install_key(install_key)
    with get_db() as conn:
        existing = conn.execute(
            "SELECT install_id FROM installs WHERE install_id = ?",
            (install_id,),
        ).fetchone()
        if existing:
            conn.execute(
                "UPDATE installs SET last_seen = ?, app_version = ? WHERE install_id = ?",
                (now, app_version, install_id),
            )
        else:
            conn.execute(
                """INSERT INTO installs (install_id, key_hash, platform, app_version, first_seen, last_seen)
                   VALUES (?, ?, ?, ?, ?, ?)""",
                (install_id, key_hash, platform, app_version, now, now),
            )
    return jsonify({"ok": True})


def verify_review_link(install_id: str, ts: str, sig: str, key_b64: str):
    """Verifies a self-contained feedback-card link (see AGENT_HANDOFF_REVIEW_LINK.md).

    Unlike verify_signed_request (used for app->server POSTs with an
    X-Install-Key header), this reads the equivalent proof from query params
    because a browser GET can't carry custom headers.
    """
    row = _get_install(install_id)
    if not row:
        return False, "install_not_registered"

    if not (ts and sig and key_b64):
        return False, "missing_link_params"

    try:
        install_key = base64.b64decode(key_b64).decode("utf-8")
    except Exception:
        return False, "invalid_key"

    if hash_install_key(install_key) != row["key_hash"]:
        return False, "key_mismatch"

    try:
        ts_val = int(ts)
    except ValueError:
        return False, "invalid_timestamp"

    if abs(time.time() - ts_val) > MAX_LINK_SKEW_SEC:
        return False, "link_expired"

    expected = hmac.new(
        install_key.encode("utf-8"), (ts + "\n" + install_id).encode("utf-8"), hashlib.sha256
    ).hexdigest()
    if not hmac.compare_digest(expected, sig.strip().lower()):
        return False, "bad_signature"

    return True, ""


def already_reviewed(install_id: str) -> bool:
    with get_db() as conn:
        row = conn.execute(
            "SELECT id FROM reviews WHERE install_id = ?",
            (install_id,),
        ).fetchone()
    return row is not None


def mint_review_token(install_id: str) -> str:
    token = secrets.token_urlsafe(32)
    expires = time.time() + TOKEN_TTL_SEC
    with get_db() as conn:
        conn.execute(
            "INSERT INTO review_tokens (token, install_id, expires_at, used) VALUES (?, ?, ?, 0)",
            (token, install_id, expires),
        )
    return token


@reviews_bp.route("/submit", methods=["POST"])
def submit_review():
    data = request.get_json(silent=True) or {}
    token = str(data.get("token", "")).strip()
    stars = int(data.get("stars", 0))
    comment = _clean_comment(data.get("comment"))
    display_name = _clean_comment(data.get("display_name"))[:64] or "Verified TruckDeck user"
    consent = bool(data.get("consent"))
    app_version = str(data.get("app_version", "")).strip()

    if not token or not consent:
        abort(400, "token and consent required")
    if stars < 1 or stars > 5:
        abort(400, "stars must be 1-5")

    with get_db() as conn:
        tok = conn.execute(
            "SELECT * FROM review_tokens WHERE token = ?",
            (token,),
        ).fetchone()
        if not tok or tok["used"]:
            abort(400, "invalid token")
        if time.time() > tok["expires_at"]:
            abort(400, "token expired")

        install_id = tok["install_id"]
        existing = conn.execute(
            "SELECT id FROM reviews WHERE install_id = ?",
            (install_id,),
        ).fetchone()
        if existing:
            abort(409, "already reviewed")

        status = "approved" if stars >= 4 and len(comment) <= 200 else "pending"
        now = _utc_now()
        conn.execute(
            """INSERT INTO reviews (install_id, stars, comment, display_name, app_version, status, created_at, approved_at)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                install_id,
                stars,
                comment or None,
                display_name,
                app_version,
                status,
                now,
                now if status == "approved" else None,
            ),
        )
        conn.execute(
            "UPDATE review_tokens SET used = 1 WHERE token = ?",
            (token,),
        )

    return jsonify({"ok": True, "status": status})


@reviews_bp.route("/public")
def public_reviews():
    with get_db() as conn:
        rows = conn.execute(
            """SELECT stars, comment, display_name, created_at, app_version
               FROM reviews WHERE status = 'approved'
               ORDER BY created_at DESC LIMIT 100"""
        ).fetchall()

    stats = _public_stats()
    reviews = [
        {
            "stars": r["stars"],
            "comment": r["comment"] or "",
            "display_name": r["display_name"] or "Verified TruckDeck user",
            "created_at": r["created_at"],
            "app_version": r["app_version"] or "",
            "verified": True,
        }
        for r in rows
    ]
    return jsonify({**stats, "reviews": reviews})


def list_pending_reviews():
    with get_db() as conn:
        return conn.execute(
            """SELECT id, install_id, stars, comment, display_name, app_version, status, created_at
               FROM reviews WHERE status = 'pending' ORDER BY created_at DESC"""
        ).fetchall()


def list_approved_reviews(limit: int = 100):
    with get_db() as conn:
        return conn.execute(
            """SELECT stars, comment, display_name, created_at, app_version, status
               FROM reviews WHERE status = 'approved'
               ORDER BY created_at DESC LIMIT ?""",
            (limit,),
        ).fetchall()


def list_all_reviews(limit: int = 50):
    with get_db() as conn:
        return conn.execute(
            """SELECT id, install_id, stars, comment, display_name, status, created_at
               FROM reviews ORDER BY created_at DESC LIMIT ?""",
            (limit,),
        ).fetchall()


def moderate_review(review_id: int, approve: bool) -> bool:
    status = "approved" if approve else "rejected"
    now = _utc_now()
    with get_db() as conn:
        cur = conn.execute(
            "UPDATE reviews SET status = ?, approved_at = ? WHERE id = ? AND status = 'pending'",
            (status, now if approve else None, review_id),
        )
        return cur.rowcount > 0
