# ⚡ CombiSpeed (Native C# .NET Windows Network Adapter Channel Bonding & Multi-WAN Bandwidth Aggregator)

![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64)-0078D7?style=for-the-badge&logo=windows)
![.NET Version](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?style=for-the-badge&logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-WPF%20%7C%20C%23%2012-10B981?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-00F0FF?style=for-the-badge)

**CombiSpeed** is an ultra-fast, native **C# .NET Windows application** that combines multiple network adapters (Wi-Fi, Gigabit Ethernet, 4G/5G Cellular USB dongles, Mobile Hotspots, and LAN connections) into a single, high-speed aggregate connection. 

Built with **C# 12 and .NET 8.0 WPF**, CombiSpeed runs directly on the Windows TCP/IP stack with low-level socket binding (`Socket.Bind(new IPEndPoint(adapterIp, 0))`), delivering zero runtime overhead, instant startup (<200ms), and minimal memory consumption (~25 MB RAM).

---

## 🌟 Application Summary

When a Windows machine is connected to multiple internet sources simultaneously (e.g. Fiber broadband via Ethernet, 5G phone hotspot via Wi-Fi, or secondary office Wi-Fi), the standard Windows routing table restricts all traffic to a single default route with the lowest metric. The secondary connections sit 100% idle!

**CombiSpeed eliminates this bottleneck.** It acts as a local high-performance asynchronous multi-threaded aggregation engine. By intercepting connections via dual-mode SOCKS5 & HTTP/HTTPS forward proxies, it binds each outgoing TCP socket directly to individual network adapter IP addresses (`Socket.Bind(new IPEndPoint(adapterIp, 0))`). This bypasses the default route limitation and forces traffic simultaneously across all active adapters, combining their speeds into a single supercharged pipeline.

---

## 🚀 Key Features

1. **Real-Time Bandwidth Throttling & Speed Shaper (NEW in v1.3.2)**:
   - High-performance asynchronous Token Bucket rate limiting in `BandwidthThrottler.cs` actively throttles byte throughput across combined connection adapters.
   - **Proportional Traffic Shaping**: Enforces exact percentage bandwidth ratios on live byte throughput (e.g. 70% Ethernet / 30% Wi-Fi), preventing faster lines from drowning out slower lines or violating target ratios.
   - **Configurable Speed Caps**: 1-Click speed throttle presets (`Unlimited`, `2 MB/s`, `5 MB/s`, `10 MB/s`, `20 MB/s`, `50 MB/s`, `100 MB/s`, or Custom) and per-adapter speed limits to prevent exceeding metered mobile data caps.
   - Full integration across **HTTP, HTTPS CONNECT, SOCKS5 TCP, SOCKS5 UDP Associate, and Windows Virtual Bonded Bridge** streams.
   - Real-time `⚡ Bandwidth Throttling` toolbar in `MainWindow.xaml` and dedicated settings in `SettingsWindow.xaml`.

2. **Application Window Version Visibility & Pinned Cyberpunk Glass Footer (NEW in v1.3.5)**:
   - Dynamic assembly and build metadata binding in `MainViewModel.cs` exposing `AppVersion` (`v1.3.5`) and `AppVersionFull` (`v1.3.5 (Build 10)`).
   - **Custom TitleBar Cyberpunk Badge**: Prominently displays the active version badge directly next to the COMBISPEED neon logo, ensuring visibility across all window states.
   - **Pinned Glassmorphic Footer Status Bar**: Pinned bottom status bar displaying complete build attribution (`v1.3.5 (Build 10)`), engine description, real-time configuration auto-sync status, and target runtime environment (`Windows x64 (.NET 8.0)`).
   - **Dynamic OS Window Title**: Sets `Title="{Binding WindowTitle}"` to synchronize OS Taskbar, Task Manager, and Alt-Tab switcher with current release branding.
   - **CLI Version Flags**: Built-in `--version`, `-v`, and `/version` support for instant terminal output.

3. **Adapter Settings & Combination Profiles Persistence**:
   - Automatically saves all selected adapter states (`IsEnabled`) and custom percentage weights (`Weight`) to disk (`%LOCALAPPDATA%\CombiSpeed\adapter_settings.json`).
   - Restores exact adapter selections and bandwidth allocations upon application restart or adapter rescan.
   - **Multi-Attribute Adapter Binding**: Uses the permanent hardware MAC address as primary key with name fallback, guaranteeing that configurations stay stably bound even across dynamic DHCP IP re-assignments.
   - **Debounced Auto-Save Engine**: 400ms debounced disk saving on UI slider dragging and checkbox toggles, preventing disk thrashing while guaranteeing zero configuration loss.
   - **Combination Profiles & Preset Switching**: Save, switch, and manage custom adapter combination profiles (e.g. `Default Combination`, `Office Mode`, `Gaming 70/30`, `Mobile Hotspot Backup`).
   - Dedicated Cyberpunk `InputDialog` for creating and saving new combination profiles on the fly.
   - Real-time `💾 Auto-Saved: HH:mm:ss` status badge directly on the adapter combination toolbar.

4. **Virtual VPN / Adapter Connection & Bonded Bridge Subsystem**:
   - Provisions a native Windows adapter connection: **`CombiSpeed Bonded Bridge`** (`CombiSpeed Virtual Adapter`).
   - Registers directly in Windows Network Connections (`ncpa.cpl`) and Windows Settings -> Network & Internet -> VPN using the Windows user-mode phonebook (`rasphone.pbk`), requiring zero driver signing or UAC elevation.
   - Unified virtual interface that merges physical network adapters (Ethernet, Wi-Fi, Cellular) into a single virtual pipe.
   - Dedicated dashboard card with live connection status (`BONDED & ACTIVE`), Windows adapter name, Virtual IP (`10.254.1.2`), Gateway (`10.254.1.1`), and 1-click controls to toggle the bridge or inspect in `ncpa.cpl`.

5. **Strict % Bandwidth Utilization (Smooth Weighted Round Robin)**:
   - Implements mathematically provable **Smooth Weighted Round Robin (SWRR)** in `LoadBalancerEngine.cs`.
   - Distributes outgoing traffic across physical network adapters strictly according to user-configured percentage weights (e.g. 70% Wi-Fi, 30% Ethernet) with zero clustering or burstiness.
   - Real-time **Dynamic Multi-Color Segmented Bandwidth Distribution Bar** visualizing each physical adapter's proportion of the total bonded pool.
   - Per-adapter `BANDWIDTH: XX.X% of Bonded Pool` indicator dynamically updated whenever sliders change.

6. **RFC 1928 SOCKS5 UDP Associate Relay (Gaming & UDP Acceleration)**:
   - Full implementation of SOCKS5 `UDP ASSOCIATE` (Command `0x03`).
   - Binds outbound UDP sockets directly to physical adapter IPs (`Socket.Bind(new IPEndPoint(adapterIp, 0))`), accelerating multiplayer gaming (Valorant, CS2, CoD, Steam), Discord/VoIP voice channels, DNS queries, and WebRTC video across bonded connections.
   - Encapsulates and returns UDP replies back to the client.

7. **PAC (Proxy Auto-Config) Domain-Based Interface Routing**:
   - Dynamically compiles RFC-compliant JavaScript `FindProxyForURL(url, host)` scripts.
   - Built-in PAC server serving `http://127.0.0.1:8080/proxy.pac` directly on the proxy port without port conflicts.
   - Granular domain and subnet bypass rules (e.g. banking portals, corporate intranet `192.168.*`, `10.*` -> Direct; all other web traffic -> Bonded).
   - Automated 1-click Windows `AutoConfigURL` registry and WinINet integration.

8. **Windows Service & Headless Background Mode**:
   - Runs headless as a background Windows Service without requiring active user logon or open GUI (`--service`, `--headless`).
   - CLI service management: `--service-install`, `--service-uninstall`, `--service-start`, `--service-stop`.
   - 1-click scripts: `Install-Service.bat` and `Uninstall-Service.bat` registering `CombiSpeedService` in Windows Service Control Manager with automatic crash recovery.

9. **Native C# High-Performance Sockets**:
   - Outgoing sockets are explicitly bound to adapter local IPv4 addresses using `Socket.Bind(new IPEndPoint(localIp, 0))`.
   - Zero-overhead bi-directional streaming using .NET `ArrayPool<byte>` memory management.

7. **Dual-Mode High-Throughput Proxy Engine**:
   - **RFC 1928 SOCKS5 Proxy Server** (port 1080): Connect & UDP Associate methods with IPv4 and Domain Name resolution.
   - **HTTP / HTTPS CONNECT Forward Proxy** (port 8080): Direct tunneling for Chrome, Edge, Firefox, Brave, and all desktop applications.

8. **Intelligent Load Balancing & Routing Algorithms**:
   - **Smooth Weighted Round-Robin (SWRR)**: Proportional distribution based on user weight ratios (e.g. 70% Ethernet, 30% Wi-Fi Hotspot).
   - **Equal-Share Multiplexing**: Automatically balances connections equally across all available healthy adapters.
   - **Least-Latency Smart Routing**: Continuously measures real-time RTT per adapter and routes through the lowest-ping adapter.
   - **Active-Passive Link Failover**: Designates a primary connection with instant auto-fallback if connection degradation or packet loss occurs.

9. **1-Click Windows System Proxy Integration**:
   - Automated toggle for Windows Internet Settings (`HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings`).
   - Uses native Win32 `wininet.dll` API (`InternetSetOption`) to apply proxy changes immediately across the entire OS without restarting browsers.
   - **Graceful Restoration**: Automatically reverts Windows proxy settings and disconnects virtual bridge on exit.

10. **Real-Time Telemetry & Throughput Visualizer**:
   - Live per-adapter download & upload speed meters (KB/s, MB/s) with distinct Cyberpunk neon accent colors.
   - 60-second real-time scrolling bandwidth waveform canvas with cyan and purple gradient fills.
   - Live RTT ping and adapter health indicators with instant online/offline detection.
   - Session data transferred counters (MB/GB) per adapter and aggregate total.

11. **Built-In Multi-Stream Bonding Benchmark (Speed Tester)**:
   - On-demand benchmark utility that tests individual adapter speeds alongside concurrent bonded aggregate throughput so you can visually verify speed combination in real-time.

12. **Futuristic Cyberpunk Glassmorphic WPF Dashboard**:
   - Precision dark UI (`#080C14` background, deep navy cards `#0F172A`, glowing cyan `#00F0FF` and emerald `#10B981` indicators).
   - Draggable frameless custom window titlebar with minimize/maximize/close.
   - Interactive weight percentage sliders, animated adapter link indicators, and real-time audit log streaming.

---

## 🛡️ MOATs (Defensible Competitive Advantages)

| Dimension | CombiSpeed (Native C# .NET v1.3.5) | RezaRafia/Speedy (Reference) | Standard Windows Routing |
|---|---|---|---|
| **Real-Time Bandwidth Throttling** | **Token Bucket asynchronous rate limiter with proportional shaping & speed caps** | None (Unrestricted, no rate limiting) | None (Line rate only) |
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

---

## 🏗️ Technical Architecture

```
                      ┌────────────────────────────────────────┐
                      │    Windows Applications / Browsers     │
                      └───────────────────┬────────────────────┘
                                          │
                     ┌────────────────────┴────────────────────┐
                     ▼                                         ▼
         ┌───────────────────────────┐             ┌───────────────────────────┐
         │ CombiSpeed Bonded Bridge  │             │  Local Proxy Endpoints    │
         │ (Windows Virtual Adapter) │             │  - HTTP/HTTPS (Port 8080) │
         │ IP: 10.254.1.2            │             │  - SOCKS5 (Port 1080)     │
         │ Gateway: 10.254.1.1       │             │  - WinINet Registry Hook  │
         └─────────────┬─────────────┘             └─────────────┬─────────────┘
                       │                                         │
                       └────────────────────┬────────────────────┘
                                            ▼
                      ┌────────────────────────────────────────┐
                      │ CombiSpeed Smooth Weighted Round-Robin │
                      │       Dynamic % Bandwidth Router       │
                      │  - Adapter 1 Share: 60% (e.g. Ethernet)│
                      │  - Adapter 2 Share: 40% (e.g. Wi-Fi)   │
                      │  - Continuous RTT Latency Prober       │
                      └───────────────────┬────────────────────┘
                                          │
                        ┌─────────────────┴─────────────────┐
                        │                                   │
             Socket.Bind(adapterIp_1)            Socket.Bind(adapterIp_2)
                        │ (60% Bandwidth)                   │ (40% Bandwidth)
                        ▼                                   ▼
            ┌─────────────────────────┐         ┌─────────────────────────┐
            │ Adapter 1 (Ethernet 3)  │         │ Adapter 2 (Wi-Fi 2)     │
            │ IP: 192.168.1.9         │         │ IP: 192.168.1.28        │
            └────────────┬────────────┘         └────────────┬────────────┘
                         │                                  │
                         ▼ (WAN 1)                          ▼ (WAN 2)
            ┌─────────────────────────────────────────────────────────────┐
            │                      Global Internet                        │
            │      Combined Bandwidth Pipe = 60% WAN 1 + 40% WAN 2        │
            └─────────────────────────────────────────────────────────────┘
```

---

## 📦 Getting Started

### Prerequisites
- Windows 10 or 11 (64-bit)
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (pre-installed on most modern Windows systems)
- Two or more active network connections (e.g. Ethernet + Wi-Fi, Wi-Fi + Mobile Hotspot, or dual LAN)

### Running the Application
```bash
# Clone the repository
git clone https://github.com/yashiscool123/CombiSpeed.git
cd CombiSpeed

# Run directly using .NET CLI
dotnet run --project CombiSpeed/CombiSpeed.csproj

# Or build release executable
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
| `--start` | `-s` | Starts CombiSpeed and automatically engages network bonding on launch. |
| `--stop` | *(none)* | Signals any running CombiSpeed instance to shutdown and safely restores system proxy. |
| `--minimized` | `-m` | Launches CombiSpeed in minimized window mode. |
| `--service` / `--headless` | `-h` | Runs CombiSpeed in headless daemon mode or as a Windows background service. |
| `--service-install` | *(none)* | Automatically registers and configures `CombiSpeedService` in Windows SCM. |
| `--service-uninstall` | *(none)* | Stops and cleanly deletes `CombiSpeedService` from Windows SCM. |
| `--service-start` | *(none)* | Starts `CombiSpeedService` via Service Control Manager. |
| `--service-stop` | *(none)* | Stops `CombiSpeedService` via Service Control Manager. |
| `--verify-persistence` | *(none)* | Runs automated self-test suite verifying adapter settings, profiles, and persistence engine. |
| `--verify-throttling` | *(none)* | Runs automated self-test suite verifying Token Bucket rate limiting and proportional traffic shaping. |

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
