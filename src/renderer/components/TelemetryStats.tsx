import React from 'react';
import { ArrowDownCircle, ArrowUpCircle, Activity, Database, Clock } from 'lucide-react';
import { TelemetryState, ProxyStatus } from '../../shared/types';
import { formatSpeed, formatBytes, formatDuration } from '../utils/formatters';

interface TelemetryStatsProps {
  telemetry: TelemetryState;
  status: ProxyStatus;
}

export const TelemetryStats: React.FC<TelemetryStatsProps> = ({ telemetry, status }) => {
  const downSpeed = formatSpeed(telemetry.totalDownloadSpeedBps);
  const upSpeed = formatSpeed(telemetry.totalUploadSpeedBps);
  const totalBytes = formatBytes(telemetry.totalDownloadBytes + telemetry.totalUploadBytes);

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      {/* Aggregated Download Speed */}
      <div className="glass-panel rounded-2xl p-4 border border-slate-800/80 relative overflow-hidden group hover:border-cyan-500/40 transition-colors">
        <div className="flex items-center justify-between text-slate-400 mb-2">
          <span className="text-xs font-medium uppercase tracking-wider">Bonded Download</span>
          <ArrowDownCircle className="w-4 h-4 text-cyan-400" />
        </div>
        <div className="flex items-baseline space-x-1.5">
          <span className="text-3xl font-extrabold text-white tracking-tight font-mono">
            {downSpeed.value}
          </span>
          <span className="text-sm font-semibold text-cyan-400 font-mono">
            {downSpeed.unit}
          </span>
        </div>
        <div className="mt-2 text-[11px] text-slate-400 flex items-center justify-between">
          <span>Total Rx:</span>
          <span className="font-mono text-slate-300 font-medium">{formatBytes(telemetry.totalDownloadBytes)}</span>
        </div>
        <div className="h-1 w-full bg-slate-800 rounded-full mt-2 overflow-hidden">
          <div
            className="h-full bg-gradient-to-r from-cyan-500 to-emerald-400 transition-all duration-500 rounded-full"
            style={{ width: `${Math.min(100, (telemetry.totalDownloadSpeedBps / (100 * 1024 * 1024)) * 100)}%` }}
          />
        </div>
      </div>

      {/* Aggregated Upload Speed */}
      <div className="glass-panel rounded-2xl p-4 border border-slate-800/80 relative overflow-hidden group hover:border-purple-500/40 transition-colors">
        <div className="flex items-center justify-between text-slate-400 mb-2">
          <span className="text-xs font-medium uppercase tracking-wider">Bonded Upload</span>
          <ArrowUpCircle className="w-4 h-4 text-purple-400" />
        </div>
        <div className="flex items-baseline space-x-1.5">
          <span className="text-3xl font-extrabold text-white tracking-tight font-mono">
            {upSpeed.value}
          </span>
          <span className="text-sm font-semibold text-purple-400 font-mono">
            {upSpeed.unit}
          </span>
        </div>
        <div className="mt-2 text-[11px] text-slate-400 flex items-center justify-between">
          <span>Total Tx:</span>
          <span className="font-mono text-slate-300 font-medium">{formatBytes(telemetry.totalUploadBytes)}</span>
        </div>
        <div className="h-1 w-full bg-slate-800 rounded-full mt-2 overflow-hidden">
          <div
            className="h-full bg-gradient-to-r from-purple-500 to-pink-500 transition-all duration-500 rounded-full"
            style={{ width: `${Math.min(100, (telemetry.totalUploadSpeedBps / (20 * 1024 * 1024)) * 100)}%` }}
          />
        </div>
      </div>

      {/* Concurrent Active Streams */}
      <div className="glass-panel rounded-2xl p-4 border border-slate-800/80 relative overflow-hidden group hover:border-emerald-500/40 transition-colors">
        <div className="flex items-center justify-between text-slate-400 mb-2">
          <span className="text-xs font-medium uppercase tracking-wider">Active Concurrency</span>
          <Activity className="w-4 h-4 text-emerald-400" />
        </div>
        <div className="flex items-baseline space-x-1.5">
          <span className="text-3xl font-extrabold text-white tracking-tight font-mono">
            {status.activeConnections}
          </span>
          <span className="text-sm font-semibold text-emerald-400 font-sans">
            active streams
          </span>
        </div>
        <div className="mt-2 text-[11px] text-slate-400 flex items-center justify-between">
          <span>Total Dispatched:</span>
          <span className="font-mono text-slate-300 font-medium">{status.totalConnections} connections</span>
        </div>
        <div className="h-1 w-full bg-slate-800 rounded-full mt-2 overflow-hidden">
          <div
            className="h-full bg-emerald-400 transition-all duration-300 rounded-full"
            style={{ width: `${Math.min(100, status.activeConnections * 5)}%` }}
          />
        </div>
      </div>

      {/* Session Volume & Uptime */}
      <div className="glass-panel rounded-2xl p-4 border border-slate-800/80 relative overflow-hidden group hover:border-amber-500/40 transition-colors">
        <div className="flex items-center justify-between text-slate-400 mb-2">
          <span className="text-xs font-medium uppercase tracking-wider">Total Volume & Uptime</span>
          <Database className="w-4 h-4 text-amber-400" />
        </div>
        <div className="flex items-baseline space-x-1.5">
          <span className="text-3xl font-extrabold text-white tracking-tight font-mono">
            {totalBytes}
          </span>
        </div>
        <div className="mt-2 text-[11px] text-slate-400 flex items-center justify-between">
          <span className="flex items-center gap-1">
            <Clock className="w-3 h-3 text-slate-400" /> Session:
          </span>
          <span className="font-mono text-slate-300 font-medium">{formatDuration(status.startedAt)}</span>
        </div>
        <div className="h-1 w-full bg-slate-800 rounded-full mt-2 overflow-hidden">
          <div className="h-full bg-amber-400 rounded-full w-full opacity-60" />
        </div>
      </div>
    </div>
  );
};
