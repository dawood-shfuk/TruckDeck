TruckDeck NAV label fonts (MapLibre glyphs)
============================================

City names on the NAV map use MapLibre glyph PBF files. By default the dashboard
probes this folder first, then falls back to OpenFreeMap CDN, then disables
city labels if neither is reachable.

To bundle fonts for offline use, run from Html\maps:

  powershell -ExecutionPolicy Bypass -File fetch_nav_fonts.ps1

Output layout (required by MapLibre):

  fonts/Noto Sans Regular/0-255.pbf
  fonts/Noto Sans Regular/256-511.pbf
  ...

The fetch script downloads common Latin/European ranges used by ETS2 city names.
