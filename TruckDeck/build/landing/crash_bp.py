"""Crash report ingestion from TruckDeck desktop clients."""
from __future__ import annotations

from datetime import datetime, timezone

from flask import Blueprint, abort, jsonify, request

from db import get_db, init_db
from signing import verify_signed_request

crash_bp = Blueprint("crash_api", __name__, url_prefix="/api/v1/crash-reports")

MAX_STACK = 16 * 1024
MAX_LOG_TAIL = 8 * 1024
MAX_PER_DAY = 3


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _get_install(install_id: str):
    with get_db() as conn:
        return conn.execute(
            "SELECT * FROM installs WHERE install_id = ?",
            (install_id,),
        ).fetchone()


@crash_bp.record_once
def _init(state):
    init_db()


@crash_bp.route("", methods=["POST"])
def submit_crash():
    install_id = request.headers.get("X-Install-Id", "").strip()
    row = _get_install(install_id)
    if not row:
        abort(403, "install not registered")

    ok, err = verify_signed_request(request, row["key_hash"])
    if not ok:
        abort(403, err)

    data = request.get_json(silent=True) or {}
    app_version = str(data.get("app_version", ""))[:32]
    os_version = str(data.get("os_version", ""))[:128]
    exception_type = str(data.get("exception_type", ""))[:256]
    message = str(data.get("message", ""))[:1024]
    stack_trace = str(data.get("stack_trace", ""))[:MAX_STACK]
    log_tail = str(data.get("log_tail", ""))[:MAX_LOG_TAIL]

    with get_db() as conn:
        since = datetime.now(timezone.utc).replace(hour=0, minute=0, second=0, microsecond=0).isoformat()
        count = conn.execute(
            "SELECT COUNT(*) AS c FROM crash_reports WHERE install_id = ? AND created_at >= ?",
            (install_id, since),
        ).fetchone()["c"]
        if count >= MAX_PER_DAY:
            abort(429, "daily crash report limit")

        conn.execute(
            """INSERT INTO crash_reports
               (install_id, app_version, os_version, exception_type, message, stack_trace, log_tail, created_at)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
            (install_id, app_version, os_version, exception_type, message, stack_trace, log_tail, _utc_now()),
        )

    return jsonify({"ok": True})


def list_crash_reports(limit: int = 50):
    with get_db() as conn:
        return conn.execute(
            """SELECT id, install_id, app_version, os_version, exception_type, message, created_at
               FROM crash_reports ORDER BY created_at DESC LIMIT ?""",
            (limit,),
        ).fetchall()


def get_crash_report(report_id: int):
    with get_db() as conn:
        return conn.execute(
            "SELECT * FROM crash_reports WHERE id = ?",
            (report_id,),
        ).fetchone()
