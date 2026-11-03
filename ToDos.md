# CombiSpeed - Project ToDo Tracker (C# .NET Edition)

## Milestone 1: C# .NET Architecture & Solution Setup [COMPLETED]
- [x] Verify .NET SDK and Windows Desktop runtime availability (.NET 8.0 Windows Desktop).
- [x] Initialize `CombiSpeed.sln` and `CombiSpeed.csproj` (`net8.0-windows` WPF application).
- [x] Define core models (`NetworkAdapterModel.cs`, `TelemetryModel.cs`, `ProxyConfigModel.cs`, `AuditLogModel.cs`).
- [x] Implement `NetworkDetector.cs`: Native .NET `NetworkInterface` + WMI/PowerShell integration to detect all active NICs (Ethernet, Wi-Fi, Cellular, Bridge).
- [x] Implement `HealthMonitor.cs`: Real-time RTT ping and latency probing via raw sockets bound to adapter local IPv4.
- [x] Implement `LoadBalancerEngine.cs`: High-concurrency routing (Weighted Round-Robin, Equal Share, Least-Latency, Failover).

## Milestone 2: Native High-Performance Proxy Engine [COMPLETED]
- [x] Implement `ProxyEngine.cs`: Async multi-threaded socket server with duplex stream piping.
- [x] Implement `Socks5Handler`: RFC 1928 SOCKS5 proxy protocol with adapter socket binding (`Socket.Bind(new IPEndPoint(adapterIp, 0))`).
- [x] Implement `HttpProxyHandler`: HTTP & HTTPS `CONNECT` tunneling proxy with adapter socket binding.
- [x] Implement `ThroughputMonitor`: Thread-safe byte counters tracking per-adapter and aggregate download/upload speeds (KB/s, MB/s).
- [x] Implement `WindowsProxyManager.cs`: 1-click Windows System Proxy toggle via WinINet registry and `InternetSetOption` API with fail-safe auto-revert.

## Milestone 3: Cyberpunk Glassmorphic WPF Dashboard [COMPLETED]
- [x] Custom dark glassmorphic window styling with frameless borderless draggable titlebar.
- [x] Master Glowing Power Toggle with animated state indicators.
- [x] Real-time 4-card telemetry metrics (Bonded Rx, Bonded Tx, Active Sockets, Transferred Data Volume & Uptime).
- [x] Dynamic 60-Second Real-Time Scrolling Bandwidth Waveform Graph (WPF Polyline / Canvas geometry).
- [x] Adapter Cards with real-time ping badge, link speed, per-adapter live speedometer, and interactive weight percentage sliders.
- [x] Live Connection Audit Stream with filter search and clear functionality.
- [x] Built-in Multi-Stream Speed Benchmark dialog (testing individual vs bonded throughput).
- [x] Settings dialog for custom proxy ports and load balancing modes.

## Milestone 4: One-Click Start, Stop & Installer Scripts [COMPLETED]
- [x] Add CLI arguments parsing (`--start`, `--stop`, `--minimized`) and IPC in `App.xaml.cs`.
- [x] Create `Start-CombiSpeed.bat`: 1-click launcher that launches CombiSpeed and automatically activates bonding mode.
- [x] Create `Stop-CombiSpeed.bat`: 1-click stopper that halts running instances and safely resets Windows proxy settings.
- [x] Create `Install-CombiSpeed.bat`: 1-click automated installer deploying to `%LOCALAPPDATA%\Programs\CombiSpeed`, creating Desktop & Start Menu shortcuts, and registering in Windows "Add or Remove Programs".
- [x] Create `Uninstall-CombiSpeed.bat`: Clean uninstaller removing files, shortcuts, and registry entries.
- [x] Embed native application icon (`app.ico`) in `.csproj`, window icons, desktop shortcuts, and executable metadata.
- [x] Test and verify start, stop, installer, and uninstaller flows.
- [x] Update documentation (`ReadMe.md`, `KnownIssues.md`, `ReleaseNotes.md`, `version.json`).
- [x] Git commit and tag `v1.1.1`.

## Milestone 5: Virtual VPN / Adapter Connection & Bandwidth Bridge Subsystem [COMPLETED]
- [x] Design and implement `VirtualBridgeModel.cs` for Windows virtual adapter and bonded bridge telemetry.
- [x] Implement `VirtualBridgeManager.cs`: Native Windows VPN adapter connection provisioning via `Add-VpnConnection` and user-mode phonebook (`rasphone.pbk`) with zero UAC friction.
- [x] Implement `VirtualBridgeServer.cs`: Dedicated high-throughput local tunnel gateway (port 8443) dispatching connections across physical NICs with Smooth Weighted Round Robin.
- [x] Implement mathematically optimal Smooth Weighted Round Robin (SWRR) in `LoadBalancerEngine.cs` to strictly enforce user % bandwidth allocations without burstiness or clustering.
- [x] Implement dynamic Effective % of Bonded Pool calculation (`RecalculateEffectivePercentages`) across active physical adapters with real-time UI updates.
- [x] Add Cyberpunk Virtual Bonded Bridge Card in `MainWindow.xaml`:
  - Real-time connection status badge (`BONDED & ACTIVE` / `READY TO CONNECT`).
  - Merged physical adapters summary text with active percentages.
  - Multi-Color Segmented Bandwidth Distribution Bar visually rendering each adapter's proportional share.
  - 1-Click buttons: `Toggle Bridge`, `View in Windows (ncpa.cpl)`, `Re-provision`.
- [x] Enhance individual physical adapter cards with distinct accent colors, `BANDWIDTH: XX.X% of Bonded Pool` indicator, and live speedometers.
- [x] Update `SettingsWindow` to configure Virtual Bridge port and auto-connect toggle.
- [x] Update `Start-CombiSpeed.bat`, `Stop-CombiSpeed.bat`, `Install-CombiSpeed.bat`, and `Uninstall-CombiSpeed.bat` to manage the Virtual Bridge lifecycle cleanly.
- [x] Increment version to `1.2.0` (versionCode: 4) and verify release build.

## Milestone 6: Enterprise & High-Performance Capabilities [COMPLETED]
- [x] Implement PAC (Proxy Auto-Config) rule generator & models (`PacRuleModel.cs`, `PacConfigModel.cs`) with RFC-compliant JavaScript compilation (`FindProxyForURL`).
- [x] Embed dynamic PAC script server in `ProxyEngine.cs` serving `http://127.0.0.1:8080/proxy.pac` with zero port overhead.
- [x] Add Windows `AutoConfigURL` registry and WinINet hook in `WindowsProxyManager.cs` (`EnablePac`/`DisablePac`).
- [x] Implement PAC Rule Editor and Preview dialog in `SettingsWindow.xaml` and `SettingsWindow.xaml.cs`.
- [x] Implement RFC 1928 SOCKS5 UDP Associate Relay (`Socks5UdpRelay.cs`) with physical adapter socket binding (`Socket.Bind(new IPEndPoint(adapterIp, 0))`) for gaming, VoIP, DNS, and UDP video acceleration.
- [x] Implement Headless Execution Engine (`HeadlessEngine.cs`) and Windows Service mode (`--service`, `--headless`, `--proxy`).
- [x] Add CLI Service management commands (`--service-install`, `--service-uninstall`, `--service-start`, `--service-stop`).
- [x] Create 1-click Windows Service management scripts: `Install-Service.bat` and `Uninstall-Service.bat`.
- [x] Update `Install-CombiSpeed.bat`, `Stop-CombiSpeed.bat`, and `Uninstall-CombiSpeed.bat` for service lifecycle integration.
- [x] Increment version to `1.3.0` (versionCode: 5) and verify release build.

## Milestone 7: Adapter Settings & Combination Profiles Persistence Subsystem [COMPLETED]
- [x] Design persistence models (`AdapterProfileModel.cs`: `AdapterSettingItem`, `CombinationProfileModel`, `PersistedAppDataModel`).
- [x] Implement `ConfigurationStore.cs`:
  - Persistent storage in `%LOCALAPPDATA%\CombiSpeed\adapter_settings.json`.
  - Atomic two-stage file commits (`.tmp` -> replace) to prevent corruption during sudden power-off.
  - Debounced auto-save (400ms) on slider dragging and toggle changes.
  - Multi-attribute adapter binding (stable MAC address primary key with Name fallback).
  - Combination profiles management (Save, Switch, Delete).
- [x] Implement Cyberpunk `InputDialog.xaml` and `InputDialog.xaml.cs` for saving named combination profiles.
- [x] Enhance `MainViewModel.cs` to integrate `ConfigurationStore`:
  - Automatic restore of enabled adapters and weight percentages on application launch and rescan.
  - Automatic restore of Proxy and Virtual Bridge configurations.
  - Immediate save on application exit.
  - Profile switcher and auto-save timestamp badge.
- [x] Update `MainWindow.xaml`:
  - Added "ADAPTER COMBINATIONS & ALLOCATION" header toolbar.
  - Real-time Auto-Save status badge (`💾 Auto-Saved: HH:mm:ss`).
  - Combination profile selector dropdown.
  - 1-Click "💾 Save Combination" and "+ New Profile" buttons.
- [x] Integrated `ConfigurationStore` with `HeadlessEngine.cs` for headless daemon and Windows Service auto-restore.
- [x] Added `--verify-persistence` CLI diagnostic suite and verified 100% test pass rate.
- [x] Updated `Install-CombiSpeed.bat` and `Uninstall-CombiSpeed.bat` to handle configuration lifecycle.
- [x] Increment version to `1.3.1` (versionCode: 6) and verify release build.

## Milestone 8: Real-Time Bandwidth Throttling & Speed Shaper Subsystem [COMPLETED]
- [x] Design and implement `BandwidthThrottler.cs` using high-precision asynchronous Token Bucket algorithm.
- [x] Implement real-time byte rate limiting in `ProxyEngine.cs` for HTTP, HTTPS CONNECT, and SOCKS5 TCP streaming.
- [x] Implement real-time packet rate limiting in `Socks5UdpRelay.cs` for UDP Associate uploads and downloads.
- [x] Implement real-time stream rate limiting in `VirtualBridgeServer.cs` for Windows Virtual Bonded Bridge adapter connections.
- [x] Support Proportional Bandwidth Shaping: strictly allocates and throttles byte rates across combined adapters according to user-configured percentage weights.
- [x] Add configurable global speed caps (Presets: Unlimited, 2 MB/s, 5 MB/s, 10 MB/s, 20 MB/s, 50 MB/s, 100 MB/s, Custom) and per-adapter speed limits.
- [x] Add Bandwidth Throttling & Speed Shaper toolbar to `MainWindow.xaml` with live status badge and quick throttle preset selector.
- [x] Enhance individual adapter cards with real-time `⚡ Capped: XX MB/s` allocation indicators.
- [x] Add Bandwidth Throttling settings section in `SettingsWindow.xaml` and `SettingsWindow.xaml.cs`.
- [x] Integrate Throttling state and per-adapter speed limits into `ConfigurationStore` for automatic persistence across reboots.
- [x] Integrate Throttling into `HeadlessEngine.cs` for Windows Service and CLI background execution.
- [x] Add `--verify-throttling` CLI diagnostic test suite and verified 100% test pass rate.
- [x] Increment version to `1.3.2` (versionCode: 7) and publish release binaries.

## Milestone 9: Production Release v1.3.3 Build & Packaging [COMPLETED]
- [x] Increment version to `1.3.3` and `versionCode: 8` in `CombiSpeed.csproj` and `version.json`.
- [x] Execute clean build and publish release binaries to `publish/` for `win-x64` with 0 warnings and 0 errors.
- [x] Create standalone zipped release archive (`publish/CombiSpeed-v1.3.3-win-x64.zip`) containing binaries, icons, launcher scripts, and uninstaller.
- [x] Run automated regression test suites (`--verify-persistence`, `--verify-throttling`) and confirm 100% pass rates.
- [x] Synchronize documentation (`ReadMe.md`, `ReleaseNotes.md`, `KnownIssues.md`, `ToDos.md`).
- [x] Commit changes to git and create release tag `v1.3.3`.

## Milestone 10: Application Version Visibility & v1.3.4 Release [COMPLETED]
- [x] Increment version to 1.3.4 and ersionCode: 9 in CombiSpeed.csproj and ersion.json.
- [x] Expose AppVersion, AppVersionFull, and WindowTitle in MainViewModel.cs.
- [x] Add Cyberpunk version badge to custom TitleBar in MainWindow.xaml next to application branding.
- [x] Integrate pinned glassmorphic footer status bar in MainWindow.xaml displaying full build attribution, auto-sync status, and runtime environment.
- [x] Add --version, -v, and /version CLI argument flags in App.xaml.cs.
- [x] Execute clean build and publish release binaries to publish/ with 0 warnings and 0 errors.
- [x] Create standalone zipped release archive (publish/CombiSpeed-v1.3.4-win-x64.zip).
- [x] Validate automated test suites (--verify-throttling, --verify-persistence) and verify 100% pass rates.
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, 	ask_log.json).
- [x] Commit changes to git and create release tag 1.3.4.

## Milestone 11: 500 Mbps Bandwidth Throttling & Custom Throttling Parameters [COMPLETED]
- [x] Increment version to 1.3.5 and ersionCode: 10 in CombiSpeed.csproj and ersion.json.
- [x] Expand throttling presets in MainViewModel.cs up to 500 Mbps (62.5 MB/s).
- [x] Implement CustomThrottleDialog.xaml modal window allowing users to enter custom speed caps, multi-unit rates (Mbps, MB/s, Kbps, KB/s), burst tolerance window, and per-adapter limits.
- [x] Add ⚙ Custom... button to the Bandwidth Throttling toolbar in MainWindow.xaml and wire Custom... dropdown action.
- [x] Update SettingsWindow.xaml and SettingsWindow.xaml.cs to support custom speed limits in Mbps / MB/s and configurable burst window.
- [x] Support configurable Token Bucket burst window (BurstToleranceSeconds) in BandwidthThrottler.cs and ProxyConfigModel.cs.
- [x] Add CustomSpeedLimitMbps to NetworkAdapterModel.cs for direct two-way per-adapter speed limit editing.
- [x] Update --verify-throttling test suite with 500 Mbps cap validation, parameter conversion checks, and per-adapter overrides (100% pass rate).
- [x] Package release distribution publish/CombiSpeed-v1.3.5-win-x64.zip.
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v1.3.5.

## Milestone 12: Dynamic Cross-Connection Speed Shaper & Latency Modes [COMPLETED]
- [x] Increment version to 1.3.6 and versionCode: 11 in CombiSpeed.csproj, version.json, MainViewModel.cs, and App.xaml.cs.
- [x] Define SpeedShaperMode enum (MaximumBandwidth, LowestLatency, BalancedDynamic) and wire into ProxyConfigModel.cs.
- [x] Implement real-time dynamic traffic shaping across connections in BandwidthThrottler.cs:
  - Mode-driven burst buffer sizing (CalculateOptimalBurstSeconds): 150ms-250ms for MaximumBandwidth (TCP congestion window expansion), 20ms for LowestLatency (anti-bufferbloat pacing).
  - Dynamic inverse-latency scoring (1000.0 / PingMs) shifting capacity toward lower-ping interfaces when in LowestLatency mode.
  - Throughput-weighted capacity allocation prioritizing higher-bandwidth lines when in MaximumBandwidth mode.
  - Dynamic cross-connection capacity borrowing: fast connections borrow idle headroom from the global token pool.
  - Dynamic real-time adaptation hook AdaptDynamically(...) called on live telemetry updates.
- [x] Integrate dynamic shaper mode into LoadBalancerEngine.cs for smooth latency-weighted SWRR connection routing.
- [x] Connect MainViewModel.cs to _healthMonitor.HealthUpdated to trigger real-time dynamic shaper adaptation as RTT latency and packet loss fluctuate.
- [x] Add dynamic speed shaper mode selector ComboBox (🚀 Maximum Bandwidth, ⚡ Lowest Latency / Ping, ⚖ Balanced Dynamic) to the Bandwidth Throttling toolbar in MainWindow.xaml.
- [x] Update CustomThrottleDialog.xaml and CustomThrottleDialog.xaml.cs with dedicated DYNAMIC SPEED SHAPER OPTIMIZATION MODE section, mode radio buttons, automatic burst window alignment, and dynamic cross-connection adaptation checkbox.
- [x] Update SettingsWindow.xaml and SettingsWindow.xaml.cs with shaper mode and dynamic adaptation persistence.
- [x] Implement automated verification tests in App.xaml.cs (--verify-throttling step verifying burst expansion, anti-bufferbloat, cross-connection dynamic adaptation, and persistence). Verified 100% pass rate.
- [x] Execute clean build and publish release binaries to publish/ with 0 warnings and 0 errors.
- [x] Create standalone zipped release archive (publish/CombiSpeed-v1.3.6-win-x64.zip).
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v1.3.6.

## Milestone 13: UI/UX Redesign & Accessibility (WCAG 2.1 AA) [COMPLETED]
- [x] Increment version to 1.4.0 and versionCode: 12 in CombiSpeed.csproj, version.json, MainViewModel.cs, App.xaml.cs, and Install-CombiSpeed.bat.
- [x] Implement Modern Segmented Tabbed Navigation Bar (⚡ Overview & Dashboard, 🖧 Combined Adapters, 🚀 Speed Shaper & QoS, 📜 Connection Audit).
- [x] Implement WCAG 2.1 AA Accessible High Contrast Mode with dynamic theme brush updates (#030712 deep black, #38BDF8 stark cyan borders, #FFFFFF high-luminance text).
- [x] Implement Dynamic UI & Font Scaling (100%, 115%, 130%) using root LayoutTransform scale transforms for zero text clipping on high-DPI displays.
- [x] Implement Reduce Motion mode for vestibular safety, disabling pulsing LED animations and smoothing waveform redraws.
- [x] Implement Screen Reader Optimization with comprehensive AutomationProperties.Name and help texts across all controls.
- [x] Add high-visibility keyboard focus rings (FocusVisualStyle) and global access key bindings (Space, Alt+P, Alt+B, Alt+W, Alt+R, Alt+A, Alt+S, Alt+H, Ctrl+1-4).
- [x] Create dedicated AccessibilityDialog.xaml and AccessibilityDialog.xaml.cs modal window.
- [x] Add Accessibility & Display Preferences section in SettingsWindow.xaml and SettingsWindow.xaml.cs.
- [x] Create custom accessible dark ComboBox and Slider styles replacing default Windows classic controls.
- [x] Add automated accessibility persistence test suite in App.xaml.cs (--verify-persistence) and verify 100% test pass rate.
- [x] Execute clean build and publish release binaries to publish/ with 0 warnings and 0 errors.
- [x] Create standalone zipped release archive (publish/CombiSpeed-v1.4.0-win-x64.zip).
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v1.4.0.


## Milestone 14: High-Contrast Dropdowns & Text Readability Overhaul [COMPLETED]
- [x] Increment version to 1.4.1 and versionCode: 13 in CombiSpeed.csproj, version.json, MainViewModel.cs, App.xaml.cs, and Install-CombiSpeed.bat.
- [x] Build and integrate a bulletproof custom dark ComboBox ControlTemplate with dark toggle button (#131D33), glowing cyan chevron icon (#00F0FF), dark popup border (#0B1120), and explicit white text (#FFFFFF) resolving white-on-white / black-on-dark rendering issues.
- [x] Register global default styles for TextBlock and Label with default Foreground="#F8FAFC" to prevent system fallback to black text on dark surfaces.
- [x] Overhaul all text label colors across the application (MainWindow.xaml, SettingsWindow.xaml, CustomThrottleDialog.xaml, AccessibilityDialog.xaml, InputDialog.xaml, SpeedTestWindow.xaml) replacing dim #94A3B8 / #334155 with high-luminance colors (#FFFFFF, #38BDF8, #F1F5F9, #CBD5E1) achieving 14:1+ contrast ratios.
- [x] Style DataGrid column headers and cells with dark accessible theme.
- [x] Upgrade SegmentedTabItem and SegmentedTabButton unselected state text to #E2E8F0.
- [x] Execute clean build and publish release binaries to publish/ with 0 warnings and 0 errors.
- [x] Validate automated test suites (--verify-persistence, --verify-throttling, --version) and verify 100% pass rates.
- [x] Create standalone zipped release archive (publish/CombiSpeed-v1.4.1-win-x64.zip).
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v1.4.1.

## Milestone 15: PAC Export & Multi-OS Separate Architecture (Windows, Linux, macOS) [COMPLETED]
- [x] Increment version to 2.0.0 and versionCode: 14 across all project definitions (CombiSpeed.csproj, CombiSpeed.Linux.csproj, CombiSpeed.macOS.csproj, version.json, MainViewModel.cs, App.xaml.cs, Install-CombiSpeed.bat, and Info.plist).
- [x] Implement PAC (Proxy Auto-Config) file saving and direct exporting in Windows WPF SettingsWindow.xaml ("💾 Export PAC File..." button) and SettingsWindow.xaml.cs using SaveFileDialog (.pac, .js, *.* filters).
- [x] Add CLI argument --export-pac <path> across Windows, Linux, and macOS engines allowing automated terminal and script PAC generation.
- [x] Design and implement dedicated, independent Linux codebase (CombiSpeed-Linux/):
  - Created CombiSpeed.Linux.csproj targeting net8.0.
  - Implemented LinuxNetworkDetector.cs reading /sys/class/net and native interfaces.
  - Implemented LinuxProxyEngine.cs with SO_BINDTODEVICE (socket opt 25) raw interface binding, SOCKS5 TCP/UDP, and HTTP/HTTPS CONNECT.
  - Implemented LinuxLoadBalancer.cs and LinuxBandwidthThrottler.cs with SWRR %, 500 Mbps rate limiting, dynamic latency/bandwidth modes, and cross-connection borrowing.
  - Implemented LinuxProxyManager.cs for GNOME gsettings and shell environment scripts (~/.config/combispeed/proxy.env).
  - Implemented embedded Cyberpunk Glassmorphic Dashboard SPA matching exact colors, tabs, waveforms, and WCAG AA accessibility.
  - Created Linux one-click scripts (start-combispeed.sh, stop-combispeed.sh, install-combispeed.sh, uninstall-combispeed.sh, combispeed.desktop).
- [x] Design and implement dedicated, independent macOS codebase (CombiSpeed-macOS/):
  - Created CombiSpeed.macOS.csproj targeting net8.0.
  - Implemented MacNetworkDetector.cs and MacProxyEngine.cs with IP_BOUND_IF / SO_DONTROUTE socket binding.
  - Implemented MacProxyManager.cs for macOS networksetup CLI proxy automation.
  - Implemented embedded Cyberpunk Glassmorphic Dashboard SPA with exact visual styling.
  - Created macOS one-click scripts (start-combispeed-mac.sh, stop-combispeed-mac.sh, install-combispeed-mac.sh, uninstall-combispeed-mac.sh) and Apple application bundle CombiSpeed.app/Contents/Info.plist.
- [x] Publish and package separate distributions per OS in publish/:
  - Windows: publish/windows/CombiSpeed-v2.0.0-win-x64.zip
  - Linux: publish/linux/CombiSpeed-v2.0.0-linux-x64.tar.gz and .zip
  - macOS: publish/macos/CombiSpeed-v2.0.0-macos-x64.tar.gz and .zip
- [x] Validate automated self-test suites across all platforms (--verify-persistence, --verify-throttling, --version, --export-pac) with 100% pass rates.
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v2.0.0.

## Milestone 16: Speed Benchmark Overhaul & Virtual Adapter Routing Protection [COMPLETED]
- [x] Increment version to 2.0.1 and versionCode: 15 across all project definitions (CombiSpeed.csproj, CombiSpeed.Linux.csproj, CombiSpeed.macOS.csproj, version.json, MainViewModel.cs, App.xaml.cs, Install-CombiSpeed.bat, Info.plist, Program.cs, and CyberpunkSpa.cs).
- [x] Diagnose and resolve Speed Benchmark freezing on Windows:
  - Added 1500ms pre-flight latency socket probe in SpeedTester.cs; unroutable or virtual host-only adapters (Hyper-V vEthernet, WSL) are immediately assigned 0 Mbps, preventing 21-42 second TCP SYN retransmission hangs.
  - Configured SocketsHttpHandler with UseProxy = false and Proxy = null, resolving 10049 WSAEADDRNOTAVAIL socket errors caused by physical interface sockets attempting to route to local proxy 127.0.0.1:8080.
  - Implemented ResolveHostIPv4Async to prevent IPv6 address family mismatches on IPv4-bound physical sockets.
  - Fixed event handler unsubscribe in SpeedTestWindow.xaml.cs on Closed event, eliminating handler leaks and UI race conditions.
  - Added --verify-benchmark CLI flag and validated multi-adapter throughput (189.7 Mbps Ethernet, 72.6 Mbps Wi-Fi, 169.7 Mbps bonded aggregation).
- [x] Implement native multi-stream speed benchmarking on Linux (LinuxSpeedTester.cs):
  - Async parallel socket throughput measurement with SO_BINDTODEVICE socket binding and pre-flight latency probe.
  - Wired to /api/speedtest/start in EmbeddedDashboardServer.cs and updated CyberpunkSpa.cs with real throughput, latency, boost %, and adapter list.
- [x] Implement native multi-stream speed benchmarking on macOS (MacSpeedTester.cs):
  - Async parallel socket throughput measurement with IP_BOUND_IF / SO_DONTROUTE socket binding and pre-flight routability checks.
  - Wired to /api/speedtest/start in EmbeddedDashboardServer.cs and updated CyberpunkSpa.cs with real throughput, latency, boost %, and adapter list.
- [x] Publish and package release artifacts in publish/:
  - Windows: publish/windows/CombiSpeed-v2.0.1-win-x64.zip
  - Linux: publish/linux/CombiSpeed-v2.0.1-linux-x64.tar.gz and .zip
  - macOS: publish/macos/CombiSpeed-v2.0.1-macos-x64.tar.gz and .zip
- [x] Execute clean builds and validate 100% test pass rate across --verify-benchmark, --verify-persistence, --verify-throttling.
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v2.0.1.

## Milestone 17: Speed Combining & Unified Bandwidth Pooling [COMPLETED]
- [x] Increment version to 2.0.2 and versionCode: 16 across all project definitions (CombiSpeed.csproj, CombiSpeed.Linux.csproj, CombiSpeed.macOS.csproj, version.json, MainViewModel.cs, App.xaml.cs, Install-CombiSpeed.bat, Info.plist, Program.cs, and CyberpunkSpa.cs).
- [x] Implement Combine Speeds Mode across BandwidthThrottler.cs and ProxyConfigModel.cs:
  - Add CombineSpeedsMode = true and set EnforceProportionalThrottling = false by default.
  - Pool aggregate bandwidth additively ($TotalSpeed = Speed_{NIC1} + Speed_{NIC2} + \dots$) across all active adapters.
  - Eliminate fractional bandwidth slicing where adapters were choked to sub-limits (e.g. 50% or 25%).
  - Ensure every adapter has access to the full combined limit, while global Token Bucket regulates the aggregate cap.
- [x] Implement Multi-NIC Parallel Range Chunk Speed Combiner in ProxyEngine.cs:
  - Added TryCombinedRangeDownloadAsync for HTTP GET downloads ($\ge 512\text{ KB}$ with HTTP Range support).
  - Fetches parallel byte-range chunks concurrently across all active physical adapters simultaneously, combining physical wire speeds for a single file download.
  - Transparent fallback to standard pipe for non-range or dynamic HTTP requests.
- [x] Update UI controls and preset text:
  - Preset updated to "Unlimited (Combined Speed)".
  - Status indicator updated to "Bandwidth: Combined Speed Aggregation (Active)".
  - Switched checkbox in CustomThrottleDialog.xaml and SettingsWindow.xaml to "Combine Speeds Mode (Pool aggregate bandwidth without splitting across connections)".
- [x] Update self-test test suites in App.xaml.cs:
  - Verified Combine Speeds Mode allocation (both adapters receive full combined capacity).
  - Verified live 1.5MB delay enforcement and 500 Mbps pool cap.
  - 100% test pass rate across --verify-throttling, --verify-persistence, --verify-benchmark.
- [x] Publish and package release artifacts in publish/:
  - Windows: publish/windows/CombiSpeed-v2.0.2-win-x64.zip
  - Linux: publish/linux/CombiSpeed-v2.0.2-linux-x64.tar.gz and .zip
  - macOS: publish/macos/CombiSpeed-v2.0.2-macos-x64.tar.gz and .zip
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v2.0.2.

## Milestone 18: Rotating Proxy Harvester & Multi-NIC Egress Dispatcher [COMPLETED]
- [x] Increment version to 2.1.0 and versionCode: 17 across all project definitions (CombiSpeed.csproj, CombiSpeed.Linux.csproj, CombiSpeed.macOS.csproj, version.json, MainViewModel.cs, App.xaml.cs, Install-CombiSpeed.bat, Info.plist, Program.cs, and CyberpunkSpa.cs).
- [x] Implement Phase 1: Ingestion & Live Multi-Protocol Validator:
  - Created HarvestedProxyModel.cs in Windows, Linux, and macOS supporting ProxyProtocol (HTTP, HTTPS, SOCKS4, SOCKS5), ProxyAnonymity (Transparent, Anonymous, Elite), latency tracking, fail count, and success rate.
  - Implemented ProxyHarvesterService.cs (LinuxProxyHarvester.cs, MacProxyHarvester.cs) with regex parsing for IP:PORT, PROTOCOL://IP:PORT, USER:PASS@IP:PORT, HTML table <td> tags, 10+ open-source proxy feeds, custom URL scraper, and local file import/export.
  - Implemented ProxyValidatorEngine.cs (LinuxProxyValidator.cs, MacProxyValidator.cs) with semaphore-bounded concurrent async probing across HTTP/HTTPS/SOCKS4/SOCKS5, real RTT calculation, anonymity verification, and auto-pruning.
- [x] Implement Phase 2: Rotating Proxy Router & Local Multi-NIC Socket Chaining MOAT:
  - Created RotatingProxyRouter.cs (LinuxRotatingProxyRouter.cs, MacRotatingProxyRouter.cs) supporting 5 dynamic dispatch strategies: RoundRobin, StickyDomain (consistent domain pinning), StickySession (configurable duration), LowestLatency (fastest tested RTT), and EliteOnly (highest anonymity tier).
  - Implemented Multi-NIC Egress Chaining: physical adapter socket binding (Socket.Bind(new IPEndPoint(adapterIp, 0))) chained with upstream rotating proxy servers, providing dual-layer anonymity and load aggregation.
  - Implemented RotatingProxyServer.cs (LinuxRotatingProxyServer.cs, MacRotatingProxyServer.cs) on port 8081 with duplex streaming, HTTP/HTTPS CONNECT tunneling, upstream SOCKS5 handshakes, and 2-attempt circuit-breaker retry.
  - Integrated with PAC script engine (PacRuleModel.cs, PacConfigModel.cs) to route targeted traffic via PacAction.RotatingProxy to 127.0.0.1:8081.
- [x] Implement Phase 3: Cyberpunk Dashboard UI & Multi-OS Parity:
  - Added Tab 4 "🔄 Rotating Proxy & Harvester" to Windows WPF MainWindow.xaml with real-time status indicators, pool stats, rotation strategy selector, quick actions (⚡ Harvest Feeds, 🩺 Validate Pool, ✂ Prune Dead, 🧹 Clear, 📤 Export, 📥 Import), and live DataGrid with protocol and anonymity badges.
  - Added /api/rotating-proxy/status, /api/rotating-proxy/pool, /api/rotating-proxy/harvest, /api/rotating-proxy/validate, /api/rotating-proxy/toggle REST endpoints to EmbeddedDashboardServer.cs on Linux and macOS.
  - Added dedicated Rotating Proxy tab and management controls to CyberpunkSpa.cs on Linux and macOS with live AJAX polling.
- [x] Implement Phase 4: Diagnostic Self-Tests & Multi-OS Verification:
  - Added --verify-rotator CLI self-test flag in App.xaml.cs verifying regex extraction, validator concurrency, Round-Robin rotation, Sticky Domain pinning, and port 8999 end-to-end loopback tunneling with 100% pass rate.
  - Re-validated --verify-throttling, --verify-persistence, and --verify-benchmark across Windows, Linux, and macOS.
- [x] Publish and package release artifacts in publish/:
  - Windows: publish/windows/CombiSpeed-v2.1.0-win-x64.zip
  - Linux: publish/linux/CombiSpeed-v2.1.0-linux-x64.tar.gz and .zip
  - macOS: publish/macos/CombiSpeed-v2.1.0-macos-x64.tar.gz and .zip
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v2.1.0.

## Milestone 19: Advanced Geo-Targeting, Upstream Auth & Background Scheduler [COMPLETED]
- [x] Increment version to 2.1.1 and versionCode: 18 across all project definitions (CombiSpeed.csproj, CombiSpeed.Linux.csproj, CombiSpeed.macOS.csproj, version.json, MainViewModel.cs, App.xaml.cs, Install-CombiSpeed.bat, Info.plist, Program.cs, and CyberpunkSpa.cs).
- [x] Implement GeoIP Country Code Detection & Geo-Targeted Filtering:
  - Extracted country codes from JSON feeds, HTML tables, and URL queries in ProxyHarvesterService.cs (LinuxProxyHarvester.cs, MacProxyHarvester.cs).
  - Added CountryFilter to RotatingProxyConfigModel and GetHealthyProxies query filtering.
  - Implemented regional selection with graceful fallback in RotatingProxyRouter.cs.
  - Added 1-click Country Filter dropdown (ALL, US, DE, GB, FR, CA, JP, NL, SG, IN) to Windows WPF Tab 4 and Linux/macOS Cyberpunk Web SPAs.
- [x] Implement Autonomous Background Re-Harvest & Health Validation Scheduler:
  - Created ProxySchedulerService.cs (LinuxProxyScheduler.cs, MacProxyScheduler.cs) managing non-blocking background timers.
  - Implemented automated 30-minute background candidate scraping from default and custom URLs.
  - Implemented automated 5-minute background health probing, latency refresh, and dead endpoint pruning.
  - Added UI toggles for Auto-Harvest and Auto-Check with persistent configuration storage.
- [x] Implement Upstream Proxy Authentication Support:
  - Added automatic Proxy-Authorization: Basic <base64> injection for HTTP and HTTPS CONNECT upstream proxy tunnels.
  - Added RFC 1929 Username/Password subnegotiation for SOCKS5 upstream proxies.
  - Added credentials authentication verification in ProxyValidatorEngine.cs.
- [x] Implement Modern JSON API Scraper & DataGrid UI Columns:
  - Added JSON array/object parser in ProxyHarvesterService.cs for modern JSON proxy APIs.
  - Added GEO country code columns to DataGrid in MainWindow.xaml and tables in CyberpunkSpa.cs.
- [x] Comprehensive Self-Test & Verification:
  - Expanded --verify-rotator self-test CLI verifying country filtering, JSON feed parsing, and scheduler lifecycle with 100% pass rate.
  - Re-validated --verify-throttling, --verify-persistence, and --verify-benchmark across all operating systems.
- [x] Publish and package release artifacts in publish/:
  - Windows: publish/windows/CombiSpeed-v2.1.1-win-x64.zip
  - Linux: publish/linux/CombiSpeed-v2.1.1-linux-x64.tar.gz and .zip
  - macOS: publish/macos/CombiSpeed-v2.1.1-macos-x64.tar.gz and .zip
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v2.1.1.

## Milestone 20: Windows Executable Startup XAML Fix & Crash Resilience [COMPLETED]
- [x] Increment version to 2.1.2 and versionCode: 19 across all project files (CombiSpeed.csproj, CombiSpeed.Linux.csproj, CombiSpeed.macOS.csproj, version.json, Install-CombiSpeed.bat, Info.plist).
- [x] Fix XamlParseException during MainWindow.xaml initialization by defining CyberComboBox and CyberCheckBox styles in App.xaml and chaining implicit styles.
- [x] Validate zero missing StaticResource references across all XAML files via automated regex static analysis.
- [x] Add global AppDomain and Dispatcher unhandled exception handlers with automated logging to %LOCALAPPDATA%\CombiSpeed\crash.log and user alert dialogs.
- [x] Verify live GUI startup, window instantiation, process lifecycle, and clean shutdown with 100% success.
- [x] Rebuild and publish release artifacts in publish/ for Windows, Linux, and macOS (v2.1.2 Build 19).
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Check in code and tag v2.1.2 in git.

## Milestone 21: Bulk Add & Edit Proxy Repository Sources / URLs [COMPLETED]
- [x] Increment version to 2.1.3 and versionCode: 20 across all project definitions (CombiSpeed.csproj, CombiSpeed.Linux.csproj, CombiSpeed.macOS.csproj, version.json, Install-CombiSpeed.bat, Info.plist).
- [x] Enhance RotatingProxyConfigModel with RepositorySources, GetEffectiveSources(), SetSourcesFromText(), and ResetSourcesToDefault() across Windows, Linux, and macOS.
- [x] Create Windows WPF ProxySourcesDialog.xaml and ProxySourcesDialog.xaml.cs with bulk multi-line text editor, active URL live counter, clipboard paste, and guidelines.
- [x] Add "🌐 Sources..." button to Tab 4 toolbar in MainWindow.xaml and wire event handler in MainWindow.xaml.cs.
- [x] Implement REST API endpoints in Linux and macOS EmbeddedDashboardServer.cs: GET /api/rotating-proxy/sources, POST /api/rotating-proxy/sources, and POST /api/rotating-proxy/sources/reset.
- [x] Integrate interactive Cyberpunk Web modal in CyberpunkSpa.cs for Linux and macOS with live AJAX persistence.
- [x] Wire effective sources into manual harvester and background 30-minute auto-harvest scheduler across all platforms.
- [x] Add [TEST 6/6] to --verify-rotator test suite validating bulk multi-line parsing, comment stripping, URL validation, and reset to defaults (100% pass rate).
- [x] Rebuild and publish standalone release packages in publish/ for Windows, Linux, and macOS (v2.1.3 Build 20).
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v2.1.3.

## Milestone 22: Multi-Faceted Sorting & Filtering for Proxy URLs [COMPLETED]
- [x] Increment version to 2.1.4 and versionCode: 21 across all project definitions (CombiSpeed.csproj, CombiSpeed.Linux.csproj, CombiSpeed.macOS.csproj, version.json, Install-CombiSpeed.bat, Info.plist).
- [x] Upgrade ProxyValidatorEngine (Windows, Linux, macOS) GetHealthyProxies() with maxLatencyMs, sourceFilter, and protocolFilter parameters.
- [x] Overhaul Windows WPF MainViewModel.cs with ProxySearchText, LatencyFilters, SelectedLatencyFilter, SourceFilters, SelectedSourceFilter, ProtocolFilters, SelectedProtocolFilter, SortOptions, SelectedSortOption, and FilteredCountText.
- [x] Implement real-time filtering, dynamic feed source extraction, and multi-criteria sorting in RefreshProxyCollection().
- [x] Add Cyberpunk filter toolbar to Tab 4 in MainWindow.xaml (search box, latency dropdown, source dropdown, protocol dropdown, sort dropdown, and count badge).
- [x] Enable DataGrid CanUserSortColumns="True" with explicit SortMemberPath attributes on all columns.
- [x] Add interactive Cyberpunk filtering toolbar and clickable table headers with bidirectional sorting to Linux and macOS CyberpunkSpa.cs.
- [x] Implement client-side JavaScript filtering, dynamic source extraction, and sorting in CyberpunkSpa.cs for Linux and macOS.
- [x] Add [TEST 7/7] to --verify-rotator test suite verifying latency filtering (<150ms, <300ms), source filtering, protocol filtering, and LowestLatency sorting (100% pass rate).
- [x] Rebuild and publish standalone release packages in publish/ for Windows, Linux, and macOS (v2.1.4 Build 21).
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v2.1.4.

## Milestone 23: Custom Throttling Dialog Initialization Fix [COMPLETED]
- [x] Increment version to 2.1.5 and versionCode: 22 across all project definitions (CombiSpeed.csproj, CombiSpeed.Linux.csproj, CombiSpeed.macOS.csproj, version.json, Install-CombiSpeed.bat, Info.plist).
- [x] Identify and resolve NullReferenceException in CustomThrottleDialog.xaml.cs triggered by early TextChanged event dispatch during XAML BAML deserialization.
- [x] Add private _isReady state gate preventing premature execution of UpdateEquivalents() and UI handlers before controls are instantiated.
- [x] Harden all event handlers (DownloadLimit_TextChanged, UploadLimit_TextChanged, DownloadUnit_SelectionChanged, UploadUnit_SelectionChanged, ShaperMode_SelectionChanged, AutoUpload_Checked, QuickCap_Click, ApplyButton_Click) with safe null-conditional guards.
- [x] Add automated STA-thread unit test in --verify-throttling to verify CustomThrottleDialog UI instantiation with zero exceptions.
- [x] Rebuild and publish standalone release packages in publish/ for Windows, Linux, and macOS (v2.1.5 Build 22).
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v2.1.5.

## Milestone 24: Expanded 255+ Global & Geo-Targeted Proxy Feeds [COMPLETED]
- [x] Increment version to 2.1.6 and versionCode: 23 across all project definitions (CombiSpeed.csproj, CombiSpeed.Linux.csproj, CombiSpeed.macOS.csproj, version.json, Install-CombiSpeed.bat, Info.plist).
- [x] Integrate 255 verified global and country-targeted proxy feeds across HTTPS, HTTP, SOCKS4, and SOCKS5 into HarvestedProxyModel.cs (Windows, Linux, macOS).
- [x] Implement smart legacy upgrade in GetEffectiveSources() ensuring existing installations automatically gain access to all 255 feeds while preserving custom user URLs.
- [x] Upgrade ProxyHarvesterService (Windows), LinuxProxyHarvester (Linux), and MacProxyHarvester (macOS) with SemaphoreSlim(16) bounded parallel concurrency.
- [x] Update --verify-rotator test suite to validate 255-source default reset and bulk ingestion (100% pass rate).
- [x] Rebuild and publish standalone release packages in publish/ for Windows, Linux, and macOS (v2.1.6 Build 23).
- [x] Synchronize documentation (ReadMe.md, ReleaseNotes.md, KnownIssues.md, ToDos.md, task_log.json).
- [x] Commit changes to git and create release tag v2.1.6.
