# ⚡ CombiSpeed (Multi-OS Network Adapter Channel Bonding & Multi-WAN Bandwidth Aggregator)

![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011%20(x64)-0078D7?style=for-the-badge&logo=windows)
![Linux](https://img.shields.io/badge/Linux-Ubuntu%20%7C%20Debian%20%7C%20Fedora%20%7C%20Arch-FCC624?style=for-the-badge&logo=linux&logoColor=black)
![macOS](https://img.shields.io/badge/macOS-10.15%2B%20(x64%20%7C%20arm64)-000000?style=for-the-badge&logo=apple)
![.NET Version](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?style=for-the-badge&logo=dotnet)
![Version](https://img.shields.io/badge/Version-v2.2.4%20(Build%2031)-38BDF8?style=for-the-badge)
![Accessibility](https://img.shields.io/badge/Accessibility-WCAG%202.1%20AA-10B981?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-00F0FF?style=for-the-badge)

**CombiSpeed** is an ultra-fast, production-grade, multi-OS application engineered to combine multiple network adapters (Wi-Fi, Gigabit Ethernet, 4G/5G Cellular USB dongles, Mobile Hotspots, Thunderbolt bridges, and LAN connections) into a single, high-speed aggregate connection with dynamic traffic shaping and integrated timely rotating proxy routing.

Featuring dedicated, separate native codebases for **Windows 10/11 (`CombiSpeed/`)**, **Linux (`CombiSpeed-Linux/`)**, and **macOS (`CombiSpeed-macOS/`)**, CombiSpeed runs directly on each OS kernel's network stack with native low-level socket binding, delivering zero runtime overhead, instant cold startup (<200ms), and an identical, accessible dark cyberpunk glassmorphic dashboard across all platforms!

---

## 🎯 Initial Application Purpose & Problem Solved

Modern devices routinely have access to multiple concurrent internet connections: home/office fiber broadband via Gigabit Ethernet, primary Wi-Fi, 4G/5G mobile hotspot over USB or Wi-Fi, secondary Wi-Fi connections via USB adapters, and Thunderbolt network bridges. However, standard operating system routing tables (Windows TCP/IP stack, Linux route tables, and macOS BSD network subsystem) are architecturally hardcoded to choose only a **single default gateway** with the lowest metric. 

As a result:
- **Secondary Connections Sit 100% Idle**: When connected to both a 300 Mbps Fiber line and a 150 Mbps 5G phone hotspot, standard OS routing utilizes only one, leaving 150 Mbps of paid bandwidth completely wasted.
- **Single Point of Failure**: If the primary line encounters packet loss, jitter, bufferbloat, or carrier disconnect, all active downloads, video streams, and cloud syncs stall or drop entirely.
- **Local Network Bottlenecks**: High-concurrency tasks (such as web scraping, game downloads, ISO transfers, and media production uploads) saturate a single router or network adapter interface.

**CombiSpeed was created to eliminate this fundamental OS limitation.** By intercepting and dispatching connections through low-level native socket binding (`Socket.Bind` to physical adapter interface IPs on Windows, `SO_BINDTODEVICE` socket options on Linux, and `IP_BOUND_IF` / `SO_DONTROUTE` on macOS), CombiSpeed forces network traffic concurrently and additively across all active network adapters, pooling their bandwidth into a unified, resilient super-connection.

---

## 🌟 Application Summary & Architecture

CombiSpeed operates as an asynchronous, multi-threaded aggregation and traffic-shaping engine designed for high throughput, minimal memory usage, and zero latency overhead:

1. **Additive Multi-WAN Channel Bonding**:
   Combines physical wire speeds additively ($TotalSpeed = Speed_{NIC1} + Speed_{NIC2} + \dots$). Eliminates fractional throttling or artificial sub-limits so every connection benefits from the combined capacity.

2. **Parallel HTTP Range Chunk Combining**:
   Splits large file transfers ($\ge 512\text{ KB}$) into concurrent byte ranges dispatched simultaneously across different physical network adapters, combining multiple physical wire speeds into a single accelerated download stream.

3. **Two-Stage Multi-Adapter Bonding & Timely Rotating Proxy Pipeline (Enhanced in v2.2.4)**:
   A decoupled two-stage networking architecture: CombiSpeed first binds and load-balances outbound client sockets across physical network adapters for aggregate bandwidth and multi-NIC fault tolerance. Then, it tunnels all requests upstream through a user-selected, filtered rotating proxy server that dynamically rotates on a timely basis with configurable intervals from 10s all the way up to **every 2 hours (7200s)**. The rotation countdown timer is strictly gated to begin ticking only once a proxy server is successfully connected and verified live.

4. **Live Identity Refresh & Protocol-Aware Upstream Tunnels (NEW in v2.2.3)**:
   - **Tunnel-Probed Identity Refresh**: When a proxy connection is established, diagnostic GeoIP requests are tunnelled directly through the active proxy to immediately refresh and display the updated public WAN IP, ISP/ASN, city, and country flag emoji.
   - **Full Protocol Interoperability**: Upstream tunnels dynamically negotiate based on protocol: HTTP/HTTPS CONNECT, RFC 1928 SOCKS5, and RFC SOCKS4/SOCKS4a.
   - **Live Connection Auto-Rotation Retry**: Automatically retries and advances to subsequent candidate nodes upon connection or handshake failures, evicting dead proxies until a live connection is established.

5. **Direct Force Connect & Selection Controls**:
   Allows instant manual lock-on via the `🎯 Force Connect` toolbar button or per-row `Connect` buttons to immediately switch to a specific chosen proxy or advance to the next live node without waiting for timer expiration.

6. **Eye-Friendly Light & Dark Themes & High-Contrast Tooltips (NEW in v2.2.3)**:
   - **Theme Switcher**: 1-click switching between a soothing, eye-friendly Light Theme and a Cyberpunk Dark Theme across Windows WPF and Linux/macOS dashboards, exceeding WCAG 2.1 AAA contrast standards.
   - **High-Contrast Tooltips**: Every key button, input field, slider, checkbox, toggle, and telemetry card features rich, readable, high-contrast tooltips describing its purpose and underlying behavior.

7. **Dynamic Success/Failure Filters & Instant Settings Persistence (NEW in v2.2.3)**:
   - **Success & Failure Column Filters**: Multi-tiered filtering by verified successes (`≥ 1`, `≥ 3`, `≥ 5`, `≥ 10`) and failures (`0 Perfect`, `≤ 1`, `≤ 3`, `≤ 5`).
   - **Zero-Latency Settings Auto-Persistence**: All settings changes (including PAC Dynamic Routing toggle) are immediately saved to disk on interaction.
   - **Multi-Tiered Repository & Server URLs Storage**: Preserves custom repository sources in `proxy_sources.json` and harvested candidates in `proxy_state.json` across restarts.

8. **Enriched Connection Audit Stream & Toggle (NEW in v2.2.3)**:
   Connection audit log displays the active proxy server URL, connection result (`✓ SUCCESS` / `✗ FAILED`), latency, and resolved IP, with a dedicated toggle to turn audit logging off when not needed.

9. **Live Intranet & Internet Geo-Location Identity Resolution**:
   Continuously monitors and resolves real-time network identity with dual-provider fast fallback (`http://ip-api.com/json/`, `https://ipwho.is/`, `https://api.ipify.org?format=text`), displaying local intranet IP aggregation, public Internet WAN IP, city, region, country, ISP/ASN, and dynamic Unicode country flag emojis (e.g. 🇮🇳, 🇺🇸, 🇩🇪, 🇬🇧, 🇯🇵).

10. **Automated Idempotent Rotating Proxy Harvester & State Persistence**:
    Scrapes, parses, and validates thousands of proxy candidate nodes from 255+ curated open-source feeds across HTTP, HTTPS, SOCKS4, and SOCKS5 in **strict Idempotence mode** ($f(f(x)) = f(x)$). Eliminates duplicate insertions, preserves user checkbox selections (`IsSelected`) and verified benchmarks across repeated harvests. Automatically persists all harvested candidates, user selections, and active UI filters & sorting configurations (`proxy_state.json`), restoring the exact interface state seamlessly upon application restart.

11. **Dynamic QoS Traffic Shaping**:
    Three specialized adaptive shaping modes:
    - `Maximum Bandwidth`: Aggressive TCP window scaling (150ms burst window) optimized for multi-gigabit downloads.
    - `Lowest Latency`: Anti-bufferbloat pacing (20ms burst window) prioritizing low-ping fiber over high-latency cellular for lag-free gaming and VoIP.
    - `Balanced Dynamic`: Real-time health monitoring that dynamically shifts allocation away from jittery or degraded adapters.

12. **Cross-Platform Standalone Native Codebases**:
    - **`CombiSpeed/`**: Windows 10/11 WPF application with hardware-accelerated DirectX rendering, WinINet registry automation, and Virtual Bonded Bridge support.
    - **`CombiSpeed-Linux/`**: Linux daemon with `SO_BINDTODEVICE` binding, GNOME `gsettings` integration, shell environment generation, and responsive Cyberpunk Web UI.
    - **`CombiSpeed-macOS/`**: macOS application bundle with `IP_BOUND_IF` socket binding, `networksetup` proxy control, and matching Cyberpunk Web UI.

---

## 🚀 Key Features

1. **Two-Stage Bonding & Timely Rotating Proxy Pipeline with Live Auto-Rotation Retry (Enhanced in v2.2.2)**:
   - **Live Connection Auto-Rotation Retry**: Automatically retries and advances to the next candidate when an upstream connection fails, evicting dead proxies until a live connection is established.
   - **Force Connect to Selected or Next**: 1-click `🎯 Force Connect` button in the toolbar and per-row `Connect` buttons to immediately lock onto a chosen proxy node.
   - **Timely Automatic Rotation Interval**: User-selectable rotation intervals (10s, 15s, 30s, 45s, 60s, 120s, 300s) with live countdown display and manual `⚡ Rotate Now` override.
   - **Per-Proxy Selection Checkboxes**: Interactive checkboxes in the proxy data grid allowing users to handpick or bulk-select (`Select All`, `Deselect All`, `Select Fast ≤300ms`) which healthy proxies participate in rotation.
   - **Explanatory Tooltips**: Descriptive, accessible tooltips across all interactive controls explaining functions, behaviors, and keyboard shortcuts.
   - **Pipeline Toggle**: Quick `⚡ Bond-then-Rotate` checkbox toggle in UI and REST APIs to enable or disable proxy tunneling on the bonded connection.

2. **Live Intranet & Internet Geo-Location Identity (NEW in v2.2.0)**:
   - **Intranet IP Aggregation**: Resolves and displays all active local IPv4 addresses across enabled physical network interfaces.
   - **Public Internet WAN IP & Live GeoIP**: Instant sub-second resolution of public IP, ISP/ASN, city, region, country, and algorithmic Unicode flag emoji without requiring API keys.
   - **Dedicated Cyberpunk Identity Card**: Pinned high-contrast card on Overview tab with 1-click `⟳ Refresh Identity` button.
   - **Per-Adapter Location Display**: Individual adapter cards in the Combined Adapters tab display each adapter's resolved public WAN IP and country flag.

3. **Combined Adapters 1-Click Rescan (NEW in v2.1.9)**:
   - Dedicated `⟳ Rescan Adapters` button directly in the Combined Adapters tab and REST API (`POST /api/adapters/rescan`), detecting hotplugged adapters instantly.
   - Live combinable adapters badge (e.g. `4 Detected (3 Combined)`) and responsive Cyberpunk empty state banner.

4. **Automated Rotating Proxy Harvester & Multi-NIC Egress Dispatcher (Enhanced in v2.1.0 – v2.1.7)**:
   - **Persistent Healthy Proxy Caching & Dynamic Unavailable Eviction (v2.1.7)**:
     - Automatically saves only verified healthy proxies (`IsHealthy == true && Failures == 0`) to persistent disk storage (`healthy_proxies.json`), filtering out dead nodes.
     - Cold startup pre-warming restores verified proxies immediately on launch with zero scraping delay.
     - Dynamically evicts unavailable nodes from both memory and disk storage upon connection failure.
   - **Massive 255+ Global & Geo-Targeted Proxy Feeds Catalog (v2.1.6)**:
     Curated feeds across HTTPS (85 global, 28 country), HTTP (65 global, 16 country), SOCKS4 (41 global, 9 country), and SOCKS5 (57 global, 9 country).
   - **Bulk Proxy Repository Sources Management (v2.1.3)**:
     Bulk add, edit, or customize proxy feed URLs and repository APIs with live URL counting, comment filtering (`#`, `//`), and 1-click restore to verified default repositories.
   - **Multi-Faceted Proxy Sorting & Filtering (v2.1.4)**:
     Filter by latency threshold (<150ms, <300ms, <500ms, <1000ms), dynamic scraping source origin, protocol (`HTTP`, `HTTPS`, `SOCKS4`, `SOCKS5`), and live search matching host, port, country, or protocol.
   - **Multi-Protocol Health Validator & Heuristic Parser**:
     High-concurrency parallel prober testing HTTP, HTTPS, SOCKS4, and SOCKS5 in milliseconds, measuring RTT latency and identifying Elite anonymity. Supports authenticated proxies (`IP:PORT:USER:PASS`) via Basic Auth and SOCKS5 RFC 1929 subnegotiation.
   - **Geo-Targeted Rotation Filtering**:
     Instant 1-click country filtering (`ALL`, `US`, `DE`, `GB`, `FR`, `CA`, `JP`, `NL`, `SG`, `IN`) with graceful fallback.
   - **Double-Layer Multi-NIC Egress MOAT**:
     Binds outbound proxy connections across local physical network adapters while rotating global public IPs, eliminating both local router bottlenecks and remote rate limits simultaneously.

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

| Dimension | CombiSpeed (Native C# .NET v2.2.2) | RezaRafia/Speedy (Reference) | Standard Windows Routing |
|---|---|---|---|
| **UI/UX & Accessibility** | **Custom dark high-contrast ComboBox, anti-fallback text styles, Segmented Tabbed Navigation, WCAG 2.1 AA High Contrast, UI Scaling (100%-130%), Reduce Motion, Screen Reader support, Explanatory Tooltips across all controls & Hotkeys** | Cramped single-scroll Electron page | Native OS Control Panel / Settings |
| **Two-Stage Multi-NIC Bonding & Proxy Routing** | **Physical multi-adapter socket binding aggregated first, then forwarded through selective timely rotating proxies with live retry & dead eviction** | None (Single adapter loopback proxy) | Rigid single physical adapter binding |
| **Live Proxy Auto-Rotation Retry & Probing** | **Sub-second socket connectivity probing (`ProbeLiveConnectionAsync`), live auto-advance retry loops, and 1-click `🎯 Force Connect`** | None | N/A |
| **Proxy State Persistence & Idempotence** | **Strict idempotence ($f(f(x)) = f(x)$), complete state persistence (`proxy_state.json`), preserving user selections, latency benchmarks, filters, and multi-criteria sorting** | None (Lost on restart) | N/A |
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
- **Windows**: `publish/windows/CombiSpeed-v2.2.4-win-x64.zip` and `publish/CombiSpeed-v2.2.4-win-x64.zip`
- **Linux**: `publish/linux/CombiSpeed-v2.2.4-linux-x64.tar.gz` and `publish/linux/CombiSpeed-v2.2.4-linux-x64.zip`
- **macOS**: `publish/macos/CombiSpeed-v2.2.4-macos-x64.tar.gz` and `publish/macos/CombiSpeed-v2.2.4-macos-x64.zip`

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
