import React from 'react';
import { Power, Shield, Settings, Gauge, RefreshCw, Layers } from 'lucide-react';
import { ProxyStatus, ProxyConfig } from '../../shared/types';

interface MasterSwitchProps {
  status: ProxyStatus;
  config: ProxyConfig;
  onTogglePower: () => void;
  onToggleWindowsProxy: (enabled: boolean) => void;
  onOpenSettings: () => void;
  onOpenSpeedTest: () => void;
  onRefreshAdapters: () => void;
  isRefreshing: boolean;
}

export const MasterSwitch: React.FC<MasterSwitchProps> = ({
  status,
  config,
  onTogglePower,
  onToggleWindowsProxy,
  onOpenSettings,
  onOpenSpeedTest,
  onRefreshAdapters,
  isRefreshing
}) => {
  return (
    <div className="glass-panel rounded-2xl p-6 relative overflow-hidden border border-slate-800/80">
      {/* Background ambient lighting */}
      <div
        className={`absolute -right-20 -top-20 w-72 h-72 rounded-full blur-3xl pointer-events-none transition-opacity duration-700 ${
          status.running ? 'bg-cyan-500/15' : 'bg-transparent'
        }`}
      />
      <div
        className={`absolute -left-20 -bottom-20 w-72 h-72 rounded-full blur-3xl pointer-events-none transition-opacity duration-700 ${
          status.running ? 'bg-emerald-500/15' : 'bg-transparent'
        }`}
      />

      <div className="relative flex flex-col md:flex-row items-center justify-between gap-6 z-10">
        {/* Left: Master Toggle Button & State */}
        <div className="flex items-center space-x-6">
          <button
            onClick={onTogglePower}
            className={`group relative w-20 h-20 rounded-2xl flex items-center justify-center transition-all duration-300 ${
              status.running
                ? 'bg-gradient-to-tr from-cyan-500 to-emerald-400 text-black shadow-xl shadow-cyan-500/30 scale-105 neon-border-cyan'
                : 'bg-slate-800/80 text-slate-400 hover:text-white hover:bg-slate-700/80 border border-slate-700/60'
            }`}
          >
            <Power
              className={`w-9 h-9 transition-transform duration-300 group-hover:scale-110 ${
                status.running ? 'stroke-[2.5]' : 'stroke-2'
              }`}
            />
            {status.running && (
              <span className="absolute -top-1 -right-1 w-4 h-4 bg-emerald-400 rounded-full border-2 border-[#0f172a] animate-ping" />
            )}
          </button>

          <div>
            <div className="flex items-center space-x-2">
              <span className="text-xs font-semibold uppercase tracking-wider text-cyan-400 flex items-center gap-1.5">
                <Layers className="w-3.5 h-3.5" />
                Multi-WAN Channel Bonding
              </span>
              <span className="px-2 py-0.5 rounded text-[10px] font-mono uppercase bg-slate-800 text-slate-300 border border-slate-700">
                Mode: {config.routingMode}
              </span>
            </div>

            <h2 className="text-2xl font-extrabold text-white tracking-tight mt-0.5">
              {status.running ? 'Bandwidth Aggregation Active' : 'Aggregation Engine Idle'}
            </h2>

            <p className="text-xs text-slate-400 mt-1 max-w-md">
              {status.running
                ? `Multiplexing concurrent connections over all active network adapters via HTTP :${status.httpPort} & SOCKS5 :${status.socksPort}.`
                : 'Power on to bind and combine all active network interfaces for accelerated throughput and failover.'}
            </p>
          </div>
        </div>

        {/* Right: Quick Controls & Actions */}
        <div className="flex flex-wrap items-center gap-3 w-full md:w-auto justify-end">
          {/* Windows System Proxy Toggle */}
          <button
            onClick={() => onToggleWindowsProxy(!status.windowsProxyActive)}
            className={`flex items-center space-x-2 px-3.5 py-2.5 rounded-xl border text-xs font-semibold transition-all ${
              status.windowsProxyActive
                ? 'bg-emerald-950/50 border-emerald-500/50 text-emerald-300 shadow-lg shadow-emerald-950/40'
                : 'bg-slate-800/60 border-slate-700/60 text-slate-300 hover:bg-slate-800 hover:text-white'
            }`}
            title="Automatically route all Windows browsers and applications through CombiSpeed"
          >
            <Shield
              className={`w-4 h-4 ${
                status.windowsProxyActive ? 'text-emerald-400 fill-emerald-400/20' : 'text-slate-400'
              }`}
            />
            <span>Win System Proxy: {status.windowsProxyActive ? 'ON' : 'OFF'}</span>
          </button>

          {/* Speed Benchmark Button */}
          <button
            onClick={onOpenSpeedTest}
            className="flex items-center space-x-1.5 px-3.5 py-2.5 rounded-xl bg-slate-800/60 border border-slate-700/60 text-slate-300 hover:text-cyan-400 hover:border-cyan-500/50 hover:bg-slate-800 transition-all text-xs font-semibold"
          >
            <Gauge className="w-4 h-4 text-cyan-400" />
            <span>Benchmark Speed</span>
          </button>

          {/* Rescan Adapters */}
          <button
            onClick={onRefreshAdapters}
            disabled={isRefreshing}
            className="p-2.5 rounded-xl bg-slate-800/60 border border-slate-700/60 text-slate-300 hover:text-white hover:bg-slate-800 transition-all text-xs"
            title="Rescan network adapters"
          >
            <RefreshCw className={`w-4 h-4 ${isRefreshing ? 'animate-spin text-cyan-400' : ''}`} />
          </button>

          {/* Settings */}
          <button
            onClick={onOpenSettings}
            className="p-2.5 rounded-xl bg-slate-800/60 border border-slate-700/60 text-slate-300 hover:text-white hover:bg-slate-800 transition-all text-xs"
            title="Proxy & Bonding Settings"
          >
            <Settings className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  );
};
