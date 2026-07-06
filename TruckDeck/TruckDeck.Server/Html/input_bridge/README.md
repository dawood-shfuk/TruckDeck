# Truck Command Deck - Input Bridge

The Telemetry Server (port `25555`) is **read-only**: it reports telemetry but
cannot send input back into ETS2 / ATS. That is why the dashboard buttons did
nothing on their own.

This bridge is a tiny local HTTP server that receives button presses from the
dashboard and injects **real keyboard key strokes** into the game using the
Windows `SendInput` API (hardware scan codes, which DirectInput games like
ETS2/ATS require).

```
Dashboard button  ->  POST /api/command/<action>  ->  Bridge  ->  key stroke  ->  Game
```

## Requirements

- Windows
- Python 3 (already installed on this PC)
- No extra packages needed (standard library only)

## Running

Double-click **`start_bridge.bat`**, or run from a terminal:

```bash
python bridge.py
```

Leave it running while you play. It listens on port `25556` by default.

> Tip: To auto-start it with Windows, put a shortcut to `start_bridge.bat` in
> your Startup folder (`Win+R` -> `shell:startup`).

## Configuring hotkeys

All hotkeys live in **`bridge_config.json`**. Map each dashboard action to the
key you have bound to it **in-game** (ETS2/ATS *Options > Keys & Buttons*):

```json
{
    "port": 25556,
    "tap_hold_ms": 60,
    "keys": {
        "engine": "e",
        "hazards": "f",
        "beacons": "o",
        "light": "l",
        "highBeam": "k",
        "wipers": "p",
        "trailer": "t",
        "cabin": "c",
        "cabinLight": "c",
        "diffLock": "v",
        "liftAxle": "z",
        "trailerLiftAxle": "x",
        "f5": "f5",
        "f6": "f6",
        "f7": "f7",
        "f8": "f8"
    }
}
```

- The **left** side (`engine`, `hazards`, ...) is the dashboard action name -
  do not rename these.
- The **right** side is the key (or key combo) to press. Set it to whatever
  the game uses.
- **Modifier combos** are supported with `+`, e.g. `"ctrl+s"`,
  `"ctrl+shift+left"`, `"alt+f5"`. Valid modifiers: `ctrl`, `shift`, `alt`,
  `win` (and side-specific `lctrl`/`rctrl`, `lshift`/`rshift`, `lalt`/`ralt`).
  The modifier(s) are held while the final key is tapped, then released.
- `cabin` and `cabinLight` are the same button (the web skin and the PWA use
  slightly different names), so keep them pointed at the same key.
- `f5`-`f8` are the Route Advisor display modes used by the "DISPLAY MODE"
  button. The defaults match the game's default Route Advisor tabs.
- `tap_hold_ms` is how long each key is held down (milliseconds).

After editing, either restart the bridge or open
<http://localhost:25556/reload> in a browser to apply changes live.

### Supported key names

- Letters: `a`-`z`
- Digits: `0`-`9`
- Function keys: `f1`-`f12`
- Special: `space`, `enter`, `esc`, `tab`, `backspace`, `capslock`,
  `shift` / `lshift` / `rshift`, `ctrl` / `lctrl` / `rctrl`,
  `alt` / `lalt` / `ralt`
- Arrows: `up`, `down`, `left`, `right`
- Navigation: `insert`, `delete`, `home`, `end`, `pageup`, `pagedown`
- Numpad: `num0`-`num9`, `num.`, `num+`, `num-`, `num*`, `num/`, `numenter`
- Punctuation: `- = [ ] ; ' ` \\ , . /`

Names are case-insensitive.

## Endpoints

| Method        | Path                      | Purpose                              |
| ------------- | ------------------------- | ------------------------------------ |
| `GET`/`POST`  | `/api/command/<action>`   | Press the key mapped to `<action>`   |
| `GET`         | `/` or `/health`          | Status + list of mapped actions      |
| `GET`         | `/reload`                 | Reload `bridge_config.json`          |

## Testing without the game

1. Start the bridge.
2. Open <http://localhost:25556/> - you should see the status JSON.
3. Open Notepad, click into it, then visit
   <http://localhost:25556/api/command/engine> in a browser. The key mapped to
   `engine` (default `e`) should be typed into Notepad.

## Troubleshooting

- **Buttons still do nothing:** make sure the bridge window is running and the
  PC's IP/firewall allows port `25556` from your phone. The dashboards call the
  bridge on the same host they were loaded from, port `25556`.
- **Key presses ignored by the game only:** run the bridge **as Administrator**.
  ETS2/ATS runs with elevated input protection (UIPI); a non-elevated sender
  cannot inject input into an elevated game window.
- **Wrong things happen in-game:** the key in `bridge_config.json` doesn't match
  your in-game binding - fix the mapping and hit `/reload`.

## Changing the port

Set `"port"` in `bridge_config.json`. If you change it, also update
`bridgePort` in `pwa/app.js` and `BRIDGE_PORT` in
`skins/truck_command_deck/dashboard.js` to match.
