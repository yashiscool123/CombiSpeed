import React from 'react';
import { Zap, Minus, Square, X, ShieldCheck } from 'lucide-react';
import { ProxyStatus } from '../../shared/types';

interface TitleBarProps {
  proxyStatus: ProxyStatus;
}

export const TitleBar: React.FC<TitleBarProps> = ({ proxyStatus }) => {
  const handleMinimize = () => window.combispeed?.minimizeWindow();
  const handleMaximize = () => window.combispeed?.maximizeWindow();
  const handleClose = () => window.combispeed?.closeWindow();

  return (
    <div className="h-10 bg-[#060910] border-b border-slate-800/80 flex items-center justify-between px-3 select-none app-drag-region z-50">
      {/* Brand & Status */}
      <div className="flex items-center space-x-3">
        <div className="flex items-center space-x-2">
          <div className="w-6 h-6 rounded-md bg-gradient-to-tr from-cyan-500 to-emerald-400 flex items-center justify-center shadow-lg shadow-cyan-500/30">
            <Zap className="w-3.5 h-3.5 text-black fill-black" />
          </div>
          <span className="font-bold text-sm tracking-wide bg-gradient-to-r from-cyan-400 via-teal-300 to-emerald-400 bg-clip-text text-transparent">
            COMBISPEED
          </span>
        </div>

        <div className="h-3.5 w-px bg-slate-800" />

        <div className="flex items-center space-x-1.5">
          <span
            className={`w-2 h-2 rounded-full ${
              proxyStatus.running
                ? 'bg-emerald-400 animate-pulse shadow-sm shadow-emerald-400'
                : 'bg-slate-600'
            }`}
          />
          <span className="text-[11px] font-medium text-slate-400">
            {proxyStatus.running ? 'BONDING ENGINE ACTIVE' : 'DISCONNECTED'}
          </span>
        </div>

        {proxyStatus.windowsProxyActive && (
          <div className="flex items-center space-x-1 px-1.5 py-0.5 rounded bg-emerald-950/60 border border-emerald-500/30 text-emerald-400 text-[10px] font-semibold">
            <ShieldCheck className="w-2.5 h-2.5" />
            <span>SYS PROXY ON</span>
          </div>
        )}
      </div>

      {/* Window Controls */}
      <div className="flex items-center space-x-1 app-no-drag">
        <button
          onClick={handleMinimize}
          className="w-7 h-7 flex items-center justify-center text-slate-400 hover:text-white hover:bg-slate-800/60 rounded transition-colors"
          title="Minimize"
        >
          <Minus className="w-3.5 h-3.5" />
        </button>
        <button
          onClick={handleMaximize}
          className="w-7 h-7 flex items-center justify-center text-slate-400 hover:text-white hover:bg-slate-800/60 rounded transition-colors"
          title="Maximize"
        >
          <Square className="w-3 h-3" />
        </button>
        <button
          onClick={handleClose}
          className="w-7 h-7 flex items-center justify-center text-slate-400 hover:text-white hover:bg-rose-600 rounded transition-colors"
          title="Close (Restores System Proxy)"
        >
          <X className="w-3.5 h-3.5" />
        </button>
      </div>
    </div>
  );
};
