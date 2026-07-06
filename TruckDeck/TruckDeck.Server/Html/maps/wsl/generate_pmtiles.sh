#!/usr/bin/env bash
# Parse ETS2/ATS install and generate PMTiles via truckermudgeon/maps (WSL).
set -eu

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
source "$SCRIPT_DIR/common.sh"

GAME=""
GAME_PATH=""
HTML_ROOT=""
LOG_FILE=""
ACTIVATE=0

while [[ $# -gt 0 ]]; do
    case "$1" in
        --game) GAME="$2"; shift 2 ;;
        --game-path) GAME_PATH="$2"; shift 2 ;;
        --html-root) HTML_ROOT="$2"; shift 2 ;;
        --log-file) LOG_FILE="$2"; shift 2 ;;
        --activate) ACTIVATE=1; shift ;;
        *) shift ;;
    esac
done

if [[ -z "$GAME" || -z "$GAME_PATH" || -z "$HTML_ROOT" ]]; then
    write_log "ERROR: --game, --game-path, and --html-root are required"
    exit 1
fi

MAP_TOOLS_ROOT="$HOME/.truckdeck/map-tools/maps"

if [[ "$GAME" == "ats" ]]; then
    MAP_MODE="usa"
    PMTILES_NAME="ats.pmtiles"
else
    MAP_MODE="europe"
    PMTILES_NAME="ets2.pmtiles"
fi

write_progress 3 "Validating game install..."

GAME_PATH="${GAME_PATH%/}"
if [[ ! -f "$GAME_PATH/base.scs" ]]; then
    write_log "ERROR: base.scs not found in $GAME_PATH"
    exit 1
fi
write_log "Game path: $GAME_PATH"

if [[ ! -d "$MAP_TOOLS_ROOT" ]]; then
    write_log "ERROR: Map tools not installed. Run setup_map_tools.sh first."
    exit 1
fi

WORK_ROOT="$HOME/.truckdeck/map-work/$GAME"
PARSER_OUT="$WORK_ROOT/parser"
GEN_OUT="$WORK_ROOT/generated"
SPRITES_OUT="$WORK_ROOT/sprites"
GENERATED_DIR="$HTML_ROOT/maps/generated"

ensure_dir "$WORK_ROOT"
ensure_dir "$PARSER_OUT"
ensure_dir "$GEN_OUT"
ensure_dir "$SPRITES_OUT"
ensure_dir "$GENERATED_DIR"

pushd "$MAP_TOOLS_ROOT" >/dev/null
trap 'popd >/dev/null' EXIT

    write_progress 8 "Parsing $GAME map data (this can take several minutes)..."
rm -rf "$PARSER_OUT"
ensure_dir "$PARSER_OUT"

npx parser -i "$GAME_PATH" -o "$PARSER_OUT" 2>&1 | while read -r line; do write_log "$line"; done
if [[ ${PIPESTATUS[0]} -ne 0 ]]; then
    write_log "ERROR: map parser failed"
    exit 1
fi

write_progress 52 "Generating sprite sheet..."
rm -rf "$SPRITES_OUT"
ensure_dir "$SPRITES_OUT"
npx generator spritesheet -m usa -m europe -i "$PARSER_OUT" -o "$SPRITES_OUT" 2>&1 | while read -r line; do write_log "$line"; done
if [[ ${PIPESTATUS[0]} -ne 0 ]]; then
    write_log "WARN: spritesheet generation failed — POI icons may be missing until sprites.png is present"
fi

write_progress 55 "Generating PMTiles ($MAP_MODE)..."
rm -rf "$GEN_OUT"
ensure_dir "$GEN_OUT"

OVERRIDES="$MAP_TOOLS_ROOT/packages/clis/generator/resources/trucksim-overrides.json"
GEN_ARGS=(generator map -m "$MAP_MODE" -i "$PARSER_OUT" -o "$GEN_OUT")
if [[ -f "$OVERRIDES" ]]; then
    GEN_ARGS+=(--dataOverridesPath "$OVERRIDES")
fi

npx "${GEN_ARGS[@]}" 2>&1 | while read -r line; do write_log "$line"; done
if [[ ${PIPESTATUS[0]} -ne 0 ]]; then
    write_log "ERROR: PMTiles generator failed"
    exit 1
fi

BUILT_FILE="$GEN_OUT/$PMTILES_NAME"
if [[ ! -f "$BUILT_FILE" ]]; then
    write_log "ERROR: Expected output not found: $BUILT_FILE"
    exit 1
fi

write_progress 88 "Copying to TruckDeck maps folder..."
DEST_GENERATED="$GENERATED_DIR/$PMTILES_NAME"
cp -f "$BUILT_FILE" "$DEST_GENERATED"
write_log "Wrote $DEST_GENERATED"

# Copy MapLibre sprite sheet (POI / company / traffic icons)
for SPRITE in sprites.json sprites.png sprites@2x.png; do
    if [[ -f "$SPRITES_OUT/$SPRITE" ]]; then
        cp -f "$SPRITES_OUT/$SPRITE" "$GENERATED_DIR/$SPRITE"
        write_log "Copied sprite asset: $SPRITE"
    fi
done
if [[ -f "$GENERATED_DIR/sprites@2x.png" && ! -f "$GENERATED_DIR/sprites.png" ]]; then
    cp -f "$GENERATED_DIR/sprites@2x.png" "$GENERATED_DIR/sprites.png"
fi
SPRITES_DIR="$HTML_ROOT/maps/sprites"
ensure_dir "$SPRITES_DIR"
for SPRITE in sprites.json sprites.png sprites@2x.png; do
    if [[ -f "$GENERATED_DIR/$SPRITE" ]]; then
        cp -f "$GENERATED_DIR/$SPRITE" "$SPRITES_DIR/$SPRITE"
    fi
done
if [[ -f "$SPRITES_DIR/sprites@2x.png" && ! -f "$SPRITES_DIR/sprites.png" ]]; then
    cp -f "$SPRITES_DIR/sprites@2x.png" "$SPRITES_DIR/sprites.png"
fi
# MapLibre on HiDPI loads sprites@2x.json — same descriptor as sprites.json (pixelRatio 2 sheet).
if [[ -f "$SPRITES_DIR/sprites.json" && ! -f "$SPRITES_DIR/sprites@2x.json" ]]; then
    cp -f "$SPRITES_DIR/sprites.json" "$SPRITES_DIR/sprites@2x.json"
fi
if [[ -f "$GENERATED_DIR/sprites.json" && ! -f "$GENERATED_DIR/sprites@2x.json" ]]; then
    cp -f "$GENERATED_DIR/sprites.json" "$GENERATED_DIR/sprites@2x.json"
fi

if [[ "$ACTIVATE" -eq 1 ]]; then
    DEST_ACTIVE="$HTML_ROOT/$PMTILES_NAME"
    cp -f "$BUILT_FILE" "$DEST_ACTIVE"
    write_log "Activated map: $DEST_ACTIVE"
fi

write_progress 90 "Generating routing graph for NAV..."
GRAPH_OUT="$WORK_ROOT/graph"
rm -rf "$GRAPH_OUT"
ensure_dir "$GRAPH_OUT"

if npx generator graph -m "$MAP_MODE" -i "$PARSER_OUT" -o "$GRAPH_OUT" 2>&1 | while read -r line; do write_log "$line"; done; then
    RAW_GRAPH="$GRAPH_OUT/$MAP_MODE-graph.json"
    RAW_NODES="$PARSER_OUT/$MAP_MODE-nodes.json"
    RAW_CITIES="$PARSER_OUT/$MAP_MODE-cities.json"
    if [[ -f "$RAW_GRAPH" && -f "$RAW_NODES" && -f "$RAW_CITIES" ]]; then
        node "$SCRIPT_DIR/build-graph.js" \
            --game "$GAME" \
            --nodes "$RAW_NODES" \
            --graph "$RAW_GRAPH" \
            --cities "$RAW_CITIES" \
            --mapToolsRoot "$MAP_TOOLS_ROOT" \
            --out "$GENERATED_DIR" 2>&1 | while read -r line; do write_log "$line"; done

        if [[ "$ACTIVATE" -eq 1 ]]; then
            cp -f "$GENERATED_DIR/$GAME-graph.json" "$HTML_ROOT/$GAME-graph.json" 2>/dev/null || true
            cp -f "$GENERATED_DIR/$GAME-cities.json" "$HTML_ROOT/$GAME-cities.json" 2>/dev/null || true
        fi
        write_log "Wrote routing graph + city lookup for NAV route line"
    else
        write_log "WARN: routing graph inputs missing, skipping NAV route data (map will still work without a route line)"
    fi
else
    write_log "WARN: routing graph generation failed, skipping NAV route data (map will still work without a route line)"
fi

SIZE_MB=$(du -m "$DEST_GENERATED" | cut -f1)
write_progress 100 "Done. $PMTILES_NAME (${SIZE_MB} MB)"
write_log "TRUCKDECK_DONE: $DEST_GENERATED"
