#!/usr/bin/env bash
# CombiSpeed - One-Click Stopper & Proxy Reset for Linux
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN="$SCRIPT_DIR/combispeed-linux"

echo "Halting CombiSpeed processes and resetting Linux system proxy..."

# Reset GNOME system proxy
if command -v gsettings >/dev/null 2>&1; then
    gsettings set org.gnome.system.proxy mode 'none' 2>/dev/null || true
fi

# Clean up proxy environment file
rm -f "$HOME/.config/combispeed/proxy.env"

# Kill running combispeed processes
pkill -f "CombiSpeed.Linux" 2>/dev/null || true
pkill -f "combispeed-linux" 2>/dev/null || true

echo "CombiSpeed stopped. Linux proxy safely restored."
