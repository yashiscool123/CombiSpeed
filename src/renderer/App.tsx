import React, { useState, useEffect } from 'react';
import { TitleBar } from './components/TitleBar';
import { MasterSwitch } from './components/MasterSwitch';
import { TelemetryStats } from './components/TelemetryStats';
import { ThroughputChart } from './components/ThroughputChart';
import { AdapterCard } from './components/AdapterCard';
import { AuditLogTable } from './components/AuditLogTable';
import { SpeedTestModal } from './components/SpeedTestModal';
import { SettingsModal } from './components/SettingsModal';
import { NetworkAdapter, ProxyStatus, TelemetryState, ProxyConfig, AuditLog } from '../shared/types';
import { Network, Sparkles } from 'lucide-react';

export default function App() {
  const [adapters, setAdapters] = useState<NetworkAdapter[]>([]);
  const [proxyStatus, setProxyStatus] = useState<ProxyStatus>({
    running: false,
    httpPort: 8080,
    socksPort: 1080,
    activeConnections: 0,
    totalConnections: 0,
    windowsProxyActive: false
  });
  const [telemetry, setTelemetry] = useState<TelemetryState>({
    totalDownloadSpeedBps: 0,
    totalUploadSpeedBps: 0,
    totalDownloadBytes: 0,
    totalUploadBytes: 0,
    history: [],
    adapters: {}
  });
  const [config, setConfig] = useState<ProxyConfig>({
    httpPort: 8080,
    socksPort: 1080,
    routingMode: 'weighted',
    enableWindowsProxy: false
  });
  const [auditLogs, setAuditLogs] = useState<AuditLog[]>([]);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isSpeedTestOpen, setIsSpeedTestOpen] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);

  useEffect(() => {
    // Initial load
    if (window.combispeed) {
      window.combispeed.getAdapters().then(setAdapters);
      window.combispeed.getProxyStatus().then(setProxyStatus);
      window.combispeed.getTelemetry().then(setTelemetry);
      window.combispeed.getAuditLogs().then(setAuditLogs);

      const unsubAdapters = window.combispeed.onAdaptersUpdated((updated) => {
        setAdapters(updated);
      });

      const unsubTelemetry = window.combispeed.onTelemetryUpdated((data) => {
        setTelemetry(data);
      });

      const unsubStatus = window.combispeed.onProxyStatusUpdated((status) => {
        setProxyStatus(status);
      });

      const unsubLogs = window.combispeed.onAuditLog((log) => {
        setAuditLogs((prev) => [log, ...prev].slice(0, 100));
      });

      return () => {
        unsubAdapters();
        unsubTelemetry();
        unsubStatus();
        unsubLogs();
      };
    }
  }, []);

  const handleTogglePower = async () => {
    if (!window.combispeed) return;
    if (proxyStatus.running) {
      const status = await window.combispeed.stopProxy();
      setProxyStatus(status);
    } else {
      const status = await window.combispeed.startProxy(config);
      setProxyStatus(status);
    }
  };

  const handleToggleWindowsProxy = async (enabled: boolean) => {
    if (!window.combispeed) return;
    const ok = await window.combispeed.setWindowsProxy(enabled);
    setProxyStatus((prev) => ({ ...prev, windowsProxyActive: ok ? enabled : prev.windowsProxyActive }));
  };

  const handleWeightChange = (id: string, weight: number) => {
    setAdapters((prev) => {
      const updated = prev.map((a) => (a.id === id ? { ...a, weight } : a));
      const weightMap: Record<string, number> = {};
      updated.forEach((a) => (weightMap[a.id] = a.weight));
      window.combispeed?.updateWeights(weightMap);
      return updated;
    });
  };

  const handleToggleEnabled = (id: string, enabled: boolean) => {
    setAdapters((prev) => {
      const updated = prev.map((a) => (a.id === id ? { ...a, enabled } : a));
      window.combispeed?.toggleAdapter(id, enabled);
      return updated;
    });
  };

  const handleRefreshAdapters = async () => {
    setIsRefreshing(true);
    try {
      const fresh = await window.combispeed?.refreshAdapters();
      if (fresh) setAdapters(fresh);
    } finally {
      setIsRefreshing(false);
    }
  };

  const handleSaveConfig = (newConfig: ProxyConfig) => {
    setConfig(newConfig);
    if (proxyStatus.running) {
      // Restart proxy with new config
      window.combispeed?.stopProxy().then(() => {
        window.combispeed?.startProxy(newConfig).then(setProxyStatus);
      });
    }
  };

  const activeAdaptersCount = adapters.filter((a) => a.enabled && a.status === 'up').length;

  return (
    <div className="h-screen w-screen flex flex-col bg-[#080c14] text-slate-100 overflow-hidden font-sans">
      {/* Sleek Custom Titlebar */}
      <TitleBar proxyStatus={proxyStatus} />

      {/* Main Scrollable Content */}
      <div className="flex-1 overflow-y-auto px-6 py-5 space-y-6">
        {/* Master Power & Quick Controls */}
        <MasterSwitch
          status={proxyStatus}
          config={config}
          onTogglePower={handleTogglePower}
          onToggleWindowsProxy={handleToggleWindowsProxy}
          onOpenSettings={() => setIsSettingsOpen(true)}
          onOpenSpeedTest={() => setIsSpeedTestOpen(true)}
          onRefreshAdapters={handleRefreshAdapters}
          isRefreshing={isRefreshing}
        />

        {/* Real-Time Telemetry Stats */}
        <TelemetryStats telemetry={telemetry} status={proxyStatus} />

        {/* Real-Time 60-Second Bandwidth Waveform Canvas */}
        <ThroughputChart telemetry={telemetry} adapters={adapters} />

        {/* Adapters Section */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-2">
              <Network className="w-4 h-4 text-cyan-400" />
              <h3 className="text-sm font-bold text-white uppercase tracking-wider">
                Active Network Adapters
              </h3>
              <span className="text-[11px] font-mono text-cyan-400 bg-cyan-950/40 border border-cyan-800/40 px-2 py-0.5 rounded">
                {activeAdaptersCount} / {adapters.length} Online
              </span>
            </div>

            <div className="flex items-center space-x-2 text-xs text-slate-400">
              <Sparkles className="w-3.5 h-3.5 text-emerald-400" />
              <span>Drag sliders to adjust load ratios or click presets</span>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {adapters.map((adapter) => (
              <AdapterCard
                key={adapter.id}
                adapter={adapter}
                onWeightChange={handleWeightChange}
                onToggleEnabled={handleToggleEnabled}
              />
            ))}
          </div>
        </div>

        {/* Connection Audit Stream */}
        <AuditLogTable logs={auditLogs} onClearLogs={() => setAuditLogs([])} />
      </div>

      {/* Modals */}
      <SpeedTestModal
        isOpen={isSpeedTestOpen}
        onClose={() => setIsSpeedTestOpen(false)}
      />

      <SettingsModal
        isOpen={isSettingsOpen}
        onClose={() => setIsSettingsOpen(false)}
        config={config}
        adapters={adapters}
        onSaveConfig={handleSaveConfig}
      />
    </div>
  );
}
