#!/usr/bin/env bash
# CombiSpeed - One-Click Stopper & Proxy Reset for macOS
set -e

echo "Halting CombiSpeed processes and resetting macOS network proxies..."

# Reset networksetup proxies for active services
SERVICES=$(networksetup -listallnetworkservices 2>/dev/null | grep -v "An asterisk" || true)

for svc in $SERVICES; do
    networksetup -setwebproxystate "$svc" off 2>/dev/null || true
    networksetup -setsecurewebproxystate "$svc" off 2>/dev/null || true
    networksetup -setsocksfirewallproxystate "$svc" off 2>/dev/null || true
    networksetup -setautoproxystate "$svc" off 2>/dev/null || true
done

rm -f "$HOME/.config/combispeed/proxy.env"

# Terminate running combispeed processes
pkill -f "CombiSpeed.macOS" 2>/dev/null || true
pkill -f "combispeed-macos" 2>/dev/null || true

echo "CombiSpeed stopped. macOS network proxies safely restored."

