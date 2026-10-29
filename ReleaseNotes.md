# Release Notes

## [1.1.1] - 2026-10-29 - One-Click Start, Stop & Installer Release

### 🚀 Highlights & Features
- **One-Click Start Script (`Start-CombiSpeed.bat`)**:
  - Automatically discovers executable across installation, publish, and build directories.
  - Launches CombiSpeed and automatically engages multi-WAN network bonding (`--start`).
  - Supports `--minimized` background launch.
- **One-Click Stop Script (`Stop-CombiSpeed.bat`)**:
  - Signals graceful shutdown to running CombiSpeed instances (`--stop`).
  - Implements multi-layered fail-safe to immediately restore Windows System Proxy to Direct Internet via Registry and WinINet APIs, preventing orphaned proxy states.
  - Terminates any lingering processes.
- **One-Click Automated Installer (`Install-CombiSpeed.bat`)**:
  - Deploys CombiSpeed to modern user application location `%LOCALAPPDATA%\Programs\CombiSpeed` without requiring administrative UAC elevation.
  - Automatically compiles fresh binaries via `.NET SDK` if release package is missing.
  - Creates **Desktop Shortcuts** (`CombiSpeed`, `Start CombiSpeed (Bonding)`, and `Stop CombiSpeed`).
  - Creates **Start Menu Folder** with application shortcuts and uninstaller.
  - Fully registers in **Windows Installed Apps / Add or Remove Programs** (`HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\CombiSpeed`) with display metadata, icon, and uninstaller.
  - Supports silent non-interactive installation (`--silent --no-launch`).
- **One-Click Clean Uninstaller (`Uninstall-CombiSpeed.bat`)**:
  - Halts proxy and restores network settings.
  - Removes Desktop and Start Menu shortcuts.
  - Cleans up Windows Add/Remove Programs registry key and application directory.
- **Embedded Application Icon & Metadata**:
  - Embedded `app.ico` into `CombiSpeed.csproj`, Windows taskbar, title bar, and generated desktop shortcuts.
- **Command-Line Interface (CLI)**:
  - Supported flags: `--start` (`-s`), `--stop`, `--minimized` (`-m`).

---

## [1.1.0] - 2026-10-29 - Native C# .NET Windows Edition

### 🚀 Highlights & Architectural Modernization
- **Full C# .NET 8.0 Windows Application (WPF)**: Re-engineered from ground up as a native Windows desktop application with zero runtime bloat.
- **Micro-Footprint & High Performance**:
  - Standalone executable size of only **~150 KB** (down from 150 MB).
  - Memory consumption of only **~25 MB RAM** (down from 200 MB).
  - Sub-200ms instantaneous startup.
- **Native OS Socket Binding**:
  - Uses `Socket.Bind(new IPEndPoint(adapterIp, 0))` on outbound sockets for explicit adapter routing.
- **Dual-Mode High-Throughput Proxy**:
  - RFC 1928 SOCKS5 Proxy server with IPv4 and Domain Name resolution.
  - HTTP/HTTPS CONNECT forward proxy for direct Windows browser and software compatibility.
- **Intelligent Routing Algorithms**:
  - Dynamic Weighted Round-Robin (WRR) with custom per-adapter load percentage sliders.
  - Equal-Share round-robin.
  - Least-Latency routing prioritizing adapters with lowest real-time ping.
  - Active-Passive failover protection.
- **1-Click Windows System Proxy Integration**:
  - Direct Win32 `wininet.dll` API (`InternetSetOption`) integration with automated registry configuration and fail-safe restoration on application exit.
- **Real-Time Telemetry & Visualizer**:
  - Live download and upload speedometers (KB/s, MB/s) per adapter and aggregate total.
  - Real-time 60-second scrolling throughput waveform graph rendered with WPF dynamic geometry.
  - Continuous RTT latency ping and packet loss health checks.
  - Session and total data transfer statistics.
- **Built-in Speed Test & Multi-Connection Benchmark**:
  - Parallel multi-connection download benchmark to test individual adapter speeds and demonstrate bonded throughput with percentage boost.
- **Futuristic Cyberpunk Glassmorphic WPF GUI**:
  - Custom frameless draggable titlebar with minimize/maximize/close.
  - Glowing master power toggle with animated state indicators.
  - Dark glassmorphic theme with neon cyan and emerald accents.

---

## [1.0.0] - 2026-10-29 - Genesis Release

### 🚀 Highlights & Features
- Initial proof-of-concept and reference architecture analysis.
