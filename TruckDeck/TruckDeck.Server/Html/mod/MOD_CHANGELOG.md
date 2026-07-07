# TruckDeck NAV — Mod Changelog & Credits

**Derived from:** Paper Sun **GPS/PC** mod (original source: `Mod source/`)  
**TruckDeck fork:** `TruckDeck.Server/Html/mod/` → packed as **`TruckDeck_NAV.scs`**  
**Current package version:** 1.50 (see `manifest.sii`)

---

## Original author & contributors

### Primary author (base mod)

| | |
|---|---|
| **Author** | **Paper Sun** |
| **Original name** | GPS/PC |
| **Original version** | 1.21 |
| **Category** | Interior / cabin accessories |
| **Concept** | Transparent in-cab GPS and PC displays mounted on windshield corners and driver plates, driven by custom UI dashboards inside ETS2 |

The original mod description (Russian + English) credited support for SCS trucks (12/2024) and many community truck packs.

### Community truck support (unchanged in TruckDeck fork)

The accessory definitions (`def/vehicle/truck/...`) still target these third-party packs. TruckDeck does **not** claim authorship of those truck models — only the GPS/PC accessory integration:

| Author / group | Trucks |
|---|---|
| **XBS** | DAF 95ATi, DAF F241, DAF NTT, MAN F2000, Mercedes SK, Sisu M, Volvo F88 |
| **Koral** | KamAZ 6460, KamAZ 65221 |
| **RJL** | Scania G/R/Streamline/T series ([Workshop](https://steamcommunity.com/workshop/filedetails/?id=2466514780), [T series](https://steamcommunity.com/sharedfiles/filedetails/?id=2466522174)) |
| **Community** | Renault Magnum Megamod ([3357926120](https://steamcommunity.com/sharedfiles/filedetails/?id=3357926120)), Scania PGR-Series 2004–2018 ([3355663140](https://steamcommunity.com/sharedfiles/filedetails/?id=3355663140)) |

### TruckDeck maintainer

| | |
|---|---|
| **Maintainer** | **TruckDeck** |
| **Product** | [TruckDeck](https://truckdeck.site) telemetry server + dashboards |
| **Fork role** | Rebrand, ETS2 1.50+ compatibility, build automation, integration with TruckDeck installer/downloads |

---

## Comparison summary (original vs TruckDeck)

| Metric | Paper Sun `Mod source/` | TruckDeck `Html/mod/` |
|---|---|---|
| Total files | ~1,236 | ~1,254 (+18) |
| Files only in original | 0 | — |
| Files only in TruckDeck | — | 18 (see below) |
| Modified files | — | 20 |
| Unchanged files | ~1,216 | ~1,216 (models, textures, most `def/`, `vehicle/`, `automat/`) |

The fork is intentionally **minimal**: Paper Sun's 3D models, materials, and per-truck accessory definitions are preserved. TruckDeck changes focus on packaging, branding, game-version UI sync, and dashboard behaviour.

---

## Changes made in the TruckDeck fork

### 1. Rebrand & metadata

| File | Change |
|---|---|
| `manifest.sii` | `display_name`: **GPS/PC** → **TruckDeck NAV**; `author`: Paper Sun → TruckDeck; `package_version`: 1.21 → **1.50** |
| `mod_description.txt` | Replaced RU/EN stub with full TruckDeck NAV description, feature list, supported trucks, install steps |
| Output archive | Packed as **`TruckDeck_NAV.scs`** (was ad-hoc `.scs` from original workflow) |

### 2. Build & packaging tooling (new)

| File | Purpose |
|---|---|
| `build_truckdeck_nav.ps1` | Sync vanilla factory GPS UI from installed ETS2, then zip mod tree → `TruckDeck_NAV.scs` |
| `build_truckdeck_nav.bat` | Windows double-click wrapper for the PowerShell script |
| `_tools/scs_extractor.exe` | Extracts `base_vehicle.scs` for UI sync (dev/build only, not shipped in `.scs`) |
| `_game_extract/dlc_volvo/` | Volvo FH 2024 GPS dashboard sources from DLC pack extract (dev/build only) |

**Pack script behaviour:**
- `-SyncOnly` — refresh UI files from game without packing
- `-PackOnly` — pack current tree without re-extracting
- Default — sync then pack
- Packs: `ui/`, `material/`, `def/`, `vehicle/`, `automat/`, `manifest.sii`, `mod_description.txt`, `mod_icon.jpg`

### 3. Vanilla GPS UI sync (new files — game compatibility)

Extracted from the player's ETS2 install so OEM truck dashboards match the current game version and do not reference missing vanilla files:

| New / synced path | Source |
|---|---|
| `ui/gps.sii` | `base_vehicle.scs` |
| `ui/dashboard/scania_2025_gps.sii` | `base_vehicle.scs` |
| `ui/dashboard/renault_t_2024_gps.sii` | `base_vehicle.scs` |
| `ui/dashboard/volvo_fh_2024_gps.sii` | DLC Volvo extract |
| `ui/dashboard/volvo_fh_2024_mph_gps.sii` | DLC Volvo extract |
| `ui/template/dashboard_text.sii` | Vanilla templates (all `dashboard_text*.sii`) |
| `ui/template/dashboard_text.daf_xf.sii` | ↑ |
| `ui/template/dashboard_text.daf_xf_euro6.sii` | ↑ |
| `ui/template/dashboard_text.man_tgx_el3.sii` | ↑ |
| `ui/template/dashboard_text.man_tgx_euro6.sii` | ↑ |
| `ui/template/dashboard_text.mercedes_actros_2009.sii` | ↑ |
| `ui/template/dashboard_text.mercedes_actros_2014.sii` | ↑ |
| `ui/template/dashboard_text.renault_t.sii` | ↑ |
| `ui/template/dashboard_text.renault_t_2024.sii` | ↑ |
| `ui/template/dashboard_text.scania_2016.sii` | ↑ |
| `ui/template/dashboard_text.scania_2025.sii` | ↑ |

Cache folder `Html/_vanilla_gps_cache/` is created during build and is **not** included in the shipped `.scs`.

### 4. Bundled UI material (new)

| File | Why |
|---|---|
| `material/ui/white.mat` | White fill material used by Paper Sun dashboard layouts |
| `material/ui/white.tobj` | Texture object for `white.mat` |

Original mod referenced `/material/ui/white.mat` from the game base. TruckDeck bundles it so the mod is self-contained and does not depend on game file paths at runtime.

### 5. Expanded GPS film hookup slots

**File:** `def/vehicle/addon_hookups/mod_ps_gps.sui`

Original: GPS transparency films (25% / 50% / 75%) only on `set_lglass` and `gps3`.

TruckDeck: same three films, but **`suitable_for[]`** expanded to additional cabin slots:

- `set_glass`, `set_dashbrd`
- `toybig`, `toymed`, `toysmall`
- `cup_holder`, `set_cuphold`, `set_table`
- `curtain_f`, `curtain_l`, `curtain_r`
- `door_l`, `door_r`

Allows mounting GPS film accessories in more interior positions on supported trucks.

### 6. Custom Paper Sun dashboard UI updates

These are the **Paper Sun `ps_gps_mod*` / `ps_pc_mod*` dashboards** (not factory OEM dashboards). All still use Paper Sun materials under `/material/ui/ps_gps/` and `/vehicle/truck/upgrade/ps_gps/`.

#### `ui/template/dashboard_text.ps_gps_mod.sii`
- Added **`txt.mod_strong.eta_red`** template — red ETA text variant for rest-time warnings

#### `ui/dashboard/ps_gps_mod.sii` (and variants `_1`, `_2`, `_3`, `_b`, `_b1`, `ps_pc_mod`, `ps_pc_mod_2`)
- Added **`area_l` / `area_r` / `area_t` / `area_b`** anchor fields on UI widgets (ETS2 1.50+ layout compatibility)
- **`ps_gps_mod.sii` fuel display:** replaced simple low-fuel overlay with **four segmented `ui_text_bar` fuel bars** (`fuel.bar1`–`fuel.bar4`) with colour bands (red → amber → green) for finer fuel level visualization
- Removed standalone `fuel.low` text block in favour of bar-based display (main GPS layout)
- Layout tweaks for ETA / fuel value positioning

#### OEM-synced dashboards (vanilla structure, minor TruckDeck pass)
Updated with additional `area_*` anchor fields to match current game UI format:

- `ui/dashboard/daf_2021.sii`, `daf_2021_mph.sii`
- `ui/dashboard/daf_xd.sii`, `daf_xd_mph.sii`
- `ui/dashboard/volvo_fh_2021.sii`, `volvo_fh_2024.sii`
- `ui/dashboard/scania_2025.sii`
- `ui/dashboard/renault_t_2024.sii`

### 7. Unchanged from original (preserved as-is)

- All **`vehicle/truck/upgrade/ps_gps/`** 3D models (`.pmd`, `.pmg`, `.puz`) and textures
- All **`material/ui/ps_gps/`** and **`material/ui/dashboard/`** assets
- All **`def/vehicle/truck/*/accessory/`** GPS/PC accessory definitions (`ps_gps*.sii`, `ps_pc*.sii`) — same `ui_path` mappings to Paper Sun dashboards
- All **`automat/`** compiled material cache files (18 files, identical hashes)
- **`mod_icon.jpg`**
- Community truck folder structure under `def/vehicle/truck/`

### 8. TruckDeck product integration

| Integration | Detail |
|---|---|
| **Installer** | `TruckDeck-Setup.exe` optional task: copy `Extras/TruckDeck_NAV.scs` → `Documents\Euro Truck Simulator 2\mod\` |
| **Release pack** | `release/Extras/TruckDeck_NAV.scs` |
| **Downloads** | `https://truckdeck.site/downloads/TruckDeck_NAV.scs` |
| **In-game use** | Install mod → enable in Mod Manager → fit GPS/PC via cabin accessory slots → run TruckDeck telemetry server → use **truckdeck_nav** skin or mirrored in-cab UI |

The mod mirrors telemetry into the cab; the **TruckDeck server** (`TruckDeck.exe` on port 25555) provides live NAV/dashboard HTML.

---

## Modified files (full list)

```
manifest.sii
mod_description.txt
def/vehicle/addon_hookups/mod_ps_gps.sui
ui/dashboard/daf_2021.sii
ui/dashboard/daf_2021_mph.sii
ui/dashboard/daf_xd.sii
ui/dashboard/daf_xd_mph.sii
ui/dashboard/ps_gps_mod.sii
ui/dashboard/ps_gps_mod_1.sii
ui/dashboard/ps_gps_mod_2.sii
ui/dashboard/ps_gps_mod_3.sii
ui/dashboard/ps_gps_mod_b.sii
ui/dashboard/ps_gps_mod_b1.sii
ui/dashboard/ps_pc_mod.sii
ui/dashboard/ps_pc_mod_2.sii
ui/dashboard/renault_t_2024.sii
ui/dashboard/scania_2025.sii
ui/dashboard/volvo_fh_2021.sii
ui/dashboard/volvo_fh_2024.sii
ui/template/dashboard_text.ps_gps_mod.sii
```

## New files (TruckDeck only)

```
build_truckdeck_nav.ps1
build_truckdeck_nav.bat
material/ui/white.mat
material/ui/white.tobj
ui/gps.sii
ui/dashboard/renault_t_2024_gps.sii
ui/dashboard/scania_2025_gps.sii
ui/template/dashboard_text.sii
ui/template/dashboard_text.daf_xf.sii
ui/template/dashboard_text.daf_xf_euro6.sii
ui/template/dashboard_text.man_tgx_el3.sii
ui/template/dashboard_text.man_tgx_euro6.sii
ui/template/dashboard_text.mercedes_actros_2009.sii
ui/template/dashboard_text.mercedes_actros_2014.sii
ui/template/dashboard_text.renault_t.sii
ui/template/dashboard_text.renault_t_2024.sii
ui/template/dashboard_text.scania_2016.sii
ui/template/dashboard_text.scania_2025.sii
```

*(Volvo FH 2024 GPS `.sii` files are synced at build time into `ui/dashboard/` from `_game_extract/` and may not be present until `build_truckdeck_nav.ps1` is run.)*

---

## How to rebuild

```powershell
cd TruckDeck/TruckDeck.Server/Html/mod
.\build_truckdeck_nav.ps1
# Output: TruckDeck_NAV.scs in this folder
```

Requires a local ETS2 install (script default: `D:\SteamLibrary\steamapps\common\Euro Truck Simulator 2`).

---

## License & attribution note

- **Paper Sun GPS/PC** — original mod assets and design. This TruckDeck fork retains Paper Sun's models, textures, and accessory layout. Please credit **Paper Sun** when distributing derivative work.
- **SCS Software** — ETS2, base vehicle UI extracts used for compatibility sync only.
- **Community truck authors** — see table above; truck defs reference their Workshop content.
- **TruckDeck** — packaging, branding, build tooling, and dashboard compatibility updates.

---

*Document generated: July 2026 — compare bases: `Mod source/` vs `TruckDeck.Server/Html/mod/`.*
