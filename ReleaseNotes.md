# Release Notes

## [1.2.0] - 2026-10-30 - Virtual VPN / Adapter Connection & Weighted Bandwidth Bridge Release

### 🚀 Highlights & Features
- **Virtual VPN / Adapter Connection & Bonded Bridge Subsystem**:
  - Automatically creates and manages a native Windows adapter connection: **`CombiSpeed Bonded Bridge`** (`CombiSpeed Virtual Adapter`).
  - Registers directly in Windows Network Connections (`ncpa.cpl`) and Windows Settings -> Network & Internet -> VPN using Windows user-mode phonebook (`rasphone.pbk`) without requiring UAC admin privileges.
  - Acts as the unified virtual network bridge merging all active physical network adapters (Ethernet, Wi-Fi, Cellular, etc.).
- **Strict Percentage Bandwidth Utilization (Smooth Weighted Round Robin)**:
  - Implements mathematically provable **Smooth Weighted Round Robin (SWRR)** in `LoadBalancerEngine.cs`.
  - Distributes all outgoing traffic streams smoothly across the physical network adapters strictly in accordance with user-configured percentage weights (e.g. 70% Wi-Fi, 30% Ethernet), eliminating traffic clustering or burstiness.
  - Automatically calculates and displays the **Effective % of Bonded Pool** (e.g. `60.0% of Bonded Pool`) for each enabled adapter.
- **Dedicated Virtual Bonded Bridge Dashboard Card**:
  - Live status indicator badge (`BONDED & ACTIVE` / `READY TO CONNECT`).
  - Displays Windows connection name, Virtual IP (`10.254.1.2`), Gateway (`10.254.1.1`), and live throughput.
  - **Dynamic Multi-Color Segmented Bandwidth Distribution Bar**: Real-time visual representation of each physical adapter's proportion of the total bonded pool.
  - Action buttons: `Toggle Bridge`, `View in Windows (ncpa.cpl)`, and `Re-provision Adapter`.
- **Individual Physical Adapter Telemetry & Indicators**:
  - Each adapter card features a unique Cyberpunk neon accent color (Cyan, Purple, Emerald, Amber, Pink).
  - Prominent `BANDWIDTH: XX.X% of Bonded Pool` indicator reflecting real-time weight changes.
  - Live individual download/upload speedometers and ping badges.
- **Fail-Safe Virtual Bridge Lifecycle Management**:
  - `Stop-CombiSpeed.bat` and `App.xaml.cs` `--stop` handler include `rasdial "CombiSpeed Bonded Bridge" /DISCONNECT`, guaranteeing direct internet is always restored.
  - `Uninstall-CombiSpeed.bat` automatically removes the VPN connection profile.
  - `Install-CombiSpeed.bat` pre-provisions the virtual adapter connection profile during installation.

---

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
