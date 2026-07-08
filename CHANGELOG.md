# TruckDeck Changelog

All notable changes to the TruckDeck Windows server, dashboards, and PWA are documented here.
Versions follow the app's `AssemblyInformationalVersion` (see tray "About").

## [1.6.5.2]

### Live Map (formerly "TruckDeck NAV")
- Renamed the `truckdeck_nav` skin to **Live Map** across the app (skin config title, main site copy, credits).
- Turn-by-turn routing removed: the skin now only shows the truck's live position. `dashboard.js` forces the route target to `null`, skipping the whole GPS/job-city matching pipeline, and manual destination picking was removed from the map init options.
- Removed the now-unused "Navigation mode" button from the dashboard header.
- `truckdeck-pmtiles-map.js`: route-dot generation is now clipped to the visible map viewport (plus a padding margin) before being handed to the road-snapping pass, so long-haul routes no longer skip snapping for performance reasons. Added `capAheadDistanceM` and `clipAheadToBounds` helpers and a `_visibleAhead` pipeline step; the map now also refreshes the route display on manual panning (`moveend`).

### Main site (`index.html`)
- Skin picker reorganized into tabs — **TruckDeck skins** and **Funbit OG skins** — backed by the server's existing `group` field per skin.
- Hero copy, featured card captions, and credits updated to describe Live Map's live-position mapping instead of turn-by-turn NAV.

### Trucker's Command Deck
- **Icons recolored to white** to match the white label text, while preserving meaningful color coding (Engine/Hazards stay red, per-axle Suspension arrows keep their blue/orange/purple accents).
- **Icon glyph fixes** so several icons actually match their function: Retarder (was a recycling symbol), Diff Lock (was an X, now a lock), Truck/Trailer Axle (was an obscure math glyph, now an up/down arrow), Roof camera (was a mountain, now a house), Air Horn (was a pager, now a horn), Wipers (rain cloud instead of a tilde-like glyph), Headlights (light bulb instead of a sun).
- **Lights section reflowed** to a 3-column × 2-row grid: Headlights/High Beam, Beacons/Hazards, and a tall Park Brake button spanning both rows in the third column.
- Increased button padding and icon/label gap so icons no longer feel cramped against button edges; capped icon font sizes (max `4vh`) for visual consistency across dense and sparse button rows.

### PWA (mobile dashboard shell)
- The dashboard drawer's skin list now sorts **TruckDeck skins before Funbit OG skins**, matching the tab order on the main site, instead of showing them in raw filesystem order.

### Build
- Version bumped to **1.6.5.2** (`AssemblyInfo.cs`, `TruckDeckSetup.iss`).

## [1.6.5.1]

### Reliability & product feedback loop
- **Update notifications**: the app checks truckdeck.site and prompts when a newer build is available.
- **Verified driver reviews**: an in-app review prompt (after real telemetry usage) links out to a signed review flow; results are tracked with `ClientState` so a device isn't prompted again after reviewing, declining, or being rate-limited (`ReviewService.TryOpenReviewFlowAsync` now returns success/failure so the caller can back off for 20 minutes on failure instead of only prompting once per run).
- **Opt-in crash reporting**, off by default, plus a new manual **"Report a bug…"** tray menu item that opens a small dialog for describing an issue and sending it (with recent log tail) to truckdeck.site.
- Workshop mod description updated to describe all of the above and cross-link the companion Windows app.

### Server hardening
- `StaticFileController` and `PmtilesMiddleware`: hardened path resolution against path-traversal (`..`, drive letters, backslashes) and fixed a root-prefix check that could incorrectly reject files directly under the `Html` root.
- Added explicit routes for map/routing assets (`ets2.pmtiles`, `ats.pmtiles`, `ets2-graph.json`, `ets2-cities.json`, `ats-graph.json`, `ats-cities.json`, `maps/generated/{fileName}`) to avoid attribute-routing edge cases with large binary files.

### TruckDeckDash skin
- Fixed the speed-limit badge to also read `navigation.SpeedLimit`/nested `data.navigation.speedLimit`, and to hide cleanly when no limit is reported instead of showing a stale value.
- Fixed the rest-timer readout via a shared `formatRestRemaining` helper (previously could show an incorrect countdown in some game states).

---

*Earlier history (1.6.3.2 and prior) predates this changelog; see the GitHub release notes and commit messages for that period.*
