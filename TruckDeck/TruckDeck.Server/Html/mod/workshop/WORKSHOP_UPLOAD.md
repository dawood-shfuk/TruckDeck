# TruckDeck NAV — Steam Workshop upload

Prepare and publish **TruckDeck NAV** using the official **[SCS Workshop Uploader](https://steamcommunity.com/app/362730)** tool (install from Steam library → Tools).

Reference: [SCS modding wiki — How to upload new mod](https://modding.scssoft.com/wiki/Tutorials/SCS_Workshop_Uploader/How_to_upload_new_mod%3F)

---

## 1. Build the Workshop folder

From PowerShell:

```powershell
cd "L:\FUNBIT TS4 src\TruckDeck\TruckDeck.Server\Html\mod"
.\workshop\build_workshop_pack.bat
```

**Browse to this folder in SCS Workshop Uploader (Mod data):**

```
L:\FUNBIT TS4 src\TruckDeck\TruckDeck.Server\Html\mod\workshop\upload
```

It must contain `versions.sii` and a `universal\` subfolder.

**Do NOT browse to:**
- `...\mod\` (source folder)
- `...\upload\universal\` (package only — missing versions.sii)
- A folder that also contains `preview.jpg` (preview is a separate uploader field)

Output layout:

```
workshop\upload\          <-- Mod data folder (browse HERE)
├── versions.sii
└── universal\
    ├── manifest.sii
    ├── mod_description.txt
    ├── mod_icon.jpg      (276 x 162 — required size)
    ├── def\, ui\, material\, vehicle\, automat\
```

**Preview image** (separate field in uploader, not inside mod data):

```
workshop\preview_640x360.jpg
```

---

## 2. Preview image (required)

- Size: **640 × 360** pixels, **JPEG**, max ~1 MB
- Suggested source: original `mod_icon.jpg` from the mod (auto-built with **TruckDeck by Dawood** caption)
- Save as: `workshop\preview_640x360.jpg`
- The build script tries to generate this from `skins\truckdeck_nav\dashboard.jpg`

---

## 3. SCS Workshop Uploader — fields

| Field | Value |
|-------|--------|
| **Game** | Euro Truck Simulator 2 |
| **Item** | New |
| **Mod data folder** | `...\mod\workshop\upload` |
| **Preview image** | `...\mod\workshop\preview_640x360.jpg` |
| **Mod name** | `TruckDeck NAV` |
| **Visibility** | Private first (test), then Public |
| **Description** | Copy from `workshop\workshop_description.txt` |
| **Change note** | `Initial Workshop release v1.50` |

### Tags (suggested)

- **Type:** Other Types → Interior / Accessories (or closest match)
- **Parts:** Cabin accessories
- **Brands:** as appropriate (multi-brand mod — tick several or use Other)

---

## 4. Before you click Upload

- [ ] **Mod data** = `workshop\upload` (contains `versions.sii` + `universal\`)
- [ ] **Preview image** = `workshop\preview_640x360.jpg` (separate browse field)
- [ ] Accept **Steam Workshop Terms of Service** in browser if prompted
- [ ] `mod_icon.jpg` inside `universal\` is **276 x 162** pixels
- [ ] No `.ps1`, `.bat`, `.md`, `.scs`, or `preview.jpg` inside `upload\`
- [ ] Credits in description mention **Paper Sun** (original GPS/PC mod)

### If validation fails

| Error | Fix |
|-------|-----|
| Syntax error in `versions.sii` | Re-run `build_workshop_pack.bat` — browse to **`upload`** folder, not `mod` or `universal` |
| NPOT texture / mipmapping | Build script auto-fixes UI textures; re-run pack |
| Referenced image not found | Usually wrong folder layout — use `upload\` with `versions.sii` at top |
| Extra files not allowed | Remove `preview.jpg` from inside mod data folder |

---

## 5. After publish

1. Copy Workshop URL to [truckdeck.site/downloads](https://truckdeck.site/downloads) page
2. Update `docs/MOD_CHANGELOG.md` with Workshop item link
3. For updates: bump `package_version` in `manifest.sii`, rebuild pack, use **Update existing** in uploader

---

## Updating an existing Workshop item

1. `.\workshop\build_workshop_pack.ps1`
2. SCS Workshop Uploader → select your existing item (not New)
3. Same `upload` folder, new change note, Upload

---

## Files in this folder

| File | Purpose |
|------|---------|
| `versions.sii` | Template copied into `upload\` |
| `build_workshop_pack.ps1` | Builds `upload\universal\` from mod source |
| `workshop_description.txt` | Paste into Steam Workshop description |
| `WORKSHOP_UPLOAD.md` | This guide |
