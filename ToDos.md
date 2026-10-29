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

## Milestone 4: Verification, Release Management & Auto Check-in [COMPLETED]
- [x] Compile and verify C# .NET solution with `dotnet build` (0 warnings, 0 errors).
- [x] Generate self-contained native Windows release publish (`publish/CombiSpeed.exe`).
- [x] Clean up any temporary files/scripts.
- [x] Update documentation (`ReadMe.md`, `KnownIssues.md`, `ReleaseNotes.md`, `version.json`).
- [x] Git commit and version tag increment (`v1.1.0`).
