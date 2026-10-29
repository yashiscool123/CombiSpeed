import React from 'react';
import { Wifi, Network, Smartphone, Share2, Globe, Radio, ArrowDown, ArrowUp } from 'lucide-react';
import { NetworkAdapter } from '../../shared/types';
import { formatSpeed, getPingColor } from '../utils/formatters';

interface AdapterCardProps {
  adapter: NetworkAdapter;
  onWeightChange: (id: string, weight: number) => void;
  onToggleEnabled: (id: string, enabled: boolean) => void;
  isReadOnly?: boolean;
}

export const AdapterCard: React.FC<AdapterCardProps> = ({
  adapter,
  onWeightChange,
  onToggleEnabled,
  isReadOnly = false
}) => {
  const getIcon = () => {
    switch (adapter.type) {
      case 'wifi':
        return <Wifi className="w-5 h-5 text-cyan-400" />;
      case 'cellular':
        return <Smartphone className="w-5 h-5 text-emerald-400" />;
      case 'bridge':
        return <Share2 className="w-5 h-5 text-purple-400" />;
      case 'ethernet':
      default:
        return <Network className="w-5 h-5 text-blue-400" />;
    }
  };

  const rxSpeed = formatSpeed(adapter.downloadSpeedBps);
  const txSpeed = formatSpeed(adapter.uploadSpeedBps);

  return (
    <div
      className={`glass-panel rounded-2xl p-5 border transition-all duration-300 relative overflow-hidden ${
        adapter.enabled
          ? 'border-slate-800/90 hover:border-cyan-500/40 bg-slate-900/60'
          : 'border-slate-800/40 opacity-60 bg-slate-950/40'
      }`}
    >
      {/* Top row: Icon, Name, Type, Enable Switch */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center space-x-3.5">
          <div className="w-11 h-11 rounded-xl bg-slate-800/80 border border-slate-700/60 flex items-center justify-center shadow-inner">
            {getIcon()}
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <h4 className="font-bold text-base text-white tracking-tight">
                {adapter.name}
              </h4>
              <span className="px-2 py-0.5 rounded text-[10px] font-mono uppercase bg-slate-800 text-slate-300 border border-slate-700">
                {adapter.type}
              </span>
              <span className="px-1.5 py-0.5 rounded text-[10px] font-mono text-cyan-400 bg-cyan-950/40 border border-cyan-800/40">
                {adapter.linkSpeed}
              </span>
            </div>
            <p className="text-xs text-slate-400 truncate max-w-xs mt-0.5" title={adapter.description}>
              {adapter.description}
            </p>
          </div>
        </div>

        {/* Status Badge & Toggle Switch */}
        <div className="flex items-center space-x-3">
          <div className="flex items-center space-x-1.5 px-2.5 py-1 rounded-full bg-slate-800/80 border border-slate-700/60">
            <Radio
              className={`w-3 h-3 ${
                adapter.status === 'up'
                  ? 'text-emerald-400 animate-pulse'
                  : adapter.status === 'degraded'
                  ? 'text-amber-400'
                  : 'text-rose-400'
              }`}
            />
            <span className={`text-[11px] font-mono font-semibold ${getPingColor(adapter.pingMs)}`}>
              {adapter.pingMs > 0 && adapter.pingMs < 999 ? `${adapter.pingMs} ms` : 'Offline'}
            </span>
          </div>

          <label className="relative inline-flex items-center cursor-pointer">
            <input
              type="checkbox"
              checked={adapter.enabled}
              onChange={(e) => onToggleEnabled(adapter.id, e.target.checked)}
              disabled={isReadOnly}
              className="sr-only peer"
            />
            <div className="w-9 h-5 bg-slate-800 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-cyan-500"></div>
          </label>
        </div>
      </div>

      {/* Middle row: IP addresses & WAN Info */}
      <div className="mt-4 pt-3 border-t border-slate-800/60 grid grid-cols-2 sm:grid-cols-3 gap-3 text-xs">
        <div>
          <span className="text-slate-500 block text-[10px] uppercase font-mono">Adapter IP (Bound)</span>
          <span className="text-slate-200 font-mono font-medium">{adapter.address}</span>
        </div>
        <div>
          <span className="text-slate-500 block text-[10px] uppercase font-mono">Hardware MAC</span>
          <span className="text-slate-200 font-mono font-medium">{adapter.mac || 'N/A'}</span>
        </div>
        <div>
          <span className="text-slate-500 block text-[10px] uppercase font-mono">Public WAN IP</span>
          <span className="text-cyan-400 font-mono font-medium flex items-center gap-1">
            <Globe className="w-3 h-3" />
            {adapter.wanIp || 'Resolving...'}
          </span>
        </div>
      </div>

      {/* Real-time Adapter Speeds */}
      <div className="mt-3 grid grid-cols-2 gap-3 bg-[#060a12]/80 p-2.5 rounded-xl border border-slate-800/80">
        <div className="flex items-center space-x-2">
          <ArrowDown className="w-4 h-4 text-cyan-400" />
          <div>
            <span className="text-[10px] text-slate-500 uppercase font-mono block">Download</span>
            <span className="text-sm font-mono font-bold text-white">
              {rxSpeed.value} <span className="text-xs font-normal text-cyan-400">{rxSpeed.unit}</span>
            </span>
          </div>
        </div>

        <div className="flex items-center space-x-2">
          <ArrowUp className="w-4 h-4 text-purple-400" />
          <div>
            <span className="text-[10px] text-slate-500 uppercase font-mono block">Upload</span>
            <span className="text-sm font-mono font-bold text-white">
              {txSpeed.value} <span className="text-xs font-normal text-purple-400">{txSpeed.unit}</span>
            </span>
          </div>
        </div>
      </div>

      {/* Bottom: Load Percentage Slider */}
      <div className="mt-4">
        <div className="flex items-center justify-between text-xs mb-1.5">
          <span className="text-slate-400 font-medium">Traffic Distribution Weight:</span>
          <span className="text-cyan-400 font-mono font-bold text-sm">{adapter.weight}%</span>
        </div>

        <div className="flex items-center space-x-3">
          <input
            type="range"
            min="0"
            max="100"
            step="5"
            value={adapter.weight}
            onChange={(e) => onWeightChange(adapter.id, parseInt(e.target.value, 10))}
            disabled={!adapter.enabled || isReadOnly}
            className="w-full h-1.5 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-cyan-400 disabled:opacity-40"
          />

          {/* Quick presets */}
          <div className="flex items-center space-x-1 shrink-0">
            {[0, 25, 50, 75, 100].map((preset) => (
              <button
                key={preset}
                onClick={() => onWeightChange(adapter.id, preset)}
                disabled={!adapter.enabled || isReadOnly}
                className={`px-1.5 py-0.5 text-[10px] rounded font-mono transition-colors ${
                  adapter.weight === preset
                    ? 'bg-cyan-500 text-black font-bold'
                    : 'bg-slate-800 text-slate-400 hover:text-white hover:bg-slate-700'
                }`}
              >
                {preset}%
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
