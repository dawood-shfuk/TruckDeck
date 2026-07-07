"""SQLite storage for reviews, tokens, and crash reports."""
from __future__ import annotations

import os
import sqlite3
from contextlib import contextmanager
from datetime import datetime, timezone
from pathlib import Path

DATA_DIR = Path(__file__).parent / "data"
DB_PATH = Path(os.environ.get("TRUCKDECK_DB_PATH", str(DATA_DIR / "truckdeck.db")))


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def init_db() -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    with get_db() as conn:
        conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS installs (
                install_id TEXT PRIMARY KEY,
                key_hash TEXT NOT NULL,
                platform TEXT NOT NULL DEFAULT 'windows',
                app_version TEXT,
                first_seen TEXT NOT NULL,
                last_seen TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS reviews (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                install_id TEXT NOT NULL UNIQUE,
                stars INTEGER NOT NULL,
                comment TEXT,
                display_name TEXT,
                app_version TEXT,
                status TEXT NOT NULL DEFAULT 'pending',
                created_at TEXT NOT NULL,
                approved_at TEXT
            );
            CREATE TABLE IF NOT EXISTS review_tokens (
                token TEXT PRIMARY KEY,
                install_id TEXT NOT NULL,
                expires_at REAL NOT NULL,
                used INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS crash_reports (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                install_id TEXT,
                app_version TEXT,
                os_version TEXT,
                exception_type TEXT,
                message TEXT,
                stack_trace TEXT,
                log_tail TEXT,
                created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_reviews_status ON reviews(status);
            CREATE INDEX IF NOT EXISTS idx_crash_install ON crash_reports(install_id, created_at);
            """
        )


@contextmanager
def get_db():
    conn = sqlite3.connect(DB_PATH, timeout=10)
    conn.row_factory = sqlite3.Row
    try:
        yield conn
        conn.commit()
    finally:
        conn.close()
