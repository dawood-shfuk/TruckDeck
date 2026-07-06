#!/usr/bin/env bash
# Build NAV routing sidecars (ets2-graph.json, ets2-cities.json) from cached parser output.
set -eu

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
source "$SCRIPT_DIR/common.sh"

GAME=""
HTML_ROOT=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --game) GAME="$2"; shift 2 ;;
        --html-root) HTML_ROOT="$2"; shift 2 ;;
        *) shift ;;
    esac
done

if [[ -z "$GAME" || -z "$HTML_ROOT" ]]; then
    write_log "ERROR: --game and --html-root are required"
    exit 1
fi

MAP_TOOLS_ROOT="$HOME/.truckdeck/map-tools/maps"
MAP_MODE="europe"
[[ "$GAME" == "ats" ]] && MAP_MODE="usa"

WORK_ROOT="$HOME/.truckdeck/map-work/$GAME"
PARSER_OUT="$WORK_ROOT/parser"
GRAPH_OUT="$WORK_ROOT/graph"
GENERATED_DIR="$HTML_ROOT/maps/generated"

if [[ ! -d "$MAP_TOOLS_ROOT" ]]; then
    write_log "ERROR: Map tools not installed. Run setup_map_tools.sh first."
    exit 1
fi

if [[ ! -f "$PARSER_OUT/$MAP_MODE-nodes.json" || ! -f "$PARSER_OUT/$MAP_MODE-cities.json" ]]; then
    write_log "ERROR: Parser cache missing at $PARSER_OUT — run full map generation first."
    exit 1
fi

ensure_dir "$GENERATED_DIR"
rm -rf "$GRAPH_OUT"
ensure_dir "$GRAPH_OUT"

write_progress 10 "Generating routing graph ($MAP_MODE)..."
pushd "$MAP_TOOLS_ROOT" >/dev/null
trap 'popd >/dev/null' EXIT

if ! npx generator graph -m "$MAP_MODE" -i "$PARSER_OUT" -o "$GRAPH_OUT" 2>&1 | while read -r line; do write_log "$line"; done; then
    write_log "ERROR: generator graph failed"
    exit 1
fi

RAW_GRAPH="$GRAPH_OUT/$MAP_MODE-graph.json"
RAW_NODES="$PARSER_OUT/$MAP_MODE-nodes.json"
RAW_CITIES="$PARSER_OUT/$MAP_MODE-cities.json"

if [[ ! -f "$RAW_GRAPH" ]]; then
    write_log "ERROR: Missing $RAW_GRAPH"
    exit 1
fi

write_progress 80 "Building compact NAV routing files..."
node "$SCRIPT_DIR/build-graph.js" \
    --game "$GAME" \
    --nodes "$RAW_NODES" \
    --graph "$RAW_GRAPH" \
    --cities "$RAW_CITIES" \
    --mapToolsRoot "$MAP_TOOLS_ROOT" \
    --out "$GENERATED_DIR" 2>&1 | while read -r line; do write_log "$line"; done

cp -f "$GENERATED_DIR/$GAME-graph.json" "$HTML_ROOT/$GAME-graph.json"
cp -f "$GENERATED_DIR/$GAME-cities.json" "$HTML_ROOT/$GAME-cities.json"

write_progress 100 "Routing data ready for NAV"
write_log "TRUCKDECK_DONE: $GENERATED_DIR/$GAME-graph.json"
