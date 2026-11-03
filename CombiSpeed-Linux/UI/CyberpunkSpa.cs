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
                        <button class="btn-cyber btn-cyber-primary" id="btnToggleRotator" onclick="toggleRotator()">▶ Start Rotator</button>
                        <button class="btn-cyber" onclick="triggerHarvest()">⚡ Harvest Proxies</button>
                        <button class="btn-cyber" onclick="triggerValidate()">🔍 Validate Pool</button>
                    </div>
                </div>
                <div class="metrics-grid" style="grid-template-columns: repeat(4, 1fr); margin-bottom: 14px;">
                    <div class="metric-card cyan">
                        <div class="metric-title">Total Candidates</div>
                        <div class="metric-value" id="rotatorPoolCount">0</div>
                        <div class="metric-sub">In database</div>
                    </div>
                    <div class="metric-card emerald">
                        <div class="metric-title">Verified Active</div>
                        <div class="metric-value" id="rotatorHealthyCount">0</div>
                        <div class="metric-sub">Ready to rotate</div>
                    </div>
                    <div class="metric-card purple">
                        <div class="metric-title">Rotator Endpoint</div>
                        <div class="metric-value" style="font-size: 18px;" id="rotatorEndpoint">127.0.0.1:8081</div>
                        <div class="metric-sub" id="rotatorStatusPill">STOPPED</div>
                    </div>
                    <div class="metric-card amber">
                        <div class="metric-title">Rotation Strategy</div>
                        <div class="metric-value" style="font-size: 18px;" id="rotatorStrategy">Round Robin</div>
                        <div class="metric-sub">Multi-NIC Bound</div>
                    </div>
                </div>
                <table class="audit-table">
                    <thead>
                        <tr>
                            <th>STATUS</th>
                            <th>ENDPOINT</th>
                            <th>PROTOCOL</th>
                            <th>ANONYMITY</th>
                            <th>LATENCY</th>
                            <th>FAILURES</th>
                            <th>SOURCE</th>
                        </tr>
                    </thead>
                    <tbody id="rotatorRows">
                        <tr><td colspan="7" style="text-align: center; color: var(--text-muted);">No proxies harvested yet. Click "⚡ Harvest Proxies" to populate.</td></tr>
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

                if (currentTab === 'tabRotator') {
                    const poolRes = await fetch('/api/rotating-proxy/pool');
                    const pool = await poolRes.json();
                    if (pool && pool.length > 0) {
                        document.getElementById('rotatorRows').innerHTML = pool.map(p => `
                            <tr>
                                <td><span style="padding: 2px 6px; border-radius: 4px; font-size: 10px; font-weight: bold; background: ${p.IsHealthy ? 'var(--emerald)' : 'var(--red)'}; color: #000;">${p.IsHealthy ? p.LatencyMs + 'ms' : 'FAIL'}</span></td>
                                <td style="font-family: Consolas;">${p.Endpoint}</td>
                                <td>${p.FormattedProtocol}</td>
                                <td>${p.FormattedAnonymity}</td>
                                <td>${p.LatencyMs} ms</td>
                                <td>${p.Failures}</td>
                                <td style="font-size: 10px; color: var(--text-muted);">${p.SourceUrl || 'Open-Source'}</td>
                            </tr>
                        `).join('');
                    }
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

        window.onload = () => {
            pollTelemetry();
            fetchRotator();
            setInterval(fetchRotator, 3000);
            drawWaveform();
        };
    </script>
</body>
</html>
""";
    }
}

