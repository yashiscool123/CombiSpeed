import React, { useState } from 'react';
import { X, Settings, Check, Copy, Sliders, Shield } from 'lucide-react';
import { ProxyConfig, NetworkAdapter, RoutingMode } from '../../shared/types';

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  config: ProxyConfig;
  adapters: NetworkAdapter[];
  onSaveConfig: (config: ProxyConfig) => void;
}

export const SettingsModal: React.FC<SettingsModalProps> = ({
  isOpen,
  onClose,
  config,
  adapters,
  onSaveConfig
}) => {
  const [formData, setFormData] = useState<ProxyConfig>({ ...config });
  const [copied, setCopied] = useState(false);

  if (!isOpen) return null;

  const handleSave = () => {
    onSaveConfig(formData);
    onClose();
  };

  const proxyEnvString = `HTTP_PROXY=http://127.0.0.1:${formData.httpPort} HTTPS_PROXY=http://127.0.0.1:${formData.httpPort} ALL_PROXY=socks5://127.0.0.1:${formData.socksPort}`;

  const copyToClipboard = () => {
    navigator.clipboard.writeText(proxyEnvString);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-md">
      <div className="glass-panel-glow rounded-3xl p-6 max-w-lg w-full border border-slate-700/80">
        {/* Header */}
        <div className="flex items-center justify-between pb-4 border-b border-slate-800">
          <div className="flex items-center space-x-2.5">
            <div className="w-8 h-8 rounded-lg bg-slate-800 border border-slate-700 flex items-center justify-center">
              <Settings className="w-4 h-4 text-cyan-400" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                CombiSpeed Bonding Configuration
              </h3>
              <p className="text-xs text-slate-400">
                Network ports, load balancing algorithms & system integration
              </p>
            </div>
          </div>

          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Form */}
        <div className="mt-5 space-y-4 text-xs">
          {/* Ports */}
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-slate-300 font-semibold block mb-1.5">
                HTTP / HTTPS Proxy Port
              </label>
              <input
                type="number"
                value={formData.httpPort}
                onChange={(e) =>
                  setFormData({ ...formData, httpPort: parseInt(e.target.value, 10) || 8080 })
                }
                className="w-full px-3 py-2 bg-slate-900 border border-slate-800 rounded-xl text-white font-mono focus:outline-none focus:border-cyan-500"
              />
              <span className="text-[10px] text-slate-500 mt-1 block">Default: 8080</span>
            </div>

            <div>
              <label className="text-slate-300 font-semibold block mb-1.5">
                SOCKS5 Proxy Port
              </label>
              <input
                type="number"
                value={formData.socksPort}
                onChange={(e) =>
                  setFormData({ ...formData, socksPort: parseInt(e.target.value, 10) || 1080 })
                }
                className="w-full px-3 py-2 bg-slate-900 border border-slate-800 rounded-xl text-white font-mono focus:outline-none focus:border-cyan-500"
              />
              <span className="text-[10px] text-slate-500 mt-1 block">Default: 1080</span>
            </div>
          </div>

          {/* Routing Algorithm */}
          <div>
            <label className="text-slate-300 font-semibold block mb-1.5 flex items-center gap-1.5">
              <Sliders className="w-3.5 h-3.5 text-cyan-400" />
              Load Balancing Algorithm
            </label>
            <select
              value={formData.routingMode}
              onChange={(e) =>
                setFormData({ ...formData, routingMode: e.target.value as RoutingMode })
              }
              className="w-full px-3 py-2 bg-slate-900 border border-slate-800 rounded-xl text-white font-sans focus:outline-none focus:border-cyan-500 cursor-pointer"
            >
              <option value="weighted">Weighted Round-Robin (User configured ratios)</option>
              <option value="equal">Equal Share (Evenly balanced across all adapters)</option>
              <option value="least-latency">Least-Latency Smart Routing (Fastest ping first)</option>
              <option value="failover">Active-Passive Link Failover (Redundancy mode)</option>
            </select>
          </div>

          {/* Primary Adapter (for failover) */}
          {formData.routingMode === 'failover' && (
            <div>
              <label className="text-slate-300 font-semibold block mb-1.5">
                Designated Primary Adapter
              </label>
              <select
                value={formData.primaryAdapterId || ''}
                onChange={(e) =>
                  setFormData({ ...formData, primaryAdapterId: e.target.value })
                }
                className="w-full px-3 py-2 bg-slate-900 border border-slate-800 rounded-xl text-white font-sans focus:outline-none focus:border-cyan-500 cursor-pointer"
              >
                <option value="">Auto (Highest Link Speed)</option>
                {adapters.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.name} ({a.address}) - {a.linkSpeed}
                  </option>
                ))}
              </select>
            </div>
          )}

          {/* Windows System Proxy */}
          <div className="pt-2 border-t border-slate-800">
            <label className="flex items-center space-x-3 cursor-pointer">
              <input
                type="checkbox"
                checked={formData.enableWindowsProxy}
                onChange={(e) =>
                  setFormData({ ...formData, enableWindowsProxy: e.target.checked })
                }
                className="w-4 h-4 rounded border-slate-700 bg-slate-900 text-cyan-500 focus:ring-0 focus:ring-offset-0 cursor-pointer"
              />
              <div>
                <span className="text-slate-200 font-medium block flex items-center gap-1.5">
                  <Shield className="w-3.5 h-3.5 text-emerald-400" />
                  Auto-enable Windows System Proxy when CombiSpeed starts
                </span>
                <span className="text-slate-500 text-[10px]">
                  Automatically routes all Windows browsers and software through the bonded pipeline
                </span>
              </div>
            </label>
          </div>

          {/* Export / Copy Proxy Env */}
          <div className="bg-[#060a12] p-3 rounded-xl border border-slate-800">
            <div className="flex items-center justify-between mb-1.5">
              <span className="text-slate-400 text-[10px] uppercase font-mono font-semibold">
                Terminal Proxy String (curl / git / npm / docker)
              </span>
              <button
                onClick={copyToClipboard}
                className="text-cyan-400 hover:text-cyan-300 flex items-center gap-1 text-[11px] font-mono"
              >
                {copied ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                <span>{copied ? 'Copied!' : 'Copy'}</span>
              </button>
            </div>
            <code className="text-[11px] text-slate-300 font-mono block break-all select-all">
              {proxyEnvString}
            </code>
          </div>
        </div>

        {/* Footer */}
        <div className="flex justify-end space-x-2.5 mt-5 pt-3 border-t border-slate-800">
          <button
            onClick={onClose}
            className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleSave}
            className="px-5 py-2 rounded-xl bg-cyan-500 hover:bg-cyan-400 text-black text-xs font-bold transition-all shadow-md shadow-cyan-500/20"
          >
            Save Settings
          </button>
        </div>
      </div>
    </div>
  );
};
