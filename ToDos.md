# CombiSpeed - Project ToDo Tracker

## Milestone 1: Architecture, Core Engine & Windows Networking [COMPLETED]
- [x] Analyze reference repository (RezaRafia/Speedy) and identify core limitations and socket binding bugs.
- [x] Scan Windows network adapters on the host machine (verified Intel I211 Ethernet + Realtek WiFi/Bridge on independent WANs).
- [x] Validate socket `localAddress` interface binding on live Windows network adapters.
- [x] Initialize Git repository, version tracking (`version.json`), and durable task persistence checkpoints.
- [x] Implement `NetworkDetector` service: Real-time adapter discovery, IP resolution, MAC, gateway, and adapter types.
- [x] Implement `PingHealthMonitor`: Periodic ICMP/TCP ping per adapter to measure live latency and detect link failures.
- [x] Implement `LoadBalancerEngine`: Weighted Round-Robin, Equal-Share, Least-Latency, and Failover routing algorithms.
- [x] Implement `Socks5ProxyServer`: RFC 1928 SOCKS5 protocol with interface socket binding (`localAddress`) and byte-level telemetry.
- [x] Implement `HttpProxyServer`: HTTP and HTTPS `CONNECT` tunnel proxy with interface socket binding and byte-level telemetry.
- [x] Implement `WindowsProxyManager`: Safe 1-click Windows System Proxy toggle via registry (`reg.exe`) with auto-revert on shutdown.

## Milestone 2: Telemetry, Speed Testing & Bonding Validation [COMPLETED]
- [x] Implement `ThroughputMonitor`: Per-adapter and aggregate real-time download/upload speed (KB/s, MB/s) and session transfer totals.
- [x] Implement `ParallelSpeedTester`: Multi-stream parallel benchmark engine to test individual adapters vs bonded aggregate throughput.
- [x] Implement connection audit logger with filtering and session details.
- [x] Execute automated bonding verification tests confirming multi-WAN egress.

## Milestone 3: Premium UI Dashboard (Electron + React + Vite) [COMPLETED]
- [x] Design futuristic dark glassmorphic UI with vibrant neon telemetry, glowing master toggle, and responsive layout.
- [x] Implement Adapter Cards with custom weight sliders, live latency badge, link status, and real-time bandwidth meters.
- [x] Implement Real-Time Bandwidth Canvas / Line Chart tracking aggregate and per-adapter throughput over 60 seconds.
- [x] Implement Settings Drawer: Custom ports (SOCKS5, HTTP), proxy mode selection, routing algorithm picker, auto-start, auto-system-proxy.
- [x] Implement window controls (minimize, maximize, close with safe proxy restoration).

## Milestone 4: Verification, Release Management & Auto Check-in [COMPLETED]
- [x] Test dual-adapter concurrent traffic and confirm multi-WAN bandwidth bonding.
- [x] Verify clean shutdown and Windows proxy settings restoration.
- [x] Build production distribution package / bundles (`npm run build`).
- [x] Update documentation (`ReadMe.md`, `KnownIssues.md`, `ToDos.md`, `ReleaseNotes.md`).
- [x] Git commit and version tag increment.
