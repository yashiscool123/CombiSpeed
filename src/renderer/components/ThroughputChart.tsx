import React, { useMemo } from 'react';
import { TelemetryState, NetworkAdapter } from '../../shared/types';
import { formatSpeed } from '../utils/formatters';

interface ThroughputChartProps {
  telemetry: TelemetryState;
  adapters: NetworkAdapter[];
}

export const ThroughputChart: React.FC<ThroughputChartProps> = ({ telemetry, adapters }) => {
  const history = telemetry.history || [];

  // Calculate chart boundaries
  const maxDownload = useMemo(() => {
    if (history.length === 0) return 1024 * 1024; // 1 MB/s default
    const peak = Math.max(...history.map(p => Math.max(p.totalDownloadBps, p.totalUploadBps)));
    return Math.max(peak * 1.2, 512 * 1024); // at least 512 KB/s scale
  }, [history]);

  // Width and height of SVG viewBox
  const viewBoxWidth = 800;
  const viewBoxHeight = 180;
  const paddingBottom = 20;
  const paddingTop = 10;
  const chartHeight = viewBoxHeight - paddingBottom - paddingTop;

  // Generate SVG path points
  const points = useMemo(() => {
    // Fill to 60 points if fewer
    const totalPoints = 60;
    const filledHistory = Array(totalPoints - history.length)
      .fill({ totalDownloadBps: 0, totalUploadBps: 0 })
      .concat(history);

    const stepX = viewBoxWidth / (totalPoints - 1);

    const downloadCoords = filledHistory.map((item, idx) => {
      const x = idx * stepX;
      const ratio = Math.min(1, item.totalDownloadBps / maxDownload);
      const y = viewBoxHeight - paddingBottom - ratio * chartHeight;
      return { x, y };
    });

    const uploadCoords = filledHistory.map((item, idx) => {
      const x = idx * stepX;
      const ratio = Math.min(1, item.totalUploadBps / maxDownload);
      const y = viewBoxHeight - paddingBottom - ratio * chartHeight;
      return { x, y };
    });

    // Create SVG path strings
    const downloadLine = downloadCoords.reduce(
      (acc, p, i) => `${acc} ${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)},${p.y.toFixed(1)}`,
      ''
    );

    const downloadArea = `${downloadLine} L ${viewBoxWidth},${viewBoxHeight - paddingBottom} L 0,${viewBoxHeight - paddingBottom} Z`;

    const uploadLine = uploadCoords.reduce(
      (acc, p, i) => `${acc} ${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)},${p.y.toFixed(1)}`,
      ''
    );

    return { downloadLine, downloadArea, uploadLine };
  }, [history, maxDownload]);

  const peakFormatted = formatSpeed(maxDownload / 1.2);

  return (
    <div className="glass-panel rounded-2xl p-5 border border-slate-800/80">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center space-x-3">
          <h3 className="text-sm font-bold text-white uppercase tracking-wider">
            Real-Time Bandwidth Waveform
          </h3>
          <span className="text-[11px] text-slate-400 font-mono">
            (Last 60 Seconds)
          </span>
        </div>

        <div className="flex items-center space-x-4 text-xs font-medium">
          <div className="flex items-center space-x-1.5">
            <span className="w-2.5 h-2.5 rounded-full bg-cyan-400 shadow-sm shadow-cyan-400" />
            <span className="text-slate-300">Bonded Rx</span>
          </div>
          <div className="flex items-center space-x-1.5">
            <span className="w-2.5 h-2.5 rounded-full bg-purple-400 shadow-sm shadow-purple-400" />
            <span className="text-slate-300">Bonded Tx</span>
          </div>
          <div className="text-[11px] text-slate-400 font-mono bg-slate-800/80 px-2 py-0.5 rounded border border-slate-700">
            Peak: {peakFormatted.value} {peakFormatted.unit}
          </div>
        </div>
      </div>

      {/* SVG Waveform Canvas */}
      <div className="relative w-full h-44 overflow-hidden rounded-xl bg-[#060a12] border border-slate-900">
        <svg
          viewBox={`0 0 ${viewBoxWidth} ${viewBoxHeight}`}
          className="w-full h-full preserve-3d"
          preserveAspectRatio="none"
        >
          <defs>
            <linearGradient id="cyanGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#00f0ff" stopOpacity="0.35" />
              <stop offset="100%" stopColor="#00f0ff" stopOpacity="0.0" />
            </linearGradient>
            <linearGradient id="purpleGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#c084fc" stopOpacity="0.2" />
              <stop offset="100%" stopColor="#c084fc" stopOpacity="0.0" />
            </linearGradient>
          </defs>

          {/* Grid lines */}
          <line
            x1="0"
            y1={viewBoxHeight - paddingBottom - chartHeight * 0.75}
            x2={viewBoxWidth}
            y2={viewBoxHeight - paddingBottom - chartHeight * 0.75}
            stroke="#1e293b"
            strokeDasharray="4 4"
            strokeWidth="0.8"
          />
          <line
            x1="0"
            y1={viewBoxHeight - paddingBottom - chartHeight * 0.5}
            x2={viewBoxWidth}
            y2={viewBoxHeight - paddingBottom - chartHeight * 0.5}
            stroke="#1e293b"
            strokeDasharray="4 4"
            strokeWidth="0.8"
          />
          <line
            x1="0"
            y1={viewBoxHeight - paddingBottom - chartHeight * 0.25}
            x2={viewBoxWidth}
            y2={viewBoxHeight - paddingBottom - chartHeight * 0.25}
            stroke="#1e293b"
            strokeDasharray="4 4"
            strokeWidth="0.8"
          />
          <line
            x1="0"
            y1={viewBoxHeight - paddingBottom}
            x2={viewBoxWidth}
            y2={viewBoxHeight - paddingBottom}
            stroke="#334155"
            strokeWidth="1"
          />

          {/* Download Area & Line */}
          <path d={points.downloadArea} fill="url(#cyanGradient)" />
          <path
            d={points.downloadLine}
            fill="none"
            stroke="#00f0ff"
            strokeWidth="2.2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {/* Upload Line */}
          <path
            d={points.uploadLine}
            fill="none"
            stroke="#c084fc"
            strokeWidth="1.8"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>

        {/* Time labels overlay */}
        <div className="absolute bottom-1 left-2 right-2 flex justify-between text-[10px] text-slate-500 font-mono pointer-events-none">
          <span>-60s</span>
          <span>-45s</span>
          <span>-30s</span>
          <span>-15s</span>
          <span>now</span>
        </div>
      </div>
    </div>
  );
};
