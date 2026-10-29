# ⚡ CombiSpeed (Network Adapter Channel Bonding & Multi-WAN Bandwidth Aggregator)

![CombiSpeed Architecture](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue?style=for-the-badge&logo=windows)
![License](https://img.shields.io/badge/License-MIT-emerald?style=for-the-badge)
![TypeScript](https://img.shields.io/badge/TypeScript-5.x-blue?style=for-the-badge&logo=typescript)
![Electron](https://img.shields.io/badge/Electron-Latest-cyan?style=for-the-badge&logo=electron)
![React](https://img.shields.io/badge/React-18-61DAFB?style=for-the-badge&logo=react)

**CombiSpeed** is an enterprise-grade Windows desktop application that combines multiple network adapters (Wi-Fi, Gigabit Ethernet, 4G/5G Cellular USB dongles, Mobile Hotspots, and LAN connections) into a unified, high-speed connection. By dispatching connections concurrently across multiple network interface cards (NICs) with low-level socket binding and intelligent load balancing, CombiSpeed maximizes total download and upload throughput while providing zero-downtime link redundancy and failover.

---

## 🌟 Application Summary

In modern home, office, and remote environments, workstations frequently have access to multiple internet sources simultaneously:
- High-speed Fiber/Broadband on Ethernet
- 5G/4G smartphone hotspot via Wi-Fi or USB tethering
- Secondary office LAN or Wi-Fi network

Under standard Windows operation, the OS TCP/IP routing table selects only one default route with the lowest metric; all other network adapters sit 100% idle and wasted! 

**CombiSpeed solves this fundamental limitation.** It acts as a local high-performance multi-threaded aggregation engine. By intercepting client requests via dual-mode SOCKS5 & HTTP/HTTPS forward proxies, it binds each outgoing TCP socket directly to individual network adapter IP addresses (`localAddress` socket binding). This bypasses the single default gateway restriction and forces traffic simultaneously across all active adapters, combining their speeds into a single supercharged pipeline.

---

## 🚀 Key Features

1. **True Multi-Adapter Socket Binding**:
   - Outgoing connections are explicitly bound to adapter local IPv4 addresses via OS-level socket configuration (`SO_BINDTODEVICE` semantics in Windows TCP stack).
   - Traffic exits through distinct physical MAC addresses and independent gateway WAN IPs.

2. **Dual-Mode High-Throughput Proxy Engine**:
   - **RFC 1928 Compliant SOCKS5 Proxy**: High efficiency TCP tunneling supporting IPv4 and Domain resolution.
   - **HTTP / HTTPS CONNECT Forward Proxy**: Full browser and system-wide compatibility without requiring third-party browser extensions.

3. **Intelligent Load Balancing & Routing Algorithms**:
   - **Dynamic Weighted Round-Robin (WRR)**: Distribute load proportionally based on user-defined weights or bandwidth capacity (e.g. 70% Fiber, 30% 5G Hotspot).
   - **Equal-Share Multiplexing**: Automatically balances connections equally across all available healthy adapters.
   - **Least-Latency Smart Dispatching**: Continuously measures real-time RTT (Round Trip Time) per adapter and routes latency-sensitive requests through the fastest responding adapter.
   - **Active-Passive Link Failover**: Designate a primary connection with instant auto-fallback if connection degradation or packet loss occurs.

4. **1-Click Windows System Proxy Integration**:
   - Toggle system-wide Windows proxy on and off with a single click.
   - Modifies Windows Internet Settings (`wininet` registry keys) dynamically so Chrome, Edge, Brave, Windows Update, and all desktop applications instantly route through the bonded adapters.
   - Automatic graceful revert: When CombiSpeed stops or exits, original Windows network proxy settings are immediately restored.

5. **Real-Time Telemetry & Throughput Visualizer**:
   - Per-adapter live download & upload speed meters (KB/s, MB/s).
   - 60-second real-time scrolling bandwidth canvas/chart with glowing multi-series waveforms.
   - Live RTT ping and adapter health indicators with instant online/offline detection.
   - Session data transferred counters (MB/GB) per adapter and aggregate total.

6. **Built-In Multi-Stream Bonding Benchmark (Speed Tester)**:
   - On-demand benchmark utility that tests individual adapter speeds alongside concurrent bonded aggregate throughput so you can visually verify speed combination in real-time.

7. **Futuristic Cyberpunk Glassmorphic Dashboard**:
   - Precision dark UI with glowing cyan and emerald telemetry, interactive percentage sliders, animated adapter link indicators, and real-time audit log streaming.

---

## 🛡️ MOATs (Defensible Competitive Advantages)

| Dimension | CombiSpeed | RezaRafia/Speedy (Reference) | Standard Windows Routing |
|---|---|---|---|
| **Socket Binding Integrity** | **Real `localAddress` interface binding** on every connection socket | **Missing `localAddress`** (causes traffic to fall back to OS default route) | Routes 100% of traffic to a single lowest-metric adapter |
| **Concurrency Reliability** | Single persistent listening daemon with connection pooling | Spawns a new listener on port 8080 per connection, throwing `EADDRINUSE` crash | N/A |
| **Proxy Protocol Support** | **Dual SOCKS5 & HTTP/HTTPS CONNECT** | SOCKS5 only (IPv4 only) | None |
| **Windows System Integration** | **1-Click Windows Proxy Registry Automation** with fail-safe revert | None (Manual proxy config required in every app) | None |
| **Adapter Health & Failover** | Real-time continuous latency probes with auto-rerouting on packet loss | None (Dead connection leads to application hang) | OS metric failover takes 30-60+ seconds |
| **Live Telemetry & Speedometer**| Real-time per-adapter MB/s gauges, canvas waveform, and data meters | Text console logs only | Task Manager adapter graph (split, not bonded) |

---

## 💡 Real-World Challenges Solved

1. **Slow Downloads & Large File Transfers**:
   - Multi-connection download managers (IDM, Free Download Manager, Steam multi-chunk downloads, Torrent clients, Git clones) open dozens of concurrent TCP streams. CombiSpeed spreads these streams across both Ethernet and Wi-Fi simultaneously, delivering the combined sum of both download pipes.
2. **Buffering & Unstable 4K/8K Video Streaming**:
   - Video platforms buffer media using concurrent chunk requests. CombiSpeed fetches video segments across all adapters, preventing buffering during peak congestion.
3. **Remote Work & Critical Video Calls**:
   - Combines a fluctuating mobile hotspot with a home broadband connection. If one connection experiences jitter or temporary dropouts, CombiSpeed's health monitor redirects traffic instantly.
4. **Data Cap Management**:
   - Flexible weight sliders allow users to cap usage on metered cellular tethering (e.g., 20%) while utilizing unlimited fiber broadband for the remaining 80%.

---

## 🏗️ Technical Architecture

```
                      ┌────────────────────────────────────────┐
                      │    Windows Applications / Browsers     │
                      └───────────────────┬────────────────────┘
                                          │ (Localhost : 8080 / 1080)
                                          ▼
                      ┌────────────────────────────────────────┐
                      │      CombiSpeed Aggregation Core       │
                      │  - HTTP/HTTPS CONNECT Forward Proxy    │
                      │  - RFC 1928 SOCKS5 Forward Proxy       │
                      │  - 1-Click Windows WinINet Proxy Hook  │
                      └───────────────────┬────────────────────┘
                                          │
                        ┌─────────────────┴─────────────────┐
                        │   Load Balancer & Health Monitor  │
                        │   - Weighted Round-Robin          │
                        │   - Least Latency Probe           │
                        │   - Active Failover Engine        │
                        └────────┬─────────────────┬────────┘
                                 │                 │
              Socket bind(localAddress: IP_1)     Socket bind(localAddress: IP_2)
                                 │                 │
                                 ▼                 ▼
             ┌─────────────────────────┐     ┌─────────────────────────┐
             │ Adapter 1 (Ethernet 3)  │     │ Adapter 2 (Wi-Fi 2)     │
             │ IP: 192.168.1.9         │     │ IP: 192.168.1.28        │
             │ Gateway: 192.168.1.1    │     │ Gateway: 192.168.1.1    │
             └────────────┬────────────┘     └────────────┬────────────┘
                          │                               │
                          ▼ (WAN: 106.213.87.143)         ▼ (WAN: 58.84.61.199)
             ┌─────────────────────────────────────────────────────────┐
             │                    Global Internet                      │
             │          Combined Throughput = WAN 1 + WAN 2            │
             └─────────────────────────────────────────────────────────┘
```

---

## 📦 Getting Started

### Prerequisites
- Windows 10 or 11 (64-bit)
- Node.js 18+ and npm
- Two or more active network connections (e.g. Ethernet + Wi-Fi, Wi-Fi + Mobile Tethering, or dual Ethernet)

### Development Setup
```bash
# Clone the repository
git clone https://github.com/yashiscool123/CombiSpeed.git
cd CombiSpeed

# Install dependencies
npm install

# Start in development mode
npm run dev

# Package for Windows
npm run build:win
```

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
