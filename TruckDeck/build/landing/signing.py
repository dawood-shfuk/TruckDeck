"""HMAC request signing for TruckDeck desktop clients."""
from __future__ import annotations

import base64
import hashlib
import hmac
import time
from typing import Optional, Tuple

from flask import Request

MAX_SKEW_SEC = 300


def hash_install_key(install_key: str) -> str:
    return hashlib.sha256(install_key.encode("utf-8")).hexdigest()


def sign_payload(install_key: str, timestamp: str, body: bytes) -> str:
    msg = timestamp.encode("utf-8") + b"\n" + body
    return hmac.new(install_key.encode("utf-8"), msg, hashlib.sha256).hexdigest()


def verify_signed_request(
    request: Request,
    key_hash: str,
    install_key: Optional[str] = None,
) -> Tuple[bool, str]:
    ts = request.headers.get("X-Timestamp", "").strip()
    sig = request.headers.get("X-Signature", "").strip().lower()
    key_hdr = request.headers.get("X-Install-Key", "").strip()

    if not ts or not sig:
        return False, "missing signature headers"

    if install_key is None:
        if not key_hdr:
            return False, "missing install key"
        try:
            install_key = base64.b64decode(key_hdr).decode("utf-8")
        except Exception:
            return False, "invalid install key"

    if hash_install_key(install_key) != key_hash:
        return False, "install key mismatch"

    try:
        ts_val = int(ts)
    except ValueError:
        return False, "invalid timestamp"

    if abs(time.time() - ts_val) > MAX_SKEW_SEC:
        return False, "timestamp expired"

    body = request.get_data() or b""
    expected = sign_payload(install_key, ts, body)
    if not hmac.compare_digest(expected, sig):
        return False, "bad signature"

    return True, ""
