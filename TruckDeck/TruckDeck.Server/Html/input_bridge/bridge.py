"""
Truck Command Deck - Input Bridge
=================================

A tiny HTTP server that turns dashboard button presses into real keyboard
key strokes on this PC, so the buttons in the web/PWA/Android dashboards can
actually control ETS2 / ATS.

Why this is needed:
    The Telemetry Server (port 25555) is READ-ONLY. It can report telemetry
    but cannot send input back into the game. This bridge fills that gap.

How it works:
    1. The dashboard sends:   POST http://<pc-ip>:25556/api/command/<action>
    2. The bridge looks up <action> in bridge_config.json -> a key name.
    3. The bridge presses that key using the Windows SendInput API, sending
       hardware SCAN CODES (DirectInput games like ETS2/ATS ignore plain
       virtual-key events, so scan codes are required).

No third-party packages are required - standard library only.
Run with:  python bridge.py     (or double-click start_bridge.bat)
"""

import ctypes
import json
import os
import sys
import time
import threading
from ctypes import wintypes
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

try:
    from joy_poll import JoyPoller, parse_joy_binding
except ImportError:
    JoyPoller = None
    parse_joy_binding = None

CONFIG_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "bridge_config.json")

# ---------------------------------------------------------------------------
# Scan-code table (Set 1 "make" codes). Keys flagged as "extended" need the
# EXTENDEDKEY flag (arrow keys, right ctrl/alt, numpad enter/slash, etc.).
# ---------------------------------------------------------------------------
SCANCODES = {
    "esc": 0x01,
    "1": 0x02, "2": 0x03, "3": 0x04, "4": 0x05, "5": 0x06,
    "6": 0x07, "7": 0x08, "8": 0x09, "9": 0x0A, "0": 0x0B,
    "-": 0x0C, "=": 0x0D, "backspace": 0x0E, "tab": 0x0F,
    "q": 0x10, "w": 0x11, "e": 0x12, "r": 0x13, "t": 0x14,
    "y": 0x15, "u": 0x16, "i": 0x17, "o": 0x18, "p": 0x19,
    "[": 0x1A, "]": 0x1B, "enter": 0x1C, "ctrl": 0x1D, "lctrl": 0x1D,
    "a": 0x1E, "s": 0x1F, "d": 0x20, "f": 0x21, "g": 0x22,
    "h": 0x23, "j": 0x24, "k": 0x25, "l": 0x26,
    ";": 0x27, "'": 0x28, "`": 0x29, "shift": 0x2A, "lshift": 0x2A,
    "\\": 0x2B,
    "z": 0x2C, "x": 0x2D, "c": 0x2E, "v": 0x2F, "b": 0x30,
    "n": 0x31, "m": 0x32, ",": 0x33, ".": 0x34, "/": 0x35,
    "rshift": 0x36, "alt": 0x38, "lalt": 0x38, "space": 0x39,
    "capslock": 0x3A,
    "f1": 0x3B, "f2": 0x3C, "f3": 0x3D, "f4": 0x3E, "f5": 0x3F,
    "f6": 0x40, "f7": 0x41, "f8": 0x42, "f9": 0x43, "f10": 0x44,
    "f11": 0x57, "f12": 0x58,
    # Numpad
    "num0": 0x52, "num1": 0x4F, "num2": 0x50, "num3": 0x51, "num4": 0x4B,
    "num5": 0x4C, "num6": 0x4D, "num7": 0x47, "num8": 0x48, "num9": 0x49,
    "num.": 0x53, "num*": 0x37, "num-": 0x4A, "num+": 0x4E,
}

# Keys that must be sent with the EXTENDEDKEY flag.
EXTENDED_KEYS = {
    "up": 0x48, "down": 0x50, "left": 0x4B, "right": 0x4D,
    "insert": 0x52, "delete": 0x53, "home": 0x47, "end": 0x4F,
    "pageup": 0x49, "pagedown": 0x51,
    "pgup": 0x49, "pgdn": 0x51, "pgdown": 0x51,
    "rctrl": 0x1D, "ralt": 0x38, "numenter": 0x1C, "num/": 0x35,
}

# Modifier names accepted on the left of a combo like "ctrl+shift+s".
# value = (scancode, extended)
MODIFIERS = {
    "ctrl": (0x1D, False), "control": (0x1D, False), "lctrl": (0x1D, False),
    "rctrl": (0x1D, True),
    "shift": (0x2A, False), "lshift": (0x2A, False), "rshift": (0x36, False),
    "alt": (0x38, False), "lalt": (0x38, False), "ralt": (0x38, True),
    "win": (0x5B, True), "lwin": (0x5B, True), "rwin": (0x5C, True),
}

# ---------------------------------------------------------------------------
# Windows SendInput plumbing (ctypes)
# ---------------------------------------------------------------------------
PUL = ctypes.POINTER(ctypes.c_ulong)


class KeyBdInput(ctypes.Structure):
    _fields_ = [
        ("wVk", ctypes.c_ushort),
        ("wScan", ctypes.c_ushort),
        ("dwFlags", ctypes.c_ulong),
        ("time", ctypes.c_ulong),
        ("dwExtraInfo", PUL),
    ]


class HardwareInput(ctypes.Structure):
    _fields_ = [
        ("uMsg", ctypes.c_ulong),
        ("wParamL", ctypes.c_short),
        ("wParamH", ctypes.c_ushort),
    ]


class MouseInput(ctypes.Structure):
    _fields_ = [
        ("dx", ctypes.c_long),
        ("dy", ctypes.c_long),
        ("mouseData", ctypes.c_ulong),
        ("dwFlags", ctypes.c_ulong),
        ("time", ctypes.c_ulong),
        ("dwExtraInfo", PUL),
    ]


class InputUnion(ctypes.Union):
    _fields_ = [("ki", KeyBdInput), ("mi", MouseInput), ("hi", HardwareInput)]


class Input(ctypes.Structure):
    _fields_ = [("type", ctypes.c_ulong), ("ii", InputUnion)]


INPUT_KEYBOARD = 1
INPUT_MOUSE = 0
KEYEVENTF_EXTENDEDKEY = 0x0001
KEYEVENTF_KEYUP = 0x0002
KEYEVENTF_SCANCODE = 0x0008

MOUSEEVENTF_MOVE = 0x0001
MOUSEEVENTF_LEFTDOWN = 0x0002
MOUSEEVENTF_LEFTUP = 0x0004
MOUSEEVENTF_RIGHTDOWN = 0x0008
MOUSEEVENTF_RIGHTUP = 0x0010
MOUSEEVENTF_MIDDLEDOWN = 0x0020
MOUSEEVENTF_MIDDLEUP = 0x0040
MOUSEEVENTF_WHEEL = 0x0800
MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000
WHEEL_DELTA = 120
MOUSE_DELTA_CLAMP = 500

_send_input = ctypes.windll.user32.SendInput


def _send_scancode(scancode, key_up, extended):
    flags = KEYEVENTF_SCANCODE
    if extended:
        flags |= KEYEVENTF_EXTENDEDKEY
    if key_up:
        flags |= KEYEVENTF_KEYUP
    extra = ctypes.c_ulong(0)
    ki = KeyBdInput(0, scancode, flags, 0, ctypes.pointer(extra))
    inp = Input(INPUT_KEYBOARD, InputUnion(ki=ki))
    _send_input(1, ctypes.pointer(inp), ctypes.sizeof(inp))


def _clamp_mouse_delta(value):
    v = int(value)
    if v > MOUSE_DELTA_CLAMP:
        return MOUSE_DELTA_CLAMP
    if v < -MOUSE_DELTA_CLAMP:
        return -MOUSE_DELTA_CLAMP
    return v


def _send_mouse(flags, dx=0, dy=0, mouse_data=0):
    extra = ctypes.c_ulong(0)
    mi = MouseInput(dx, dy, mouse_data, flags, 0, ctypes.pointer(extra))
    inp = Input(INPUT_MOUSE, InputUnion(mi=mi))
    sent = _send_input(1, ctypes.pointer(inp), ctypes.sizeof(inp))
    return sent == 1


def mouse_move(dx, dy):
    dx = _clamp_mouse_delta(dx)
    dy = _clamp_mouse_delta(dy)
    if dx == 0 and dy == 0:
        return True
    flags = MOUSEEVENTF_MOVE | MOUSEEVENTF_MOVE_NOCOALESCE
    return _send_mouse(flags, dx, dy)


MOUSE_CLICK_FLAGS = {
    ("left", "down"): MOUSEEVENTF_LEFTDOWN,
    ("left", "up"): MOUSEEVENTF_LEFTUP,
    ("right", "down"): MOUSEEVENTF_RIGHTDOWN,
    ("right", "up"): MOUSEEVENTF_RIGHTUP,
    ("middle", "down"): MOUSEEVENTF_MIDDLEDOWN,
    ("middle", "up"): MOUSEEVENTF_MIDDLEUP,
}


def mouse_click(button, state):
    key = (str(button).strip().lower(), str(state).strip().lower())
    flags = MOUSE_CLICK_FLAGS.get(key)
    if flags is None:
        return False
    _send_mouse(flags)
    return True


def mouse_scroll(delta):
    delta = int(delta)
    if delta == 0:
        return True
    _send_mouse(MOUSEEVENTF_WHEEL, mouse_data=delta)
    return True


def resolve_key(name):
    """Return (scancode, extended) for a key name, or None if unknown."""
    if name is None:
        return None
    key = str(name).strip().lower()
    if key in EXTENDED_KEYS:
        return EXTENDED_KEYS[key], True
    if key in SCANCODES:
        return SCANCODES[key], False
    return None


def tap_combo(combo, hold_ms):
    """Tap a key or a modifier combo such as 'e', 'f5', 'ctrl+s',
    'ctrl+shift+left'. Modifiers are held while the main key is tapped, then
    released in reverse order. Returns False if any key name is unknown."""
    parts = [p.strip().lower() for p in str(combo).split("+") if p.strip()]
    if not parts:
        return False

    main = resolve_key(parts[-1])
    if main is None:
        return False

    mods = []
    for name in parts[:-1]:
        if name in MODIFIERS:
            mods.append(MODIFIERS[name])
        else:
            extra = resolve_key(name)  # allow any key to act as a held key
            if extra is None:
                return False
            mods.append(extra)

    for scancode, extended in mods:
        _send_scancode(scancode, key_up=False, extended=extended)
    main_scancode, main_extended = main
    _send_scancode(main_scancode, key_up=False, extended=main_extended)
    time.sleep(max(0, hold_ms) / 1000.0)
    _send_scancode(main_scancode, key_up=True, extended=main_extended)
    for scancode, extended in reversed(mods):
        _send_scancode(scancode, key_up=True, extended=extended)
    return True


# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------
def load_config():
    with open(CONFIG_PATH, "r", encoding="utf-8") as fh:
        cfg = json.load(fh)
    cfg.setdefault("port", 25556)
    cfg.setdefault("tap_hold_ms", 60)
    cfg.setdefault("keys", {})
    cfg.setdefault("dashboard", {})
    cfg["dashboard"].setdefault("screenCycleJoy", "joy1.b1")
    return cfg


CONFIG = load_config()
try:
    _CONFIG_MTIME = os.path.getmtime(CONFIG_PATH)
except OSError:
    _CONFIG_MTIME = 0

# Dash-only events (consumed by the web dashboard via GET /api/dashboard/events).
_dashboard_lock = threading.Lock()
_dashboard_events = {"screenCycle": 0}
_joy_poller = None
_joy_warned_bindings = set()


def restart_joy_poller():
    global _joy_poller
    if _joy_poller:
        _joy_poller.stop()
        _joy_poller = None

    joy_spec = (CONFIG.get("dashboard") or {}).get("screenCycleJoy", "")
    if not joy_spec or not str(joy_spec).strip():
        return
    if JoyPoller is None or parse_joy_binding is None:
        if joy_spec not in _joy_warned_bindings:
            _joy_warned_bindings.add(joy_spec)
            print("[bridge] joy_poll module missing — screenCycleJoy disabled")
        return
    parsed = parse_joy_binding(joy_spec)
    if parsed is None:
        if joy_spec not in _joy_warned_bindings:
            _joy_warned_bindings.add(joy_spec)
            print(f"[bridge] invalid screenCycleJoy — disabled: {joy_spec!r}")
        return

    try:
        from joy_poll import list_joystick_devices
        devices = list_joystick_devices()
    except Exception as exc:  # noqa: BLE001
        if joy_spec not in _joy_warned_bindings:
            _joy_warned_bindings.add(joy_spec)
            print(f"[bridge] could not enumerate joysticks — screenCycleJoy disabled: {exc}")
        return

    device_index, _button_index = parsed
    if not devices:
        if joy_spec not in _joy_warned_bindings:
            _joy_warned_bindings.add(joy_spec)
            print("[bridge] no joystick devices found — screenCycleJoy disabled")
        return
    if device_index < 0 or device_index >= len(devices):
        if joy_spec not in _joy_warned_bindings:
            _joy_warned_bindings.add(joy_spec)
            print(
                f"[bridge] joystick device {device_index + 1} not found "
                f"({len(devices)} device(s)) — screenCycleJoy disabled"
            )
        return

    try:
        _joy_poller = JoyPoller(joy_spec, lambda: queue_dashboard_event("screenCycle"))
        _joy_poller.start()
        _joy_warned_bindings.discard(joy_spec)
    except Exception as exc:  # noqa: BLE001
        if joy_spec not in _joy_warned_bindings:
            _joy_warned_bindings.add(joy_spec)
            print(f"[bridge] could not start joy poller — screenCycleJoy disabled: {exc}")


def _joy_retry_loop():
    while True:
        time.sleep(30)
        joy_spec = (CONFIG.get("dashboard") or {}).get("screenCycleJoy", "")
        if not joy_spec or not str(joy_spec).strip():
            continue
        joy_thread = getattr(_joy_poller, "_thread", None) if _joy_poller else None
        if joy_thread is not None and joy_thread.is_alive():
            continue
        restart_joy_poller()


def queue_dashboard_event(name):
    with _dashboard_lock:
        if name in _dashboard_events:
            _dashboard_events[name] += 1


def take_dashboard_events():
    with _dashboard_lock:
        out = dict(_dashboard_events)
        for key in _dashboard_events:
            _dashboard_events[key] = 0
        return out


def reload_config():
    global CONFIG, _CONFIG_MTIME
    CONFIG = load_config()
    try:
        _CONFIG_MTIME = os.path.getmtime(CONFIG_PATH)
    except OSError:
        pass
    restart_joy_poller()
    return CONFIG


def maybe_reload_config():
    """Hot-reload the config if the file changed on disk, so buttons added
    to bridge_config.json work immediately without restarting the bridge."""
    global CONFIG, _CONFIG_MTIME
    try:
        mtime = os.path.getmtime(CONFIG_PATH)
    except OSError:
        return
    if mtime != _CONFIG_MTIME:
        try:
            CONFIG = load_config()
            _CONFIG_MTIME = mtime
            print(f"[bridge] config changed - reloaded ({len(CONFIG['keys'])} actions)")
            restart_joy_poller()
        except Exception as exc:  # noqa: BLE001
            print(f"[bridge] config reload failed, keeping old config: {exc}")


# ---------------------------------------------------------------------------
# HTTP server
# ---------------------------------------------------------------------------
class BridgeHandler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args):  # quieter console
        return

    def _read_json_body(self):
        length = int(self.headers.get("Content-Length", 0) or 0)
        if length <= 0:
            return {}
        raw = self.rfile.read(length)
        if not raw:
            return {}
        try:
            return json.loads(raw.decode("utf-8"))
        except (json.JSONDecodeError, UnicodeDecodeError):
            return None

    def _cors(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "*")

    def _json(self, status, payload):
        try:
            body = json.dumps(payload).encode("utf-8")
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self._cors()
            self.end_headers()
            self.wfile.write(body)
        except (BrokenPipeError, ConnectionAbortedError, ConnectionResetError):
            # Phone/tablet dashboards often close the POST before reading the body.
            pass

    def handle_one_request(self):
        try:
            super().handle_one_request()
        except (BrokenPipeError, ConnectionAbortedError, ConnectionResetError):
            pass

    def do_OPTIONS(self):
        self.send_response(204)
        self._cors()
        self.end_headers()

    def do_GET(self):
        self._handle()

    def do_POST(self):
        self._handle()

    def _handle(self):
        path = self.path.split("?", 1)[0].rstrip("/")

        if path in ("", "/", "/health", "/status"):
            dash = CONFIG.get("dashboard") or {}
            joy_spec = dash.get("screenCycleJoy", "")
            joy_thread = getattr(_joy_poller, "_thread", None) if _joy_poller else None
            self._json(200, {
                "status": "ok",
                "service": "Truck Command Deck Input Bridge",
                "port": CONFIG["port"],
                "mouse": True,
                "actions": sorted(CONFIG["keys"].keys()),
                "joy": {
                    "enabled": joy_thread is not None and joy_thread.is_alive(),
                    "binding": joy_spec or None,
                },
            })
            return

        if path == "/reload":
            try:
                cfg = reload_config()
                self._json(200, {"status": "reloaded", "actions": sorted(cfg["keys"].keys())})
            except Exception as exc:  # noqa: BLE001
                self._json(500, {"status": "error", "message": str(exc)})
            return

        if path == "/api/dashboard/events":
            self._json(200, {"status": "ok", "events": take_dashboard_events()})
            return

        if path == "/api/dashboard/screenCycle":
            queue_dashboard_event("screenCycle")
            self._json(200, {"status": "ok", "action": "screenCycle"})
            return

        if path.startswith("/api/command/"):
            maybe_reload_config()  # pick up newly added buttons without a restart
            action = path[len("/api/command/"):]
            key = CONFIG["keys"].get(action)
            if key is None:
                print(f"[bridge] unknown action: {action!r}")
                self._json(404, {"status": "error", "message": f"No key mapped for action '{action}'"})
                return
            ok = tap_combo(key, CONFIG["tap_hold_ms"])
            if ok:
                print(f"[bridge] {action} -> '{key}'")
                self._json(200, {"status": "ok", "action": action, "key": key})
            else:
                print(f"[bridge] invalid key '{key}' for action '{action}'")
                self._json(400, {"status": "error", "message": f"Unknown key name '{key}'"})
            return

        if path == "/api/mouse/move":
            body = self._read_json_body()
            if body is None:
                self._json(400, {"status": "error", "message": "Invalid JSON body"})
                return
            dx = body.get("dx", 0)
            dy = body.get("dy", 0)
            mouse_move(dx, dy)
            self._json(200, {"status": "ok", "dx": dx, "dy": dy})
            return

        if path == "/api/mouse/click":
            body = self._read_json_body()
            if body is None:
                self._json(400, {"status": "error", "message": "Invalid JSON body"})
                return
            button = body.get("button", "left")
            state = body.get("state", "down")
            ok = mouse_click(button, state)
            if ok:
                self._json(200, {"status": "ok", "button": button, "state": state})
            else:
                self._json(400, {"status": "error", "message": f"Invalid button/state: {button}/{state}"})
            return

        if path == "/api/mouse/scroll":
            body = self._read_json_body()
            if body is None:
                self._json(400, {"status": "error", "message": "Invalid JSON body"})
                return
            delta = body.get("delta", WHEEL_DELTA)
            mouse_scroll(delta)
            self._json(200, {"status": "ok", "delta": delta})
            return

        self._json(404, {"status": "error", "message": "Not found"})


class BridgeServer(ThreadingHTTPServer):
    # Do NOT reuse the address: on Windows this makes a second launch fail
    # loudly (port already in use) instead of silently shadowing the first.
    allow_reuse_address = False


def main():
    port = int(CONFIG["port"])
    try:
        server = BridgeServer(("0.0.0.0", port), BridgeHandler)
    except OSError as exc:
        print(f"\n[bridge] ERROR: could not bind port {port}: {exc}")
        print("[bridge] Port 25556 is usually already used by TruckDeck.exe (built-in bridge).")
        print("[bridge] Close TruckDeck or this window — you do not need start_bridge.bat while TruckDeck runs.")
        print("[bridge] To run the Python bridge standalone, close TruckDeck first or change \"port\" in bridge_config.json.")
        input("\nPress Enter to exit.")
        sys.exit(1)
    print("=" * 56)
    print(" Truck Command Deck - Input Bridge")
    print("=" * 56)
    print(f" Listening on   : http://0.0.0.0:{port}")
    print(f" Config file    : {CONFIG_PATH}")
    print(f" Mapped actions : {', '.join(sorted(CONFIG['keys'].keys()))}")
    print(" Mouse API      : /api/mouse/move, /click, /scroll")
    dash = CONFIG.get("dashboard") or {}
    joy = dash.get("screenCycleJoy", "")
    if joy:
        print(f" Dash screen joy : {joy}")
    else:
        print(" Dash screen joy : (disabled — set dashboard.screenCycleJoy)")
    print(" Edit bridge_config.json to change hotkeys, then either")
    print(" restart, or open http://localhost:%d/reload to apply." % port)
    print(" Press Ctrl+C to stop.")
    print("=" * 56)
    restart_joy_poller()
    threading.Thread(target=_joy_retry_loop, name="joy-retry", daemon=True).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n[bridge] shutting down")
        if _joy_poller:
            _joy_poller.stop()
        server.shutdown()


if __name__ == "__main__":
    if sys.platform != "win32":
        print("This bridge uses the Windows SendInput API and must run on Windows.")
        sys.exit(1)
    main()
