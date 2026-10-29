# Release Notes

## [1.0.0] - 2026-10-29 - Genesis Release

### 🚀 Highlights & Features
- **Multi-Adapter Channel Bonding Engine**: Combines multiple physical and wireless network adapters (Ethernet, Wi-Fi, 4G/5G Cellular, Hotspots) by binding outgoing sockets directly to adapter local IPv4 addresses.
- **Dual High-Throughput Proxy Core**:
  - RFC 1928 SOCKS5 Proxy server with IPv4 and Domain Name resolution.
  - HTTP/HTTPS CONNECT forward proxy for direct, seamless browser and Windows application compatibility.
- **Intelligent Routing Algorithms**:
  - Dynamic Weighted Round-Robin (WRR) with custom per-adapter load percentage allocation.
  - Equal-Share distribution.
  - Least-Latency routing prioritizing adapters with lowest real-time ping.
  - Active-Passive failover protection.
- **1-Click Windows System Proxy Integration**: Safe automated Windows registry toggle (`wininet`) to instantly apply or remove proxy across the entire OS without manual configuration.
- **Real-Time Telemetry & Visualizer**:
  - Live download and upload speedometers (KB/s, MB/s) per adapter and aggregate total.
  - Real-time 60-second scrolling throughput waveform graph.
  - Live RTT latency ping and packet loss health checks.
  - Session and total data transfer statistics.
- **Built-in Speed Test & Multi-Connection Benchmark**:
  - Parallel multi-connection download benchmark to test individual adapter speeds and demonstrate bonded throughput.
- **Futuristic Glassmorphic Desktop GUI**: Modern cyberpunk-inspired dark theme with neon cyan and emerald indicators, responsive layout, and detailed connection audit logs.

### 🐛 Bug Fixes & Improvements over Reference (Speedy)
- Fixed missing `localAddress` socket binding in Speedy that caused traffic to bypass secondary adapters and leak back to the default OS gateway route.
- Fixed `EADDRINUSE` port conflict in Speedy caused by invoking `net.createServer().listen()` inside each incoming socket callback.
- Added full HTTP/HTTPS forward proxy support, eliminating the limitation to SOCKS5-only client software.
- Added live continuous health checks and automatic failover handling for disconnected adapters.
