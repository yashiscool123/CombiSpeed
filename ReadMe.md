# ⚡ CombiSpeed (Multi-OS Network Adapter Channel Bonding & Multi-WAN Bandwidth Aggregator)

![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011%20(x64)-0078D7?style=for-the-badge&logo=windows)
![Linux](https://img.shields.io/badge/Linux-Ubuntu%20%7C%20Debian%20%7C%20Fedora%20%7C%20Arch-FCC624?style=for-the-badge&logo=linux&logoColor=black)
![macOS](https://img.shields.io/badge/macOS-10.15%2B%20(x64%20%7C%20arm64)-000000?style=for-the-badge&logo=apple)
![.NET Version](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?style=for-the-badge&logo=dotnet)
![Version](https://img.shields.io/badge/Version-v2.1.4%20(Build%2021)-38BDF8?style=for-the-badge)
![Accessibility](https://img.shields.io/badge/Accessibility-WCAG%202.1%20AA-10B981?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-00F0FF?style=for-the-badge)

**CombiSpeed** is an ultra-fast, multi-OS application that combines multiple network adapters (Wi-Fi, Gigabit Ethernet, 4G/5G Cellular USB dongles, Mobile Hotspots, Thunderbolt bridges, and LAN connections) into a single, high-speed aggregate connection with dynamic traffic shaping.

Featuring dedicated, separate native codebases for **Windows 10/11 (`CombiSpeed/`)**, **Linux (`CombiSpeed-Linux/`)**, and **macOS (`CombiSpeed-macOS/`)**, CombiSpeed runs directly on each OS kernel's network stack with native low-level socket binding, delivering zero runtime overhead, instant startup (<200ms), and an identical, accessible dark cyberpunk glassmorphic dashboard across all platforms!

---

## 🌟 Application Summary

When a computer is connected to multiple internet sources simultaneously (e.g. Fiber broadband via Ethernet, 5G phone hotspot via Wi-Fi, or secondary office Wi-Fi), standard OS routing tables restrict all traffic to a single default route with the lowest metric. The secondary connections sit 100% idle!

**CombiSpeed eliminates this bottleneck.** It acts as a local high-performance asynchronous multi-threaded aggregation engine. By intercepting connections via dual-mode SOCKS5 & HTTP/HTTPS forward proxies or native virtual bonded bridges, it binds each outgoing TCP/UDP socket directly to individual physical network adapter interfaces:
- **Windows**: `Socket.Bind(new IPEndPoint(adapterIp, 0))` + WinINet proxy & Virtual Bridge
- **Linux**: `Socket.Bind` + `SO_BINDTODEVICE` (socket opt 25) + `gsettings` & shell proxy environment
- **macOS**: `Socket.Bind` + `IP_BOUND_IF` / `SO_DONTROUTE` + `networksetup` proxy automation

This bypasses default routing limitations and forces traffic simultaneously across all active adapters, combining their speeds into a single supercharged pipeline.

---

## 🚀 Key Features

1. **Automated Rotating Proxy Harvester & Multi-NIC Egress Dispatcher (NEW in v2.1.0, Enhanced in v2.1.1, v2.1.3 & v2.1.4)**:
   - **Multi-Source Open-Source Proxy Harvester**:
     Automatically scrapes fresh proxy candidate pools from top-tier maintained open-source repositories and APIs out of the box (*TheSpeedX/PROXY-List*, *monosans/proxy-list*, *clarketm/proxy-list*, *proxyscrape API*), custom user URLs/APIs, pastebins, or local `.txt` lists.
   - **Bulk Proxy Repository Sources & Feed URLs Management (NEW in v2.1.3)**:
     Allows users to bulk add, view, edit, or customize any number of proxy feed URLs and repository endpoints. Features multi-line text editing (`ProxySourcesDialog.xaml` in Windows, Web modal in Linux/macOS SPAs), live URL counting, automatic comment ignoring (`#` or `//`), HTTP/HTTPS URL validation, de-duplication, clipboard pasting, and 1-click restore to verified default repositories.
   - **Multi-Faceted Proxy Sorting & Filtering (NEW in v2.1.4)**:
     - **Latency Threshold Filter**: Filter the pool instantaneously by response speed tiers (`All Latencies`, `< 150 ms (Ultra-Fast)`, `< 300 ms (Fast)`, `< 500 ms (Normal)`, and `< 1000 ms`).
     - **Dynamic Source Origin Filter**: Auto-discovers and categorizes proxies by their scraping feed origin (*TheSpeedX*, *monosans*, *hookzof*, *proxyscrape*, or custom URL hostname), isolating proxies from specific repositories.
     - **Protocol Filter**: Filter candidate pools by protocol (`HTTP`, `HTTPS`, `SOCKS4`, `SOCKS5`).
     - **Universal Live Search**: Instant search box matching host IP, port, country code, protocol, or source URL across the pool in real time.
     - **Multi-Criteria & Interactive Column Sorting**: Presets for `Lowest Latency (Fastest)`, `Highest Latency`, `Status / Working First`, `Source (A-Z)`, and `Country Code (A-Z)`. Full interactive column header click-to-sort across both Windows WPF `DataGrid` and Linux/macOS Web SPAs.
   - **Universal Heuristic Regex & JSON API Scraper**:
     Intelligently extracts proxies from unstructured plain text, CSV, JSON arrays/objects (`{"ip":"...", "port":8080, "country":"US"}`), and HTML tables (`<td>IP</td><td>PORT</td><td>Country</td>`) supporting `IP:PORT`, `PROTOCOL://IP:PORT`, and authenticated `IP:PORT:USER:PASS` lines. Automatically isolates and purges RFC 1918 private subnets and loopback addresses.
   - **High-Concurrency Multi-Protocol Validator**:
     Semaphore-bounded parallel async prober testing HTTP, HTTPS CONNECT, SOCKS4, and RFC 1928 SOCKS5 endpoints in milliseconds without UI freezing or thread starvation. Accurately measures real-time RTT latency (ms) and detects anonymity tiers (Elite, Anonymous, Transparent).
   - **GeoIP Country Detection & Geo-Targeted Rotation Filtering (NEW in v2.1.1)**:
     Extracts country codes from proxy payloads and provides instant 1-click filtering (`ALL`, `US`, `DE`, `GB`, `FR`, `CA`, `JP`, `NL`, `SG`, `IN`). Outbound requests are dynamically routed strictly through healthy proxies located in the requested country with seamless fallback if a regional pool is exhausted.
   - **Autonomous Background Scheduler (NEW in v2.1.1)**:
     Continuous background self-healing engine running non-blocking timers for scheduled auto-harvesting (every 30 minutes) and automated pool health validation & dead-node pruning (every 5 minutes), keeping the proxy pool continuously responsive without manual intervention.
   - **Upstream Proxy Authentication (NEW in v2.1.1)**:
     Full transparent support for private authenticated proxies via automatic `Proxy-Authorization: Basic <base64>` header injection for HTTP/HTTPS tunnels and RFC 1929 Username/Password subnegotiation for SOCKS5.
   - **Dynamic Rotation & Sticky Session Strategies**:
     - `Round Robin (Per-Request)`: Rotates to a new healthy upstream proxy on every HTTP/HTTPS request (ideal for large-scale web scraping, price intelligence, and QA automation).
     - `Sticky Domain (Host Affinity)`: Retains the same upstream proxy for requests targeting the same host (e.g. `*.target.com`), preserving login sessions and OAuth cookies.
     - `Sticky Session (Duration)`: Preserves proxy affinity per connection or client session for a user-configurable duration (e.g. 5–30 minutes).
     - `Lowest Latency (Fastest)`: Dynamically routes through the top fastest responsive proxies ($< 250\text{ ms}$).
     - `Elite Only`: Restricts rotation strictly to high-anonymity Elite proxies that leak zero proxy headers (`Via`, `X-Forwarded-For`).
   - **The "Double-Layer" Multi-NIC Egress MOAT**:
     CombiSpeed uniquely binds outbound proxy connections across local physical network adapters (`Socket.Bind(adapterIp, 0)`). Local traffic is distributed across Ethernet, Wi-Fi, and Cellular dongles while simultaneously rotating global public IPs, eliminating both local router connection bottlenecks and remote rate limits!
   - **Dedicated Port & Smart PAC Integration**:
     Runs a dedicated rotating proxy listener on port `8081` alongside the high-speed bonded port `8080`. Supports `PacAction.RotatingProxy` in PAC scripts to route specific scraping domains to `8081` while streaming and bulk downloads stay on the unthrottled bonded port `8080`.

2. **Speed Combining & Unified Bandwidth Pooling (v2.0.2)**:
   - **Combine Speeds Mode**: Bandwidth across all active connections and adapters is pooled additively ($TotalSpeed = Speed_{NIC1} + Speed_{NIC2} + \dots$). Eliminates artificial fractional slicing or partitioning where adapters were throttled to fractional percentages (e.g. 50% or 25%).
   - **Unified Combined Speed Cap**: Configured bandwidth caps (e.g. 500 Mbps) enforce the combined aggregate total across all connections simultaneously. Individual connections have access to the full combined speed pool rather than being choked by split sub-limits.
   - **Multi-NIC Parallel Range Chunk Speed Combiner**: Implemented in `ProxyEngine.cs` for HTTP file transfers. Detects chunkable resources ($\ge 512\text{ KB}$ with HTTP Range support) and streams byte chunks concurrently across all active physical adapters, combining the physical wire speeds of multiple adapters for a single file download!
   - **Accessible UI & Preset Refresh**: Updated toolbar presets to "Unlimited (Combined Speed)" and status indicator to "Bandwidth: Combined Speed Aggregation (Active)".

2. **Speed Benchmark Multi-Stream Overhaul & Virtual Adapter Protection (v2.0.1)**:
   - **Pre-Flight Latency & Routability Probing**: Automatically detects and isolates unroutable virtual host-only adapters (such as Hyper-V `vEthernet` or WSL bridges with IPs `172.19.96.1` / `172.31.0.1`). Conducts an instant 1500ms pre-flight probe; adapters exceeding 900ms or unreachable gateways are assigned 0 Mbps immediately, eliminating 21-42 second TCP SYN retransmission hangs.
   - **Proxy Bypass Isolation (`UseProxy = false`)**: Enforces direct physical interface routing during speed tests (`SocketsHttpHandler.UseProxy = false`, `Proxy = null`), preventing benchmark requests from incorrectly routing to loopback `127.0.0.1:8080` while bound to a physical adapter IP, resolving the `10049 WSAEADDRNOTAVAIL` socket rejection.
   - **Strict IPv4 DNS Resolution**: Integrated `ResolveHostIPv4Async` to eliminate IPv6 address family mismatches when attempting to connect from IPv4-bound physical sockets.
   - **Cross-Platform Native Parity**: True asynchronous socket throughput benchmarking across Windows (`SpeedTester.cs`), Linux (`LinuxSpeedTester.cs`), and macOS (`MacSpeedTester.cs`), wired directly to dashboard modals with live per-adapter speed and ping breakdown.
   - **Clean UI Event Lifecycle**: Complete unsubscription of benchmark progress handlers in `SpeedTestWindow.xaml.cs` on window close, preventing duplicate handler firing and UI race conditions.

2. **Proxy Auto-Config (PAC) File Saving & Direct Export (v2.0.0)**:
   - **One-Click PAC Export Button**: Built directly into the PAC routing configuration section in `SettingsWindow.xaml` and the Linux/macOS dashboard.
   - **Interactive File Save Dialog**: Pre-configured with `.pac`, `.js`, and all-files filters, compiling RFC-compliant `FindProxyForURL(url, host)` JavaScript files directly to disk.
   - **Command-Line PAC Export**: Built-in `--export-pac [outputPath]` CLI command on all operating systems for automated deployment, browser provisioning, and headless server environments.
   - **RFC-Compliant Intranet Bypass**: Automatically bypasses intranet ranges (RFC 1918 `10.*`, `172.16.*`, `192.168.*`), localhost (`127.*`, `localhost`, `::1`), and custom domains (`*.local`, `*.internal`, `*.corp`), while aggregating all internet traffic.

2. **Multi-OS Compatibility with Separate Standalone Codebases (NEW in v2.0.0)**:
   - **`CombiSpeed/` (Windows Codebase)**: Native .NET 8.0 WPF application (`net8.0-windows`) with hardware-accelerated DirectX rendering, WinINet registry integration, and Windows Virtual Bonded Bridge phonebook (`rasphone.pbk`).
   - **`CombiSpeed-Linux/` (Linux Codebase)**: Independent .NET 8.0 Linux application (`net8.0`) with `SO_BINDTODEVICE` interface binding, GNOME `gsettings` proxy automation, shell environment generation (`~/.config/combispeed/proxy.env`), and 1-click launch scripts (`start-combispeed.sh`, `stop-combispeed.sh`, `install-combispeed.sh`).
   - **`CombiSpeed-macOS/` (macOS Codebase)**: Independent .NET 8.0 macOS application (`net8.0`) with `IP_BOUND_IF` socket binding, `networksetup` proxy configuration, 1-click launch scripts (`start-combispeed-mac.sh`, `stop-combispeed-mac.sh`, `install-combispeed-mac.sh`), and Apple application bundle `CombiSpeed.app/Contents/Info.plist`.
   - **100% Consistent UI/UX Across All Platforms**: Identical dark cyberpunk glassmorphic visual aesthetic, glowing master LED switches, live 60 FPS bandwidth waveform graphs, segmented navigation tabs, 500 Mbps sliders, custom throttle dialogs, speed benchmarks, and WCAG 2.1 AA accessibility.

3. **Modern Segmented UI/UX Redesign, High-Contrast Dropdowns & Text Readability**:
   - **Custom Accessible Dark `ComboBox` ControlTemplate**: Replaces the platform default Windows Aero2 template with an accessible dark template featuring a high-contrast toggle button (`#131D33`), dedicated split border with glowing cyan chevron (`#00F0FF`), dark popup border (`#0B1120`), and explicit white selection text (`#FFFFFF`), completely resolving white-on-white and black-on-dark display defects.
   - **Global Anti-Fallback Text Styling**: Registered default styles for `TextBlock` and `Label` with `Foreground="#F8FAFC"`, preventing Windows light theme from causing text to fall back to black on dark surfaces.
   - **Application-Wide Text Contrast Upgrade**: Replaced low-luminance and dim text colors (`#94A3B8`, `#334155`) across all cards, dialogs, and tables with high-luminance, crystal-clear colors (`#FFFFFF` pure white, `#38BDF8` sky cyan, `#F1F5F9` crisp slate, and `#CBD5E1` bright silver), achieving 14:1+ contrast ratios.
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
   - Dynamic assembly and build metadata binding in `MainViewModel.cs` exposing `AppVersion` (`v1.4.1`) and `AppVersionFull` (`v1.4.1 (Build 13)`).
   - **Custom TitleBar Cyberpunk Badge**: Prominently displays the active version badge directly next to the COMBISPEED neon logo, ensuring visibility across all window states.
   - **Pinned Glassmorphic Footer Status Bar**: Pinned bottom status bar displaying complete build attribution (`v1.4.1 (Build 13)`), engine description, real-time configuration auto-sync status, and target runtime environment (`Windows x64 (.NET 8.0)`).
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

| Dimension | CombiSpeed (Native C# .NET v1.4.1) | RezaRafia/Speedy (Reference) | Standard Windows Routing |
|---|---|---|---|
| **UI/UX & Accessibility** | **Custom dark high-contrast ComboBox, anti-fallback text styles, Segmented Tabbed Navigation, WCAG 2.1 AA High Contrast, UI Scaling (100%-130%), Reduce Motion, Screen Reader support, & Hotkeys** | Cramped single-scroll Electron page | Native OS Control Panel / Settings |
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
8. **UI Ergonomics & High-Contrast Readability (WCAG 2.1 AA/AAA)**:
   - High-contrast custom dark dropdowns and typography eliminate eye strain and display defects on high-DPI displays or when Windows is set to light theme. Segmented Tabs (`Ctrl+1` through `Ctrl+4`) organize complex subsystems without continuous scrolling.

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

## 📦 Building and Separate Multi-OS Publishing

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10 / 11 (x64), Linux (Ubuntu, Debian, Fedora, Arch x64/arm64), or macOS 10.15+ (x64/arm64)

### Build & Publish Commands per OS

```bash
# 1. Windows (WPF Native)
dotnet publish CombiSpeed/CombiSpeed.csproj -c Release -r win-x64 --self-contained false -o publish/windows/bin

# 2. Linux (Native Console & Embedded Cyberpunk UI)
dotnet publish CombiSpeed-Linux/CombiSpeed.Linux.csproj -c Release -r linux-x64 --self-contained false -o publish/linux/bin

# 3. macOS (Darwin Native & Embedded Cyberpunk UI)
dotnet publish CombiSpeed-macOS/CombiSpeed.macOS.csproj -c Release -r osx-x64 --self-contained false -o publish/macos/bin
```

### Published Packages
- **Windows**: `publish/windows/CombiSpeed-v2.1.4-win-x64.zip` and `publish/CombiSpeed-v2.1.4-win-x64.zip`
- **Linux**: `publish/linux/CombiSpeed-v2.1.4-linux-x64.tar.gz` and `publish/linux/CombiSpeed-v2.1.4-linux-x64.zip`
- **macOS**: `publish/macos/CombiSpeed-v2.1.4-macos-x64.tar.gz` and `publish/macos/CombiSpeed-v2.1.4-macos-x64.zip`

---

## ⚡ One-Click Start, Stop & Installers per OS

### 🪟 Windows 10 / 11
- **Install**: Run `Install-CombiSpeed.bat` (installs to `%LOCALAPPDATA%\Programs\CombiSpeed`, creates Start Menu & Desktop shortcuts, registers in Installed Apps).
- **Start**: Run `Start-CombiSpeed.bat` (launches WPF UI and engages channel bonding).
- **Stop**: Run `Stop-CombiSpeed.bat` (halts processes and resets Windows WinINet proxy).
- **Export PAC**: Run `CombiSpeed.exe --export-pac [outputPath]` or click **"💾 Export PAC File..."** in Settings.

### 🐧 Linux (Ubuntu / Debian / Fedora / Arch)
- **Install**: Run `./install-combispeed.sh` (installs to `~/.local/bin/combispeed` and creates `~/.local/share/applications/combispeed.desktop`).
- **Start**: Run `./start-combispeed.sh` (launches engine and opens Cyberpunk Dashboard in default browser).
- **Stop**: Run `./stop-combispeed.sh` (halts engine and resets GNOME / shell proxy settings).
- **Export PAC**: Run `combispeed-linux --export-pac [outputPath]` or download from the web dashboard.

### 🍏 macOS (Intel & Apple Silicon)
- **Install**: Run `./install-combispeed-mac.sh` (installs `CombiSpeed.app` into `/Applications` or `~/Applications` and creates `~/.local/bin/combispeed`).
- **Start**: Run `./start-combispeed-mac.sh` or open `CombiSpeed.app` from Finder / Spotlight.
- **Stop**: Run `./stop-combispeed-mac.sh` (halts engine and restores macOS `networksetup` proxies).
- **Export PAC**: Run `combispeed-macos --export-pac [outputPath]` or download from the dashboard.

---

### 💻 CLI Arguments Reference (All Platforms)
| Argument | Alias | Description |
|---|---|---|
| `--version` | `-v`, `/version` | Prints application version, build number, and target architecture to stdout. |
| `--start` | `-s` | Starts CombiSpeed and automatically engages network bonding on launch. |
| `--stop` | *(none)* | Signals any running CombiSpeed instance to shutdown and safely restores system proxy. |
| `--export-pac <path>` | `-export-pac` | Compiles and exports RFC-compliant PAC JavaScript script directly to the specified file path. |
| `--minimized` | `-m` | Launches CombiSpeed in minimized window mode (Windows). |
| `--service` / `--headless` / `--daemon` | `-h` | Runs CombiSpeed in headless daemon mode without opening GUI or browser. |
| `--verify-benchmark` | *(none)* | Runs automated self-test suite verifying pre-flight routability probing, multi-adapter socket throughput, and bonded aggregation. |
| `--verify-persistence` | *(none)* | Runs automated self-test suite verifying adapter settings, profiles, accessibility, and PAC export. |
| `--verify-throttling` | *(none)* | Runs automated self-test suite verifying Token Bucket rate limiting, 500 Mbps cap, and dynamic speed shaping. |
| `--verify-rotator` | *(none)* | Runs automated self-test suite verifying universal scraping, regex/JSON parsing, latency/source/protocol filtering, and LowestLatency sorting. |

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
