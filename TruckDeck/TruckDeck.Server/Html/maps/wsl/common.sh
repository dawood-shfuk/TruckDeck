#!/usr/bin/env bash
# Shared helpers for TruckDeck WSL map generation scripts.

LOG_FILE=""

write_log() {
    local line="[$(date +%H:%M:%S)] $*"
    echo "$line"
    if [[ -n "$LOG_FILE" ]]; then
        echo "$line" >> "$LOG_FILE"
    fi
}

write_progress() {
    write_log "TRUCKDECK_PROGRESS:$1 $2"
}

require_cmd() {
    if ! command -v "$1" >/dev/null 2>&1; then
        write_log "ERROR: Missing command: $1"
        exit 1
    fi
}

ensure_dir() {
    mkdir -p "$1"
}
