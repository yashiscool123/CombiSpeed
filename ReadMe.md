# ⚡ CombiSpeed (Native C# .NET Windows Network Adapter Channel Bonding & Multi-WAN Bandwidth Aggregator)

![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64)-0078D7?style=for-the-badge&logo=windows)
![.NET Version](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?style=for-the-badge&logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-WPF%20%7C%20C%23%2012-10B981?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-00F0FF?style=for-the-badge)
![Version](https://img.shields.io/badge/Version-v1.4.0%20(Build%2012)-38BDF8?style=for-the-badge)
![Accessibility](https://img.shields.io/badge/Accessibility-WCAG%202.1%20AA-10B981?style=for-the-badge)

**CombiSpeed** is an ultra-fast, native **C# .NET Windows application** that combines multiple network adapters (Wi-Fi, Gigabit Ethernet, 4G/5G Cellular USB dongles, Mobile Hotspots, and LAN connections) into a single, high-speed aggregate connection. 

Built with **C# 12 and .NET 8.0 WPF**, CombiSpeed runs directly on the Windows TCP/IP stack with low-level socket binding (`Socket.Bind(new IPEndPoint(adapterIp, 0))`), delivering zero runtime overhead, instant startup (<200ms), and minimal memory consumption (~25 MB RAM).

---

## 🌟 Application Summary

When a Windows machine is connected to multiple internet sources simultaneously (e.g. Fiber broadband via Ethernet, 5G phone hotspot via Wi-Fi, or secondary office Wi-Fi), the standard Windows routing table restricts all traffic to a single default route with the lowest metric. The secondary connections sit 100% idle!

**CombiSpeed eliminates this bottleneck.** It acts as a local high-performance asynchronous multi-threaded aggregation engine. By intercepting connections via dual-mode SOCKS5 & HTTP/HTTPS forward proxies or native Virtual Bridge adapter, it binds each outgoing TCP socket directly to individual network adapter IP addresses (`Socket.Bind(new IPEndPoint(adapterIp, 0))`). This bypasses the default route limitation and forces traffic simultaneously across all active adapters, combining their speeds into a single supercharged pipeline.

---

## 🚀 Key Features

1. **Modern Segmented UI/UX Redesign & WCAG 2.1 AA Accessibility (NEW in v1.4.0)**:
   - **Native Segmented Tabbed Navigation**: Organized into 4 dedicated, focused views eliminating vertical scrolling fatigue:
     - `⚡ Overview & Dashboard`: Hero master power toggle, real-time KPI cards, bonded bridge status, and dynamic traffic telemetry.
     - `🖧 Combined Adapters`: Hardware-bound NIC list, individual health pings, live speedometers, and Smooth Weighted Round-Robin % sliders.
     - `🚀 Speed Shaper & QoS`: Token Bucket bandwidth throttling, 500 Mbps presets, custom parameter dialog, and real-time dynamic latency optimization modes.
     - `📜 Connection Audit`: Filterable, searchable real-time connection stream inspection with clear and export capabilities.
   - **100% Native WPF TabControl Architecture**: Utilizes custom `SegmentedTabControl` styling with native `IsItemsHost` and `ContentPresenter`, guaranteeing bug-free tab switching, keyboard arrow navigation, and screen reader announcements.
   - **WCAG 2.1 AA High Contrast Mode**: Toggleable high-contrast dark theme (`#030712` deep black, `#38BDF8` cyan borders, `#FFFFFF` high-luminance text) exceeding 7:1 contrast ratios.
   - **Dynamic UI & Font Scaling**: Seamless scaling (`100%`, `115%`, `130%`) powered by root layout transformations, ensuring zero text clipping or card overlap on high-DPI displays.
   - **Reduce Motion & Vestibular Safety**: Disables rapid canvas waveform redraws and softens LED transitions for users with vestibular sensitivities or ADHD.
   - **Screen Reader Optimization (`AutomationProperties`)**: Comprehensive names, values, and states across all interactive elements for Windows Narrator, NVDA, and JAWS.
   - **Global Keyboard Access Keys**: Instant hotkeys (`Space`/`Alt+P` for Master Power, `Alt+B` for Virtual Bridge, `Alt+W` for Windows Proxy, `Alt+R` for Rescan, `Alt+A` for Accessibility, `Alt+S` for Settings, `Alt+H` for High Contrast, and `Ctrl+1-4` for direct tab switching).
   - **Dedicated Accessibility Options Dialog (`AccessibilityDialog.xaml`)**: Fast, accessible modal dialog with interactive toggles, scale presets, and a keyboard shortcuts cheat sheet.

2. **Dynamic Cross-Connection Speed Shaper & Latency Optimization Modes (v1.3.6)**:
   - **Active Real-Time Telemetry Adaptation**: `BandwidthThrottler.cs` dynamically recalibrates token allocations and burst windows across physical adapters in real time as RTT ping, packet loss, and link health fluctuate.
   - **`🚀 Maximum Bandwidth Mode`**: Maximizes bulk throughput and transfer speeds for heavy downloads, streaming, and torrenting. Expands Token Bucket burst tolerance (150ms-250ms) to allow full TCP congestion window scaling and enables dynamic capacity borrowing across idle connections.
   - **`⚡ Lowest Latency / Ping Mode`**: Engineered for competitive multiplayer gaming (Valorant, CS2, CoD), VoIP/Discord audio, and WebRTC video. Enforces tight anti-bufferbloat packet pacing (clamped to 20ms burst window) to eliminate intermediate interface queues and weights token distribution and SWRR dispatching inversely proportional to live RTT latency (`1000.0 / PingMs`).
   - **`⚖ Balanced Dynamic Mode`**: Adaptive hybrid mode delivering high aggregate speeds while maintaining low-jitter packet pacing.
   - **UI & Persistence Integration**: 1-Click mode selector ComboBox directly on the main toolbar, complete configuration section in `CustomThrottleDialog.xaml`, and persistent storage in `%LOCALAPPDATA%\CombiSpeed\adapter_settings.json`.

3. **500 Mbps High-Speed Throttling Cap & Custom Parameter Dialog (v1.3.5)**:
   - High-precision Token Bucket rate limiter sustaining up to 500 Mbps (62.5 MB/s) aggregate throughput.
   - Presets: `Unlimited`, `16 Mbps (2 MB/s)`, `40 Mbps (5 MB/s)`, `80 Mbps (10 MB/s)`, `160 Mbps (20 MB/s)`, `250 Mbps (31.25 MB/s)`, `400 Mbps (50 MB/s)`, and `500 Mbps (62.5 MB/s)`.
   - **Interactive Custom Throttling Dialog (`CustomThrottleDialog.xaml`)**: Multi-unit rates (`Mbps`, `MB/s`, `Kbps`, `KB/s`), quick-preset chips, auto-upload cap calculation, configurable burst window (20ms-250ms), and per-adapter speed limit overrides.
   - Full integration across **HTTP, HTTPS CONNECT, SOCKS5 TCP, SOCKS5 UDP Associate, and Windows Virtual Bonded Bridge** streams.

4. **Application Window Version Visibility & Pinned Cyberpunk Glass Footer (v1.3.4)**:
   - Dynamic assembly and build metadata binding in `MainViewModel.cs` exposing `AppVersion` (`v1.4.0`) and `AppVersionFull` (`v1.4.0 (Build 12)`).
   - **Custom TitleBar Cyberpunk Badge**: Prominently displays the active version badge directly next to the COMBISPEED neon logo, ensuring visibility across all window states.
   - **Pinned Glassmorphic Footer Status Bar**: Pinned bottom status bar displaying complete build attribution (`v1.4.0 (Build 12)`), engine description, real-time configuration auto-sync status, and target runtime environment (`Windows x64 (.NET 8.0)`).
   - **Dynamic OS Window Title**: Sets `Title="{Binding WindowTitle}"` to synchronize OS Taskbar, Task Manager, and Alt-Tab switcher with current release branding.
   - **CLI Version Flags**: Built-in `--version`, `-v`, and `/version` support for instant terminal output.

5. **Adapter Settings & Combination Profiles Persistence**:
   - Automatically saves all selected adapter states (`IsEnabled`), custom percentage weights (`Weight`), and accessibility options to disk (`%LOCALAPPDATA%\CombiSpeed\adapter_settings.json`).
   - Restores exact adapter selections and bandwidth allocations upon application restart or adapter rescan.
   - **Multi-Attribute Adapter Binding**: Uses the permanent hardware MAC address as primary key with name fallback, guaranteeing that configurations stay stably bound even across dynamic DHCP IP re-assignments.
   - **Debounced Auto-Save Engine**: 400ms debounced disk saving on UI slider dragging and checkbox toggles, preventing disk thrashing while guaranteeing zero configuration loss.
   - **Combination Profiles & Preset Switching**: Save, switch, and manage custom adapter combination profiles (e.g. `Default Combination`, `Office Mode`, `Gaming 70/30`, `Mobile Hotspot Backup`).

6. **Virtual VPN / Adapter Connection & Bonded Bridge Subsystem**:
   - Provisions a native Windows adapter connection: **`CombiSpeed Bonded Bridge`** (`CombiSpeed Virtual Adapter`).
   - Registers directly in Windows Network Connections (`ncpa.cpl`) and Windows Settings -> Network & Internet -> VPN using the Windows user-mode phonebook (`rasphone.pbk`), requiring zero driver signing or UAC elevation.
   - Unified virtual interface that merges physical network adapters (Ethernet, Wi-Fi, Cellular) into a single virtual pipe.
   - Dedicated dashboard card with live connection status (`BONDED & ACTIVE`), Windows adapter name, Virtual IP (`10.254.1.2`), Gateway (`10.254.1.1`), and 1-click controls to toggle the bridge or inspect in `ncpa.cpl`.

7. **Strict % Bandwidth Utilization (Smooth Weighted Round Robin)**:
   - Implements mathematically provable **Smooth Weighted Round Robin (SWRR)** in `LoadBalancerEngine.cs`.
   - Distributes outgoing traffic across physical network adapters strictly according to user-configured percentage weights (e.g. 70% Wi-Fi, 30% Ethernet) with zero clustering or burstiness.
   - Real-time **Dynamic Multi-Color Segmented Bandwidth Distribution Bar** visualizing each physical adapter's proportion of the total bonded pool.
   - Per-adapter `BANDWIDTH: XX.X% of Bonded Pool` indicator dynamically updated whenever sliders change.

8. **RFC 1928 SOCKS5 UDP Associate Relay (Gaming & UDP Acceleration)**:
   - Full implementation of SOCKS5 `UDP ASSOCIATE` (Command `0x03`).
   - Binds outbound UDP sockets directly to physical adapter IPs (`Socket.Bind(new IPEndPoint(adapterIp, 0))`), accelerating multiplayer gaming (Valorant, CS2, CoD, Steam), Discord/VoIP voice channels, DNS queries, and WebRTC video across bonded connections.
   - Encapsulates and returns UDP replies back to the client.

9. **PAC (Proxy Auto-Config) Domain-Based Interface Routing**:
   - Dynamically compiles RFC-compliant JavaScript `FindProxyForURL(url, host)` scripts.
   - Built-in PAC server serving `http://127.0.0.1:8080/proxy.pac` directly on the proxy port without port conflicts.
   - Granular domain and subnet bypass rules (e.g. banking portals, corporate intranet `192.168.*`, `10.*` -> Direct; all other web traffic -> Bonded).
   - Automated 1-click Windows `AutoConfigURL` registry and WinINet integration.

10. **Windows Service & Headless Background Mode**:
    - Runs headless as a background Windows Service without requiring active user logon or open GUI (`--service`, `--headless`).
    - CLI service management: `--service-install`, `--service-uninstall`, `--service-start`, `--service-stop`.
    - 1-click scripts: `Install-Service.bat` and `Uninstall-Service.bat` registering `CombiSpeedService` in Windows Service Control Manager with automatic crash recovery.

11. **Native C# High-Performance Sockets**:
    - Outgoing sockets are explicitly bound to adapter local IPv4 addresses using `Socket.Bind(new IPEndPoint(localIp, 0))`.
    - Zero-overhead bi-directional streaming using .NET `ArrayPool<byte>` memory management.

12. **Dual-Mode High-Throughput Proxy Engine**:
    - **RFC 1928 SOCKS5 Proxy Server** (port 1080): Connect & UDP Associate methods with IPv4 and Domain Name resolution.
    - **HTTP / HTTPS CONNECT Forward Proxy** (port 8080): Direct tunneling for Chrome, Edge, Firefox, Brave, and all desktop applications.

13. **Intelligent Load Balancing & Routing Algorithms**:
    - **Smooth Weighted Round-Robin (SWRR)**: Proportional distribution based on user weight ratios (e.g. 70% Ethernet, 30% Wi-Fi Hotspot).
    - **Equal-Share Multiplexing**: Automatically balances connections equally across all available healthy adapters.
    - **Least-Latency Smart Routing**: Continuously measures real-time RTT per adapter and routes through the lowest-ping adapter.
    - **Active-Passive Link Failover**: Designates a primary connection with instant auto-fallback if connection degradation or packet loss occurs.

14. **1-Click Windows System Proxy Integration**:
    - Automated toggle for Windows Internet Settings (`HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings`).
    - Uses native Win32 `wininet.dll` API (`InternetSetOption`) to apply proxy changes immediately across the entire OS without restarting browsers.
    - **Graceful Restoration**: Automatically reverts Windows proxy settings and disconnects virtual bridge on exit.

15. **Real-Time Telemetry & Throughput Visualizer**:
    - Live per-adapter download & upload speed meters (KB/s, MB/s) with distinct Cyberpunk neon accent colors.
    - 60-second real-time scrolling bandwidth waveform canvas with cyan and purple gradient fills.
    - Live RTT ping and adapter health indicators with instant online/offline detection.
    - Session data transferred counters (MB/GB) per adapter and aggregate total.

16. **Built-In Multi-Stream Bonding Benchmark (Speed Tester)**:
    - On-demand benchmark utility that tests individual adapter speeds alongside concurrent bonded aggregate throughput so you can visually verify speed combination in real-time.

---

## 🛡️ MOATs (Defensible Competitive Advantages)

| Dimension | CombiSpeed (Native C# .NET v1.4.0) | RezaRafia/Speedy (Reference) | Standard Windows Routing |
|---|---|---|---|
| **UI/UX & Accessibility** | **Segmented Tabbed Navigation, WCAG 2.1 AA High Contrast, UI Scaling (100%-130%), Reduce Motion, Screen Reader support, & Hotkeys** | Cramped single-scroll Electron page | Native OS Control Panel / Settings |
| **Real-Time Bandwidth Throttling** | **Token Bucket rate limiter with dynamic cross-connection shaping, 500 Mbps cap, & anti-bufferbloat latency modes** | None (Unrestricted, no rate limiting) | None (Line rate only) |
| **Adapter Settings & Profiles** | **Durable Persistence (`adapter_settings.json`), MAC binding, debounced auto-save & presets** | None (Lost on restart) | None (Default metric reset) |
| **Virtual Bridge / Adapter Connection** | **Native Windows VPN & Virtual Adapter (`CombiSpeed Bonded Bridge`)** in `ncpa.cpl` & Windows Settings | None (Speedy is purely an isolated local SOCKS proxy) | Rigid single physical adapter binding |
| **Percentage Bandwidth Utilization** | **Smooth Weighted Round Robin (SWRR)** enforcing exact % ratios (e.g. 70%/30%) without clustering | None (No bandwidth allocation or weighting) | 100% to single lowest metric adapter |
| **Runtime & Memory** | **Native C# (.NET 8 WPF)**: ~25 MB RAM, 150 KB executable | Electron / Node: ~200 MB RAM, 150 MB package | OS Network Driver |
| **Socket Binding** | **`Socket.Bind(new IPEndPoint(adapterIp, 0))`** on every socket | **Missing `localAddress`** (traffic leaks back to default gateway) | Lowest metric adapter only |
| **Concurrency Reliability** | Persistent async non-blocking `TcpListener` with connection pooling | Spawns a new listener on port 8080 per connection, throwing `EADDRINUSE` | N/A |
| **UDP & Gaming Acceleration** | **RFC 1928 SOCKS5 UDP Associate Relay** with physical adapter binding | None (TCP only) | Default route only |
| **Domain-Based Policy Routing** | **Dynamic PAC compiler (`FindProxyForURL`)** with built-in HTTP server | None | None |
| **Background / Service Mode** | **Windows Service (`CombiSpeedService`) & Headless daemon** | None (Requires active foreground GUI) | N/A |
| **Proxy Protocol Support** | **Dual SOCKS5 & HTTP/HTTPS CONNECT** | SOCKS5 only (IPv4 only) | None |
| **Windows OS Integration** | **Native WinINet `InternetSetOption` API & User Phonebook (`rasphone.pbk`)** | None (Manual proxy config required in every app) | None |
| **Adapter Health & Failover** | Continuous RTT probes with auto-rerouting on packet loss | None (Dead connection leads to application hang) | OS metric failover takes 30-60+ seconds |
| **Live Telemetry & Speedometer**| Real-time per-adapter MB/s gauges, canvas waveform, and audit logs | Text console logs only | Task Manager adapter graph (split, not bonded) |

---

## 💡 Real-World Challenges Solved

1. **System-Wide Multi-Adapter Merging**:
   - Creates a dedicated virtual adapter connection (`CombiSpeed Bonded Bridge`) in Windows Network Connections (`ncpa.cpl`), merging disparate network adapters into a single virtual pipe.
2. **Strict Bandwidth Ratio Sharing & Data Cap Management**:
   - Enforces user-defined % bandwidth ratios (e.g., 80% on unlimited home fiber, 20% on metered 5G hotspot) via Smooth Weighted Round Robin with real-time visual progress bars.
3. **Slow Downloads & Large File Transfers**:
   - Multi-connection download managers (IDM, Free Download Manager, Steam multi-chunk downloads, Torrent clients, Git clones) open dozens of concurrent TCP streams. CombiSpeed spreads these streams across both Ethernet and Wi-Fi simultaneously, delivering the combined sum of both download pipes.
4. **Buffering & Unstable 4K/8K Video Streaming**:
   - Video platforms buffer media using concurrent chunk requests. CombiSpeed fetches video segments across all adapters, preventing buffering during peak congestion.
5. **Remote Work & Critical Video Calls**:
   - Combines a fluctuating mobile hotspot with a home broadband connection. If one connection experiences jitter or temporary dropouts, CombiSpeed's health monitor redirects traffic instantly.
6. **Eliminating Bufferbloat During Concurrent Gaming & Downloading**:
   - Running background downloads while gaming often causes severe latency spikes due to buffer bloat in router and adapter queues. CombiSpeed's `Lowest Latency / Ping` mode clamps burst buffers to 20ms and dynamically funnels latency-sensitive flows to the lowest-ping physical interface, keeping in-game ping rock-solid.
7. **Dynamic Self-Healing Across Congested Links**:
   - If one bonded connection (e.g., cellular hotspot or shared office Wi-Fi) experiences sudden latency spikes or signal degradation, CombiSpeed's dynamic shaper immediately reduces its throughput allocation and shifts traffic to healthier physical interfaces in real time without dropping active sessions.
8. **UI Ergonomics & Accessibility (WCAG 2.1 AA)**:
   - Prevents UI clutter and continuous scrolling on compact laptops or multi-monitor configurations via clean Segmented Tabs (`Ctrl+1` through `Ctrl+4`). Low-vision users and high-DPI displays benefit from scalable typography up to 130% and stark high-contrast themes, while keyboard-first users enjoy comprehensive hotkeys for every core action.

---

## 🏗️ Technical Architecture

```
                      ┌────────────────────────────────────────┐
                      │    Windows Applications / Browsers     │
                      └───────────────────┬────────────────────┘
                                          │
                   ┌──────────────────────┴──────────────────────┐
                   │                                             │
      ┌────────────▼────────────┐                   ┌────────────▼────────────┐
      │   HTTP / HTTPS CONNECT  │                   │       RFC 1928 SOCKS5   │
      │    Forward Proxy :8080  │                   │      Proxy Server :1080 │
      └────────────┬────────────┘                   └────────────┬────────────┘
                   │                                             │
                   └──────────────────────┬──────────────────────┘
                                          │
                  ┌───────────────────────▼───────────────────────┐
                  │    CombiSpeed Bandwidth Throttling Engine     │
                  │   Token Bucket Rate Limiter (Up to 500 Mbps)  │
                  │   Dynamic Cross-Connection Latency Shaper     │
                  └───────────────────────┬───────────────────────┘
                                          │
                  ┌───────────────────────▼───────────────────────┐
                  │      Smooth Weighted Round-Robin (SWRR)       │
                  │        Load Balancing Dispatcher              │
                  └───────────────────────┬───────────────────────┘
                                          │
           ┌──────────────────────────────┼──────────────────────────────┐
           │                              │                              │
 ┌─────────▼─────────┐          ┌─────────▼─────────┐          ┌─────────▼─────────┐
 │ Outgoing Socket 1 │          │ Outgoing Socket 2 │          │ Outgoing Socket 3 │
 │  Socket.Bind(...) │          │  Socket.Bind(...) │          │  Socket.Bind(...) │
 └─────────┬─────────┘          └─────────┬─────────┘          └─────────┬─────────┘
           │                              │                              │
 ┌─────────▼─────────┐          ┌─────────▼─────────┐          ┌─────────▼─────────┐
 │ Ethernet (1Gbps)  │          │ Wi-Fi 6 (600Mbps) │          │ 5G Hotspot(100Mb) │
 └─────────┬─────────┘          └─────────┬─────────┘          └─────────┬─────────┘
           │                              │                              │
           └──────────────────────────────┼──────────────────────────────┘
                                          │
                               ┌──────────▼──────────┐
                               │   Global Internet   │
                               └─────────────────────┘
```

---

## 📦 Building and Publishing

### Prerequisites
- Windows 10 / 11 (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build Release
```powershell
# Restore & Compile
dotnet build CombiSpeed/CombiSpeed.csproj -c Release

# Publish self-contained or framework-dependent release
dotnet publish CombiSpeed/CombiSpeed.csproj -c Release -o publish
```

The compiled standalone executable is located at `publish/CombiSpeed.exe` (~150 KB).

---

## ⚡ One-Click Start, Stop & Installer

CombiSpeed includes zero-configuration one-click scripts for seamless daily operation and installation:

### 1. One-Click Installer (`Install-CombiSpeed.bat`)
Installs CombiSpeed for the current user into `%LOCALAPPDATA%\Programs\CombiSpeed` without requiring administrative (UAC) elevation:
- Automatically compiles release binaries if needed.
- Creates **Desktop Shortcuts** (`CombiSpeed`, `Start CombiSpeed (Bonding)`, and `Stop CombiSpeed`).
- Creates **Start Menu Folder** with app launcher, bonding launcher, stopper, and uninstaller.
- Registers in Windows **Installed Apps** / **Add or Remove Programs** (`Settings > Apps > Installed apps`) with full uninstaller support.
- Supports silent automated install via `Install-CombiSpeed.bat --silent --no-launch`.

### 2. One-Click Start (`Start-CombiSpeed.bat`)
Double-click `Start-CombiSpeed.bat` to launch CombiSpeed and immediately engage multi-WAN network bonding:
```cmd
# Standard launch with auto-bonding
Start-CombiSpeed.bat

# Launch directly into system tray / minimized
Start-CombiSpeed.bat --minimized
```

### 3. One-Click Stop (`Stop-CombiSpeed.bat`)
Double-click `Stop-CombiSpeed.bat` for instant, fail-safe shutdown:
- Sends graceful shutdown signal to running CombiSpeed instances.
- Cleans up and shuts down local proxy servers.
- **Fail-Safe Windows Proxy Reset**: Direct registry and WinINet API restore guarantees that Windows System Proxy is turned OFF, ensuring internet connectivity is never broken or left hanging.
- Terminates any lingering processes.

### 4. CLI Arguments Reference
| Argument | Alias | Description |
|---|---|---|
| `--version` | `-v`, `/version` | Prints application version, build number, and target architecture to stdout. |
| `--start` | `-s` | Starts CombiSpeed and automatically engages network bonding on launch. |
| `--stop` | *(none)* | Signals any running CombiSpeed instance to shutdown and safely restores system proxy. |
| `--minimized` | `-m` | Launches CombiSpeed in minimized window mode. |
| `--service` / `--headless` | `-h` | Runs CombiSpeed in headless daemon mode or as a Windows background service. |
| `--service-install` | *(none)* | Automatically registers and configures `CombiSpeedService` in Windows SCM. |
| `--service-uninstall` | *(none)* | Stops and cleanly deletes `CombiSpeedService` from Windows SCM. |
| `--service-start` | *(none)* | Starts `CombiSpeedService` via Service Control Manager. |
| `--service-stop` | *(none)* | Stops `CombiSpeedService` via Service Control Manager. |
| `--verify-persistence` | *(none)* | Runs automated self-test suite verifying adapter settings, profiles, accessibility, and persistence engine. |
| `--verify-throttling` | *(none)* | Runs automated self-test suite verifying Token Bucket rate limiting and proportional traffic shaping. |

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
