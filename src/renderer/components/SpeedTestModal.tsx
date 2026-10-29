import React, { useState, useEffect } from 'react';
import { X, Gauge, Zap, TrendingUp, CheckCircle2, AlertCircle } from 'lucide-react';
import { SpeedTestProgress, SpeedTestResult } from '../../shared/types';

interface SpeedTestModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const SpeedTestModal: React.FC<SpeedTestModalProps> = ({ isOpen, onClose }) => {
  const [isRunning, setIsRunning] = useState(false);
  const [progress, setProgress] = useState<SpeedTestProgress>({
    stage: 'idle',
    percent: 0,
    message: 'Ready to benchmark individual adapters vs combined bonded speed.'
  });
  const [result, setResult] = useState<SpeedTestResult | null>(null);

  useEffect(() => {
    if (!isOpen) return;

    const unsubscribe = window.combispeed?.onSpeedTestProgress((p) => {
      setProgress(p);
      if (p.results) {
        setResult(p.results);
        setIsRunning(false);
      }
      if (p.stage === 'error') {
        setIsRunning(false);
      }
    });

    return () => {
      unsubscribe?.();
    };
  }, [isOpen]);

  const handleStartTest = async () => {
    setIsRunning(true);
    setResult(null);
    setProgress({
      stage: 'testing-adapters',
      percent: 5,
      message: 'Initializing multi-stream speed benchmark...'
    });

    try {
      const res = await window.combispeed.runSpeedTest();
      setResult(res);
    } catch (err: any) {
      setProgress({
        stage: 'error',
        percent: 0,
        message: err.message || 'Speed test failed'
      });
    } finally {
      setIsRunning(false);
    }
  };

  if (!isOpen) return null;

  const maxSpeed = result
    ? Math.max(
        result.bondedResult.downloadMbps,
        ...result.adapterResults.map((a) => a.downloadMbps),
        10
      ) * 1.15
    : 100;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-md">
      <div className="glass-panel-glow rounded-3xl p-6 max-w-xl w-full border border-cyan-500/30 relative">
        {/* Header */}
        <div className="flex items-center justify-between pb-4 border-b border-slate-800">
          <div className="flex items-center space-x-2.5">
            <div className="w-8 h-8 rounded-lg bg-cyan-500/20 border border-cyan-500/40 flex items-center justify-center">
              <Gauge className="w-4 h-4 text-cyan-400" />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">
                Multi-WAN Bonding Speed Benchmark
              </h3>
              <p className="text-xs text-slate-400">
                Measures individual adapter throughput vs aggregated bonded bandwidth
              </p>
            </div>
          </div>

          <button
            onClick={onClose}
            disabled={isRunning}
            className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Body */}
        <div className="mt-5 space-y-5">
          {/* Progress / Status banner */}
          <div className="bg-[#060a12] rounded-2xl p-4 border border-slate-800">
            <div className="flex items-center justify-between text-xs mb-2">
              <span className="text-slate-300 font-medium">{progress.message}</span>
              <span className="text-cyan-400 font-mono font-bold">{progress.percent}%</span>
            </div>

            <div className="h-2 w-full bg-slate-800 rounded-full overflow-hidden">
              <div
                className="h-full bg-gradient-to-r from-cyan-500 via-teal-400 to-emerald-400 transition-all duration-300 rounded-full"
                style={{ width: `${progress.percent}%` }}
              />
            </div>
          </div>

          {/* Results Comparison Chart */}
          {result && (
            <div className="space-y-4">
              <div className="p-4 rounded-2xl bg-gradient-to-r from-cyan-950/40 to-emerald-950/40 border border-cyan-500/30 flex items-center justify-between">
                <div>
                  <span className="text-[11px] font-semibold uppercase tracking-wider text-cyan-400 block">
                    Bonded Aggregate Speed
                  </span>
                  <div className="flex items-baseline space-x-1.5 mt-0.5">
                    <span className="text-3xl font-extrabold text-white font-mono">
                      {result.bondedResult.downloadMbps}
                    </span>
                    <span className="text-sm font-bold text-cyan-400">Mbps</span>
                  </div>
                </div>

                <div className="text-right">
                  <div className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-emerald-500/20 border border-emerald-500/40 text-emerald-400 text-xs font-bold">
                    <TrendingUp className="w-3.5 h-3.5" />
                    <span>+{result.boostPercentage}% Boost</span>
                  </div>
                  <span className="text-[10px] text-slate-400 block mt-1 font-mono">
                    Avg Latency: {result.bondedResult.latencyMs} ms
                  </span>
                </div>
              </div>

              {/* Bar comparison breakdown */}
              <div className="space-y-2.5 text-xs">
                <span className="text-slate-400 font-semibold block text-[11px] uppercase tracking-wider">
                  Throughput Breakdown:
                </span>

                {result.adapterResults.map((item) => (
                  <div key={item.id} className="space-y-1">
                    <div className="flex justify-between text-slate-300">
                      <span className="font-medium truncate max-w-xs">{item.name}</span>
                      <span className="font-mono font-bold text-white">
                        {item.downloadMbps} Mbps
                      </span>
                    </div>
                    <div className="h-2.5 w-full bg-slate-800 rounded-full overflow-hidden">
                      <div
                        className="h-full bg-slate-600 rounded-full"
                        style={{ width: `${(item.downloadMbps / maxSpeed) * 100}%` }}
                      />
                    </div>
                  </div>
                ))}

                {/* Bonded Total Bar */}
                <div className="space-y-1 pt-1">
                  <div className="flex justify-between text-cyan-300 font-semibold">
                    <span className="flex items-center gap-1.5">
                      <Zap className="w-3.5 h-3.5 text-cyan-400 fill-cyan-400" />
                      Combined Bonded Speed
                    </span>
                    <span className="font-mono font-bold text-cyan-400">
                      {result.bondedResult.downloadMbps} Mbps
                    </span>
                  </div>
                  <div className="h-3 w-full bg-slate-800 rounded-full overflow-hidden">
                    <div
                      className="h-full bg-gradient-to-r from-cyan-400 to-emerald-400 rounded-full shadow-lg shadow-cyan-400/50"
                      style={{ width: `${(result.bondedResult.downloadMbps / maxSpeed) * 100}%` }}
                    />
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Action Button */}
          <div className="flex justify-end space-x-3 pt-2">
            <button
              onClick={onClose}
              disabled={isRunning}
              className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold transition-colors"
            >
              Close
            </button>
            <button
              onClick={handleStartTest}
              disabled={isRunning}
              className="flex items-center space-x-1.5 px-5 py-2 rounded-xl bg-gradient-to-r from-cyan-500 to-emerald-400 hover:from-cyan-400 hover:to-emerald-300 text-black text-xs font-bold shadow-lg shadow-cyan-500/25 transition-all disabled:opacity-50"
            >
              <Zap className="w-3.5 h-3.5 fill-black" />
              <span>{isRunning ? 'Benchmarking...' : result ? 'Run Test Again' : 'Start Speed Test'}</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
