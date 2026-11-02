#!/usr/bin/env bash
# CombiSpeed - One-Click Installer for Linux
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_DIR="$HOME/.local/bin"
DESKTOP_DIR="$HOME/.local/share/applications"
ICON_DIR="$HOME/.local/share/icons/hicolor/scalable/apps"

mkdir -p "$INSTALL_DIR"
mkdir -p "$DESKTOP_DIR"
mkdir -p "$ICON_DIR"

echo "Installing CombiSpeed to $INSTALL_DIR..."

if [ -f "$SCRIPT_DIR/combispeed-linux" ]; then
    cp "$SCRIPT_DIR/combispeed-linux" "$INSTALL_DIR/combispeed"
    chmod +x "$INSTALL_DIR/combispeed"
elif [ -f "$SCRIPT_DIR/CombiSpeed.Linux" ]; then
    cp "$SCRIPT_DIR/CombiSpeed.Linux" "$INSTALL_DIR/combispeed"
    chmod +x "$INSTALL_DIR/combispeed"
fi

if [ -f "$SCRIPT_DIR/combispeed.desktop" ]; then
    cp "$SCRIPT_DIR/combispeed.desktop" "$DESKTOP_DIR/combispeed.desktop"
    chmod +x "$DESKTOP_DIR/combispeed.desktop"
fi

echo "====================================================================="
echo " CombiSpeed installed successfully to $INSTALL_DIR/combispeed"
echo " Desktop application entry created: $DESKTOP_DIR/combispeed.desktop"
echo " You can launch CombiSpeed from your Application Menu or run 'combispeed'"
echo "====================================================================="
