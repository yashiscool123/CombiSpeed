# Known Issues & Feature Backlog

## Known Issues (Reference Speedy vs CombiSpeed)

| ID | Issue Description | Reference (Speedy) Status | CombiSpeed Resolution Status |
|---|---|---|---|
| ISSUE-001 | Speedy SOCKS proxy failed to bind outgoing socket to adapter IP (`net.connect` omitted `localAddress`), resulting in default gateway routing rather than adapter bonding. | Bug in Speedy `socks.ts:63` | **Resolved**: CombiSpeed explicitly binds `{ localAddress: adapter.address }` on every outgoing TCP socket connection. |
| ISSUE-002 | Speedy spawned a new server listener on port 8080 for each incoming client connection inside `socks.ts:78`, resulting in `EADDRINUSE` port conflict and crash under concurrent load. | Bug in Speedy `socks.ts:78` | **Resolved**: CombiSpeed uses a unified direct socket duplex pipe without re-listening on local ports for every request. |
| ISSUE-003 | Speedy only supported SOCKS5 IPv4 and lacked HTTP/HTTPS proxy support, breaking standard Windows system proxy and browser out-of-the-box usage. | Missing in Speedy | **Resolved**: CombiSpeed implements both RFC 1928 SOCKS5 and HTTP/HTTPS CONNECT forward proxy, supporting 100% of Windows applications. |
| ISSUE-004 | Lack of Windows System Proxy automation required cumbersome manual browser configuration. | Missing in Speedy | **Resolved**: CombiSpeed introduces a 1-click Windows System Proxy toggle that configures `Internet Settings` in the Windows registry with automatic revert on exit. |
| ISSUE-005 | No real-time bandwidth speedometer, per-adapter latency pings, or adapter health checks. If an adapter disconnected, requests would hang or fail. | Missing in Speedy | **Resolved**: CombiSpeed integrates continuous per-adapter ICMP/TCP latency pings, health checks with auto-failover, and real-time live throughput telemetry. |
| ISSUE-006 | Single-connection downloads cannot span multiple adapters over standard single-stream TCP unless parallel streams or segmented downloading are utilized. | Inherent to TCP | **Mitigated**: CombiSpeed provides multi-stream parallel socket bonding and a built-in multi-connection speed benchmark tool that exercises all bonded adapters simultaneously. |
| ISSUE-007 | Sudden application crash or termination leaving Windows System Proxy enabled, blocking direct internet. | Risk in System Proxying | **Resolved**: Multi-layered safety net: `CombiSpeed.exe --stop` IPC signal, WinINet option refresh, and registry fallback in `Stop-CombiSpeed.bat` ensure direct internet is always restored. |
| ISSUE-008 | Complicated manual installation or requirement for elevated UAC admin rights. | Deployment friction | **Resolved**: `Install-CombiSpeed.bat` deploys cleanly to `%LOCALAPPDATA%\Programs\CombiSpeed` without UAC prompts, auto-creates Desktop/Start Menu shortcuts, and integrates into Windows Apps & Features. |
| ISSUE-009 | Proxy-only solutions require application-level proxy awareness and cannot bond system-wide traffic through a dedicated Windows network interface connection. | Speedy had no virtual adapter or VPN | **Resolved**: CombiSpeed v1.2.0 introduces the **Virtual Bonded Bridge & Adapter Connection** (`CombiSpeed Bonded Bridge`), creating a native Windows network connection in `ncpa.cpl` and Windows Settings that merges active physical adapters and multiplexes bandwidth based on the exact % set for each connection via Smooth Weighted Round Robin. |
| ISSUE-010 | Adapter selections, customized percentage ratios, and combination profiles lost on restart or rescan. | Speedy had no persistence | **Resolved**: CombiSpeed v1.3.1 implements durable configuration persistence in `%LOCALAPPDATA%\CombiSpeed\adapter_settings.json` with debounced auto-save, MAC address binding, and combination profiles. |
| ISSUE-011 | Connection-based round-robin multiplexing did not throttle actual byte transfer speeds or enforce bandwidth limits across combined adapters. | Speedy had no throttling or rate limiting | **Resolved**: CombiSpeed v1.3.2 implements high-performance asynchronous Token Bucket `BandwidthThrottler` across all TCP, UDP, and Virtual Bridge streams, enforcing strict proportional speed shaping and configurable bandwidth caps. |

## Feature Backlog
- [x] One-Click Start, Stop & Installer scripts with CLI flags (`--start`, `--stop`, `--minimized`).
- [x] Windows Virtual Adapter / VPN Connection & Bandwidth Bridge merging physical NICs based on % weights.
- [x] PAC (Proxy Auto-Config) rule generator for domain-based interface routing.
- [x] Windows service mode for background headless execution without GUI.
- [x] UDP associate relay support for SOCKS5 (gaming/streaming UDP acceleration).
- [x] Adapter settings and combination profile persistence with debounced auto-save and startup restoration.
- [x] Real-time bandwidth throttling & speed shaper enforcing proportional bandwidth and speed caps across combined connection adapters.
- [x] Application version display in main window titlebar badge, glassmorphic footer status bar, and --version CLI flag.
