#!/usr/bin/env bash
# CombiSpeed - One-Click Uninstaller for macOS
set -e

echo "Uninstalling CombiSpeed..."

# Reset proxies
SERVICES=$(networksetup -listallnetworkservices 2>/dev/null | grep -v "An asterisk" || true)
for svc in $SERVICES; do
    networksetup -setwebproxystate "$svc" off 2>/dev/null || true
    networksetup -setsecurewebproxystate "$svc" off 2>/dev/null || true
    networksetup -setsocksfirewallproxystate "$svc" off 2>/dev/null || true
    networksetup -setautoproxystate "$svc" off 2>/dev/null || true
done

rm -rf "/Applications/CombiSpeed.app"
rm -rf "$HOME/Applications/CombiSpeed.app"
rm -f "$HOME/.local/bin/combispeed"
rm -f "$HOME/.config/combispeed/proxy.env"

echo "CombiSpeed macOS uninstalled cleanly."

