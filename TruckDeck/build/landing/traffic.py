"""Visitor and bot traffic tracking with referer breakdown."""
import fcntl
import json
import re
from pathlib import Path
from typing import Callable
from urllib.parse import urlparse

from flask import Request

DATA_DIR = Path(__file__).parent / "data"
TRAFFIC_FILE = DATA_DIR / "traffic.json"

BOT_UA_RE = re.compile(
    r"(bot|crawl|spider|slurp|mediapartners|preview|archiver|scanner|"
    r"googlebot|bingbot|duckduckbot|baiduspider|yandexbot|applebot|"
    r"facebookexternalhit|twitterbot|linkedinbot|whatsapp|telegrambot|"
    r"discordbot|slackbot|semrush|ahrefs|mj12bot|dotbot|petalbot|"
    r"uptime|pingdom|statuscake|headlesschrome|phantomjs|"
    r"curl/|wget/|python-requests|httpx/|aiohttp|okhttp|java/|"
    r"go-http-client|libwww|scrapy|postman|insomnia)",
    re.IGNORECASE,
)

SKIP_PREFIXES = (
    "/admin",
    "/static",
    "/health",
    "/favicon.ico",
    "/landing-assets",
    "/downloads/",
    "/demo/",
)

TRACK_PATHS = {"/", "/downloads", "/contributors", "/live-demo"}


def _empty_stats() -> dict:
    return {
        "visitors": {"total": 0, "referers": {}},
        "bots": {"total": 0, "referers": {}},
    }


def is_bot(user_agent: str) -> bool:
    if not user_agent or not user_agent.strip():
        return True
    return bool(BOT_UA_RE.search(user_agent))


def normalize_referer(referer: str, host: str = "") -> str:
    if not referer or not referer.strip():
        return "(direct)"
    try:
        parsed = urlparse(referer.strip())
    except ValueError:
        return "(invalid)"

    netloc = (parsed.netloc or "").lower()
    if not netloc:
        return "(direct)"

    if host and netloc == host.lower():
        return "(internal)"

    return netloc[:120]


def should_track(request: Request) -> bool:
    if request.method != "GET":
        return False

    path = request.path
    for prefix in SKIP_PREFIXES:
        if path.startswith(prefix):
            return False

    if path.startswith("/dl/"):
        return False

    if path in TRACK_PATHS:
        return True

    return False


def _read_stats_unlocked(handle) -> dict:
    handle.seek(0)
    raw = handle.read().strip()
    if not raw:
        return _empty_stats()
    try:
        data = json.loads(raw)
    except json.JSONDecodeError:
        return _empty_stats()

    for key in ("visitors", "bots"):
        bucket = data.setdefault(key, {"total": 0, "referers": {}})
        bucket.setdefault("total", 0)
        bucket.setdefault("referers", {})
    return data


def _write_stats_unlocked(handle, data: dict) -> None:
    handle.seek(0)
    handle.truncate()
    json.dump(data, handle, indent=2)
    handle.flush()


def _mutate_traffic(mutator: Callable[[dict], None]) -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    with TRAFFIC_FILE.open("a+", encoding="utf-8") as handle:
        fcntl.flock(handle, fcntl.LOCK_EX)
        try:
            data = _read_stats_unlocked(handle)
            mutator(data)
            _write_stats_unlocked(handle, data)
        finally:
            fcntl.flock(handle, fcntl.LOCK_UN)


def record_hit(request: Request) -> None:
    if not should_track(request):
        return

    ua = request.headers.get("User-Agent", "")
    host = (request.host or "").split(":")[0]
    referer = normalize_referer(request.headers.get("Referer", ""), host)
    bucket_key = "bots" if is_bot(ua) else "visitors"

    def mutate(data: dict) -> None:
        bucket = data[bucket_key]
        bucket["total"] = bucket.get("total", 0) + 1
        refs = bucket.setdefault("referers", {})
        refs[referer] = refs.get(referer, 0) + 1

    _mutate_traffic(mutate)


def get_traffic_stats() -> dict:
    if not TRAFFIC_FILE.is_file():
        return _empty_stats()
    try:
        with TRAFFIC_FILE.open("r", encoding="utf-8") as handle:
            return _read_stats_unlocked(handle)
    except OSError:
        return _empty_stats()


def referer_rows(bucket: dict, limit: int = 15) -> list[dict]:
    refs = bucket.get("referers", {})
    rows = sorted(refs.items(), key=lambda item: (-item[1], item[0]))
    return [{"referer": name, "count": count} for name, count in rows[:limit]]


def reset_traffic() -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    TRAFFIC_FILE.write_text(json.dumps(_empty_stats(), indent=2), encoding="utf-8")
