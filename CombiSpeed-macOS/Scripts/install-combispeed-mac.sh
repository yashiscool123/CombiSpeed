#!/usr/bin/env bash
# CombiSpeed - One-Click Installer for macOS
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN="$SCRIPT_DIR/combispeed-macos"
APP_DIR="/Applications/CombiSpeed.app"
USER_APP_DIR="$HOME/Applications/CombiSpeed.app"
TARGET_APP="$USER_APP_DIR"

if [ -w "/Applications" ]; then
    TARGET_APP="$APP_DIR"
fi

echo "Installing CombiSpeed to $TARGET_APP..."

mkdir -p "$TARGET_APP/Contents/MacOS"
mkdir -p "$TARGET_APP/Contents/Resources"

if [ -f "$SCRIPT_DIR/Info.plist" ]; then
    cp "$SCRIPT_DIR/Info.plist" "$TARGET_APP/Contents/Info.plist"
elif [ -f "$SCRIPT_DIR/../CombiSpeed.app/Contents/Info.plist" ]; then
    cp "$SCRIPT_DIR/../CombiSpeed.app/Contents/Info.plist" "$TARGET_APP/Contents/Info.plist"
fi

if [ -f "$BIN" ]; then
    cp "$BIN" "$TARGET_APP/Contents/MacOS/CombiSpeed"
    chmod +x "$TARGET_APP/Contents/MacOS/CombiSpeed"
elif [ -f "$SCRIPT_DIR/CombiSpeed.macOS" ]; then
    cp "$SCRIPT_DIR/CombiSpeed.macOS" "$TARGET_APP/Contents/MacOS/CombiSpeed"
    chmod +x "$TARGET_APP/Contents/MacOS/CombiSpeed"
fi

# Link to /usr/local/bin or ~/.local/bin
LINK_DIR="$HOME/.local/bin"
mkdir -p "$LINK_DIR"
ln -sf "$TARGET_APP/Contents/MacOS/CombiSpeed" "$LINK_DIR/combispeed"

echo "====================================================================="
echo " CombiSpeed installed successfully to $TARGET_APP"
echo " CLI shortcut available at $LINK_DIR/combispeed"
echo " You can launch CombiSpeed from Finder / Spotlight or run 'combispeed'"
echo "====================================================================="

