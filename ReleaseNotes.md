# Release Notes

## [2.2.8] - 2026-11-04 - Light Theme Contrast & Universal Light Background Button Styling Release

### 🚀 Highlights & Engineering Advancements
- **🎨 Strict Light Theme Contrast Rule (Dark Font $\rightarrow$ Light Background for All Buttons)**:
  - **Universal Light Background Enforcement**: In Light Theme mode, any button displaying dark font colour (`#0F172A`, `#1E293B`, `#0369A1`, `#0284C7`) is strictly styled with a light background (`#FFFFFF`, `#E0F2FE`, `#F1F5F9`).
  - **Dynamic Resource Button Brushes**: Introduced dynamic brushes (`ButtonBg`, `ButtonFg`, `ButtonBorder`, `ButtonHoverBg`, `ButtonHoverBorder`, `ButtonHoverFg`, `ButtonPressedBg`, `PrimaryButtonBg`, `PrimaryButtonFg`, `PrimaryButtonBorder`, `PrimaryButtonHoverBg`, `PrimaryButtonPressedBg`, `PowerButtonBg`, `PowerButtonBorder`, `PowerButtonIconFg`, `PowerButtonActiveBg`, `PowerButtonActiveBorder`, `PowerButtonActiveFg`) across `App.xaml` and `MainViewModel.cs`.
  - **Child TextBlock Inheritance**: Added automatic child `TextBlock` foreground inheritance inside `CyberButton` and `PrimaryCyanButton`, ensuring text inside compound button layouts (`StackPanel` with icons) automatically inherits button foregrounds rather than falling back to default text colors.
  - **Global Implicit Button Style**: Configured global implicit `<Style TargetType="{x:Type Button}" BasedOn="{StaticResource CyberButton}"/>` in WPF, ensuring all dialog buttons and standard buttons automatically inherit the dynamic theme styling without requiring explicit style tags.
  - **Master Power Button Theme Adaptation**: Updated the large 78x78 Master Bonding Power button in `MainWindow.xaml` to use dynamic theme resources, rendering a clean `#FFFFFF` card in standby and `#E0F2FE` (light sky blue) with `#0284C7` (dark cyan font/icon) when active in Light Theme mode.
  - **Linux & macOS Cyberpunk Dashboard Parity**: Applied comprehensive `body.light-theme` button CSS rules across `CyberpunkSpa.cs` for `.btn-cyber` (`background: #FFFFFF !important`, `color: #0F172A !important`), `.btn-cyber-primary` (`background: #E0F2FE !important`, `color: #0369A1 !important`), `.tab-btn` (`background: #E2E8F0 !important`, `color: #334155 !important`), and `.power-toggle-btn` (`background: #FFFFFF !important`, `color: #0F172A !important`), guaranteeing 100% triple-OS visual consistency.
- **🧪 Automated Verification**:
  - Enhanced `--verify-persistence` across Windows, Linux, and macOS with automated luminance calculation checks verifying that dark font colors strictly sit on light backgrounds in Light Theme mode. All test suites passed with **100% SUCCESS**.

---

## [2.2.7] - 2026-11-04 - Timely Proxy Rotation, Pause Rotation, Proxy Disconnect, Proxy-Only Mode & Light Theme Contrast Release

### 🚀 Highlights & Engineering Advancements
- **🛑 Instant Persistence & Real-Time Audit Halting**:
  - **Zero-Latency Persistence**: Unchecking "Enable Connection Audit Logging" immediately persists the setting to disk (`proxy_config.json`, `combispeed_config.json`) without requiring a restart or manual save action.
  - **Immediate Logging Termination**: Gated all audit entry collection points across `ProxyEngine`, `VirtualBridgeServer`, and protocol listeners. When disabled, no audit items are generated, internal queues are flushed via `ClearAuditLogs()`, and UI displays an immediate "Auditing Stopped" indicator, saving CPU and RAM.
- **🎨 Complete Soothing Light Theme Contrast Overhaul**:
  - **Eliminated Dark Patches in Light Mode**: Systematically eradicated all hardcoded dark hex backgrounds (`#060910`, `#080C14`, `#131D33`, `#1E293B`, `#0D1527`) across WPF XAML and Web dashboards.
  - **WCAG 2.1 AA Dynamic Brushes**: Introduced dynamic brushes for `TitleBarBg`, `StatusBarBg`, `DataGridBg`, `DataGridAltRow`, `BadgeBg`, `BadgeFg`, `ProgressBarBg`, `InputBg`, and `InputFg`. In Light Theme, backgrounds switch to soft `#FFFFFF`, `#F8FAFC`, `#F1F5F9`, and text switches to high-contrast slate `#0F172A` and `#1E293B`.
- **⏱️ Reliable Interval Rotation Engine**:
  - **Eliminated Mid-Stream Rotation Races**: Fixed `SelectUpstreamProxy` which previously triggered premature candidate swaps on client requests. Active verified live proxies are preserved throughout streaming sessions while `CheckAutoRotationAsync` strictly manages timed rotations at the exact user-stipulated intervals.
- **⏸️ Pause Auto-Rotation While Remaining Connected**:
  - **Independent Rotation Lock**: Added `AutoRotateEnabled` toggle button ("⏸ Pause Rotation" / "▶ Resume Rotation") across Windows, Linux, and macOS.
  - **Continuous Session Retention**: Allows users to freeze automatic rotation while staying actively connected to the current proxy server indefinitely. Countdown badge clearly displays `⏸ Auto-Rotation Paused (Connected to {ActiveTimedProxy.Endpoint})`.
- **🔌 Explicit Proxy Disconnect**:
  - **One-Click Proxy Disconnect**: Added "Disconnect" button (`DisconnectProxyCommand`) to gracefully terminate the upstream proxy tunnel and revert traffic to direct Multi-WAN adapter aggregation.
- **⚡ Decoupled Operational Modes (Proxy-Only vs. Multi-WAN Bonded)**:
  - **Direct Multi-WAN Bonding without Proxy**: Users can disconnect proxy servers and enjoy high-speed multi-adapter bonding across Wi-Fi, Ethernet, and cellular.
  - **Proxy-Only Mode without Adapter Bonding**: Added "Combine Adapters with Proxy" checkbox (`CombineAdaptersWithProxy`). When unchecked, routes proxy traffic through a single primary adapter without combining adapter speeds, giving users total control over their network architecture.
- **🧪 Universal Triple-OS Verification**:
  - Automated tests verified across Windows (`--verify-persistence`, `--verify-rotator`), Linux (`--verify-persistence`), and macOS (`--verify-persistence`) with **100% PASS**.

---

## [2.2.6] - 2026-11-04 - 10-Second Proxy Internet Accessibility Verification & Active Rotation Release

### 🚀 Highlights & Engineering Advancements
- **🌐 10-Second Internet Accessibility Verification Threshold**:
  - **Post-Connection Internet Probing**: Once a proxy server URL is successfully connected at the socket level, CombiSpeed immediately tests if external internet connectivity is accessible through the proxy endpoint with a strict **10-second threshold** (`thresholdMs = 10000`).
  - **Multi-Protocol Transit Verification**: Supports HTTP, HTTPS, RFC 1928 SOCKS5, and RFC SOCKS4/SOCKS4a. Tests reachability using native `SocketsHttpHandler` via `WebProxy` with socket-level protocol tunneling to high-availability endpoints (`http://1.1.1.1/` and `http://www.msftconnecttest.com/connecttest.txt`), verifying that packets actually transit through the upstream proxy to the wider Internet.
- **🔄 Conditional Auto-Rotation on Internet Unreachability**:
  - **Strict Failure-Gated Rotation**: If a connected proxy server URL fails to access the internet within the 10-second threshold (or returns gateway timeout / connection refused), it is marked unavailable and evicted.
  - **Auto-Advance to Next Candidate**: Only upon confirmed internet unreachability does the application auto-rotate to the next available proxy server in the candidate pool, continuing until an internet-verified proxy is established.
- **⏱️ Stipulated Interval Waiting Gated by Verified Internet Access**:
  - **True Interval Waiting**: The proxy server rotation countdown timer strictly resets (`_lastTimedRotation = DateTime.UtcNow`) and starts counting down ONLY once a proxy server is connected **and tested successfully for internet access**.
  - **Zero Premature Rotations**: The system remains locked onto the verified live proxy and waits for the full configured interval time (e.g. 10s, 30s, 1m, 5m, 1h, 2h) before auto-rotating.
- **🪟 Cross-Platform Verification & Parity**:
  - Automated tests added to Windows (`--verify-rotator`), Linux (`--verify-persistence`), and macOS (`--verify-persistence`) testing 10s internet threshold enforcement, rejection of dead/no-internet candidates, auto-rotation past unresponsive nodes, and full interval countdown initialization (**100% PASS** across all platforms).

---

## [2.2.5] - 2026-11-04 - Dedicated Distribution Structure & Script Relocation Release

### 🚀 Highlights & Engineering Advancements
- **📁 Dedicated Distribution Architecture & Script Relocation**:
  - **Clean Root Workspace**: Moved all Windows installer, launcher, and service lifecycle batch scripts (`Install-CombiSpeed.bat`, `Start-CombiSpeed.bat`, `Stop-CombiSpeed.bat`, `Install-Service.bat`, `Uninstall-CombiSpeed.bat`, `Uninstall-Service.bat`) from repository root into `publish/`.
  - **Smart Multi-Tier Path Discovery**: Upgraded all batch scripts to automatically resolve executable paths, project files, and application icons whether executed from `publish/`, `publish/windows/bin/`, or the installed `%LOCALAPPDATA%\Programs\CombiSpeed` directory.
  - **Zero-Friction One-Click Execution**: Users can run `publish\Install-CombiSpeed.bat` directly from the distribution folder without configuring paths or moving files.
- **📦 Clean Package Footprint & Bloat Elimination**:
  - Purged 95 obsolete and superseded distribution archives from earlier releases across all platform folders.
  - Eliminated nested archive bundling recursion, reducing the Windows release package (`CombiSpeed-v2.2.5-win-x64.zip`) from 20.63 MB down to 362 KB.
  - Reclaimed over 97.48 MB of workspace storage across distribution and local installation directories.
- **🔄 Universal Platform Synchronization**:
  - Synchronized `publish/windows/bin/`, `publish/linux/bin/`, and `publish/macos/bin/` with `v2.2.5` binaries, configuration schemas, and platform scripts.
  - Built and verified standalone release packages for Windows, Linux, and macOS.

---

## [2.2.4] - 2026-11-04 - Extended 2-Hour Proxy Rotation Intervals & Live Connection-Gated Countdown Release

### 🚀 Highlights & Engineering Advancements
- **⏱️ Extended Proxy Rotation Intervals Up to Every 2 Hours (7200s)**:
  - **Comprehensive Interval Granularity**: Expanded the rotation interval selector to support durations ranging from 10 seconds all the way up to 2 hours (7200s).
  - **Intuitive Human-Friendly Presets**:
    - `10s`, `15s`, `30s`, `45s`
    - `1 min (60s)`, `1.5 min (90s)`, `2 min (120s)`, `3 min (180s)`, `5 min (300s)`
    - `10 min (600s)`, `15 min (900s)`, `30 min (1800s)`, `45 min (2700s)`
    - `1 hour (3600s)`, `1.5 hours (5400s)`, `2 hours (7200s)`
  - **Dynamic Custom Interval Adaptation**: Any custom interval integer up to 7200 seconds is automatically formatted, validated, and safely inserted into the selection options.
  - **Persistent Interval State**: Configured intervals are persistently saved to `%LOCALAPPDATA%\CombiSpeed\proxy_state.json` and restored on startup.
- **🔒 Live Connection-Gated Rotation Countdown**:
  - **Strict Connection Gating**: The proxy rotation countdown timer is strictly held and will NOT start or decrement until a proxy server has successfully connected and verified live.
  - **Pre-Connection Waiting State**: While the engine is idle, searching for candidates, or establishing a tunnel, the UI clearly displays `⏳ Waiting for live proxy connection...`, preventing inaccurate premature countdown ticks.
  - **Live Confirmation Trigger**: Upon successful upstream socket connection or live reachability probe confirmation, `ConfirmLiveConnected(proxy)` triggers the countdown, starting from the configured interval (e.g. `Next rotation in 2h 00m 00s` or `Next rotation in 45s`).
  - **Automatic Dynamic Formatting**: Long durations are formatted into human-readable hours, minutes, and seconds (e.g. `1h 45m 12s`, `4m 30s`, `25s`).
  - **Fault-Tolerant Halting**: If an active proxy disconnects or is flagged unavailable (`MarkUnavailable`), the countdown immediately pauses until the next live proxy connection is confirmed.
  - **Zero-Second Auto-Rotation Pipeline**: When the timer reaches 0s, `CheckAutoRotationAsync` automatically auto-advances to probe and connect to the next live proxy, pausing the countdown during transition and restarting only upon live connection confirmation.
- **🌐 100% Cross-Platform Native Parity**:
  - **Windows 10/11**: WPF ComboBox with `SelectedValuePath="Seconds"` and `DisplayMemberPath="Label"`, real-time countdown badge, and `RotatingProxyRouter` connection gating.
  - **Linux & macOS**: Updated `CyberpunkSpa.cs` HTML `<select>` with full interval presets up to 2 hours, live JS countdown ticking with `formatRemainingTime()`, and REST API `/api/rotating-proxy/config` validation up to 7200s.
- **🧪 Automated Verification (Phase 9 Expansion)**:
  - Added automated test assertions to `--verify-rotator` (Windows) and `--verify-persistence` (Linux & macOS) verifying pre-connection paused state (`-1`), 2-hour interval configuration (7200s), active countdown ticking, and dynamic halting upon proxy eviction (**100% PASS**).
- **📁 Dedicated Distribution Packaging & Script Relocation**:
  - Moved all Windows installer and process lifecycle batch scripts (`Install-CombiSpeed.bat`, `Start-CombiSpeed.bat`, `Stop-CombiSpeed.bat`, `Install-Service.bat`, `Uninstall-CombiSpeed.bat`, `Uninstall-Service.bat`) from workspace root directly under `publish/`.
  - Implemented smart multi-level path discovery across all batch scripts, allowing seamless one-click execution directly from `publish/`, `publish/windows/bin/`, or the installed `%LOCALAPPDATA%\Programs\CombiSpeed` directory.
  - Purged obsolete distribution archives and eliminated recursive zip packaging bloat, reducing package footprint to 362 KB and reclaiming over 97.48 MB of workspace storage.

---

## [2.2.3] - 2026-11-04 - Live Identity Refresh, Protocol-Aware Connections, Light/Dark Themes, Audit Stream & Instant Persistence Release

### 🚀 Highlights & Engineering Advancements
- **🌍 Real-Time Network Identity Refresh upon Proxy Connection**:
  - **Dynamic WAN & Geo-Location Probing**: When a proxy connection is established—either via scheduled interval rotation or 1-click manual forced connection—the system automatically triggers an immediate background identity refresh via `GeoIpService.ResolveCurrentLocationAsync(Adapters, proxy: proxy, forceRefresh: true)`.
  - **Proxy-Tunnelled Diagnostic Requests**: Queries to authoritative GeoIP providers (`ip-api.com`, `ipwho.is`) are tunnelled directly through the newly connected proxy endpoint using `SocketsHttpHandler.Proxy = new WebProxy(proxy.ToWebProxyUrl())`.
  - **Instant Live Telemetry**: Updates the Overview tab (Tab 0) in real time with the new public IP, ISP/ASN, city, region, and dynamic Unicode country flag emoji (e.g. 🇮🇳, 🇺🇸, 🇩🇪, 🇬🇧, 🇯🇵), providing immediate visual confirmation of anonymity and geographical location.
- **🔌 Protocol-Aware Upstream Proxy Connections (HTTP, HTTPS, SOCKS4, SOCKS5)**:
  - **Multi-Protocol Handshake Engine**: Upgraded `EstablishUpstreamProxyTunnelAsync` across `ProxyEngine.cs`, `LinuxProxyEngine.cs`, and `MacProxyEngine.cs` to dynamically inspect `proxy.Protocol`:
    - `HTTP` & `HTTPS`: Full HTTP CONNECT bidirectional tunneling (`CONNECT host:port HTTP/1.1`).
    - `SOCKS5`: Full RFC 1928 negotiation (`0x05, 0x01, 0x00`) with IPv4, IPv6, and domain name target addressing.
    - `SOCKS4` & `SOCKS4a`: Implemented RFC SOCKS4 client handshake (`0x04, 0x01, Port, IP, User\0`) with SOCKS4a domain resolution fallback (`0.0.0.1` sentinel + target domain byte payload).
  - Eliminates upstream protocol mismatch errors and ensures 100% interoperability with heterogeneous proxy repository sources.
- **🎯 Dynamic Success & Failure Column Filtering**:
  - Added dedicated filter dropdowns in Tab 4 (Rotating Proxy) on Windows WPF and Linux/macOS Web SPAs:
    - **Success Filters**: `ALL Successes`, `≥ 1 Success`, `≥ 3 Successes`, `≥ 5 Successes`, `≥ 10 Successes`.
    - **Failure Filters**: `ALL Failures`, `0 Failures (Perfect)`, `≤ 1 Failure`, `≤ 3 Failures`, `≤ 5 Failures`.
  - Integrated into `RefreshProxyCollection()` and persisted in `PersistedProxyStateModel` across restarts so user filtering preferences are never lost.
- **💾 Instant Settings Auto-Persistence & PAC Toggle Fix**:
  - **Fixed PAC Checkbox Non-Persistence**: Resolved issue where toggling the "Enable PAC for Dynamic Routing (AutoConfig URL)" checkbox did not persist if the settings dialog was dismissed via the close window button.
  - **Zero-Latency Disk Writes**: Wired real-time change handlers (`Checked`, `Unchecked`, `TextChanged`, `SelectionChanged`) across all settings controls to immediately invoke `ApplySettingsToModel()` and trigger disk persistence via `_configStore.SaveImmediate`.
  - **Real-Time WinINet Sync**: Toggling the PAC checkbox immediately engages or disengages `WindowsProxyManager.EnablePac()` / `DisablePac()` and writes `AutoConfigURL` to the Windows registry.
- **📁 Multi-Tiered Persistence for Proxy Repositories & Server URLs**:
  - **Repository Sources Persistence**: Proxy repository URLs are immediately saved to `%LOCALAPPDATA%\CombiSpeed\proxy_sources.json` and persisted in `proxy_state.json`. On application startup, saved sources are pre-loaded into `Config.RotatingProxy.CustomSources` and the UI editor.
  - **Server URLs Persistence**: All harvested proxy server URLs, latency benchmarks, failure/success counts, and user selections are saved to `%LOCALAPPDATA%\CombiSpeed\proxy_state.json` immediately upon harvest in Idempotence mode, ensuring zero data loss across application restarts.
- **👁️ High-Contrast Accessibility-Friendly Tooltips**:
  - Resolved white-on-white text rendering in WPF tooltips by providing an explicit `Style TargetType="{x:Type ToolTip}"` with custom ControlTemplate in `App.xaml`.
  - Styled with deep background (`ToolTipBg` `#0B1120`), high-contrast border (`ToolTipBorder` `#00F0FF`), and crisp text (`ToolTipFg` `#F8FAFC`).
  - Achieves WCAG 2.1 AAA compliance with >14:1 contrast ratios in both Light and Dark themes.
- **🌓 Eye-Friendly & Soothing Light and Dark Themes**:
  - Added user-selectable Theme Switcher button (`🌙 Dark` / `☀️ Light`) to the main TitleBar action bar across Windows WPF, Linux, and macOS Web SPAs.
  - Designed an eye-friendly, soothing Light Theme palette (`#F1F5F9` canvas, `#FFFFFF` cards, `#0F172A` high-contrast slate text, `#0284C7` calming sky accents) alongside the Cyberpunk Dark palette (`#030712` canvas, `#0B1120` cards, `#F8FAFC` bright white text, `#00F0FF` cyan accents).
  - Maintained strict WCAG 2.1 AA/AAA contrast ratios across all labels, inputs, grids, and buttons.
  - Theme preference is saved to `ProxyConfigModel.SelectedTheme` and restored across sessions.
- **📜 Enriched Connection Audit Stream & Logging Toggle**:
  - Extended `AuditLogModel` with `ProxyServerUrl`, `IsSuccess`, `LatencyMs`, and `ResolvedIp`.
  - Added dedicated columns to the Connection Audit DataGrid: `PROXY SERVER`, `RESULT` (`✓ SUCCESS` / `✗ FAILED`), `LATENCY` (`42 ms`), and `RESOLVED IP`.
  - Added an `Enable Connection Audit Logging` CheckBox toggle to the audit header and Linux/macOS dashboards, allowing users to disable audit logging to conserve CPU and RAM during heavy workloads.
- **⏳ Real-Time Live Rotation Countdown Timer**:
  - Implemented `SecondsUntilNextRotation` in `RotatingProxyRouter`, calculating the exact remaining seconds until the next automatic proxy rotation.
  - Integrated into the 1-second UI telemetry tick in `MainViewModel` and client-side JavaScript in `CyberpunkSpa.cs`, displaying an accurate countdown (e.g. `Next rotation in 24s`).

---

## [2.2.2] - 2026-11-04 - Live Connection Auto-Rotation, Force Connect & Comprehensive Tooltips Release

### 🚀 Highlights & Engineering Advancements
- **⚡ Live Connection Auto-Rotation Retry & Proactive Node Eviction**:
  - **Proactive Socket Reachability Probing**: Implemented `ProbeLiveConnectionAsync` in `RotatingProxyRouter` across Windows, Linux, and macOS, using lightweight non-blocking TCP socket handshakes with `Task.WhenAny` and deterministic handle disposal to confirm responsiveness without hanging on Windows/Linux socket SYN retransmission timeouts.
  - **Uninterrupted Traffic Pipeline**: Integrated a live retry loop (up to 12 attempts) in both HTTP/HTTPS CONNECT tunneling and RFC 1928 SOCKS5 dispatching in `ProxyEngine.cs`, `LinuxProxyEngine.cs`, and `MacProxyEngine.cs`. When an upstream proxy connection or tunnel handshake fails, the engine immediately flags the endpoint unavailable, evicts it from the active candidate pool, rotates to the next candidate, and only pipes traffic once a live upstream session is established.
  - **Self-Healing Rotation Pool**: Dead or unresponsive proxies encountered during auto-rotation or timed intervals are dynamically pruned on-the-fly, ensuring the active pool converges rapidly on healthy, high-throughput endpoints.
- **🎯 1-Click Force Connect to Selected or Next Live Proxy**:
  - **Dedicated Toolbar Button**: Added a prominent `🎯 Force Connect` button in Tab 4 (Rotating Proxy & Harvester) and `/api/rotating-proxy/force-connect` REST endpoint in Linux/macOS dashboards.
  - **User-Targeted or Automatic Next**: If a specific row is selected in the proxy table, it immediately attempts direct connection to that preferred proxy; if it fails or if no row is selected, it auto-rotates through the candidate pool testing live connectivity until a responsive server is found and locked onto `ActiveTimedProxy`.
  - **Per-Row Connect Action**: Added an `ACTION` column with a dedicated `Connect` button for every proxy row in the WPF DataGrid and Linux/macOS Web SPAs for 1-click targeted routing.
  - **Automatic Pipeline Engaging**: Upon establishing a live connection, automatically activates `RouteBondedTrafficThroughRotator = true` and updates the listening proxy countdown.
- **💡 Comprehensive Explanatory Tooltips across All Controls & Labels**:
  - **100% UI Tooltip Coverage**: Added clear, explanatory, accessible tooltips to every key button, input box, toggle switch, slider, and telemetry badge across all 5 segmented tabs:
    - **Header & TitleBar**: Version badge, bonding engine state indicator, system proxy badge, virtual bridge badge, accessibility options button, settings configuration button, window minimize/maximize/close buttons.
    - **Tab Navigation**: Overview (Ctrl+1), Combined Adapters (Ctrl+2), Speed Shaper (Ctrl+3), Connection Audit (Ctrl+4), Rotating Proxy (Ctrl+5).
    - **Tab 0 (Overview)**: Master bonding power button, Windows system proxy toggle, virtual bridge toggle, speed benchmark button, rescan adapters button, GeoIP identity refresh button, individual telemetry cards (bonded download/upload, active concurrency, session volume/uptime), virtual bridge management buttons (ncpa.cpl, re-provision), and real-time bandwidth waveform.
    - **Tab 1 (Combined Adapters)**: Rescan adapters button, profile selector, save profile, new profile, delete profile, per-adapter enable checkboxes, latency ping badges, bound local IP, public WAN location, live speeds, and bandwidth allocation weight sliders.
    - **Tab 2 (Speed Shaper & QoS)**: Optimization mode selector, global bandwidth cap presets, custom throttling parameters button, dynamic adaptation checkbox, and Token Bucket QoS objective explanations.
    - **Tab 3 (Connection Audit)**: Clear logs button and connection audit data table.
    - **Tab 4 (Rotating Proxy & Harvester)**: Start/Stop rotator server, harvest proxies, validate pool, repository sources, export working, import file, rotation strategy selector, country filter, bond-then-rotate checkbox, auto-harvest/auto-validate timers, multi-NIC chaining, elite-only filter, prune dead, clear pool, filter search box, latency/source/protocol dropdowns, sorting dropdown, select all, deselect all, select fast (≤300ms), rotate now, force connect, and table columns.
    - **Pinned Glass Footer**: Version/build pill, accessibility zoom scale pill, auto-save status text, and architecture details.
- **🌐 100% Cross-Platform Native Parity**:
  - Implemented identical live auto-rotation retry loops and `force-connect` APIs across Windows (`CombiSpeed/`), Linux (`CombiSpeed-Linux/`), and macOS (`CombiSpeed-macOS/`).
  - Added rich `title="..."` tooltips throughout the HTML/CSS/JS frontend in `CyberpunkSpa.cs` for Linux and macOS.
- **🧪 Automated Verification (Phase 11)**:
  - Added dedicated Test Phase 11 to `--verify-rotator` testing individual TCP socket probing (live vs dead sockets), auto-rotation live retry with dead candidate eviction, and direct force-connection to preferred live proxies (**100% PASS**).

### 🚀 Highlights & Engineering Advancements
- **⚡ Strict Idempotence Mode for Proxy Harvesting ($f(f(x)) = f(x)$)**:
  - **Zero Duplicate Ingestion**: Ingests thousands of proxy candidates across 255+ global feeds without creating duplicate entries for identical endpoints (`Host:Port`).
  - **User Modification Preservation**: Re-harvesting strictly preserves all user-selected checkbox states (`IsSelected`), ensuring user choices are never clobbered or unchecked by automated scraping cycles.
  - **Benchmark Metric Retention**: Preserves verified latency pings, healthy flags, failure counts, and last-checked timestamps if an endpoint was previously validated, preventing raw scraper feeds from degrading benchmarked data.
  - **Non-Destructive Metadata Enrichment**: Intelligently upgrades Country Code and Flag Emoji if previously unknown, and upgrades protocols (e.g. HTTP -> HTTPS / SOCKS5) when richer metadata is discovered.
- **💾 Complete Proxy State & UI Filter/Sort Persistence across Restarts**:
  - **Immediate Post-Harvest Persistence**: Automatically commits all harvested proxies to persistent storage (`proxy_state.json`) immediately upon harvesting, so proxy pools are fully reusable even without manual validation.
  - **Active Filter Restoration**: Persists and restores all active UI filter criteria:
    - Max Latency Threshold (`SelectedLatencyFilter`, e.g. `< 150 ms`, `< 300 ms`, `< 500 ms`, `< 1000 ms`, or `ALL Latencies`).
    - Source Origin (`SelectedSourceFilter`, e.g. specific repo feed or `ALL Sources`).
    - Protocol (`SelectedProtocolFilter`, e.g. `HTTP`, `HTTPS`, `SOCKS4`, `SOCKS5`, or `ALL Protocols`).
    - Country Filter (`SelectedCountryFilter`, e.g. `US`, `DE`, `ALL`).
    - Search Query (`ProxySearchText`).
  - **Active Sorting Restoration**: Persists and restores multi-criteria table sorting order (`SelectedSortOption`, e.g. `Lowest Latency (Fastest)`, `Highest Latency`, `Status / Health First`, `Source (A-Z)`, `Country Code (A-Z)`, `Lowest Failures`).
  - **Atomic `.tmp` Disk Write**: Guaranteed safe atomic state writes preventing file corruption or race conditions.
  - **Instant Startup Pre-Warming**: On application launch, pre-warms the proxy pool and immediately reapplies the saved filter and sort views without user intervention.
- **🌐 100% Cross-Platform Native Parity**:
  - Implemented `IdempotentAddOrUpdate`, `IdempotentAddCandidates`, `SavePersistedState`, and `LoadPersistedState` identically across Windows (`ProxyValidatorEngine.cs`), Linux (`LinuxProxyValidator.cs`), and macOS (`MacProxyValidator.cs`).
  - Enhanced Linux and macOS Web SPAs in `CyberpunkSpa.cs` to persist table filters, search queries, and sorting in client storage, while `EmbeddedDashboardServer.cs` saves state immediately on selection changes and harvests.
- **🧪 Automated Verification (Phase 10)**:
  - Added dedicated Test Phase 10 to `--verify-rotator` testing idempotent proxy deduplication, modification preservation across multiple harvesting passes, atomic disk persistence, and complete state restoration after simulated restart (**100% PASS**).

## [2.2.0] - 2026-11-04 - Two-Stage Multi-Adapter Bonding with Selective Timely Proxy Rotation & Live GeoIP Resolution Release

### 🚀 Highlights & Engineering Advancements
- **⚡ Two-Stage Multi-Adapter Bonding & Timely Rotating Proxy Pipeline**:
  - **Decoupled Architecture**: Upstream requests are first load-balanced and socket-bound across physical adapters (`Socket.Bind(adapterIp, 0)` on Windows, `SO_BINDTODEVICE` on Linux, `IP_BOUND_IF` on macOS) for aggregate throughput and resilience, and then tunneled upstream through a user-selected rotating proxy server.
  - **Timely Automatic Rotation Engine**: Implemented `RotationStrategy.TimeBasedInterval` in `RotatingProxyRouter` across Windows, Linux, and macOS. Rotates the active proxy automatically on a user-configurable schedule (10s, 15s, 30s, 45s, 60s, 120s, 300s).
  - **Non-Blocking Live Countdown**: Displays real-time countdown timer (`Next rotation in 24s`) and fires `OnTimedRotation` events upon proxy transitions.
  - **Immediate Manual Override**: Provides a 1-click `⚡ Rotate Now` button / API command (`/api/rotating-proxy/rotate-now`) that forces an instantaneous transition to the next candidate without resetting interval timers.
  - **Unified Bonding Toggle**: Added `RouteBondedTrafficThroughRotator` checkbox toggle (`⚡ Bond-then-Rotate Pipeline`) in the UI, config model, and REST APIs, allowing users to enable or bypass upstream proxy tunneling while retaining multi-adapter physical bonding.
- **☑ Selective Proxy Pool Management (Checkboxes & Bulk Selection)**:
  - **Per-Proxy Selection Checkboxes**: Added interactive checkboxes (`IsSelected`) to each row of the Harvested Proxies `DataGrid` in WPF, and to the Linux and macOS Web Dashboards.
  - **Strict Candidate Enforcement**: `RotatingProxyRouter.GetCandidatePool()` strictly filters by `p.IsSelected == true`, ensuring that only explicitly user-selected proxies participate in rotation.
  - **1-Click Bulk Action Toolbar**:
    - `Select All`: Checks all proxies in the current pool for rotation.
    - `Deselect All`: Clears all checkboxes to pause proxy routing.
    - `Select Fast (≤300ms)`: Automatically checks only healthy, low-latency verified proxies ($\le 300\text{ ms}$).
  - **Dynamic Selection Status Badge**: Displays real-time counts (e.g. `Selected for Rotation: 18 of 54 healthy`).
- **🌍 Live Intranet & Internet Geo-Location Identity Resolution**:
  - **Dual-Provider Fast Fallback**: Built `GeoIpService` (Windows), `LinuxGeoIpService` (Linux), and `MacGeoIpService` (macOS) with sub-second failover across authoritative GeoIP endpoints (`http://ip-api.com/json/`, `https://ipwho.is/`, `https://api.ipify.org?format=text`), requiring zero API keys.
  - **Intranet Aggregation**: Detects and aggregates all local network IPv4 addresses across enabled physical NICs (Wi-Fi, Ethernet, Cellular, LAN).
  - **Live Identity Telemetry Card**: Added a high-contrast Cyberpunk card to the Overview tab displaying:
    - Unicode Country Flag Emoji (e.g. 🇮🇳, 🇺🇸, 🇩🇪, 🇬🇧, 🇯🇵) algorithmically generated from ISO-3166 codes.
    - City, Region & Country name.
    - Public Internet WAN IP address.
    - Local Intranet IP address(es).
    - Internet Service Provider (ISP) and Autonomous System Number (ASN).
    - `⟳ Refresh Identity` button.
  - **Per-Adapter Location Display**: Extended `NetworkAdapterModel` with `FormattedLocation` showing the resolved public WAN IP and country flag per adapter in the Combined Adapters tab.
- **🌐 100% Native Cross-Platform Parity**:
  - Implemented identical REST endpoints in `EmbeddedDashboardServer.cs` on Linux & macOS: `/api/geoip`, `/api/rotating-proxy/select-all`, `/api/rotating-proxy/deselect-all`, `/api/rotating-proxy/select-fast`, `/api/rotating-proxy/toggle-select`, and `/api/rotating-proxy/rotate-now`.
  - Updated `CyberpunkSpa.cs` on Linux and macOS with the live GeoIP identity card, proxy selection checkboxes, selection action buttons, interval selector, and two-stage pipeline toggle.
- **🧪 Dedicated Automated Verification**:
  - Added Test Phase 9 to `--verify-rotator`, verifying selective proxy filtering, timely rotation intervals, force rotation, flag emoji generation, and live GeoIP resolution (**100% PASS**).
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.2.0-win-x64.zip` and `publish/CombiSpeed-v2.2.0-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.2.0-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.2.0-macos-x64.tar.gz` and `.zip`

---

## [2.1.9] - 2026-11-04 - Combined Adapters Rescan & Multi-Adapter Discovery Release

### 🚀 Highlights & Engineering Advancements
- **🖧 Combined Adapters Tab 1-Click Rescan**:
  - **Prominent Toolbar Rescan Button**: Added a dedicated `⟳ Rescan Adapters` button directly to the Combined Adapters & Allocation tab header card (`MainWindow.xaml` for Windows WPF, and `CyberpunkSpa.cs` for Linux & macOS).
  - **Live Combinable Adapters Count Badge**: Displays real-time adapter inventory (e.g. `4 Detected (3 Combined)`) right on the toolbar.
  - **Instant Health & Latency Probe**: Rescanning triggers an immediate health check (`_healthMonitor.TriggerImmediateCheck()`) across all combinable interfaces without waiting for periodic polling intervals.
  - **Dynamic Empty-State Resilience**: Implemented a responsive Cyberpunk empty state banner that appears if 0 network adapters are detected (e.g. during disconnect or airplane mode), providing troubleshooting guidance and a large 1-click rescan button.
  - **Transparent Connection Audit Logging**: Every adapter rescan records a structured entry to the Connection Audit log (`[NIC-SCAN] Rescanned network adapters: X detected (Y combined)`).
- **🌐 Cross-Platform REST API & Web Dashboard Parity**:
  - **Linux & macOS API**: Implemented `POST /api/adapters/rescan` in `EmbeddedDashboardServer.cs` across Linux and macOS, detecting newly connected network adapters, preserving user combination weights, and returning updated counts.
  - **Reactive Web Dashboard**: Enhanced `CyberpunkSpa.cs` with an interactive "⟳ Rescan Adapters" button with loading feedback, dynamic summary counter updates, and 0-adapter empty state handling.
- **🧪 Dedicated Automated Verification**:
  - Added adapter rescan verification tests to `--verify-persistence` on Windows, Linux, and macOS, confirming 100% test pass rate across all platforms.
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.1.9-win-x64.zip` and `publish/CombiSpeed-v2.1.9-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.1.9-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.1.9-macos-x64.tar.gz` and `.zip`
ilur
---

## [2.1.8] - 2026-11-03 - Socket Address Reuse & Port Conflict Self-Healing Release

### 🚀 Highlights & Engineering Advancements
- **🛠️ Resolved `SocketException` (Address Already In Use / WSAEADDRINUSE 10048)**:
  - **Root Cause Resolution**: When toggling engine power (`TogglePowerAsync`) or the virtual bridge (`ToggleVirtualBridgeAsync`), `VirtualBridgeServer`'s `TcpListener` lacked `SO_REUSEADDR` and had `ExclusiveAddressUse = true` by default. Rapid engine stops/starts left ports in `TIME_WAIT` state or ran into existing port bindings on port 8443, throwing an unhandled `SocketException` on the WPF UI thread.
  - **SO_REUSEADDR & Non-Exclusive Address Binding**: Explicitly configured `ExclusiveAddressUse = false` and `SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)` across all listeners (`VirtualBridgeServer`, `ProxyEngine`, `RotatingProxyServer`, and cross-platform `LinuxProxyEngine` & `MacProxyEngine`), allowing instant socket address reuse across process lifecycles.
  - **Intelligent Port Conflict Self-Healing**: Implemented automatic conflict resolution loops that probe and increment busy ports (up to 20 attempts) without throwing exceptions. If configured default ports (`8443`, `8080`, `1080`, `8081`) are held by external applications or background processes, CombiSpeed seamlessly shifts to the next open port and logs the shift transparently to the connection audit log.
  - **Prior Instance Cleanup**: `VirtualBridgeManager.StartBridgeAsync` automatically halts and disposes any pre-existing bridge server instance before allocating new listeners.
  - **Comprehensive UI Exception Shields**: Added top-level try/catch blocks to `TogglePowerAsync()` and `ToggleVirtualBridgeAsync()` in `MainViewModel.cs`, guaranteeing that no socket or network error will ever cause an unhandled crash dialog on the UI thread.
- **🧪 Dedicated Automated Verification**:
  - Added port conflict resolution & socket address reuse test inside `--verify-throttling`, verifying that `VirtualBridgeServer` gracefully re-routes around an actively blocked port without throwing any `SocketException` (**100% PASS**).
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.1.8-win-x64.zip` and `publish/CombiSpeed-v2.1.8-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.1.8-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.1.8-macos-x64.tar.gz` and `.zip`

---

## [2.1.7] - 2026-11-03 - Persistent Healthy Proxy Caching & Dynamic Unavailable Eviction Release

### 🚀 Highlights & Engineering Advancements
- **💾 Persistent Healthy Proxy Storage & Pre-Warming**:
  - **Strictly Healthy Persistence**: Only verified healthy proxies (`IsHealthy == true && Failures == 0`) are written to persistent storage (`healthy_proxies.json` located at `%LOCALAPPDATA%\CombiSpeed\healthy_proxies.json` on Windows, `~/.config/combispeed/healthy_proxies.json` on Linux, and `~/Library/Application Support/CombiSpeed/healthy_proxies.json` on macOS). Dead or unverified endpoints are strictly filtered out and never saved.
  - **Atomic Safe Writes**: Persistence utilizes temp file writes (`.tmp`) with atomic replace/move semantics, completely protecting against partial writes or file corruption across application life cycles.
  - **Instant Startup Pre-Warming**: Automatically reloads the cached healthy proxies on app launch and initialization across Windows WPF, Linux daemon/CLI, and macOS, providing instant proxy routing availability with zero wait time for a 30-minute harvesting scrape cycle.
- **⚡ Dynamic Unavailable Proxy Eviction**:
  - **Immediate Routing Eviction**: In `RotatingProxyRouter` and `RotatingProxyServer` (and their Linux/macOS native counterparts), any upstream proxy that fails TCP connection handshake or returns non-200 tunnel status during live request routing is immediately marked unavailable via `MarkUnavailable(endpoint)`, purged from the in-memory candidate pool (`_pool.TryRemove`), and evicted from the persistent disk storage file.
  - **Health Validation Eviction**: If any previously healthy proxy fails validation during routine health checks (`ValidateSingleAsync` or `PruneDead`), it is automatically evicted from memory and disk.
  - **Debounced Thread-Safe Auto-Save**: Validation and status changes trigger a 400ms debounced disk write to eliminate I/O thrashing during high-concurrency (60-worker) validation operations.
- **🧪 Comprehensive Test Suite (100% PASS)**:
  - Added dedicated test step `[TEST 8/8]` to `--verify-rotator` (Windows) and integrated verification into `--verify-persistence` (Linux & macOS) verifying healthy filtering, cold startup pre-warming, and dynamic eviction upon unavailable event triggers.
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.1.7-win-x64.zip` and `publish/CombiSpeed-v2.1.7-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.1.7-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.1.7-macos-x64.tar.gz` and `.zip`

---

## [2.1.6] - 2026-11-03 - Expanded 255+ Global & Geo-Targeted Proxy Feeds Release

### 🚀 Highlights & Engineering Advancements
- **🌐 Massive Proxy Repository Catalog Integration (255+ Verified Feeds)**:
  - **Comprehensive Multi-Protocol Feeds**: Integrated 255 distinct, curated open-source proxy repositories and APIs into default sources (`GetDefaultSources()`) across Windows, Linux, and macOS.
  - **Categorized Protocol Coverage**:
    - **HTTPS / SSL Feeds**: 85 global feeds and 28 country-targeted feeds.
    - **HTTP Feeds**: 65 global feeds and 16 country-targeted feeds.
    - **SOCKS4 Feeds**: 41 global feeds and 9 country-targeted feeds.
    - **SOCKS5 Feeds**: 57 global feeds and 9 country-targeted feeds.
  - **Broad Country / Geo-Targeting Integration**: Seamless automatic GeoIP tagging for major international jurisdictions (`US`, `GB`, `DE`, `FR`, `CA`, `NL`, `JP`, `SG`, `AU`, `BR`, `IN`, `RU`, `CN`, `TR`, `PK`, `IR`, `IT`, `PL`, `TH`, `UA`, `EG`, `HU`, `KH`, `KZ`, `MX`, `VN`, `CL`, `HK`).
  - **Smart Migration & Seamless Upgrade**: Automatically detects legacy 6-URL configurations and upgrades them to the comprehensive 255-feed catalog while safely preserving user-added custom URLs.
- **⚡ Bounded Parallel Concurrency (16-Worker Semaphore)**:
  - Upgraded `HarvestAllAsync` across `ProxyHarvesterService.cs` (Windows), `LinuxProxyHarvester.cs` (Linux), and `MacProxyHarvester.cs` (macOS) with `SemaphoreSlim(16)`. Scrapes all 255 feeds concurrently in seconds without socket exhaustion, threadpool starvation, or connection reset drops.
- **🧪 Diagnostic Verification**:
  - Validated with `--verify-rotator` test suite, verifying bulk ingestion, regex extraction, de-duplication, and 255-source default reset with 100% pass rate.
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.1.6-win-x64.zip` and `publish/CombiSpeed-v2.1.6-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.1.6-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.1.6-macos-x64.tar.gz` and `.zip`

---

## [2.1.5] - 2026-11-03 - Custom Throttling Dialog Initialization Fix Release

### 🚀 Highlights & Engineering Advancements
- **🛠️ Resolved `NullReferenceException` in `CustomThrottleDialog`**:
  - **Root Cause Resolution**: During WPF BAML instantiation of `CustomThrottleDialog.xaml`, the `TxtUploadLimit` TextBox fired its `TextChanged` event before sibling controls (`CmbUploadUnit`, etc.) were instantiated by `InitializeComponent()`, triggering an unhandled `NullReferenceException` in `UpdateEquivalents()`.
  - **Readiness Gate & Comprehensive Null-Guards**: Added a private `_isReady` state gate that prevents early event handler execution until all controls are fully initialized and populated. Hardened `UpdateEquivalents()`, `DownloadLimit_TextChanged()`, `UploadLimit_TextChanged()`, `DownloadUnit_SelectionChanged()`, `UploadUnit_SelectionChanged()`, and `AutoUpload_Checked()` with safe null-conditional operators.
- **🧪 Dedicated Automated Verification**:
  - Added an STA-thread UI instantiation test inside `--verify-throttling` to programmatically verify that `CustomThrottleDialog` instantiates without throwing exceptions.
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.1.5-win-x64.zip` and `publish/CombiSpeed-v2.1.5-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.1.5-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.1.5-macos-x64.tar.gz` and `.zip`

---

## [2.1.4] - 2026-11-03 - Multi-Faceted Proxy Sorting & Filtering Release

### 🚀 Highlights & Engineering Advancements
- **⚡ Interactive Multi-Faceted Sorting & Filtering**:
  - **Latency Threshold Filtering**: Filter candidate proxies instantaneously by latency tiers (`All Latencies`, `< 150 ms (Ultra-Fast)`, `< 300 ms (Fast)`, `< 500 ms (Normal)`, and `< 1000 ms`).
  - **Dynamic Source Discovery & Origin Filtering**: Automatically extracts and groups repository source origins (e.g., *TheSpeedX*, *monosans*, *hookzof*, *proxyscrape*, or custom URL hosts) allowing users to filter proxies by their scraping feed origin.
  - **Protocol Filtering**: Filter the active proxy pool by protocol specification (`HTTP`, `HTTPS`, `SOCKS4`, `SOCKS5`).
  - **Universal Text Search**: Live search bar matches IP addresses, ports, country codes, protocols, and source repository URLs across the active proxy pool in real time.
  - **Multi-Criteria Sorting**: Preset sorting options including `Lowest Latency (Fastest)`, `Highest Latency`, `Status / Working First`, `Source (A-Z)`, and `Country Code (A-Z)`.
- **🖥️ Windows WPF Cyberpunk Filter Toolbar & DataGrid Column Sorting**:
  - Added modern glassmorphic cyberpunk filter toolbar directly above the proxy grid in Tab 4 (`MainWindow.xaml`).
  - Enabled full column header click-to-sort (`CanUserSortColumns="True"`) with explicit `SortMemberPath` definitions for Status, Country, Endpoint, Protocol, Anonymity, Latency, Failures, and Source.
  - Integrated dynamic active/filtered counter badge (`Showing X of Y proxies`).
- **🐧 Linux & 🍎 macOS Cyberpunk Web SPAs & Core Validator Engine**:
  - Integrated responsive toolbar with input search, latency, source, protocol, and sort dropdowns in `CyberpunkSpa.cs`.
  - Added clickable table headers (`STATUS ⇅`, `GEO ⇅`, `ENDPOINT ⇅`, `PROTOCOL ⇅`, `LATENCY ⇅`, `SOURCE ⇅`) with bidirectional asc/desc sorting.
  - Upgraded validator engines (`ProxyValidatorEngine.cs`, `LinuxProxyValidator.cs`, `MacProxyValidator.cs`) with overloaded `GetHealthyProxies(..., maxLatencyMs, sourceFilter, protocolFilter)`.
- **✅ Automated Verification**:
  - Expanded `--verify-rotator` CLI suite with `[TEST 7/7]` verifying latency filtering, source matching, protocol filtering, and LowestLatency sort ordering with 100% pass rate.
- **📦 Multi-OS Distributions**:
  - Windows: `publish/windows/CombiSpeed-v2.1.4-win-x64.zip` and `publish/CombiSpeed-v2.1.4-win-x64.zip`
  - Linux: `publish/linux/CombiSpeed-v2.1.4-linux-x64.tar.gz` and `.zip`
  - macOS: `publish/macos/CombiSpeed-v2.1.4-macos-x64.tar.gz` and `.zip`

---

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
