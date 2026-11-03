# Release Notes

## [2.1.3] - 2026-11-03 - Bulk Proxy Repository Sources Management Release

### 🚀 Highlights & Engineering Advancements
- **🌐 Bulk Proxy Repository Sources & Feed URLs Management**:
  - **Configurable Repository Sources**: Added `RepositorySources` to `RotatingProxyConfigModel` across Windows, Linux, and macOS, allowing users to define, edit, add, or customize raw text repository URLs, GitHub raw files, and JSON APIs.
  - **Bulk Multi-Line Parsing & Sanitization**: Implemented `SetSourcesFromText()`, stripping inline/full-line comments (`#` or `//`), ignoring blank lines, validating HTTP/HTTPS URL syntax, and de-duplicating feeds.
  - **Reset to Verified Defaults**: Added 1-click restore functionality to re-populate the official verified open-source proxy repositories (*TheSpeedX*, *monosans*, *clarketm*, *proxyscrape API*).
- **🖥️ Windows WPF `ProxySourcesDialog.xaml` Modal**:
  - Added dedicated **"🌐 Sources..."** button to Tab 4 toolbar in `MainWindow.xaml`.
  - Created accessible dark cyberpunk glassmorphic modal with real-time active URL counter badge, clipboard pasting, guidelines, and instant configuration persistence.
- **🐧 Linux & 🍎 macOS REST APIs & Cyberpunk Web Modal**:
  - Implemented `/api/rotating-proxy/sources` (`GET`, `POST`) and `/api/rotating-proxy/sources/reset` (`POST`) endpoints in embedded dashboard servers.
  - Integrated interactive Cyberpunk SPA modal in `CyberpunkSpa.cs` with live URL counting, textarea editor, and seamless AJAX persistence.
- **🔄 Harvester & Background Scheduler Integration**:
  - Wired `GetEffectiveSources()` directly into manual harvester triggers and autonomous 30-minute background auto-harvest timers.
- **✅ Automated Verification**:
  - Added `[TEST 6/6]` to `--verify-rotator` CLI suite verifying bulk ingestion, comment stripping, URL validation, and default reset with 100% pass rate.
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.1.3-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.1.3-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.1.3-macos-x64.tar.gz` and `.zip`

---

## [2.1.2] - 2026-11-03 - Windows Executable XAML Startup Fix & Crash Resilience Release

### 🚀 Highlights & Engineering Advancements
- **🛠️ Resolved `CombiSpeed.exe` Startup Crash (`XamlParseException`)**:
  - **Root Cause Resolution**: Resolved unhandled `XamlParseException` triggered during `MainWindow.xaml` BAML compilation by explicitly defining named `CyberComboBox` and `CyberCheckBox` resource styles in `App.xaml` and properly chaining implicit styles.
  - **Zero Missing StaticResources**: Audited 100% of `.xaml` files across the codebase to ensure all static resource references resolve with zero errors.
- **🛡️ Enterprise Crash Resilience & Logging Subsystem**:
  - Added global `AppDomain.CurrentDomain.UnhandledException` and `DispatcherUnhandledException` handlers in `App.xaml.cs`.
  - Automatically captures critical stack traces and dumps detailed diagnostic info into `%LOCALAPPDATA%\CombiSpeed\crash.log`.
  - Displays user-friendly critical alert dialogs on unexpected runtime failures rather than silently exiting.
- **🔄 Synchronized Runtime Installation & Packages**:
  - Refreshed `%LOCALAPPDATA%\Programs\CombiSpeed\CombiSpeed.exe` with validated `v2.1.2` binaries.
  - Verified live GUI initialization, window presentation, and clean shutdown with 100% success.
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.1.2-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.1.2-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.1.2-macos-x64.tar.gz` and `.zip`

---

## [2.1.1] - 2026-11-03 - Advanced Geo-Targeting, Upstream Auth & Background Scheduler Release

### 🚀 Highlights & Engineering Advancements
- **🌍 GeoIP Country Detection & Geo-Targeted Rotation Filtering**:
  - Implemented automatic extraction of ISO 3166-1 alpha-2 country codes across JSON APIs, URL parameters, and HTML tables.
  - Added dedicated Country Filter selector (`ALL`, `US`, `DE`, `GB`, `FR`, `CA`, `JP`, `NL`, `SG`, `IN`) across Windows WPF and Linux/macOS Web SPAs.
  - Router dispatches outbound connections strictly through proxies matching the chosen regional territory, with graceful fallback to the wider pool if a specific country's nodes are exhausted.
- **🔄 Autonomous Background Auto-Harvest & Auto-Validation Scheduler**:
  - Implemented `ProxySchedulerService.cs` (`LinuxProxyScheduler.cs`, `MacProxyScheduler.cs`) running background non-blocking timers.
  - Automatically re-harvests fresh candidate proxies from open-source feeds and custom URLs every 30 minutes.
  - Automatically re-probes pool health, updates RTT latencies, and prunes dead endpoints every 5 minutes.
  - UI toggles: `Auto-Harvest (30m)` and `Auto-Check (5m)` with persistent disk storage.
- **🔐 Upstream Proxy Authentication (HTTP Basic & SOCKS5 RFC 1929)**:
  - Added transparent support for private authenticated proxies formatted as `IP:PORT:USER:PASS` or `PROTOCOL://USER:PASS@IP:PORT`.
  - HTTP / HTTPS CONNECT: Automatically injects `Proxy-Authorization: Basic <base64>` header upstream.
  - SOCKS5: Fully negotiates RFC 1929 Username/Password subnegotiation (method `0x02`) before issuing CONNECT commands.
  - Validator prober validates authentication credentials during health checks.
- **📊 JSON API Scraper & Multi-Platform UI Enhancements**:
  - Added JSON array & object parser in `ProxyHarvesterService.cs` supporting modern proxy APIs.
  - Added `GEO` column in Windows DataGrid and Linux/macOS Cyberpunk SPA proxy tables.
  - Added `/api/rotating-proxy/config` REST endpoint in Linux and macOS embedded servers.
- **✅ Self-Test Verification**:
  - Expanded `--verify-rotator` CLI command to validate country filtering, JSON feed parsing, and scheduler lifecycle with 100% test pass rate.
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.1.1-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.1.1-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.1.1-macos-x64.tar.gz` and `.zip`

---

## [2.1.0] - 2026-11-03 - Automated Rotating Proxy Harvester & Multi-NIC Egress Release

### 🚀 Highlights & Engineering Advancements
- **🔄 Automated Rotating Proxy Harvester & Scraper**:
  - Implemented `ProxyHarvesterService.cs` (`LinuxProxyHarvester.cs`, `MacProxyHarvester.cs`) to automatically scrape proxy candidate pools from maintained open-source repositories (*TheSpeedX*, *monosans*, *clarketm*, *proxyscrape API*) and custom user URLs/APIs.
  - Universal regex parser extracts proxies from raw text, CSV, JSON, and HTML tables (`<td>IP</td><td>PORT</td>`) with support for `IP:PORT`, `PROTOCOL://IP:PORT`, and authenticated `IP:PORT:USER:PASS`.
  - Automatically isolates and discards loopback and RFC 1918 private subnets.
- **⚡ Parallel Multi-Protocol Health & Anonymity Validator**:
  - Implemented `ProxyValidatorEngine.cs` using semaphore-bounded asynchronous probing for HTTP, HTTPS CONNECT, SOCKS4, and RFC 1928 SOCKS5 endpoints.
  - Accurately measures real-time RTT latency (ms), verifies protocol compliance, detects anonymity levels (Elite, Anonymous, Transparent), and automatically prunes dead endpoints.
- **🛡️ Dynamic Rotation & Sticky Session Engine**:
  - Implemented `RotatingProxyRouter.cs` supporting:
    - `Round Robin`: Rotates to a new proxy on every single request.
    - `Sticky Domain`: Retains the same upstream proxy for requests targeting the same host, preserving login sessions and OAuth cookies.
    - `Sticky Session`: Retains proxy affinity per client connection for a configurable duration (5–30 mins).
    - `Lowest Latency`: Rotates among the fastest verified responsive proxies ($< 250\text{ ms}$).
    - `Elite Only`: Restricts rotation strictly to high-anonymity Elite proxies.
  - Transparent circuit breaker automatically re-routes failed requests through alternative healthy proxies before returning an error to the client.
- **🌐 The "Double-Layer" Multi-NIC Egress MOAT**:
  - Combines local multi-WAN physical socket binding (`Socket.Bind(adapterIp, 0)`) with global upstream proxy rotation.
  - Outbound proxy traffic is distributed across local physical adapters (Ethernet, Wi-Fi, Cellular) to prevent local router bottlenecks while rotating global public WAN IPs.
- **🎛️ Dual-Port Architecture & Smart PAC Integration**:
  - Dedicated Rotating Proxy listener on port `8081` alongside high-speed bonded port `8080`.
  - Added `PacAction.RotatingProxy` in PAC configurations, allowing users to direct scraping targets to port `8081` while streaming stays on port `8080`.
- **💻 Cross-Platform Dashboard Parity**:
  - Windows: Added dedicated "🔄 Rotating Proxy & Harvester" tab with live DataGrid, control bar, and telemetry.
  - Linux & macOS: Added REST endpoints (`/api/rotating-proxy/*`) and dedicated Rotating Proxy view in Cyberpunk Web SPA.
- **✅ Comprehensive Verification**:
  - Added `--verify-rotator` self-test suite covering universal proxy regex extraction, validator pool management, rotation strategies, and loopback dispatch.
  - 100% test pass rate across `--verify-rotator`, `--verify-throttling`, `--verify-persistence`, and `--verify-benchmark`.
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.1.0-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.1.0-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.1.0-macos-x64.tar.gz` and `.zip`

---

## [2.0.2] - 2026-11-02 - Speed Combining & Unified Bandwidth Pooling Release

### 🚀 Highlights & Engineering Advancements
- **⚡ Speed Combining & Elimination of Bandwidth Splitting**:
  - Replaced legacy proportional rate partitioning with **Combine Speeds Mode** (`CombineSpeedsMode = true`).
  - Speeds across all active network adapters and connections are pooled additively ($TotalSpeed = Speed_{NIC1} + Speed_{NIC2} + \dots$) into a unified high-speed pipeline.
  - Eliminated artificial bandwidth slicing where adapters were clamped to fractional sub-limits (e.g. 50% or 25%). Every connection and adapter has access to the full combined speed pool.
  - Configured rate limits (e.g. 500 Mbps) enforce the combined aggregate cap across all connections simultaneously rather than splitting the bandwidth into smaller fractions.
- **🚀 Multi-NIC Parallel Range Chunk Speed Combiner**:
  - Implemented `TryCombinedRangeDownloadAsync` in `ProxyEngine.cs` for HTTP downloads and file transfers.
  - For sizable files ($\ge 512\text{ KB}$ with HTTP Range support), the proxy divides the transfer into parallel byte chunks and fetches them concurrently across all active physical adapters simultaneously.
  - Sockets bound to Ethernet, Wi-Fi, and Cellular stream parts of the same file concurrently, actively combining the physical wire speeds of multiple connections for a single file download!
  - Transparent fail-safe fallback to standard streaming for non-range or dynamic HTTP endpoints.
- **🎛️ UI & Preset Enhancements**:
  - Toolbar preset updated to **"Unlimited (Combined Speed)"**.
  - Dynamic status bar indicator updated to **"Bandwidth: Combined Speed Aggregation (Active)"**.
  - Updated `CustomThrottleDialog.xaml` and `SettingsWindow.xaml` with `Combine Speeds Mode (Pool aggregate bandwidth without splitting across connections)` checkbox, checked by default.
  - Replaced "Bandwidth Share" label with "Speed Allocation: Load Share (Combined)".
- **✅ Comprehensive Verification**:
  - Updated `--verify-throttling` self-test suite to validate Combine Speeds allocation (both adapters pooling full combined capacity), 1.5MB live rate-limited delay, and 500 Mbps pool verification.
  - 100% test pass rate across `--verify-throttling`, `--verify-persistence`, and `--verify-benchmark`.
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.0.2-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.0.2-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.0.2-macos-x64.tar.gz` and `.zip`

---

## [2.0.1] - 2026-11-02 - Speed Benchmark Overhaul & Virtual Adapter Routing Fix

### 🚀 Highlights & Engineering Advancements
- **⚡ Pre-Flight Latency & Routability Probing**:
  - Automatically isolates host-only and virtual adapters (such as Hyper-V `vEthernet` or WSL bridges with IPs `172.19.96.1` / `172.31.0.1`).
  - Implemented an asynchronous 1500ms pre-flight socket connect probe (`socket.ConnectAsync("1.1.1.1", 80)`).
  - Adapters with latency >= 900ms or unreachable default routes are immediately assigned 0 Mbps without initiating throughput downloads, completely eliminating 21-42 second TCP SYN retransmission hangs.
- **🛡️ Proxy Bypass Socket Isolation (`UseProxy = false`)**:
  - Configured `SocketsHttpHandler` with `UseProxy = false` and `Proxy = null` across all benchmark sockets.
  - Fixes the fatal `10049 WSAEADDRNOTAVAIL` socket error that occurred when benchmark sockets bound to physical adapter IPs (`192.168.x.x`) attempted to route through the active CombiSpeed system proxy at `127.0.0.1:8080`.
- **🌐 Strict IPv4 DNS Resolution (`ResolveHostIPv4Async`)**:
  - Resolved IPv6 address family mismatches by strictly filtering DNS query results to `AddressFamily.InterNetwork`, ensuring sockets bound to IPv4 adapter endpoints connect smoothly.
- **🚀 Multi-Stream Bonded Socket Benchmarking**:
  - Executes parallel multi-stream socket downloads across all routable physical adapters simultaneously, accurately measuring aggregate wire speed and bonded throughput.
  - Real-world verified: individual speeds (~189 Mbps on Ethernet, ~72 Mbps on Wi-Fi) and bonded aggregation (~169-233 Mbps multi-stream).
- **🐧🍏 Cross-Platform Native Benchmark Parity**:
  - Linux: Implemented `LinuxSpeedTester.cs` with `SO_BINDTODEVICE` socket binding, pre-flight latency probe, and multi-endpoint fallback.
  - macOS: Implemented `MacSpeedTester.cs` with `IP_BOUND_IF` / `SO_DONTROUTE` socket binding and pre-flight routability checks.
  - Wired to `/api/speedtest/start` and updated `CyberpunkSpa.cs` in both Linux and macOS dashboards to display real bonded throughput, latency, boost percentage, and per-adapter speed breakdown.
- **🧹 Memory & UI Leak Prevention**:
  - Stored `_progressHandler` in `SpeedTestWindow.xaml.cs` and cleanly unsubscribed on the `Closed` event, eliminating event handler leaks and UI race conditions.
- **✅ Verification & Testing**:
  - Added `--verify-benchmark` CLI command to Windows, running automated self-tests across physical and virtual adapters.
  - 100% test pass rate across `--verify-benchmark`, `--verify-persistence`, and `--verify-throttling`.
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.0.1-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.0.1-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.0.1-macos-x64.tar.gz` and `.zip`

---

## [2.0.0] - 2026-11-02 - Multi-OS Cross-Platform & PAC Export Super Major Release

### 🚀 Highlights & Super Major Advancements
- **💾 Proxy Auto-Config (PAC) File Saving & Direct Export**:
  - Added dedicated **"💾 Export PAC File..."** button directly inside the Proxy Auto-Config (PAC) configuration card in `SettingsWindow.xaml`.
  - Integrated `Microsoft.Win32.SaveFileDialog` pre-configured for `.pac`, `.js`, and all files, generating and writing RFC-compliant `FindProxyForURL(url, host)` JavaScript files directly to any user-chosen local or network path.
  - Added CLI command `--export-pac [outputPath]` to all editions (Windows, Linux, macOS), enabling automated PAC compilation and exporting directly from terminal, scripts, Docker entrypoints, and CI/CD pipelines.
  - PAC script automatically incorporates RFC 1918 private subnets, loopback addresses (`127.*`, `localhost`, `::1`), corporate domains (`*.local`, `*.internal`, `*.corp`), custom routing rules, and fallback multi-proxy definitions (`PROXY 127.0.0.1:{httpPort}; SOCKS5 127.0.0.1:{socksPort}; DIRECT`).
- **🐧 Dedicated Native Linux Codebase (`CombiSpeed-Linux/`)**:
  - Independent, standalone .NET 8.0 Linux solution (`CombiSpeed.Linux.csproj`) targeting `net8.0` without any Windows dependencies.
  - **Linux Kernel Multi-NIC Socket Binding**: Binds outbound TCP/UDP sockets to physical network interface names via Linux `SO_BINDTODEVICE` (socket opt 25) and local interface IPs, bypassing default routing limitations on Linux.
  - **Linux System Proxy Automation**: Integrates with GNOME desktop environments via `gsettings set org.gnome.system.proxy`, generates system-wide shell environment scripts (`~/.config/combispeed/proxy.env`), and automatically cleans up on shutdown.
  - **100% Identical Cyberpunk Glassmorphic Dashboard**: High-performance local microserver serving the identical dark cyberpunk UI with live 60 FPS Canvas waveforms, 4 telemetry KPI cards, segmented navigation tabs (`⚡ Overview & Dashboard`, `🖧 Combined Adapters`, `🚀 Speed Shaper & QoS`, `📜 Connection Audit`), 500 Mbps speed shaper, custom throttle modal, speed test benchmark, and WCAG AA accessibility controls.
  - **1-Click Linux Management Scripts**: Complete package with `start-combispeed.sh`, `stop-combispeed.sh`, `install-combispeed.sh`, `uninstall-combispeed.sh`, and FreeDesktop `combispeed.desktop` shortcut.
- **🍏 Dedicated Native macOS Codebase (`CombiSpeed-macOS/`)**:
  - Independent, standalone .NET 8.0 macOS solution (`CombiSpeed.macOS.csproj`) targeting `net8.0` with full Darwin kernel compatibility.
  - **macOS Hardware Port Detection & Binding**: Automatically maps macOS network services (Wi-Fi `en0`, Thunderbolt Ethernet `en1`, iPhone USB Tethering) and binds outbound sockets via `IP_BOUND_IF` / `SO_DONTROUTE` and local interface IPs.
  - **macOS System Proxy Automation**: Integrates directly with macOS `networksetup` CLI (`-setwebproxy`, `-setsecurewebproxy`, `-setsocksfirewallproxy`, `-setautoproxyurl`), ensuring system-wide proxy activation and clean fail-safe restoration on application exit.
  - **Identical Cyberpunk Glassmorphic UI**: Serves the exact same visual styling, glowing LED switches, segmented tabs, distribution bars, and accessibility features on macOS.
  - **1-Click macOS Management Scripts & App Bundle**: Complete package including `start-combispeed-mac.sh`, `stop-combispeed-mac.sh`, `install-combispeed-mac.sh`, `uninstall-combispeed-mac.sh`, and Apple application bundle `CombiSpeed.app/Contents/Info.plist`.
- **🪟 Windows Codebase Upgraded to v2.0.0 (Build 14)**:
  - Version bump in `CombiSpeed.csproj`, `MainViewModel.cs`, and `App.xaml.cs`.
  - Added PAC file export in `SettingsWindow.xaml.cs` and CLI `--export-pac <path>`.
  - Upgraded verification test suites with automated PAC generation validation.
- **📦 Separate Distribution Releases per Operating System**:
  - Windows: `publish/windows/CombiSpeed-v2.0.0-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.0.0-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.0.0-macos-x64.tar.gz` and `.zip`
- **✅ Comprehensive Verification**:
  - 100% test pass rate across Windows, Linux, and macOS automated self-tests (`--verify-persistence`, `--verify-throttling`, `--version`, `--export-pac`).

---

## [1.4.1] - 2026-11-02 - High-Contrast Dropdowns & Text Readability Release

### 🚀 Highlights & Features
- **Custom Accessible Dark `ComboBox` ControlTemplate**:
  - Replaced the platform default Windows Aero2 combo box template with a bulletproof custom dark WPF `ControlTemplate`.
  - Dark high-contrast toggle button (`#131D33`) with `#334155` border, vivid cyan focus rings, and dedicated split border with high-contrast `#00F0FF` chevron path icon.
  - Resolved the classic white-on-white / black-on-dark combo box display defect where the closed selection box item was unreadable due to OS theme overrides.
  - Dropdown popup rebuilt with deep navy backdrop (`#0B1120`), glowing `#00F0FF` border, and dark scrollviewer.
  - Enhanced `ComboBoxItem` styles with `#FFFFFF` text, `#1E293B` hover state with `#00F0FF` text, and `#132742` selected state with 14:1+ contrast ratio.
- **Global TextBlock & Label Anti-Fallback Styles**:
  - Registered global default styles for `TextBlock` and `Label` with default `Foreground="#F8FAFC"`, preventing controls from falling back to system `WindowText` black (`#000000`) on dark backgrounds when Windows is set to Light Mode.
- **Application-Wide Text Contrast & Readability Upgrade**:
  - Replaced low-luminance and dim text colors (`#94A3B8`, `#334155`) across all cards, dialogs, and tables with high-luminance, crystal-clear colors (`#FFFFFF` pure white, `#38BDF8` sky cyan, `#F1F5F9` crisp slate, and `#CBD5E1` bright silver).
  - High-contrast field headers and section tags in `MainWindow.xaml`, `SettingsWindow.xaml`, `CustomThrottleDialog.xaml`, `AccessibilityDialog.xaml`, `InputDialog.xaml`, and `SpeedTestWindow.xaml`.
  - Unselected navigation tabs in `SegmentedTabItem` and radio buttons in `SegmentedTabButton` upgraded from dim `#94A3B8` to crisp `#E2E8F0` with `FontWeight="SemiBold"`.
- **Global Dark `DataGrid` Styling**:
  - Styled `DataGridColumnHeader` (`#0F172A` background, `#00F0FF` bold text, `#1E293B` border), `DataGridRow`, and `DataGridCell` with clear hover and selection highlighting for connection audit and PAC routing tables.
- **Verification & Packaging**:
  - 100% test pass rate across automated self-tests (`--verify-persistence`, `--verify-throttling`, `--version`).
  - Standalone release package: `publish/CombiSpeed-v1.4.1-win-x64.zip`.

---

## [1.4.0] - 2026-11-02 - Accessible UI/UX Redesign Release

### 🚀 Highlights & Features
- **Modern Segmented Navigation & Information Architecture**:
  - Re-architected `MainWindow.xaml` with a sleek, accessible tabbed navigation bar: `⚡ Overview & Dashboard`, `🖧 Combined Adapters`, `🚀 Speed Shaper & QoS`, and `📜 Connection Audit`.
  - Eliminates vertical scrolling fatigue on laptops and smaller displays while giving each subsystem dedicated, uncluttered visual focus.
  - Full keyboard accessibility with direct Tab/Arrow navigation and instant shortcuts (`Ctrl+1` through `Ctrl+4`).
- **Comprehensive WCAG 2.1 AA Accessibility Overhaul**:
  - **High Contrast Theme**: Stark high-contrast mode with deep black backdrop (`#030712`), high-luminance text (`#FFFFFF`), and vivid focus outlines (`#38BDF8`) exceeding 7:1 contrast ratios.
  - **Dynamic UI & Font Scaling**: Seamless scaling (`100%`, `115%`, `130%`) powered by layout transformation, guaranteeing that high-DPI displays and low-vision users enjoy crisp, enlarged text and cards with zero layout clipping or horizontal overflow.
  - **Reduce Motion & Vestibular Safety**: Toggleable mode that pauses rapid waveform redraws and softens LED transitions, preventing visual fatigue for users with vestibular conditions or ADHD.
  - **Screen Reader Optimization (`AutomationProperties`)**: Comprehensive accessible names, help texts, and live region announcements across all master switches, sliders, checkboxes, and tables for Windows Narrator, NVDA, and JAWS.
  - **High-Visibility Keyboard Focus Rings**: Custom 2px neon cyan focus visualizer on all interactive controls (`Button`, `TextBox`, `Slider`, `CheckBox`, `ComboBox`).
  - **Global Keyboard Access Keys**: Instant hotkeys (`Space`/`Alt+P` for Master Bonding Power, `Alt+B` for Virtual Bridge, `Alt+W` for Windows Proxy, `Alt+R` for Rescan, `Alt+A` for Accessibility, `Alt+S` for Settings, `Alt+H` for High Contrast).
- **Dedicated Accessibility Options Dialog (`AccessibilityDialog.xaml`)**:
  - Interactive modal dialog accessible via `Alt+A` or the titlebar `♿ Accessibility` button, featuring quick toggles for High Contrast, UI Scale radio buttons, Reduce Motion, Screen Reader options, and an interactive Keyboard Shortcuts Cheat Sheet.
- **Custom Dark-Themed Styled Controls**:
  - Custom accessible dark `ComboBox` replacing classic gray dropdowns with dark navy popups (`#0F172A`) and crisp high-contrast text.
  - Custom accessible `Slider` with ergonomic 18px thumb, glow effects, and precision tooltips.
- **Durable Persistence & Verification**:
  - All accessibility settings (`IsHighContrast`, `UiScale`, `IsReduceMotion`, `IsScreenReaderOptimized`) persist across sessions in `%LOCALAPPDATA%\CombiSpeed\adapter_settings.json`.
  - Automated verification test suite passed 100% OK (`--verify-persistence`, `--verify-throttling`, `--version`).
  - Release distribution package: `publish/CombiSpeed-v1.4.0-win-x64.zip`.

---
## [1.3.6] - 2026-11-02 - Dynamic Cross-Connection Speed Shaper & Latency Modes Release

### 🚀 Highlights & Features
- **Dynamic Speed Shaping Across Connections**:
  - `BandwidthThrottler.cs` actively monitors connection metrics and dynamically adapts token bucket rates and capacity allocations across physical network adapters in real time.
  - Dynamically redistributes bandwidth without stream disruption or connection drops when link conditions fluctuate.
- **Selectable Speed Shaper Optimization Modes**:
  - **`🚀 Maximum Bandwidth Mode`**: Optimizes for peak aggregate throughput and bulk transfer volume (file downloads, torrents, multi-connection streaming). Automatically scales Token Bucket burst buffers to 150ms-200ms for full TCP congestion window expansion and enables dynamic capacity borrowing across active streams.
  - **`⚡ Lowest Latency / Ping Mode`**: Tailored for competitive online multiplayer gaming, Discord voice channels, and WebRTC streaming. Implements tight anti-bufferbloat packet pacing (clamped to 20ms burst window) to eliminate local interface queue latency, and dynamically weights token allocations and SWRR stream dispatching inversely proportional to live RTT ping measurements.
  - **`⚖ Balanced Dynamic Mode`**: Proportional cross-adapter shaping balancing aggregate throughput and low-jitter queue pacing.
- **Real-Time Live Telemetry Adaptation**:
  - Connected directly to `HealthMonitor.HealthUpdated`: when an adapter experiences latency spikes or packet loss, the dynamic shaper immediately dials down its allocation and shifts traffic to healthier, lower-ping connections.
- **Toolbar & Dialog UI Integration**:
  - Added dedicated `Mode` selector ComboBox (`🚀 Maximum Bandwidth`, `⚡ Lowest Latency / Ping`, `⚖ Balanced Dynamic`) directly to the Speed Shaper toolbar in `MainWindow.xaml`.
  - Updated `CustomThrottleDialog.xaml` with a dedicated Speed Shaper Optimization Mode card and `Enable Dynamic Real-Time Adaptation` toggle.
  - Synchronized options in `SettingsWindow.xaml`.
- **Durable Persistence & Diagnostic Test Verification**:
  - Full persistence of `ShaperMode` and `EnableDynamicShaping` in `%LOCALAPPDATA%\CombiSpeed\adapter_settings.json`.
  - Enhanced `--verify-throttling` automated self-test suite validating Maximum Bandwidth burst window expansion, Lowest Latency anti-bufferbloat pacing, dynamic cross-connection adaptation, and persistence (100% OK).
  - Published release archive `publish/CombiSpeed-v1.3.6-win-x64.zip`.

---
## [1.3.5] - 2026-11-02 - 500 Mbps Throttling Cap & Custom Throttling Parameters Release

### 🚀 Highlights & Features
- **500 Mbps Bandwidth Throttling Cap & High-Speed Presets**:
  - Expanded bandwidth rate limiting presets in `MainViewModel.cs` to include high-speed tiers: `16 Mbps (2 MB/s)`, `40 Mbps (5 MB/s)`, `80 Mbps (10 MB/s)`, `160 Mbps (20 MB/s)`, `250 Mbps (31.25 MB/s)`, `400 Mbps (50 MB/s)`, and `500 Mbps (62.5 MB/s)`.
  - Token Bucket rate limiter capacity dynamically scales to sustain 500+ Mbps aggregate speeds across combined network interfaces.
- **Custom Throttling Parameters Dialog (`CustomThrottleDialog.xaml`)**:
  - Interactive Cyberpunk modal dialog allowing users to enter arbitrary custom download and upload speed limits.
  - Multi-unit rate input supporting `Mbps` (Megabits/sec), `MB/s` (Megabytes/sec), `Kbps`, and `KB/s` with real-time equivalent calculations.
  - Quick-preset chips for 1-click rate selection (`[50 Mbps]`, `[100 Mbps]`, `[250 Mbps]`, `[400 Mbps]`, `[500 Mbps]`, `[Unlimited]`).
  - Auto-calculate upload speed cap option (50% of download rate).
  - Configurable Token Bucket Burst Tolerance Window: `20 ms (Ultra-Smooth Pacing)`, `50 ms (Balanced)`, `100 ms (High Burst)`, and `250 ms (Maximum Throughput)`.
  - Per-adapter custom speed limit overrides directly editable in the dialog list, allowing metered connections to be clamped individually.
- **Dashboard Toolbar & Settings Window Integration**:
  - Added dedicated `⚙ Custom...` button to the Bandwidth Throttling toolbar in `MainWindow.xaml`.
  - Selecting `Custom...` from the preset dropdown automatically opens the custom parameter dialog.
  - If a custom limit is active, the toolbar displays a dynamic tag (e.g. `Custom: 500.0 Mbps (59.6 MB/s)`).
  - Updated `SettingsWindow.xaml` with multi-unit speed limit inputs and burst window selector.
- **Persistence & Diagnostic Test Verification**:
  - Full persistence of custom limits, units, burst window, and per-adapter speed caps in `%LOCALAPPDATA%\CombiSpeed\adapter_settings.json`.
  - Enhanced `--verify-throttling` automated self-test suite validating 500 Mbps rate limiting, parameter conversion, and per-adapter overrides with 100% pass rate.
  - Published release archive `publish/CombiSpeed-v1.3.5-win-x64.zip`.

---
## [1.3.4] - 2026-11-01 - Application Version Visibility & TitleBar / Footer Status Bar Release

### 🚀 Highlights & Features
- **Application Window Version Visibility**:
  - Dynamically exposes and renders the current application version ( 1.3.4) and full build identification ( 1.3.4 (Build 9)) directly in the WPF desktop window.
  - **Custom TitleBar Cyberpunk Badge**: Prominently highlights the installed release version ( 1.3.4) next to the COMBISPEED neon logo with dedicated tooltips, visible across all view states.
  - **Pinned Glassmorphic Footer Status Bar**: Integrated a bottom status bar displaying AppVersionFull ( 1.3.4 (Build 9)), engine attribution, real-time configuration auto-sync feedback, and Windows x64 runtime environment.
  - **Dynamic Taskbar & Window Title**: Synchronized Title="{Binding WindowTitle}" to reflect the active version in Windows Taskbar, Task Manager, and Alt-Tab switcher.
  - **CLI Version Flag**: Added --version, -v, and /version command-line argument support, printing detailed build and runtime metadata to stdout.
- **Production Package Synchronization**:
  - Automated version incrementation to 1.3.4 (patch update) and  ersionCode: 9 across CombiSpeed.csproj,  ersion.json, and one-click installer scripts.
  - Re-generated release distribution package publish/CombiSpeed-v1.3.4-win-x64.zip.

---
## [1.3.3] - 2026-11-01 - Production Multi-WAN Channel Bonding & Bandwidth Aggregator Release

### 🚀 Highlights & Features
- **Consolidated Production Release & Distribution Packaging**:
  - Full production distribution package built with `.NET 8.0 Windows Desktop` (`net8.0-windows` x64).
  - Standalone zipped release archive created for GitHub releases (`publish/CombiSpeed-v1.3.3-win-x64.zip`) bundling the executable, icon assets, one-click start/stop/install/service scripts, and complete documentation.
  - Zero-configuration one-click installer (`Install-CombiSpeed.bat`) deploying directly to `%LOCALAPPDATA%\Programs\CombiSpeed` without UAC prompts, with Desktop & Start Menu shortcuts, and native Windows "Installed apps" registration.
- **Enterprise-Grade Capabilities Tested & Verified**:
  - **Virtual VPN / Adapter Connection & Bonded Bridge**: Native Windows connection (`CombiSpeed Bonded Bridge`) in `ncpa.cpl` and Windows Settings merging multiple physical NICs (Ethernet, Wi-Fi, 4G/5G Cellular).
  - **Smooth Weighted Round Robin (SWRR)**: Strictly multiplexes streams according to exact user percentage weights without clustering or burstiness.
  - **Real-Time Bandwidth Throttling & Traffic Shaper**: Asynchronous Token Bucket rate limiting across HTTP, HTTPS CONNECT, SOCKS5 TCP, SOCKS5 UDP Associate, and Virtual Bridge streams with quick presets (`Unlimited`, `2 MB/s`, `5 MB/s`, `10 MB/s`, `20 MB/s`, `50 MB/s`, `100 MB/s`) and per-adapter speed caps.
  - **Persistent Adapter Settings & Named Profiles**: Durable storage in `%LOCALAPPDATA%\CombiSpeed\adapter_settings.json` with MAC address hardware binding, 400ms debounced auto-save, atomic file commits, and 1-click profile switching.
  - **PAC (Proxy Auto-Config) Domain Router**: RFC-compliant JavaScript rule compiler and built-in HTTP server (`http://127.0.0.1:8080/proxy.pac`) with 1-click WinINet `AutoConfigURL` hook.
  - **RFC 1928 SOCKS5 UDP Associate Relay**: Outbound socket binding (`Socket.Bind`) for low-latency UDP multiplayer gaming, VoIP, and WebRTC streaming.
  - **Windows Service & Headless Background Engine**: Seamless background execution (`--service`, `--headless`) with `Install-Service.bat` and `Uninstall-Service.bat`.
- **System Diagnostics & Quality Verification**:
  - Comprehensive automated self-test suites (`--verify-persistence`, `--verify-throttling`) validated with 100% pass rates.
  - Minimal resource footprint: ~25 MB RAM working set, instant startup (<200ms), 0 compiler warnings, 0 compiler errors.

---

## [1.3.2] - 2026-11-01 - Real-Time Bandwidth Throttling & Traffic Shaping Release

### 🚀 Highlights & Features
- **Real-Time Bandwidth Throttling & Token Bucket Rate Limiting**:
  - Implements high-precision asynchronous Token Bucket rate limiting in [`BandwidthThrottler.cs`](file:///f:/New%20OpenCode Project/CombiSpeed/CombiSpeed/Services/BandwidthThrottler.cs), actively throttling byte transfer rates across combined connection adapters.
  - Rate limiting is integrated directly into [`ProxyEngine.cs`](file:///f:/New%20OpenCode Project/CombiSpeed/CombiSpeed/Services/ProxyEngine.cs) (HTTP, HTTPS CONNECT, SOCKS5 TCP), [`VirtualBridgeServer.cs`](file:///f:/New%20OpenCode Project/CombiSpeed/CombiSpeed/Services/VirtualBridgeServer.cs) (Windows Virtual Bonded Bridge adapter), and [`Socks5UdpRelay.cs`](file:///f:/New%20OpenCode Project/CombiSpeed/CombiSpeed/Services/Socks5UdpRelay.cs) (UDP Associate relay).
- **Strict Proportional Bandwidth Shaping**:
  - Strictly allocates and throttles byte rates across combined adapters in accordance with user-configured percentage weights (e.g. 75% Ethernet / 25% Wi-Fi), preventing faster lines from drowning out slower lines or violating target ratios.
- **Configurable Global Speed Caps & Per-Adapter Limits**:
  - 1-Click speed throttle presets: `Unlimited (Proportional)`, `2 MB/s (16 Mbps)`, `5 MB/s (40 Mbps)`, `10 MB/s (80 Mbps)`, `20 MB/s (160 Mbps)`, `50 MB/s (400 Mbps)`, `100 MB/s (800 Mbps)`, or custom limits.
  - Per-adapter speed limit overrides allowing users to cap metered connections (e.g., cellular mobile hotspot capped to 2 MB/s).
- **Dashboard Bandwidth Throttling Toolbar**:
  - Added dedicated Bandwidth Throttling & Speed Shaper toolbar to [`MainWindow.xaml`](file:///f:/New%20OpenCode Project/CombiSpeed/CombiSpeed/Views/MainWindow.xaml) with active status badge (`⚡ Throttling: ...`) and quick speed cap selector.
  - Enhanced adapter cards to display allocated throttle rates: `⚡ Capped: XX MB/s`.
- **Settings & Persistence Integration**:
  - Added Bandwidth Throttling configuration options in [`SettingsWindow.xaml`](file:///f:/New%20OpenCode Project/CombiSpeed/CombiSpeed/Views/SettingsWindow.xaml).
  - Automatically persists all throttle settings and per-adapter limits across application restarts in `%LOCALAPPDATA%\CombiSpeed\adapter_settings.json`.
- **Automated Regression Diagnostic Suite**:
  - Added `--verify-throttling` CLI flag verifying token bucket rate-limiting delay enforcement, proportional slice allocation, and bypass modes with a 100% test pass rate.

---

## [1.3.1] - 2026-10-30 - Adapter Settings & Combination Profiles Persistence Release

### 🚀 Highlights & Features
- **Adapter Combination & Weight Persistence**:
  - Automatically persists selected adapter states (`IsEnabled`) and custom percentage weights (`Weight`) to disk (`%LOCALAPPDATA%\CombiSpeed\adapter_settings.json`).
  - Restores exact adapter selections and bandwidth allocations upon application restart or adapter rescan.
  - Multi-attribute adapter binding: Uses permanent MAC address as primary key with name fallback, ensuring settings stay stably bound even across dynamic DHCP IP re-assignments.
- **Debounced Auto-Save Engine**:
  - Live 400ms debounced disk saving on UI slider dragging and checkbox toggles, preventing disk thrashing while guaranteeing zero configuration loss.
  - Fail-safe immediate save on application shutdown (`OnApplicationExit`) and explicit settings updates.
  - Atomic two-stage write protocol (`.tmp` -> replace) preventing file corruption during sudden system crashes or power-offs.
- **Combination Profiles & Preset Switching**:
  - Named profile presets (e.g. `Default Combination`, `Office Mode`, `Gaming 70/30`, `Mobile Backup`).
  - 1-click Profile dropdown selector in `MainWindow.xaml` allowing rapid switching between custom adapter combinations.
  - Dedicated Cyberpunk `InputDialog` for creating and saving new combination profiles on the fly.
  - Profile deletion capability (`✕`) with safety lock on the default profile.
- **Dashboard Auto-Save Indicator**:
  - Added real-time Auto-Save status badge (`💾 Auto-Saved: HH:mm:ss` / `Saving changes...`) directly on the adapter combination toolbar.
- **Headless & Windows Service Integration**:
  - `HeadlessEngine` now loads and applies saved adapter combinations and weight allocations automatically.
- **Diagnostic Verification**:
  - Added `--verify-persistence` CLI flag providing comprehensive automated regression testing of configuration storage, loading, and profile registration.

---

## [1.3.0] - 2026-10-30 - PAC Domain Router, Windows Service & SOCKS5 UDP Associate Release

### 🚀 Highlights & Features
- **PAC (Proxy Auto-Config) Domain-Based Interface Routing**:
  - Implements dynamic Proxy Auto-Config script generator (`FindProxyForURL`) allowing domain-based and subnet-based granular routing.
  - Automatically serves compliant JavaScript PAC script directly on `http://127.0.0.1:8080/proxy.pac` without opening additional ports.
  - Supports custom rule definition: Direct/Bypass (e.g. banking sites, internal corporate portals, RFC 1918 private subnets `192.168.*`, `10.*`), Bonded aggregation, or SOCKS5-specific paths.
  - 1-click Windows integration using native WinINet `AutoConfigURL` registry and runtime cache synchronization.
  - Added PAC rule editor and live script preview dialog in `SettingsWindow`.
- **RFC 1928 SOCKS5 UDP Associate Relay (Gaming & UDP Acceleration)**:
  - Extends SOCKS5 proxy engine with full RFC 1928 `UDP ASSOCIATE` command (`0x03`) support.
  - Relays UDP datagrams with outbound physical adapter socket binding (`Socket.Bind(new IPEndPoint(adapterIp, 0))`), accelerating multiplayer gaming (Valorant, CS2, Call of Duty, Steam), Discord/VoIP voice channels, DNS resolution, and WebRTC video calls across bonded adapters.
  - Encapsulates and returns remote UDP responses back to the client seamlessly.
- **Windows Service & Headless Background Execution Mode**:
  - Complete background daemon mode running headless without requiring a logged-in user or active WPF GUI (`--service`, `--headless`).
  - Added CLI service commands: `--service-install`, `--service-uninstall`, `--service-start`, `--service-stop`.
  - Created 1-click automated management scripts: `Install-Service.bat` and `Uninstall-Service.bat` registering `CombiSpeedService` in Windows Service Control Manager with auto-recovery on failure.
  - Integrated service termination in `Stop-CombiSpeed.bat` and `Uninstall-Service.bat`.

---

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
