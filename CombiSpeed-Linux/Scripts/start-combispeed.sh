#!/usr/bin/env bash
# CombiSpeed - One-Click Launcher for Linux
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN="$SCRIPT_DIR/combispeed-linux"

if [ ! -f "$BIN" ]; then
    BIN="$SCRIPT_DIR/CombiSpeed.Linux"
fi

if [ ! -f "$BIN" ]; then
    echo "Executable not found in $SCRIPT_DIR. Running with dotnet..."
    dotnet run --project "$SCRIPT_DIR/../CombiSpeed.Linux.csproj" -c Release -- --start "$@"
    exit 0
fi

chmod +x "$BIN"
echo "Starting CombiSpeed Bonding Engine..."
exec "$BIN" --start "$@"

