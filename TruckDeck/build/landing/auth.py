"""Admin authentication — single admin, one-time registration."""
import json
import secrets
import time
from datetime import datetime, timezone
from functools import wraps
from pathlib import Path
from typing import Optional

from flask import abort, redirect, request, session, url_for
from werkzeug.security import check_password_hash, generate_password_hash

DATA_DIR = Path(__file__).parent / "data"
ADMIN_FILE = DATA_DIR / "admin.json"
MIN_PASSWORD_LEN = 12
MAX_LOGIN_ATTEMPTS = 5
LOGIN_WINDOW_SEC = 900

_login_attempts: dict[str, list[float]] = {}


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def admin_exists() -> bool:
    if not ADMIN_FILE.is_file():
        return False
    try:
        data = json.loads(ADMIN_FILE.read_text(encoding="utf-8"))
        return bool(data.get("username") and data.get("password_hash"))
    except (json.JSONDecodeError, OSError):
        return False


def load_admin() -> Optional[dict]:
    if not ADMIN_FILE.is_file():
        return None
    try:
        return json.loads(ADMIN_FILE.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError):
        return None


def create_admin(username: str, password: str) -> None:
    if admin_exists():
        raise ValueError("Admin account already exists")
    username = username.strip()
    if len(username) < 3:
        raise ValueError("Username must be at least 3 characters")
    if len(password) < MIN_PASSWORD_LEN:
        raise ValueError(f"Password must be at least {MIN_PASSWORD_LEN} characters")

    payload = {
        "username": username,
        "password_hash": generate_password_hash(password, method="scrypt"),
        "created_at": _utc_now(),
    }
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    ADMIN_FILE.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    try:
        ADMIN_FILE.chmod(0o600)
    except OSError:
        pass


def update_password(current_password: str, new_password: str) -> None:
    admin = load_admin()
    if not admin:
        raise ValueError("No admin account")
    if not check_password_hash(admin["password_hash"], current_password):
        raise ValueError("Current password is incorrect")
    if len(new_password) < MIN_PASSWORD_LEN:
        raise ValueError(f"Password must be at least {MIN_PASSWORD_LEN} characters")

    admin["password_hash"] = generate_password_hash(new_password, method="scrypt")
    admin["password_updated_at"] = _utc_now()
    ADMIN_FILE.write_text(json.dumps(admin, indent=2), encoding="utf-8")


def verify_login(username: str, password: str) -> bool:
    admin = load_admin()
    if not admin:
        return False
    if username.strip() != admin.get("username"):
        return False
    return check_password_hash(admin["password_hash"], password)


def _client_ip() -> str:
    forwarded = request.headers.get("X-Forwarded-For", "")
    if forwarded:
        return forwarded.split(",")[0].strip()
    return request.remote_addr or "unknown"


def login_rate_limited() -> bool:
    ip = _client_ip()
    now = time.time()
    attempts = _login_attempts.get(ip, [])
    attempts = [t for t in attempts if now - t < LOGIN_WINDOW_SEC]
    _login_attempts[ip] = attempts
    return len(attempts) >= MAX_LOGIN_ATTEMPTS


def record_failed_login() -> None:
    ip = _client_ip()
    _login_attempts.setdefault(ip, []).append(time.time())


def clear_login_attempts() -> None:
    ip = _client_ip()
    _login_attempts.pop(ip, None)


def issue_csrf_token() -> str:
    token = secrets.token_urlsafe(32)
    session["csrf_token"] = token
    return token


def validate_csrf(token: Optional[str]) -> bool:
    expected = session.get("csrf_token")
    if not expected or not token:
        return False
    return secrets.compare_digest(expected, token)


def login_admin(username: str) -> None:
    session.clear()
    session["admin_user"] = username
    session["admin_logged_in"] = True
    session.permanent = True
    issue_csrf_token()


def logout_admin() -> None:
    session.clear()


def is_logged_in() -> bool:
    if not session.get("admin_logged_in"):
        return False
    admin = load_admin()
    if not admin:
        return False
    return session.get("admin_user") == admin.get("username")


def login_required(view):
    @wraps(view)
    def wrapped(*args, **kwargs):
        if not is_logged_in():
            return redirect(url_for("admin.login"))
        return view(*args, **kwargs)
    return wrapped


def registration_allowed() -> bool:
    return not admin_exists()


def require_registration_open():
    if not registration_allowed():
        abort(404)
