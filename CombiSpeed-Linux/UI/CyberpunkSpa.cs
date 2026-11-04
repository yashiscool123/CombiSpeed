namespace CombiSpeed.Linux.UI;

public static class CyberpunkSpa
{
    public static string GetHtml(string osName = "LINUX EDITION")
    {
        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>CombiSpeed - Multi-Adapter Bandwidth Combiner & Speed Shaper</title>
    <style>
        :root {
            --bg-dark: #0B0F19;
            --card-bg: #131D33;
            --card-border: #1E293B;
            --cyan: #00F0FF;
            --purple: #7928CA;
            --emerald: #10B981;
            --amber: #F59E0B;
            --red: #EF4444;
            --text-high: #FFFFFF;
            --text-mid: #F1F5F9;
            --text-muted: #CBD5E1;
            --ui-scale: 1.0;
        }

        body.high-contrast {
            --bg-dark: #000000;
            --card-bg: #050B14;
            --card-border: #00F0FF;
            --text-high: #FFFFFF;
            --text-mid: #FFFFFF;
            --text-muted: #38BDF8;
        }

        body.reduce-motion * {
            animation: none !important;
            transition: none !important;
        }

        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
            color: var(--text-high);
        }

        body {
            background-color: var(--bg-dark);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            overflow-x: hidden;
            zoom: var(--ui-scale);
        }

        /* Top Header Bar */
        header {
            background: rgba(19, 29, 51, 0.85);
            backdrop-filter: blur(12px);
            border-bottom: 1px solid var(--card-border);
            padding: 12px 24px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            position: sticky;
            top: 0;
            z-index: 100;
        }

        .brand-cluster {
            display: flex;
            align-items: center;
            gap: 12px;
        }

        .logo-icon {
            width: 32px;
            height: 32px;
            background: linear-gradient(135deg, var(--cyan), var(--purple));
            border-radius: 8px;
            display: flex;
            align-items: center;
            justify-content: center;
            box-shadow: 0 0 15px rgba(0, 240, 255, 0.4);
            font-weight: bold;
            font-size: 18px;
        }

        .brand-text h1 {
            font-size: 18px;
            font-weight: 800;
            letter-spacing: 1.5px;
            background: linear-gradient(90deg, #FFFFFF, var(--cyan));
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }

        .brand-text p {
            font-size: 10px;
            font-weight: 600;
            letter-spacing: 0.8px;
            color: var(--cyan);
        }

        .header-badges {
            display: flex;
            gap: 8px;
            align-items: center;
        }

        .badge {
            background: #0B1120;
            border: 1px solid var(--card-border);
            padding: 4px 10px;
            border-radius: 6px;
            font-size: 11px;
            font-weight: 700;
            letter-spacing: 0.5px;
            color: var(--text-mid);
        }

        .badge-cyan {
            border-color: var(--cyan);
            color: var(--cyan);
        }

        .btn-cyber {
            background: #131D33;
            border: 1px solid var(--card-border);
            color: var(--text-high);
            padding: 6px 14px;
            border-radius: 6px;
            font-size: 12px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s ease;
            display: inline-flex;
            align-items: center;
            gap: 6px;
        }

        .btn-cyber:hover {
            border-color: var(--cyan);
            box-shadow: 0 0 10px rgba(0, 240, 255, 0.3);
            background: #1E293B;
        }

        .btn-cyber-primary {
            background: linear-gradient(135deg, rgba(0,240,255,0.2), rgba(121,40,202,0.2));
            border-color: var(--cyan);
        }

        .power-toggle-btn {
            background: #131D33;
            border: 2px solid var(--emerald);
            padding: 8px 18px;
            border-radius: 24px;
            font-size: 13px;
            font-weight: 800;
            cursor: pointer;
            display: flex;
            align-items: center;
            gap: 8px;
            box-shadow: 0 0 15px rgba(16, 185, 129, 0.3);
            transition: all 0.3s ease;
        }

        .power-toggle-btn.standby {
            border-color: #64748B;
            box-shadow: none;
            color: #94A3B8;
        }

        .led-dot {
            width: 10px;
            height: 10px;
            border-radius: 50%;
            background: var(--emerald);
            box-shadow: 0 0 8px var(--emerald);
            animation: pulse 1.8s infinite;
        }

        .standby .led-dot {
            background: #64748B;
            box-shadow: none;
            animation: none;
        }

        @keyframes pulse {
            0%, 100% { opacity: 1; transform: scale(1); }
            50% { opacity: 0.5; transform: scale(0.9); }
        }

        /* Main Content Container */
        main {
            flex: 1;
            padding: 20px 24px;
            max-width: 1300px;
            margin: 0 auto;
            width: 100%;
        }

        /* 4 Telemetry Metric Cards */
        .metrics-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
            gap: 16px;
            margin-bottom: 20px;
        }

        .metric-card {
            background: var(--card-bg);
            border: 1px solid var(--card-border);
            border-radius: 10px;
            padding: 16px;
            display: flex;
            flex-direction: column;
            gap: 6px;
            position: relative;
            overflow: hidden;
        }

        .metric-card::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: 4px;
            height: 100%;
            background: var(--cyan);
        }

        .metric-card.purple::before { background: var(--purple); }
        .metric-card.emerald::before { background: var(--emerald); }
        .metric-card.amber::before { background: var(--amber); }

        .metric-title {
            font-size: 11px;
            font-weight: 700;
            letter-spacing: 0.8px;
            color: var(--cyan);
            text-transform: uppercase;
        }

        .metric-value {
            font-size: 26px;
            font-weight: 800;
            font-family: Consolas, monospace;
            color: var(--text-high);
        }

        .metric-sub {
            font-size: 11px;
            color: var(--text-muted);
        }

        /* Segmented Navigation Tabs */
        .tabs-nav {
            display: flex;
            gap: 6px;
            background: #080C14;
            padding: 6px;
            border-radius: 10px;
            border: 1px solid var(--card-border);
            margin-bottom: 20px;
            overflow-x: auto;
        }

        .tab-btn {
            background: transparent;
            border: 1px solid transparent;
            padding: 10px 18px;
            border-radius: 8px;
            font-size: 13px;
            font-weight: 700;
            cursor: pointer;
            color: #E2E8F0;
            transition: all 0.2s ease;
            white-space: nowrap;
        }

        .tab-btn:hover {
            background: #131D33;
            color: var(--text-high);
        }

        .tab-btn.active {
            background: #1E293B;
            border-color: var(--cyan);
            color: var(--cyan);
            box-shadow: 0 0 12px rgba(0, 240, 255, 0.2);
        }

        /* Tab Panels */
        .tab-panel {
            display: none;
        }

        .tab-panel.active {
            display: block;
        }

        /* Waveform Canvas Card */
        .waveform-card {
            background: var(--card-bg);
            border: 1px solid var(--card-border);
            border-radius: 10px;
            padding: 18px;
            margin-bottom: 20px;
        }

        .waveform-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 12px;
        }

        canvas {
            width: 100%;
            height: 180px;
            background: #080C14;
            border-radius: 8px;
            border: 1px solid #1E293B;
            display: block;
        }

        /* Virtual Bridge Card */
        .bridge-card {
            background: var(--card-bg);
            border: 1px solid var(--card-border);
            border-radius: 10px;
            padding: 18px;
            margin-bottom: 20px;
        }

        .distribution-bar {
            height: 12px;
            width: 100%;
            border-radius: 6px;
            background: #080C14;
            display: flex;
            overflow: hidden;
            margin: 12px 0;
            border: 1px solid var(--card-border);
        }

        .bar-segment {
            height: 100%;
            transition: width 0.3s ease;
        }

        /* Adapter Cards */
        .adapter-list {
            display: flex;
            flex-direction: column;
            gap: 14px;
        }

        .adapter-card {
            background: var(--card-bg);
            border: 1px solid var(--card-border);
            border-radius: 10px;
            padding: 18px;
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .adapter-top {
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .adapter-info h3 {
            font-size: 15px;
            font-weight: 700;
        }

        .adapter-info p {
            font-size: 12px;
            color: var(--text-muted);
            font-family: Consolas, monospace;
        }

        .adapter-metrics {
            display: flex;
            gap: 12px;
            align-items: center;
        }

        .slider-row {
            display: flex;
            align-items: center;
            gap: 16px;
        }

        .slider-row input[type="range"] {
            flex: 1;
            accent-color: var(--cyan);
            height: 6px;
        }

        /* Speed Shaper Toolbar */
        .shaper-card {
            background: var(--card-bg);
            border: 1px solid var(--card-border);
            border-radius: 10px;
            padding: 20px;
            margin-bottom: 20px;
        }

        .preset-row {
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
            margin: 14px 0;
        }

        .select-dark {
            background: #080C14;
            border: 1px solid var(--card-border);
            color: var(--text-high);
            padding: 8px 14px;
            border-radius: 6px;
            font-size: 13px;
        }

        /* Connection Audit Table */
        .audit-table {
            width: 100%;
            border-collapse: collapse;
            font-size: 12px;
        }

        .audit-table th {
            text-align: left;
            padding: 10px;
            background: #080C14;
            color: var(--cyan);
            font-weight: 700;
            border-bottom: 1px solid var(--card-border);
        }

        .audit-table td {
            padding: 10px;
            border-bottom: 1px solid #1E293B;
            font-family: Consolas, monospace;
        }

        /* Modal Styles */
        .modal-overlay {
            position: fixed;
            top: 0;
            left: 0;
            width: 100vw;
            height: 100vh;
            background: rgba(3, 7, 18, 0.8);
            backdrop-filter: blur(8px);
            display: none;
            align-items: center;
            justify-content: center;
            z-index: 1000;
        }

        .modal-overlay.open {
            display: flex;
        }

        .modal-card {
            background: var(--card-bg);
            border: 1px solid var(--cyan);
            border-radius: 12px;
            width: 90%;
            max-width: 600px;
            max-height: 85vh;
            overflow-y: auto;
            padding: 24px;
            box-shadow: 0 0 30px rgba(0, 240, 255, 0.2);
        }

        .modal-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 16px;
            border-bottom: 1px solid var(--card-border);
            padding-bottom: 12px;
        }
    </style>
</head>
<body>

    <!-- Header Bar -->
    <header>
        <div class="brand-cluster">
            <div class="logo-icon">⚡</div>
            <div class="brand-text">
                <h1>COMBISPEED</h1>
                <p>MULTI-ADAPTER SPEED BONDING & QOS SHAPER</p>
            </div>
            <div class="header-badges">
                <span class="badge badge-cyan">v2.1.0</span>
                <span class="badge">{{osName}}</span>
            </div>
        </div>

        <div style="display: flex; gap: 10px; align-items: center;">
            <button class="btn-cyber" onclick="openAccessibilityModal()">♿ Display</button>
            <button class="btn-cyber" onclick="openSettingsModal()">⚙ Settings</button>
            <button id="btnPower" class="power-toggle-btn" onclick="togglePower()">
                <div class="led-dot"></div>
                <span id="powerText">BONDING ACTIVE</span>
            </button>
        </div>
    </header>

    <!-- Main Content -->
    <main>
        <!-- 4 Metric Cards -->
        <div class="metrics-grid">
            <div class="metric-card">
                <div class="metric-title">Bonded Download (Rx)</div>
                <div class="metric-value" id="valRx">0.0 KB/s</div>
                <div class="metric-sub" id="peakRx">Peak: 0.0 MB/s</div>
            </div>
            <div class="metric-card purple">
                <div class="metric-title">Bonded Upload (Tx)</div>
                <div class="metric-value" id="valTx">0.0 KB/s</div>
                <div class="metric-sub" id="peakTx">Peak: 0.0 MB/s</div>
            </div>
            <div class="metric-card emerald">
                <div class="metric-title">Active Sockets</div>
                <div class="metric-value" id="valSockets">0</div>
                <div class="metric-sub" id="subAdapters">2 Adapters Merged</div>
            </div>
            <div class="metric-card amber">
                <div class="metric-title">Transferred Volume</div>
                <div class="metric-value" id="valTransferred">0 MB</div>
                <div class="metric-sub" id="valUptime">Uptime: 00:00:00</div>
            </div>
        </div>

        <!-- Segmented Tab Navigation -->
        <div class="tabs-nav">
            <button class="tab-btn active" onclick="switchTab('tabOverview')">⚡ Overview & Dashboard</button>
            <button class="tab-btn" onclick="switchTab('tabAdapters')">🖧 Combined Adapters</button>
            <button class="tab-btn" onclick="switchTab('tabShaper')">🚀 Speed Shaper & QoS</button>
            <button class="tab-btn" onclick="switchTab('tabAudit')">📜 Connection Audit</button>
            <button class="tab-btn" onclick="switchTab('tabRotator')">🔄 Rotating Proxy</button>
        </div>

        <!-- TAB 1: Overview -->
        <div id="tabOverview" class="tab-panel active">
            <!-- Live Intranet & Internet Geo-Location Identity Card -->
            <div class="bridge-card" style="margin-bottom: 16px;">
                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <div style="display: flex; align-items: center; gap: 14px;">
                        <div id="geoFlagBox" style="font-size: 30px; width: 48px; height: 48px; display: flex; align-items: center; justify-content: center; background: rgba(0,240,255,0.06); border: 1px solid var(--border); border-radius: 10px;">🌐</div>
                        <div>
                            <div style="display: flex; align-items: center; gap: 8px;">
                                <span style="font-size: 13px; font-weight: 700; color: var(--cyan);">LIVE NETWORK IDENTITY &amp; GEO-LOCATION</span>
                                <span class="badge" style="background: rgba(16,185,129,0.15); color: var(--green); border: 1px solid var(--green);">RESOLVED</span>
                            </div>
                            <div style="display: flex; gap: 16px; margin-top: 6px; font-size: 11px; flex-wrap: wrap;">
                                <span><span style="color: var(--text-muted);">Location:</span> <strong id="geoLocText" style="color: var(--cyan);">Resolving...</strong></span>
                                <span><span style="color: var(--text-muted);">WAN IP:</span> <strong id="geoWanIp" style="color: var(--amber); font-family: monospace;">Resolving...</strong></span>
                                <span><span style="color: var(--text-muted);">Intranet:</span> <strong id="geoIntranetIp" style="color: var(--purple); font-family: monospace;">Resolving...</strong></span>
                                <span><span style="color: var(--text-muted);">ISP:</span> <strong id="geoIsp" style="color: var(--text-main);">Resolving...</strong></span>
                            </div>
                        </div>
                    </div>
                    <button class="btn-cyber" onclick="fetchGeoIp(true)">⟳ Refresh Identity</button>
                </div>
            </div>

            <div class="waveform-card">
                <div class="waveform-header">
                    <span style="font-size: 13px; font-weight: 700; color: var(--cyan);">LIVE 60-SECOND REAL-TIME BANDWIDTH WAVEFORM</span>
                    <div style="display: flex; gap: 12px; font-size: 11px;">
                        <span style="color: var(--cyan);">■ Download (Rx)</span>
                        <span style="color: var(--purple);">■ Upload (Tx)</span>
                    </div>
                </div>
                <canvas id="waveformCanvas"></canvas>
            </div>

            <div class="bridge-card">
                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <div>
                        <span style="font-size: 13px; font-weight: 700; color: var(--cyan);">VIRTUAL BONDED BRIDGE SUBSYSTEM</span>
                        <p id="bridgeSummary" style="font-size: 12px; color: var(--text-muted); margin-top: 4px;">Bonded Gateway active on 127.0.0.1:8443.</p>
                    </div>
                    <button class="btn-cyber btn-cyber-primary" onclick="toggleBridge()">Toggle Bridge Gateway</button>
                </div>
                <div class="distribution-bar" id="distBar"></div>
            </div>

            <div style="display: flex; gap: 12px; justify-content: flex-end;">
                <button class="btn-cyber" onclick="openSpeedTestModal()">⚡ Benchmark Multi-Stream Speed</button>
                <button class="btn-cyber" onclick="toggleSystemProxy()">🌐 Toggle System Proxy</button>
            </div>
        </div>

        <!-- TAB 2: Combined Adapters -->
        <div id="tabAdapters" class="tab-panel">
            <div style="display: flex; justify-content: space-between; align-items: center; background: var(--surface); border: 1px solid var(--border); border-radius: 12px; padding: 14px 18px; margin-bottom: 16px;">
                <div>
                    <span style="font-size: 13px; font-weight: 700; color: var(--cyan); letter-spacing: 0.5px;">COMBINED ADAPTERS &amp; BANDWIDTH ALLOCATION</span>
                    <div style="font-size: 11px; color: var(--text-muted); margin-top: 3px;" id="adaptersSummaryText">Scan and aggregate physical Wi-Fi, Ethernet, Cellular &amp; Bridge connections.</div>
                </div>
                <div style="display: flex; gap: 8px; align-items: center;">
                    <button class="btn-cyber" onclick="rescanAdapters()" id="btnRescanAdapters" title="Rescan network adapters">⟳ Rescan Adapters</button>
                </div>
            </div>
            <div class="adapter-list" id="adapterCardsContainer"></div>
        </div>

        <!-- TAB 3: Speed Shaper & QoS -->
        <div id="tabShaper" class="tab-panel">
            <div class="shaper-card">
                <span style="font-size: 13px; font-weight: 700; color: var(--cyan);">DYNAMIC CROSS-CONNECTION SPEED SHAPER & THROTTLING</span>
                <p style="font-size: 12px; color: var(--text-muted); margin-top: 4px;">Dynamic Token Bucket rate limiter adjusting bufferbloat and latency across connections.</p>

                <div style="margin-top: 16px;">
                    <label style="font-size: 12px; font-weight: 700;">Optimization Mode:</label>
                    <select id="selShaperMode" class="select-dark" onchange="changeShaperMode(this.value)">
                        <option value="BalancedDynamic">⚖ Balanced Dynamic (Adaptive QoS Distribution)</option>
                        <option value="MaximumBandwidth">🚀 Maximum Bandwidth (Throughput Aggregation Priority)</option>
                        <option value="LowestLatency">⚡ Lowest Ping / Latency (Anti-Bufferbloat & Gaming Priority)</option>
                    </select>
                </div>

                <div style="margin-top: 16px;">
                    <label style="font-size: 12px; font-weight: 700;">Bandwidth Throttling Speed Cap:</label>
                    <div class="preset-row">
                        <button class="btn-cyber" onclick="setSpeedLimit(0)">Unlimited (Max Wire)</button>
                        <button class="btn-cyber" onclick="setSpeedLimit(500)">500 Mbps</button>
                        <button class="btn-cyber" onclick="setSpeedLimit(250)">250 Mbps</button>
                        <button class="btn-cyber" onclick="setSpeedLimit(100)">100 Mbps</button>
                        <button class="btn-cyber" onclick="setSpeedLimit(50)">50 Mbps</button>
                        <button class="btn-cyber" onclick="setSpeedLimit(25)">25 Mbps</button>
                        <button class="btn-cyber" onclick="setSpeedLimit(10)">10 Mbps</button>
                        <button class="btn-cyber btn-cyber-primary" onclick="openCustomThrottleModal()">⚙ Custom...</button>
                    </div>
                </div>

                <div style="margin-top: 14px;">
                    <label style="display: flex; align-items: center; gap: 8px; font-size: 12px; cursor: pointer;">
                        <input type="checkbox" id="chkBorrowing" checked onchange="toggleBorrowing(this.checked)">
                        Allow dynamic cross-connection bandwidth borrowing (fast connections borrow idle headroom)
                    </label>
                </div>
            </div>
        </div>

        <!-- TAB 4: Connection Audit -->
        <div id="tabAudit" class="tab-panel">
            <div class="shaper-card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;">
                    <span style="font-size: 13px; font-weight: 700; color: var(--cyan);">LIVE CONNECTION AUDIT STREAM</span>
                    <button class="btn-cyber" onclick="clearAudit()">Clear Log</button>
                </div>
                <table class="audit-table">
                    <thead>
                        <tr>
                            <th>TIME</th>
                            <th>PROTOCOL</th>
                            <th>DESTINATION</th>
                            <th>PORT</th>
                            <th>OUTBOUND ADAPTER</th>
                            <th>STATUS</th>
                        </tr>
                    </thead>
                    <tbody id="auditRows"></tbody>
                </table>
            </div>
        </div>

        <!-- TAB 5: Rotating Proxy & Harvester -->
        <div id="tabRotator" class="tab-panel">
            <div class="shaper-card" style="margin-bottom: 16px;">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px;">
                    <div>
                        <span style="font-size: 14px; font-weight: 700; color: var(--cyan);">AUTOMATED ROTATING PROXY HARVESTER &amp; DISPATCHER</span>
                        <div style="font-size: 11px; color: var(--text-muted); margin-top: 3px;">Harvests open-source proxies, evaluates anonymity &amp; latency, and rotates requests across physical adapters.</div>
                    </div>
                    <div style="display: flex; gap: 8px;">
                        <button class="btn-cyber btn-cyber-primary" id="btnToggleRotator" onclick="toggleRotator()" title="Start or stop the local Rotating Proxy HTTP/SOCKS5 server on port 8081">▶ Start Rotator</button>
                        <button class="btn-cyber" onclick="triggerHarvest()" title="Fetch latest public proxy lists from configured GitHub, API, and URL sources in idempotence mode">⚡ Harvest Proxies</button>
                        <button class="btn-cyber" onclick="triggerValidate()" title="Test all harvested proxies for connectivity, protocol compatibility, response time, and anonymity">🔍 Validate Pool</button>
                        <button class="btn-cyber" onclick="openSourcesModal()" title="Bulk add, edit, or customize proxy repository URLs &amp; APIs">🌐 Sources...</button>
                    </div>
                </div>
                <div class="metrics-grid" style="grid-template-columns: repeat(4, 1fr); margin-bottom: 14px;">
                    <div class="metric-card cyan" title="Total unique proxy servers discovered and stored in local database">
                        <div class="metric-title">Total Candidates</div>
                        <div class="metric-value" id="rotatorPoolCount">0</div>
                        <div class="metric-sub">In database</div>
                    </div>
                    <div class="metric-card emerald" title="Number of healthy proxies that passed connectivity and anonymity validation">
                        <div class="metric-title">Verified Active</div>
                        <div class="metric-value" id="rotatorHealthyCount">0</div>
                        <div class="metric-sub">Ready to rotate</div>
                    </div>
                    <div class="metric-card purple" title="Local proxy listener address (127.0.0.1:8081). Point applications here.">
                        <div class="metric-title">Rotator Endpoint</div>
                        <div class="metric-value" style="font-size: 18px;" id="rotatorEndpoint">127.0.0.1:8081</div>
                        <div class="metric-sub" id="rotatorStatusPill">STOPPED</div>
                    </div>
                    <div class="metric-card amber" title="Active rotation dispatching strategy">
                        <div class="metric-title">Rotation Strategy</div>
                        <div class="metric-value" style="font-size: 18px;" id="rotatorStrategy">Round Robin</div>
                        <div class="metric-sub">Multi-NIC Bound</div>
                    </div>
                </div>
                <div style="display: flex; gap: 14px; align-items: center; margin-bottom: 12px; background: rgba(0,0,0,0.3); padding: 10px; border-radius: 8px;">
                    <span style="font-size: 12px; font-weight: bold; color: var(--cyan);">🌍 Country:</span>
                    <select id="rotatorCountryFilter" class="select-cyber" style="padding: 4px 8px; font-size: 11px; background: #0b1120; color: #fff; border: 1px solid var(--border);" onchange="updateRotatorConfig()" title="Filter rotation strictly to proxies located in a specific country, or ALL">
                        <option value="ALL">All Countries</option>
                        <option value="US">🇺🇸 United States (US)</option>
                        <option value="DE">🇩🇪 Germany (DE)</option>
                        <option value="GB">🇬🇧 United Kingdom (GB)</option>
                        <option value="FR">🇫🇷 France (FR)</option>
                        <option value="CA">🇨🇦 Canada (CA)</option>
                        <option value="JP">🇯🇵 Japan (JP)</option>
                        <option value="NL">🇳🇱 Netherlands (NL)</option>
                        <option value="SG">🇸🇬 Singapore (SG)</option>
                        <option value="IN">🇮🇳 India (IN)</option>
                    </select>
                    <label style="font-size: 11px; color: var(--text-main); display: flex; align-items: center; gap: 4px; margin-left: 8px;" title="Background auto-harvest fresh proxies every 30 minutes in idempotence mode">
                        <input type="checkbox" id="chkAutoHarvest" onchange="updateRotatorConfig()"> 🔄 Auto-Harvest (30m)
                    </label>
                    <label style="font-size: 11px; color: var(--text-main); display: flex; align-items: center; gap: 4px; margin-left: 8px;" title="Continuous background health checks and dead pruning every 5 minutes">
                        <input type="checkbox" id="chkAutoValidate" onchange="updateRotatorConfig()"> 🩺 Auto-Check (5m)
                    </label>
                    <label style="font-size: 11px; color: var(--text-main); display: flex; align-items: center; gap: 4px; margin-left: 8px;" title="Dispatches outgoing proxy connections across all enabled physical adapters simultaneously">
                        <input type="checkbox" id="chkChainNic" onchange="updateRotatorConfig()"> ⚡ Multi-NIC Chaining
                    </label>
                    <label style="font-size: 11px; color: var(--cyan); display: flex; align-items: center; gap: 4px; margin-left: 8px; font-weight: bold;" title="Bond-then-Rotate Two-Stage Pipeline: First combines all physical adapters for aggregate bandwidth, then routes outbound traffic through the active rotating proxy">
                        <input type="checkbox" id="chkBondRotate" onchange="updateRotatorConfig()"> ⚡ Bond-then-Rotate
                    </label>
                </div>

                <!-- Sorting & Multi-Faceted Filtering Bar -->
                <div style="display: flex; flex-wrap: wrap; gap: 10px; align-items: center; margin-bottom: 12px; background: rgba(0,0,0,0.4); padding: 10px 14px; border-radius: 8px; border: 1px solid var(--border);">
                    <input type="text" id="filterSearch" placeholder="🔍 Search IP, port, source..." class="select-dark" style="width: 170px; padding: 4px 8px; font-size: 11px;" oninput="applyTableFilters()" title="Search proxies by IP address, port number, country code, or repository source">
                    <select id="filterLatency" class="select-dark" style="padding: 4px 8px; font-size: 11px;" onchange="applyTableFilters()" title="Filter visible proxies by maximum round-trip latency threshold">
                        <option value="0">All Latencies</option>
                        <option value="150">&lt; 150 ms (Ultra-Fast)</option>
                        <option value="300">&lt; 300 ms (Fast)</option>
                        <option value="500">&lt; 500 ms (Normal)</option>
                        <option value="1000">&lt; 1000 ms</option>
                    </select>
                    <select id="filterSource" class="select-dark" style="padding: 4px 8px; font-size: 11px;" onchange="applyTableFilters()" title="Filter proxies harvested from a specific repository or API feed">
                        <option value="ALL">All Sources</option>
                    </select>
                    <select id="filterProtocol" class="select-dark" style="padding: 4px 8px; font-size: 11px;" onchange="applyTableFilters()" title="Filter proxies by supported protocol (HTTP, HTTPS, SOCKS4, SOCKS5)">
                        <option value="ALL">All Protocols</option>
                        <option value="HTTP">HTTP</option>
                        <option value="HTTPS">HTTPS</option>
                        <option value="SOCKS4">SOCKS4</option>
                        <option value="SOCKS5">SOCKS5</option>
                    </select>
                    <select id="filterSort" class="select-dark" style="padding: 4px 8px; font-size: 11px;" onchange="applyTableFilters()" title="Sort the proxy list dynamically">
                        <option value="latency_asc">Lowest Latency (Fastest)</option>
                        <option value="latency_desc">Highest Latency</option>
                        <option value="health_first">Status / Working First</option>
                        <option value="source_asc">Source (A-Z)</option>
                        <option value="geo_asc">Country Code (A-Z)</option>
                    </select>
                    <span id="filterCountBadge" style="margin-left: auto; font-size: 11px; font-weight: bold; color: var(--cyan); font-family: Consolas;">Showing 0 proxies</span>
                </div>

                <!-- Selection & Timely Rotation Action Bar -->
                <div style="display: flex; flex-wrap: wrap; gap: 10px; align-items: center; margin-bottom: 12px; background: rgba(0,240,255,0.06); padding: 10px 14px; border-radius: 8px; border: 1px solid var(--cyan);">
                    <span style="font-size: 11px; font-weight: bold; color: var(--cyan);">☑ POOL SELECTION:</span>
                    <button class="btn-cyber" style="padding: 4px 10px; font-size: 11px;" onclick="selectAllProxies()" title="Select all proxies in view to participate in rotation">Select All</button>
                    <button class="btn-cyber" style="padding: 4px 10px; font-size: 11px;" onclick="deselectAllProxies()" title="Uncheck all proxies so none are flagged for rotation">Deselect All</button>
                    <button class="btn-cyber" style="padding: 4px 10px; font-size: 11px;" onclick="selectFastProxies()" title="Automatically select all healthy proxies with response times under 300ms">Select Fast (≤300ms)</button>
                    <button class="btn-cyber" style="padding: 4px 10px; font-size: 11px;" onclick="forceRotateNow()" title="Immediately rotate to next proxy in pool, testing candidates until a live connection is established">⚡ Rotate Now</button>
                    <button class="btn-cyber" style="padding: 4px 10px; font-size: 11px;" onclick="forceConnectSelectedOrNext()" title="Forcefully connects immediately to the next live responsive proxy in the candidate pool, probing until connected">🎯 Force Connect</button>

                    <span id="selectionSummaryPill" class="badge" style="background: rgba(16,185,129,0.15); color: var(--green); border: 1px solid var(--green); margin-left: auto;" title="Count of healthy proxies selected to participate in rotation">Selected: 0 healthy</span>
                    <span id="rotationCountdownBadge" class="badge" style="background: rgba(245,158,11,0.15); color: var(--amber); border: 1px solid var(--amber);" title="Time remaining until the next automatic proxy server rotation">Next rotation: 30s</span>

                    <span style="font-size: 11px; color: var(--text-muted); margin-left: 8px;">Interval:</span>
                    <select id="rotatorIntervalSelect" class="select-dark" style="padding: 3px 6px; font-size: 11px; width: 65px;" onchange="updateRotatorConfig()" title="Timed rotation interval in seconds">
                        <option value="10">10s</option>
                        <option value="15">15s</option>
                        <option value="30">30s</option>
                        <option value="45">45s</option>
                        <option value="60">60s</option>
                        <option value="120">120s</option>
                        <option value="300">300s</option>
                    </select>
                </div>

                <table class="audit-table">
                    <thead>
                        <tr>
                            <th style="width: 40px;" title="Select for rotation pool">USE</th>
                            <th style="width: 65px;" title="Forcefully connect to proxy">ACTION</th>
                            <th style="cursor: pointer;" onclick="toggleTableSort('status')" title="Click to sort by status">STATUS ⇅</th>
                            <th style="cursor: pointer;" onclick="toggleTableSort('geo')" title="Click to sort by country">GEO ⇅</th>
                            <th style="cursor: pointer;" onclick="toggleTableSort('endpoint')" title="Click to sort by endpoint">ENDPOINT ⇅</th>
                            <th style="cursor: pointer;" onclick="toggleTableSort('protocol')" title="Click to sort by protocol">PROTOCOL ⇅</th>
                            <th title="Anonymity level">ANONYMITY</th>
                            <th style="cursor: pointer;" onclick="toggleTableSort('latency')" title="Click to sort by latency">LATENCY ⇅</th>
                            <th title="Connection failures count">FAILURES</th>
                            <th style="cursor: pointer;" onclick="toggleTableSort('source')" title="Click to sort by source">SOURCE ⇅</th>
                        </tr>
                    </thead>
                    <tbody id="rotatorRows">
                        <tr><td colspan="10" style="text-align: center; color: var(--text-muted);">No proxies harvested yet. Click "⚡ Harvest Proxies" to populate.</td></tr>
                    </tbody>
                </table>
            </div>
        </div>
    </main>

    <!-- Settings Modal with PAC Export -->
    <div id="modalSettings" class="modal-overlay">
        <div class="modal-card">
            <div class="modal-header">
                <h2 style="font-size: 15px; color: var(--cyan);">PROXY CONFIGURATION & PAC RULES</h2>
                <button class="btn-cyber" onclick="closeModal('modalSettings')">✕</button>
            </div>
            <div style="display: flex; flex-direction: column; gap: 14px;">
                <div>
                    <label style="font-size: 12px;">HTTP/HTTPS Port:</label>
                    <input type="number" id="txtHttpPort" value="8080" class="select-dark" style="width: 100%;">
                </div>
                <div>
                    <label style="font-size: 12px;">SOCKS5 Port:</label>
                    <input type="number" id="txtSocksPort" value="1080" class="select-dark" style="width: 100%;">
                </div>
                <div style="border-top: 1px solid var(--card-border); padding-top: 14px;">
                    <div style="display: flex; justify-content: space-between; align-items: center;">
                        <span style="font-size: 12px; font-weight: 700; color: var(--cyan);">PROXY AUTO-CONFIG (PAC) SCRIPT</span>
                        <a href="/api/pac/export" download="proxy.pac" class="btn-cyber btn-cyber-primary" style="text-decoration: none;">💾 Export PAC File</a>
                    </div>
                    <p style="font-size: 11px; color: var(--text-muted); margin-top: 4px;">Export the compiled RFC-compliant JavaScript PAC file (proxy.pac) to configure any browser or OS.</p>
                </div>
                <button class="btn-cyber btn-cyber-primary" style="margin-top: 10px;" onclick="closeModal('modalSettings')">Done</button>
            </div>
        </div>
    </div>

    <!-- Accessibility Modal -->
    <div id="modalAccessibility" class="modal-overlay">
        <div class="modal-card">
            <div class="modal-header">
                <h2 style="font-size: 15px; color: var(--cyan);">ACCESSIBILITY & DISPLAY PREFERENCES</h2>
                <button class="btn-cyber" onclick="closeModal('modalAccessibility')">✕</button>
            </div>
            <div style="display: flex; flex-direction: column; gap: 14px;">
                <label style="display: flex; align-items: center; gap: 8px; font-size: 13px;">
                    <input type="checkbox" id="chkHighContrast" onchange="toggleHighContrast(this.checked)">
                    High Contrast Mode (WCAG 2.1 AA Compliance)
                </label>
                <label style="display: flex; align-items: center; gap: 8px; font-size: 13px;">
                    <input type="checkbox" id="chkReduceMotion" onchange="toggleReduceMotion(this.checked)">
                    Reduce Motion (Disable Pulse Animations & Smooth Graph)
                </label>
                <div>
                    <label style="font-size: 12px;">UI & Font Scaling:</label>
                    <select id="selScale" class="select-dark" style="width: 100%;" onchange="changeScale(this.value)">
                        <option value="1.0">100% (Standard Layout)</option>
                        <option value="1.15">115% (Comfortable Large)</option>
                        <option value="1.30">130% (High-DPI / Accessibility Focus)</option>
                    </select>
                </div>
                <button class="btn-cyber btn-cyber-primary" style="margin-top: 10px;" onclick="closeModal('modalAccessibility')">Apply & Close</button>
            </div>
        </div>
    </div>

    <!-- Custom Throttle Modal -->
    <div id="modalCustomThrottle" class="modal-overlay">
        <div class="modal-card">
            <div class="modal-header">
                <h2 style="font-size: 15px; color: var(--cyan);">CUSTOM SPEED LIMIT & BURST TOLERANCE</h2>
                <button class="btn-cyber" onclick="closeModal('modalCustomThrottle')">✕</button>
            </div>
            <div style="display: flex; flex-direction: column; gap: 14px;">
                <div>
                    <label style="font-size: 12px;">Custom Speed Limit (Mbps):</label>
                    <input type="number" id="txtCustomCap" value="350" min="1" max="500" class="select-dark" style="width: 100%;">
                </div>
                <div>
                    <label style="font-size: 12px;">Burst Buffer Window:</label>
                    <select id="selBurstWindow" class="select-dark" style="width: 100%;">
                        <option value="0.20">200 ms (Enhanced Burst / Maximum Bandwidth)</option>
                        <option value="0.10" selected>100 ms (Standard Balanced QoS)</option>
                        <option value="0.02">20 ms (Ultra-Smooth Pacing / Lowest Ping)</option>
                    </select>
                </div>
                <button class="btn-cyber btn-cyber-primary" style="margin-top: 10px;" onclick="applyCustomThrottle()">Apply Limit</button>
            </div>
        </div>
    </div>

    <!-- Speed Test Modal -->
    <div id="modalSpeedTest" class="modal-overlay">
        <div class="modal-card" style="max-width: 520px; width: 90%;">
            <div class="modal-header">
                <h2 style="font-size: 15px; color: var(--cyan);">MULTI-STREAM SPEED BENCHMARK</h2>
                <button class="btn-cyber" onclick="closeModal('modalSpeedTest')">✕</button>
            </div>
            <div style="text-align: center; padding: 20px 0;">
                <div style="font-size: 44px; font-weight: 800; font-family: Consolas;" id="testSpeedVal">0.0</div>
                <div style="font-size: 14px; color: var(--cyan); margin-top: 4px;">Mbps Aggregate Bonded Throughput</div>
                <div id="testExtraStats" style="display: none; margin-top: 10px; font-size: 13px; color: var(--text-main);">
                    <span>Latency: <strong id="testLatency" style="color: var(--cyan);">0</strong> ms</span> &nbsp;|&nbsp; 
                    <span>Bonded Boost: <strong id="testBoost" style="color: var(--neon-green);">+0%</strong></span>
                </div>
                <div id="testAdapterList" style="margin-top: 18px; text-align: left; display: flex; flex-direction: column; gap: 8px; max-height: 200px; overflow-y: auto;"></div>
                <p id="testStatus" style="font-size: 12px; color: var(--text-muted); margin-top: 14px;">Ready to run multi-stream speed test across all active adapters.</p>
                <button id="btnRunTest" class="btn-cyber btn-cyber-primary" style="margin-top: 18px; width: 100%;" onclick="runBenchmark()">Start Benchmark</button>
            </div>
        </div>
    </div>

    <!-- Proxy Repository Sources & URLs Modal -->
    <div id="modalProxySources" class="modal-overlay">
        <div class="modal-card" style="max-width: 650px; width: 90%;">
            <div class="modal-header">
                <h2 style="font-size: 15px; color: var(--cyan);">🌐 PROXY REPOSITORY SOURCES &amp; FEED URLS</h2>
                <button class="btn-cyber" onclick="closeModal('modalProxySources')">✕</button>
            </div>
            <div style="display: flex; flex-direction: column; gap: 12px; margin-top: 10px;">
                <p style="font-size: 11px; color: var(--text-muted); margin: 0;">
                    Bulk add or edit proxy repository and feed URLs (one URL per line). Lines starting with # or // are ignored.
                </p>
                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <span style="font-size: 12px; color: var(--cyan); font-weight: bold;" id="sourcesCountBadge">Active URLs: 0</span>
                    <div style="display: flex; gap: 6px;">
                        <button class="btn-cyber" style="font-size: 11px; padding: 4px 8px;" onclick="resetSourcesToDefault()">↺ Reset Defaults</button>
                        <button class="btn-cyber" style="font-size: 11px; padding: 4px 8px;" onclick="document.getElementById('txtProxySourcesArea').value = ''; updateSourcesCount();">🧹 Clear</button>
                    </div>
                </div>
                <textarea id="txtProxySourcesArea" class="select-dark" style="width: 100%; height: 260px; font-family: Consolas; font-size: 12px; line-height: 1.5; resize: vertical;" placeholder="https://raw.githubusercontent.com/...&#10;https://api.proxyscrape.com/..." oninput="updateSourcesCount()"></textarea>
                <div id="sourcesFeedback" style="font-size: 11px; color: var(--emerald); min-height: 16px;"></div>
                <div style="display: flex; justify-content: flex-end; gap: 8px; margin-top: 6px;">
                    <button class="btn-cyber" onclick="closeModal('modalProxySources')">Cancel</button>
                    <button class="btn-cyber btn-cyber-primary" onclick="saveProxySources()">💾 Save Sources</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Live Telemetry & UI Script -->
    <script>
        let currentTab = 'tabOverview';
        let rxHistory = new Array(60).fill(0);
        let txHistory = new Array(60).fill(0);
        let peakRx = 0, peakTx = 0;

        function switchTab(tabId) {
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
            event.target.classList.add('active');
            document.getElementById(tabId).classList.add('active');
            currentTab = tabId;
        }

        function closeModal(id) { document.getElementById(id).classList.remove('open'); }
        function openSettingsModal() { document.getElementById('modalSettings').classList.add('open'); }
        function openAccessibilityModal() { document.getElementById('modalAccessibility').classList.add('open'); }
        function openCustomThrottleModal() { document.getElementById('modalCustomThrottle').classList.add('open'); }
        function openSpeedTestModal() { document.getElementById('modalSpeedTest').classList.add('open'); }

        function toggleHighContrast(val) {
            document.body.classList.toggle('high-contrast', val);
            fetch('/api/accessibility', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ isHighContrast: val }) });
        }

        function toggleReduceMotion(val) {
            document.body.classList.toggle('reduce-motion', val);
            fetch('/api/accessibility', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ isReduceMotion: val }) });
        }

        function changeScale(val) {
            document.documentElement.style.setProperty('--ui-scale', val);
            fetch('/api/accessibility', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ uiScale: parseFloat(val) }) });
        }

        async function togglePower() {
            const res = await fetch('/api/power/toggle', { method: 'POST' });
            const data = await res.json();
            updatePowerButton(data.isActive);
        }

        function updatePowerButton(isActive) {
            const btn = document.getElementById('btnPower');
            const txt = document.getElementById('powerText');
            if (isActive) {
                btn.classList.remove('standby');
                txt.innerText = 'BONDING ACTIVE';
            } else {
                btn.classList.add('standby');
                txt.innerText = 'STANDBY';
            }
        }

        async function toggleBridge() {
            await fetch('/api/bridge/toggle', { method: 'POST' });
        }

        async function toggleSystemProxy() {
            await fetch('/api/proxy/toggle', { method: 'POST' });
        }

        async function changeShaperMode(mode) {
            await fetch('/api/shaper/mode', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ mode }) });
        }

        async function setSpeedLimit(mbps) {
            await fetch('/api/shaper/throttle', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ limitMbps: mbps }) });
        }

        async function applyCustomThrottle() {
            const cap = parseFloat(document.getElementById('txtCustomCap').value) || 0;
            const burst = parseFloat(document.getElementById('selBurstWindow').value) || 0.1;
            await fetch('/api/shaper/throttle', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ limitMbps: cap, burstTolerance: burst }) });
            closeModal('modalCustomThrottle');
        }

        async function toggleBorrowing(val) {
            await fetch('/api/shaper/borrowing', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ borrowing: val }) });
        }

        async function updateAdapter(id, isEnabled, weight) {
            await fetch('/api/adapters/update', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ id, isEnabled, weight }) });
        }

        async function rescanAdapters() {
            const btn = document.getElementById('btnRescanAdapters');
            if (btn) {
                btn.disabled = true;
                btn.innerText = '⟳ Rescanning...';
            }
            try {
                const res = await fetch('/api/adapters/rescan', { method: 'POST' });
                if (res.ok) {
                    const data = await res.json();
                    if (data.adapters) {
                        renderAdapters(data.adapters);
                        renderDistributionBar(data.adapters);
                    }
                }
            } catch (e) {
                console.error('Failed to rescan adapters:', e);
            } finally {
                if (btn) {
                    btn.disabled = false;
                    btn.innerText = '⟳ Rescan Adapters';
                }
            }
        }

        async function clearAudit() {
            await fetch('/api/audit/clear', { method: 'POST' });
            document.getElementById('auditRows').innerHTML = '';
        }

        async function runBenchmark() {
            const btn = document.getElementById('btnRunTest');
            const status = document.getElementById('testStatus');
            const val = document.getElementById('testSpeedVal');
            const extra = document.getElementById('testExtraStats');
            const list = document.getElementById('testAdapterList');
            btn.disabled = true;
            status.innerText = 'Measuring individual adapter & multi-stream bonded throughput...';
            val.innerText = '0.0';
            extra.style.display = 'none';
            list.innerHTML = '';

            let counter = 0;
            const timer = setInterval(() => {
                counter += 12.5;
                if (counter > 300) counter = 300;
                val.innerText = counter.toFixed(1);
            }, 80);

            try {
                const res = await fetch('/api/speedtest/start', { method: 'POST' });
                clearInterval(timer);
                if (!res.ok) throw new Error('Benchmark request failed (HTTP ' + res.status + ')');
                const data = await res.json();

                val.innerText = (data.bondedMbps || 0).toFixed(1);
                document.getElementById('testLatency').innerText = data.latencyMs || 0;
                document.getElementById('testBoost').innerText = '+' + (data.boostPercent || 0) + '%';
                extra.style.display = 'block';

                if (data.adapters && data.adapters.length > 0) {
                    list.innerHTML = data.adapters.map(a => `
                        <div style="background: rgba(255,255,255,0.03); border: 1px solid var(--card-border); padding: 8px 12px; border-radius: 6px; display: flex; justify-content: space-between; align-items: center; font-size: 12px;">
                            <span style="font-weight: 600; color: var(--text-main);">${a.Name}</span>
                            <span style="font-family: Consolas, monospace; color: var(--cyan);">${(a.DownloadMbps || 0).toFixed(1)} Mbps <span style="color: var(--text-muted); font-size: 11px;">(${a.LatencyMs}ms)</span></span>
                        </div>
                    `).join('');
                }
                status.innerText = 'Benchmark complete. Bonded aggregation achieved optimal wire speed.';
            } catch (err) {
                clearInterval(timer);
                status.innerText = 'Benchmark error: ' + err.message;
            } finally {
                btn.disabled = false;
            }
        }

        // Live telemetry polling loop
        async function pollTelemetry() {
            try {
                const res = await fetch('/api/telemetry');
                if (res.ok) {
                    const data = await res.json();
                    renderTelemetry(data);
                }
            } catch (e) { }
            setTimeout(pollTelemetry, 1000);
        }

        function renderTelemetry(data) {
            updatePowerButton(data.isEngineActive);

            const rxMbps = (data.totalDownloadKbps / 1000).toFixed(1);
            const txMbps = (data.totalUploadKbps / 1000).toFixed(1);

            document.getElementById('valRx').innerText = `${rxMbps} MB/s`;
            document.getElementById('valTx').innerText = `${txMbps} MB/s`;

            if (rxMbps > peakRx) peakRx = rxMbps;
            if (txMbps > peakTx) peakTx = txMbps;

            document.getElementById('peakRx').innerText = `Peak: ${peakRx} MB/s`;
            document.getElementById('peakTx').innerText = `Peak: ${peakTx} MB/s`;

            document.getElementById('valSockets').innerText = data.activeConnections;
            document.getElementById('valTransferred').innerText = `${(data.totalBytesDownloaded / (1024*1024)).toFixed(1)} MB`;

            // Waveform update
            rxHistory.shift();
            rxHistory.push(data.totalDownloadKbps);
            txHistory.shift();
            txHistory.push(data.totalUploadKbps);
            drawWaveform();

            // Adapters
            renderAdapters(data.adapters);

            // Distribution bar
            renderDistributionBar(data.adapters);

            // Audit
            if (data.auditLogs) renderAudit(data.auditLogs);
        }

        function renderAdapters(adapters) {
            if (!adapters) return;
            const container = document.getElementById('adapterCardsContainer');
            if (adapters.length === 0) {
                container.innerHTML = `
                    <div style="text-align: center; padding: 40px; color: var(--text-muted); background: var(--surface); border: 1px dashed var(--border); border-radius: 12px; grid-column: 1 / -1;">
                        <div style="font-size: 28px; margin-bottom: 8px;">⚠️</div>
                        <div style="font-size: 15px; font-weight: 700; color: #fff;">No Combinable Network Adapters Found</div>
                        <div style="font-size: 12px; margin-top: 4px; margin-bottom: 16px;">Connect your Wi-Fi, Ethernet, 4G/5G Cellular USB dongle, or mobile hotspot.</div>
                        <button class="btn-cyber" onclick="rescanAdapters()">⟳ Rescan All Adapters</button>
                    </div>`;
                const summaryEl = document.getElementById('adaptersSummaryText');
                if (summaryEl) summaryEl.innerText = '0 adapters detected. Click Rescan to detect active network adapters.';
                return;
            }

            const enabledCount = adapters.filter(a => a.isEnabled).length;
            const summaryEl = document.getElementById('adaptersSummaryText');
            if (summaryEl) summaryEl.innerText = `${adapters.length} adapters detected (${enabledCount} combinable/active).`;

            let html = '';

            adapters.forEach(a => {
                html += `
                    <div class="adapter-card">
                        <div class="adapter-top">
                            <div class="adapter-info">
                                <h3>${a.name}</h3>
                                <p>${a.ipAddress} | MAC: ${a.macAddress} | Link: ${a.linkSpeedMbps} Mbps</p>
                            </div>
                            <div class="adapter-metrics">
                                <span class="badge" style="border-color: ${a.accentColor}; color: ${a.accentColor}; font-weight: 700;">PING: ${a.pingMs}ms</span>
                                <span class="badge badge-cyan">POOL: ${a.effectivePercentage}%</span>
                                <label style="display: flex; align-items: center; gap: 6px; font-size: 12px; cursor: pointer;">
                                    <input type="checkbox" ${a.isEnabled ? 'checked' : ''} onchange="updateAdapter('${a.id}', this.checked, ${a.weight})">
                                    ACTIVE
                                </label>
                            </div>
                        </div>
                        <div class="slider-row">
                            <span style="font-size: 12px; font-weight: 700; width: 80px;">WEIGHT: ${a.weight}%</span>
                            <input type="range" min="0" max="100" value="${a.weight}" onchange="updateAdapter('${a.id}', ${a.isEnabled}, parseInt(this.value))">
                            <span style="font-size: 12px; font-family: Consolas;">Rx: ${(a.downloadSpeedKbps/1000).toFixed(1)} MB/s | Tx: ${(a.uploadSpeedKbps/1000).toFixed(1)} MB/s</span>
                        </div>
                    </div>
                `;
            });

            container.innerHTML = html;
        }

        function renderDistributionBar(adapters) {
            if (!adapters) return;
            const bar = document.getElementById('distBar');
            let html = '';
            adapters.filter(a => a.isEnabled).forEach(a => {
                html += `<div class="bar-segment" style="width: ${a.effectivePercentage}%; background: ${a.accentColor};"></div>`;
            });
            bar.innerHTML = html;
        }

        function renderAudit(logs) {
            const tbody = document.getElementById('auditRows');
            let html = '';
            logs.slice(0, 20).forEach(l => {
                html += `
                    <tr>
                        <td>${new Date(l.timestamp).toLocaleTimeString()}</td>
                        <td style="color: var(--cyan);">${l.protocol}</td>
                        <td>${l.targetHost}</td>
                        <td>${l.targetPort}</td>
                        <td>${l.outboundAdapter}</td>
                        <td style="color: var(--emerald);">${l.status}</td>
                    </tr>
                `;
            });
            tbody.innerHTML = html;
        }

        function drawWaveform() {
            const canvas = document.getElementById('waveformCanvas');
            const ctx = canvas.getContext('2d');
            const w = canvas.width = canvas.offsetWidth;
            const h = canvas.height = canvas.offsetHeight;

            ctx.clearRect(0, 0, w, h);

            // Draw grid
            ctx.strokeStyle = '#1E293B';
            ctx.lineWidth = 1;
            for (let y = 0; y < h; y += 40) {
                ctx.beginPath();
                ctx.moveTo(0, y);
                ctx.lineTo(w, y);
                ctx.stroke();
            }

            const maxVal = Math.max(100, ...rxHistory, ...txHistory);

            // Draw Rx (Cyan)
            ctx.strokeStyle = '#00F0FF';
            ctx.lineWidth = 2;
            ctx.beginPath();
            rxHistory.forEach((val, i) => {
                const x = (i / (rxHistory.length - 1)) * w;
                const y = h - (val / maxVal) * (h - 20) - 10;
                if (i === 0) ctx.moveTo(x, y);
                else ctx.lineTo(x, y);
            });
            ctx.stroke();

            // Draw Tx (Purple)
            ctx.strokeStyle = '#7928CA';
            ctx.lineWidth = 2;
            ctx.beginPath();
            txHistory.forEach((val, i) => {
                const x = (i / (txHistory.length - 1)) * w;
                const y = h - (val / maxVal) * (h - 20) - 10;
                if (i === 0) ctx.moveTo(x, y);
                else ctx.lineTo(x, y);
            });
            ctx.stroke();
        }

        let rawPoolData = [];
        let currentSortField = 'latency';
        let currentSortDirection = 'asc';
        let knownSources = new Set();

        async function fetchRotator() {
            try {
                const res = await fetch('/api/rotating-proxy/status');
                const st = await res.json();
                document.getElementById('rotatorPoolCount').innerText = st.poolCount;
                document.getElementById('rotatorHealthyCount').innerText = st.healthyCount;
                document.getElementById('rotatorEndpoint').innerText = '127.0.0.1:' + st.port;
                document.getElementById('rotatorStrategy').innerText = st.strategy;
                document.getElementById('rotatorStatusPill').innerText = st.isRunning ? 'ONLINE (ACTIVE)' : 'STOPPED';
                document.getElementById('rotatorStatusPill').style.color = st.isRunning ? 'var(--emerald)' : 'var(--text-muted)';
                document.getElementById('btnToggleRotator').innerText = st.isRunning ? '🛑 Stop Rotator' : '▶ Start Rotator';

                if (document.getElementById('rotatorCountryFilter') && !document.getElementById('rotatorCountryFilter').matches(':focus')) {
                    document.getElementById('rotatorCountryFilter').value = st.countryFilter || 'ALL';
                    document.getElementById('chkAutoHarvest').checked = !!st.autoHarvestEnabled;
                    document.getElementById('chkAutoValidate').checked = !!st.autoValidateEnabled;
                    document.getElementById('chkChainNic').checked = !!st.chainPhysicalAdapters;
                    if (document.getElementById('chkBondRotate')) document.getElementById('chkBondRotate').checked = !!st.routeBondedTrafficThroughRotator;
                    if (document.getElementById('rotatorIntervalSelect')) document.getElementById('rotatorIntervalSelect').value = st.rotationIntervalSeconds || 30;
                }

                if (document.getElementById('selectionSummaryPill')) {
                    document.getElementById('selectionSummaryPill').innerText = 'Selected: ' + (st.selectedCount || 0) + ' healthy';
                }
                if (document.getElementById('rotationCountdownBadge')) {
                    document.getElementById('rotationCountdownBadge').innerText = 'Next rotation: ' + (st.secondsUntilNextRotation || 30) + 's';
                }

                if (currentTab === 'tabRotator') {
                    const poolRes = await fetch('/api/rotating-proxy/pool');
                    const pool = await poolRes.json();
                    if (pool) {
                        rawPoolData = pool;
                        updateSourceDropdownOptions(pool);
                        applyTableFilters();
                    }
                }
            } catch(e) {}
        }

        function updateSourceDropdownOptions(pool) {
            const select = document.getElementById('filterSource');
            if (!select) return;
            let added = false;
            for (const p of pool) {
                const s = p.SourceUrl || 'Open-Source';
                if (!knownSources.has(s)) {
                    knownSources.add(s);
                    added = true;
                }
            }
            if (added) {
                const curVal = select.value || 'ALL';
                let html = '<option value="ALL">All Sources</option>';
                Array.from(knownSources).sort().forEach(src => {
                    let label = src;
                    try {
                        const u = new URL(src);
                        const host = u.hostname.replace(/^www\./, '');
                        const lastSeg = u.pathname.split('/').filter(Boolean).pop();
                        label = lastSeg ? `${host} (${lastSeg})` : host;
                    } catch(e) {
                        if (src.length > 32) label = src.substring(0, 32) + '...';
                    }
                    html += `<option value="${src}">${label}</option>`;
                });
                select.innerHTML = html;
                select.value = curVal;
            }
        }

        function toggleTableSort(field) {
            if (currentSortField === field) {
                currentSortDirection = currentSortDirection === 'asc' ? 'desc' : 'asc';
            } else {
                currentSortField = field;
                currentSortDirection = 'asc';
            }
            const sortSelect = document.getElementById('filterSort');
            if (sortSelect) {
                if (field === 'latency') sortSelect.value = currentSortDirection === 'asc' ? 'latency_asc' : 'latency_desc';
                else if (field === 'status') sortSelect.value = 'health_first';
                else if (field === 'source') sortSelect.value = 'source_asc';
                else if (field === 'geo') sortSelect.value = 'geo_asc';
            }
            applyTableFilters();
        }

        function applyTableFilters() {
            if (!rawPoolData || rawPoolData.length === 0) {
                document.getElementById('rotatorRows').innerHTML = '<tr><td colspan="8" style="text-align: center; color: var(--text-muted); padding: 16px;">No proxies harvested yet. Click "⚡ Harvest Proxies" to populate.</td></tr>';
                const badge = document.getElementById('filterCountBadge');
                if (badge) badge.innerText = 'Showing 0 proxies';
                return;
            }

            const searchQ = (document.getElementById('filterSearch')?.value || '').toLowerCase().trim();
            const latencyMax = parseInt(document.getElementById('filterLatency')?.value || '0', 10);
            const sourceSel = document.getElementById('filterSource')?.value || 'ALL';
            const protoSel = (document.getElementById('filterProtocol')?.value || 'ALL').toUpperCase();
            const sortSel = document.getElementById('filterSort')?.value || 'latency_asc';

            if (sortSel === 'latency_asc') { currentSortField = 'latency'; currentSortDirection = 'asc'; }
            else if (sortSel === 'latency_desc') { currentSortField = 'latency'; currentSortDirection = 'desc'; }
            else if (sortSel === 'health_first') { currentSortField = 'status'; currentSortDirection = 'asc'; }
            else if (sortSel === 'source_asc') { currentSortField = 'source'; currentSortDirection = 'asc'; }
            else if (sortSel === 'geo_asc') { currentSortField = 'geo'; currentSortDirection = 'asc'; }

            try {
                if (document.getElementById('filterLatency')) localStorage.setItem('combispeed_filter_latency', document.getElementById('filterLatency').value);
                if (document.getElementById('filterProtocol')) localStorage.setItem('combispeed_filter_proto', document.getElementById('filterProtocol').value);
                if (document.getElementById('filterSort')) localStorage.setItem('combispeed_filter_sort', document.getElementById('filterSort').value);
                if (document.getElementById('filterSearch')) localStorage.setItem('combispeed_filter_search', document.getElementById('filterSearch').value);
            } catch(e) {}

            let filtered = rawPoolData.filter(p => {
                if (searchQ) {
                    const matchEndpoint = (p.Endpoint || '').toLowerCase().includes(searchQ);
                    const matchGeo = (p.CountryCode || '').toLowerCase().includes(searchQ);
                    const matchSrc = (p.SourceUrl || '').toLowerCase().includes(searchQ);
                    const matchProto = (p.FormattedProtocol || '').toLowerCase().includes(searchQ);
                    if (!matchEndpoint && !matchGeo && !matchSrc && !matchProto) return false;
                }
                if (latencyMax > 0) {
                    if (p.LatencyMs <= 0 || p.LatencyMs > latencyMax) return false;
                }
                if (sourceSel !== 'ALL') {
                    if ((p.SourceUrl || 'Open-Source') !== sourceSel) return false;
                }
                if (protoSel !== 'ALL') {
                    if (!(p.FormattedProtocol || '').toUpperCase().includes(protoSel)) return false;
                }
                return true;
            });

            filtered.sort((a, b) => {
                let res = 0;
                switch (currentSortField) {
                    case 'latency':
                        const latA = a.LatencyMs > 0 ? a.LatencyMs : 999999;
                        const latB = b.LatencyMs > 0 ? b.LatencyMs : 999999;
                        res = latA - latB;
                        break;
                    case 'status':
                        res = (b.IsHealthy ? 1 : 0) - (a.IsHealthy ? 1 : 0);
                        if (res === 0) res = (a.LatencyMs || 999999) - (b.LatencyMs || 999999);
                        break;
                    case 'geo':
                        res = (a.CountryCode || '').localeCompare(b.CountryCode || '');
                        break;
                    case 'endpoint':
                        res = (a.Endpoint || '').localeCompare(b.Endpoint || '');
                        break;
                    case 'protocol':
                        res = (a.FormattedProtocol || '').localeCompare(b.FormattedProtocol || '');
                        break;
                    case 'source':
                        res = (a.SourceUrl || '').localeCompare(b.SourceUrl || '');
                        break;
                    default:
                        res = (a.LatencyMs || 999999) - (b.LatencyMs || 999999);
                        break;
                }
                return currentSortDirection === 'desc' ? -res : res;
            });

            const badge = document.getElementById('filterCountBadge');
            if (badge) {
                badge.innerText = `Showing ${filtered.length} of ${rawPoolData.length} proxies`;
            }

            if (filtered.length === 0) {
                document.getElementById('rotatorRows').innerHTML = '<tr><td colspan="8" style="text-align: center; color: var(--text-muted); padding: 18px;">No proxies match the selected filters.</td></tr>';
                return;
            }

            document.getElementById('rotatorRows').innerHTML = filtered.map(p => `
                <tr>
                    <td style="text-align: center;"><input type="checkbox" ${p.IsSelected ? 'checked' : ''} onchange="toggleSelectProxy('${p.Endpoint}')" title="Include ${p.Endpoint} in rotation pool"></td>
                    <td style="text-align: center;"><button class="btn-cyber" style="padding: 2px 6px; font-size: 10px;" onclick="forceConnectSelectedOrNext('${p.Endpoint}')" title="Forcefully connect to this specific proxy server">Connect</button></td>
                    <td><span style="padding: 2px 6px; border-radius: 4px; font-size: 10px; font-weight: bold; background: ${p.IsHealthy ? 'var(--emerald)' : 'var(--red)'}; color: #000;" title="${p.IsHealthy ? 'Healthy / Live' : 'Failed'}">${p.IsHealthy ? p.LatencyMs + 'ms' : 'FAIL'}</span></td>
                    <td style="font-weight: bold; color: var(--cyan);">${p.CountryCode || 'UN'}</td>
                    <td style="font-family: Consolas;">${p.Endpoint}</td>
                    <td>${p.FormattedProtocol}</td>
                    <td>${p.FormattedAnonymity}</td>
                    <td>${p.LatencyMs} ms</td>
                    <td>${p.Failures}</td>
                    <td style="font-size: 10px; color: var(--text-muted); max-width: 220px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;" title="${p.SourceUrl || 'Open-Source'}">${p.SourceUrl || 'Open-Source'}</td>
                </tr>
            `).join('');
        }

        async function updateRotatorConfig() {
            const countryFilter = document.getElementById('rotatorCountryFilter').value;
            const autoHarvest = document.getElementById('chkAutoHarvest').checked;
            const autoValidate = document.getElementById('chkAutoValidate').checked;
            const chainPhysicalAdapters = document.getElementById('chkChainNic').checked;
            const routeBondedTrafficThroughRotator = document.getElementById('chkBondRotate')?.checked || false;
            const rotationIntervalSeconds = parseInt(document.getElementById('rotatorIntervalSelect')?.value || '30', 10);
            await fetch('/api/rotating-proxy/config', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ countryFilter, autoHarvest, autoValidate, chainPhysicalAdapters, routeBondedTrafficThroughRotator, rotationIntervalSeconds })
            });
            fetchRotator();
        }

        async function selectAllProxies() {
            await fetch('/api/rotating-proxy/select-all', { method: 'POST' });
            fetchRotator();
        }

        async function deselectAllProxies() {
            await fetch('/api/rotating-proxy/deselect-all', { method: 'POST' });
            fetchRotator();
        }

        async function selectFastProxies() {
            await fetch('/api/rotating-proxy/select-fast', { method: 'POST' });
            fetchRotator();
        }

        async function toggleSelectProxy(endpoint) {
            await fetch('/api/rotating-proxy/toggle-select', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ endpoint })
            });
            fetchRotator();
        }

        async function forceRotateNow() {
            const res = await fetch('/api/rotating-proxy/rotate-now', { method: 'POST' });
            const data = await res.json();
            alert(data.status || `Rotated to: ${data.active || 'Next proxy'} (${data.country || 'Global'})`);
            fetchRotator();
        }

        async function forceConnectSelectedOrNext(endpoint = null) {
            const body = endpoint ? JSON.stringify({ endpoint }) : "{}";
            const res = await fetch('/api/rotating-proxy/force-connect', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: body
            });
            const data = await res.json();
            alert(data.status || (data.success ? 'Connected!' : 'Connection failed'));
            fetchRotator();
        }

        async function fetchGeoIp(force = false) {
            try {
                const res = await fetch('/api/geoip');
                const data = await res.json();
                if (data) {
                    if (document.getElementById('geoFlagBox')) document.getElementById('geoFlagBox').innerText = data.FlagEmoji || '🌐';
                    if (document.getElementById('geoLocText')) document.getElementById('geoLocText').innerText = data.LocationFormatted || `${data.City}, ${data.Country}`;
                    if (document.getElementById('geoWanIp')) document.getElementById('geoWanIp').innerText = data.InternetIp || 'Unknown';
                    if (document.getElementById('geoIntranetIp')) document.getElementById('geoIntranetIp').innerText = data.IntranetIp || '127.0.0.1';
                    if (document.getElementById('geoIsp')) document.getElementById('geoIsp').innerText = data.Isp || 'Unknown';
                }
            } catch(e) {}
        }

        async function triggerHarvest() {
            await fetch('/api/rotating-proxy/harvest', { method: 'POST' });
            alert('Harvesting initiated! Fresh candidate proxies are being scraped from open-source feeds.');
            fetchRotator();
        }

        async function triggerValidate() {
            await fetch('/api/rotating-proxy/validate', { method: 'POST' });
            alert('Validation initiated! Testing health and latency in parallel.');
            fetchRotator();
        }

        async function toggleRotator() {
            await fetch('/api/rotating-proxy/toggle', { method: 'POST' });
            fetchRotator();
        }

        async function openSourcesModal() {
            try {
                const res = await fetch('/api/rotating-proxy/sources');
                const data = await res.json();
                document.getElementById('txtProxySourcesArea').value = data.rawText || '';
                updateSourcesCount();
                document.getElementById('sourcesFeedback').innerText = '';
                document.getElementById('modalProxySources').classList.add('open');
            } catch (err) {
                alert('Failed to load proxy sources: ' + err.message);
            }
        }

        function updateSourcesCount() {
            const text = document.getElementById('txtProxySourcesArea').value || '';
            const lines = text.split('\n').map(l => l.trim()).filter(l => l && !l.startsWith('#') && !l.startsWith('//') && (l.startsWith('http://') || l.startsWith('https://')));
            document.getElementById('sourcesCountBadge').innerText = 'Active URLs: ' + lines.length;
        }

        async function resetSourcesToDefault() {
            try {
                const res = await fetch('/api/rotating-proxy/sources/reset', { method: 'POST' });
                const data = await res.json();
                document.getElementById('txtProxySourcesArea').value = (data.sources || []).join('\n');
                updateSourcesCount();
                document.getElementById('sourcesFeedback').innerText = 'Restored ' + data.count + ' default repository URLs.';
            } catch (err) {
                document.getElementById('sourcesFeedback').innerText = 'Reset error: ' + err.message;
            }
        }

        async function saveProxySources() {
            const rawText = document.getElementById('txtProxySourcesArea').value || '';
            try {
                const res = await fetch('/api/rotating-proxy/sources', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ rawText })
                });
                const data = await res.json();
                document.getElementById('sourcesFeedback').innerText = 'Saved ' + data.count + ' repository URLs successfully!';
                setTimeout(() => { closeModal('modalProxySources'); }, 800);
            } catch (err) {
                document.getElementById('sourcesFeedback').innerText = 'Save error: ' + err.message;
            }
        }

        window.onload = () => {
            try {
                const savedLat = localStorage.getItem('combispeed_filter_latency');
                if (savedLat && document.getElementById('filterLatency')) document.getElementById('filterLatency').value = savedLat;
                const savedProto = localStorage.getItem('combispeed_filter_proto');
                if (savedProto && document.getElementById('filterProtocol')) document.getElementById('filterProtocol').value = savedProto;
                const savedSort = localStorage.getItem('combispeed_filter_sort');
                if (savedSort && document.getElementById('filterSort')) document.getElementById('filterSort').value = savedSort;
                const savedSearch = localStorage.getItem('combispeed_filter_search');
                if (savedSearch && document.getElementById('filterSearch')) document.getElementById('filterSearch').value = savedSearch;
            } catch(e) {}
            pollTelemetry();
            fetchRotator();
            fetchGeoIp();
            setInterval(fetchRotator, 3000);
            drawWaveform();
        };
    </script>
</body>
</html>
""";
    }
}

