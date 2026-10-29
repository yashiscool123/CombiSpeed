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

1. **Native C# High-Performance Sockets**:
   - Outgoing sockets are explicitly bound to adapter local IPv4 addresses using `Socket.Bind(new IPEndPoint(localIp, 0))`.
   - Zero-overhead bi-directional streaming using .NET `ArrayPool<byte>` memory management.

2. **Dual-Mode High-Throughput Proxy Engine**:
   - **RFC 1928 SOCKS5 Proxy Server** (port 1080): Connect method with IPv4 and Domain Name resolution.
   - **HTTP / HTTPS CONNECT Forward Proxy** (port 8080): Direct tunneling for Chrome, Edge, Firefox, Brave, and all desktop applications.

3. **Intelligent Load Balancing & Routing Algorithms**:
   - **Dynamic Weighted Round-Robin (WRR)**: Proportional distribution based on user weight ratios (e.g. 70% Ethernet, 30% Wi-Fi Hotspot).
   - **Equal-Share Multiplexing**: Automatically balances connections equally across all available healthy adapters.
   - **Least-Latency Smart Routing**: Continuously measures real-time RTT per adapter and routes through the lowest-ping adapter.
   - **Active-Passive Link Failover**: Designates a primary connection with instant auto-fallback if connection degradation or packet loss occurs.

4. **1-Click Windows System Proxy Integration**:
   - Automated toggle for Windows Internet Settings (`HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings`).
   - Uses native Win32 `wininet.dll` API (`InternetSetOption`) to apply proxy changes immediately across the entire OS without restarting browsers.
   - **Graceful Restoration**: Automatically reverts Windows proxy settings on exit so the user's internet is never broken.

5. **Real-Time Telemetry & Throughput Visualizer**:
   - Live per-adapter download & upload speed meters (KB/s, MB/s).
   - 60-second real-time scrolling bandwidth waveform canvas with cyan and purple gradient fills.
   - Live RTT ping and adapter health indicators with instant online/offline detection.
   - Session data transferred counters (MB/GB) per adapter and aggregate total.

6. **Built-In Multi-Stream Bonding Benchmark (Speed Tester)**:
   - On-demand benchmark utility that tests individual adapter speeds alongside concurrent bonded aggregate throughput so you can visually verify speed combination in real-time.

7. **Futuristic Cyberpunk Glassmorphic WPF Dashboard**:
   - Precision dark UI (`#080C14` background, deep navy cards `#0F172A`, glowing cyan `#00F0FF` and emerald `#10B981` indicators).
   - Draggable frameless custom window titlebar with minimize/maximize/close.
   - Interactive weight percentage sliders, animated adapter link indicators, and real-time audit log streaming.

---

## 🛡️ MOATs (Defensible Competitive Advantages)

| Dimension | CombiSpeed (Native C# .NET) | RezaRafia/Speedy (Reference) | Standard Windows Routing |
|---|---|---|---|
| **Runtime & Memory** | **Native C# (.NET 8 WPF)**: ~25 MB RAM, 150 KB executable | Electron / Node: ~200 MB RAM, 150 MB package | OS Network Driver |
| **Socket Binding** | **`Socket.Bind(new IPEndPoint(adapterIp, 0))`** on every socket | **Missing `localAddress`** (traffic leaks back to default gateway) | Lowest metric adapter only |
| **Concurrency Reliability** | Persistent async non-blocking `TcpListener` with connection pooling | Spawns a new listener on port 8080 per connection, throwing `EADDRINUSE` | N/A |
| **Proxy Protocol Support** | **Dual SOCKS5 & HTTP/HTTPS CONNECT** | SOCKS5 only (IPv4 only) | None |
| **Windows OS Integration** | **Native WinINet `InternetSetOption` API** with fail-safe revert | None (Manual proxy config required in every app) | None |
| **Adapter Health & Failover** | Continuous RTT probes with auto-rerouting on packet loss | None (Dead connection leads to application hang) | OS metric failover takes 30-60+ seconds |
| **Live Telemetry & Speedometer**| Real-time per-adapter MB/s gauges, canvas waveform, and audit logs | Text console logs only | Task Manager adapter graph (split, not bonded) |

---

## 💡 Real-World Challenges Solved

1. **Slow Downloads & Large File Transfers**:
   - Multi-connection download managers (IDM, Free Download Manager, Steam multi-chunk downloads, Torrent clients, Git clones) open dozens of concurrent TCP streams. CombiSpeed spreads these streams across both Ethernet and Wi-Fi simultaneously, delivering the combined sum of both download pipes.
2. **Buffering & Unstable 4K/8K Video Streaming**:
   - Video platforms buffer media using concurrent chunk requests. CombiSpeed fetches video segments across all adapters, preventing buffering during peak congestion.
3. **Remote Work & Critical Video Calls**:
   - Combines a fluctuating mobile hotspot with a home broadband connection. If one connection experiences jitter or temporary dropouts, CombiSpeed's health monitor redirects traffic instantly.
4. **Data Cap Management**:
   - Flexible weight sliders allow users to cap usage on metered cellular tethering (e.g., 20%) while utilizing unlimited fiber broadband for the remaining 80%.

---

## 🏗️ Technical Architecture

```
                      ┌────────────────────────────────────────┐
                      │    Windows Applications / Browsers     │
                      └───────────────────┬────────────────────┘
                                          │ (Localhost : 8080 / 1080)
                                          ▼
                      ┌────────────────────────────────────────┐
                      │    CombiSpeed C# .NET Aggregation Core │
                      │  - HTTP/HTTPS CONNECT Forward Proxy    │
                      │  - RFC 1928 SOCKS5 Forward Proxy       │
                      │  - 1-Click WinINet Registry Hook       │
                      └───────────────────┬────────────────────┘
                                          │
                        ┌─────────────────┴─────────────────┐
                        │   Load Balancer & Health Monitor  │
                        │   - Weighted Round-Robin (WRR)    │
                        │   - Least Latency Probe           │
                        │   - Active Failover Engine        │
                        └────────┬─────────────────┬────────┘
                                 │                 │
              Socket.Bind(adapterIp_1)            Socket.Bind(adapterIp_2)
                                 │                 │
                                 ▼                 ▼
             ┌─────────────────────────┐     ┌─────────────────────────┐
             │ Adapter 1 (Ethernet 3)  │     │ Adapter 2 (Wi-Fi 2)     │
             │ IP: 192.168.1.9         │     │ IP: 192.168.1.28        │
             └────────────┬────────────┘     └────────────┬────────────┘
                          │                               │
                          ▼ (WAN 1)                       ▼ (WAN 2)
             ┌─────────────────────────────────────────────────────────┐
             │                    Global Internet                      │
             │          Combined Throughput = WAN 1 + WAN 2            │
             └─────────────────────────────────────────────────────────┘
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

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
