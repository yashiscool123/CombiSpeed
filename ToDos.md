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
