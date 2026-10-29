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

## Feature Backlog
- [ ] PAC (Proxy Auto-Config) rule generator for domain-based interface routing.
- [ ] Windows service mode for background headless execution without GUI.
- [ ] UDP associate relay support for SOCKS5 (gaming/streaming UDP acceleration).
