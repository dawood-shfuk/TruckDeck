"""
Capture the next joystick button press and save it to bridge_config.json
as dashboard.screenCycleJoy (ETS2-style, e.g. joy6.b69).
"""

import json
import os
import sys

from joy_poll import backend_name, capture_next_button, format_joy_binding, list_joystick_devices

CONFIG_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "bridge_config.json")


def load_config():
    with open(CONFIG_PATH, encoding="utf-8") as fh:
        return json.load(fh)


def save_config(cfg):
    with open(CONFIG_PATH, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(cfg, fh, indent=4, ensure_ascii=False)
        fh.write("\n")


def main():
    if len(sys.argv) >= 2 and sys.argv[1].strip():
        binding = sys.argv[1].strip()
        print("TruckDeck — set screen-cycle joystick binding manually")
        print("=" * 48)
        print(f"Using: {binding}")
        cfg = load_config()
        dash = cfg.setdefault("dashboard", {})
        dash["screenCycleJoy"] = binding
        save_config(cfg)
        print("Saved to bridge_config.json")
        print("Reload the bridge or open http://localhost:25556/reload")
        return 0

    print("TruckDeck — bind screen-cycle joystick button")
    print("=" * 48)

    try:
        devices = list_joystick_devices()
    except OSError as exc:
        print(f"Joystick error: {exc}")
        print()
        print("Try closing ETS2 / other apps using the wheel, then run again.")
        print("Or set manually: capture_screen_cycle_joy.bat joy6.b69")
        return 1

    backend = backend_name()
    if backend == "winmm":
        print("Using WinMM joystick API (DirectInput unavailable on this Python).")
        print("If the saved binding does not work in ETS2, copy joyN.bN from")
        print("Options > Controls and run: capture_screen_cycle_joy.bat joyN.bN")
        print()

    if not devices:
        print("No joystick devices found. Plug in your wheel / controller and try again.")
        return 1

    print("Detected devices:")
    for idx, name in devices:
        print(f"  joy{idx + 1}: {name}")
    print()
    print("Press the button you want to use for dashboard screen cycle...")
    print("(Ctrl+C to cancel)")
    print()

    try:
        dev_idx, btn_idx, dev_name = capture_next_button()
    except KeyboardInterrupt:
        print("\nCancelled.")
        return 1

    binding = format_joy_binding(dev_idx, btn_idx)
    print()
    print(f"Captured: {binding}")
    print(f"  Device : joy{dev_idx + 1} — {dev_name}")
    print(f"  Button : b{btn_idx + 1} (button {btn_idx + 1})")

    cfg = load_config()
    dash = cfg.setdefault("dashboard", {})
    old = dash.get("screenCycleJoy", "")
    dash["screenCycleJoy"] = binding
    save_config(cfg)

    print()
    if old and old != binding:
        print(f"Updated bridge_config.json (was: {old!r})")
    else:
        print("Saved to bridge_config.json")
    print()
    print("If the input bridge is running, open http://localhost:25556/reload")
    print("or restart start_bridge.bat to apply.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
