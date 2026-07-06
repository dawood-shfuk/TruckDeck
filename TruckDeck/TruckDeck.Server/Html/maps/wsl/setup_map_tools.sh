#!/usr/bin/env bash
# Installs truckermudgeon/maps + tippecanoe inside WSL Ubuntu.
set -eu

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
source "$SCRIPT_DIR/common.sh"

LOG_FILE=""
while [[ $# -gt 0 ]]; do
    case "$1" in
        --log-file) LOG_FILE="$2"; shift 2 ;;
        *) shift ;;
    esac
done

MAP_TOOLS_ROOT="$HOME/.truckdeck/map-tools/maps"
TIPPECANOE_SRC="$HOME/.truckdeck/src/tippecanoe"

write_progress 2 "Checking WSL environment..."

export DEBIAN_FRONTEND=noninteractive

write_progress 8 "Updating apt packages..."
sudo apt-get update -y 2>&1 | while read -r line; do write_log "$line"; done

write_progress 12 "Installing build dependencies..."
sudo apt-get install -y \
    git curl ca-certificates gnupg \
    build-essential python3 pkg-config \
    libsqlite3-dev zlib1g-dev 2>&1 | while read -r line; do write_log "$line"; done
write_log "TRUCKDECK_TOOL:git:$(git --version 2>&1 | head -n1)"

write_progress 20 "Installing Node.js 24..."
node_version_ok() {
    command -v node >/dev/null 2>&1 || return 1
    local ver
    ver=$(node -v | sed 's/^v//')
    [[ "$(printf '%s\n%s\n' "24.13.0" "$ver" | sort -V | head -n1)" == "24.13.0" ]]
}

NODE_UPGRADED=false
if ! node_version_ok; then
    curl -fsSL https://deb.nodesource.com/setup_24.x | sudo -E bash - 2>&1 | while read -r line; do write_log "$line"; done
    sudo apt-get install -y nodejs 2>&1 | while read -r line; do write_log "$line"; done
    NODE_UPGRADED=true
fi
if ! node_version_ok; then
    write_log "ERROR: Node.js 24.13+ required (got $(node -v 2>/dev/null || echo missing), npm $(npm -v 2>/dev/null || echo missing))"
    exit 1
fi
write_log "Node: $(node -v)"
write_log "npm: $(npm -v)"
write_log "TRUCKDECK_TOOL:node:$(node -v) · npm $(npm -v)"

write_progress 30 "Installing tippecanoe..."
if command -v tippecanoe >/dev/null 2>&1; then
    write_log "tippecanoe already installed: $(tippecanoe --version 2>&1 | head -n1)"
else
    if apt-cache show tippecanoe >/dev/null 2>&1; then
        sudo apt-get install -y tippecanoe 2>&1 | while read -r line; do write_log "$line"; done || true
    fi
    if ! command -v tippecanoe >/dev/null 2>&1; then
        write_log "Building tippecanoe from source..."
        ensure_dir "$(dirname "$TIPPECANOE_SRC")"
        if [[ ! -d "$TIPPECANOE_SRC/.git" ]]; then
            git clone --depth 1 https://github.com/felt/tippecanoe.git "$TIPPECANOE_SRC"
        fi
        pushd "$TIPPECANOE_SRC" >/dev/null
        make -j"$(nproc)" 2>&1 | while read -r line; do write_log "$line"; done
        sudo make install 2>&1 | while read -r line; do write_log "$line"; done
        popd >/dev/null
    fi
fi
require_cmd tippecanoe
write_log "tippecanoe: $(tippecanoe --version 2>&1 | head -n1)"
write_log "TRUCKDECK_TOOL:tippecanoe:$(tippecanoe --version 2>&1 | head -n1)"

write_progress 45 "Preparing map tools directory..."
ensure_dir "$(dirname "$MAP_TOOLS_ROOT")"

if [[ ! -d "$MAP_TOOLS_ROOT/.git" ]]; then
    if [[ -d "$MAP_TOOLS_ROOT" ]]; then
        write_log "Removing incomplete map-tools folder..."
        rm -rf "$MAP_TOOLS_ROOT"
    fi
    write_progress 50 "Cloning truckermudgeon/maps..."
    git clone --recurse-submodules https://github.com/truckermudgeon/maps.git "$MAP_TOOLS_ROOT" 2>&1 | while read -r line; do write_log "$line"; done
else
    write_log "Map tools repo already present at $MAP_TOOLS_ROOT"
fi

pushd "$MAP_TOOLS_ROOT" >/dev/null
if [[ "$NODE_UPGRADED" == true ]] || [[ -d node_modules ]]; then
    write_log "Removing existing node_modules for clean npm install..."
    rm -rf node_modules
fi

write_progress 65 "Installing npm packages..."
npm install --no-fund --no-audit 2>&1 | while read -r line; do write_log "$line"; done
if [[ ${PIPESTATUS[0]} -ne 0 ]]; then
    write_log "ERROR: npm install failed"
    exit 1
fi

write_progress 80 "Building native parser addon..."
npm run build -w packages/clis/parser 2>&1 | while read -r line; do write_log "$line"; done
if [[ ${PIPESTATUS[0]} -ne 0 ]]; then
    write_log "ERROR: parser build failed"
    exit 1
fi

if [[ ! -d node_modules ]]; then
    write_log "ERROR: npm install did not create node_modules"
    exit 1
fi

popd >/dev/null

write_progress 100 "Map tools ready."
write_log "TRUCKDECK_TOOL:maptools:installed"
write_log "TRUCKDECK_DONE: $MAP_TOOLS_ROOT"
