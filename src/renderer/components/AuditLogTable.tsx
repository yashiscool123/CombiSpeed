import React, { useState } from 'react';
import { Terminal, Search, Trash2, ArrowRight } from 'lucide-react';
import { AuditLog } from '../../shared/types';
import { formatBytes } from '../utils/formatters';

interface AuditLogTableProps {
  logs: AuditLog[];
  onClearLogs: () => void;
}

export const AuditLogTable: React.FC<AuditLogTableProps> = ({ logs, onClearLogs }) => {
  const [filter, setFilter] = useState('');

  const filteredLogs = logs.filter(
    (log) =>
      log.targetHost.toLowerCase().includes(filter.toLowerCase()) ||
      log.adapterName.toLowerCase().includes(filter.toLowerCase()) ||
      log.protocol.toLowerCase().includes(filter.toLowerCase())
  );

  return (
    <div className="glass-panel rounded-2xl p-5 border border-slate-800/80 flex flex-col h-72">
      {/* Header & Filter */}
      <div className="flex items-center justify-between pb-3 border-b border-slate-800">
        <div className="flex items-center space-x-2">
          <Terminal className="w-4 h-4 text-cyan-400" />
          <h3 className="text-sm font-bold text-white uppercase tracking-wider">
            Connection Audit & Dispatch Stream
          </h3>
          <span className="text-[11px] font-mono text-slate-500 bg-slate-800 px-2 py-0.5 rounded">
            {logs.length} logs
          </span>
        </div>

        <div className="flex items-center space-x-2">
          <div className="relative">
            <Search className="w-3.5 h-3.5 text-slate-400 absolute left-2.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              placeholder="Filter host / adapter..."
              className="pl-8 pr-3 py-1 bg-slate-900 border border-slate-800 rounded-lg text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500/50 w-44"
            />
          </div>

          <button
            onClick={onClearLogs}
            className="p-1.5 rounded-lg bg-slate-800/60 hover:bg-slate-800 text-slate-400 hover:text-white transition-colors"
            title="Clear logs"
          >
            <Trash2 className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

      {/* Logs Table */}
      <div className="overflow-y-auto flex-1 mt-2 text-xs font-mono">
        {filteredLogs.length === 0 ? (
          <div className="h-full flex items-center justify-center text-slate-500 text-xs italic">
            No connections dispatched yet. Start the bonding engine to inspect traffic.
          </div>
        ) : (
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="text-[10px] text-slate-500 border-b border-slate-800/60 pb-1">
                <th className="py-1.5 px-2">TIME</th>
                <th className="py-1.5 px-2">PROTOCOL</th>
                <th className="py-1.5 px-2">TARGET DESTINATION</th>
                <th className="py-1.5 px-2">DISPATCHED ADAPTER</th>
                <th className="py-1.5 px-2 text-right">VOLUME</th>
                <th className="py-1.5 px-2 text-right">DURATION</th>
                <th className="py-1.5 px-2 text-center">STATUS</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/40">
              {filteredLogs.map((log) => {
                const date = new Date(log.timestamp);
                const timeStr = date.toTimeString().split(' ')[0];

                return (
                  <tr key={log.id} className="hover:bg-slate-800/30 transition-colors">
                    <td className="py-1 px-2 text-slate-400 text-[11px]">{timeStr}</td>
                    <td className="py-1 px-2">
                      <span
                        className={`px-1.5 py-0.5 rounded text-[10px] font-bold ${
                          log.protocol === 'SOCKS5'
                            ? 'bg-purple-950/60 text-purple-300 border border-purple-800/50'
                            : log.protocol.includes('CONNECT')
                            ? 'bg-cyan-950/60 text-cyan-300 border border-cyan-800/50'
                            : 'bg-blue-950/60 text-blue-300 border border-blue-800/50'
                        }`}
                      >
                        {log.protocol}
                      </span>
                    </td>
                    <td className="py-1 px-2 text-slate-200 truncate max-w-xs" title={`${log.targetHost}:${log.targetPort}`}>
                      {log.targetHost}:{log.targetPort}
                    </td>
                    <td className="py-1 px-2">
                      <span className="inline-flex items-center gap-1 text-emerald-400 font-medium">
                        <ArrowRight className="w-2.5 h-2.5 text-slate-500" />
                        {log.adapterName}
                      </span>
                    </td>
                    <td className="py-1 px-2 text-right text-slate-300">
                      {formatBytes(log.bytesTransferred)}
                    </td>
                    <td className="py-1 px-2 text-right text-slate-400">
                      {log.durationMs}ms
                    </td>
                    <td className="py-1 px-2 text-center">
                      <span
                        className={`inline-block w-2 h-2 rounded-full ${
                          log.status === 'completed'
                            ? 'bg-emerald-400'
                            : log.status === 'active'
                            ? 'bg-cyan-400 animate-pulse'
                            : 'bg-rose-400'
                        }`}
                        title={log.status}
                      />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};
