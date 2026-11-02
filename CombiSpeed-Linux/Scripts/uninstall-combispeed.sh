#!/usr/bin/env bash
# CombiSpeed - One-Click Uninstaller for Linux
set -e

echo "Uninstalling CombiSpeed..."

# Reset proxy
if command -v gsettings >/dev/null 2>&1; then
    gsettings set org.gnome.system.proxy mode 'none' 2>/dev/null || true
fi
rm -f "$HOME/.config/combispeed/proxy.env"

# Remove binaries and shortcuts
rm -f "$HOME/.local/bin/combispeed"
rm -f "$HOME/.local/share/applications/combispeed.desktop"

echo "CombiSpeed uninstalled cleanly."
