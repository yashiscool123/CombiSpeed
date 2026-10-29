export function formatSpeed(bytesPerSec: number): { value: string; unit: string } {
  if (!bytesPerSec || bytesPerSec <= 0) {
    return { value: '0.00', unit: 'KB/s' };
  }

  const kBytes = bytesPerSec / 1024;
  if (kBytes < 1000) {
    return { value: kBytes.toFixed(2), unit: 'KB/s' };
  }

  const mBytes = kBytes / 1024;
  if (mBytes < 1000) {
    return { value: mBytes.toFixed(2), unit: 'MB/s' };
  }

  const gBytes = mBytes / 1024;
  return { value: gBytes.toFixed(2), unit: 'GB/s' };
}

export function formatSpeedMbps(bytesPerSec: number): string {
  const mbps = (bytesPerSec * 8) / (1024 * 1024);
  return `${mbps.toFixed(2)} Mbps`;
}

export function formatBytes(bytes: number): string {
  if (!bytes || bytes <= 0) return '0 B';

  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));

  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
}

export function formatDuration(startedAt?: number): string {
  if (!startedAt) return '00:00:00';
  const elapsedSec = Math.floor((Date.now() - startedAt) / 1000);

  const hours = Math.floor(elapsedSec / 3600);
  const minutes = Math.floor((elapsedSec % 3600) / 60);
  const seconds = elapsedSec % 60;

  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`;
}

export function getPingColor(pingMs: number): string {
  if (pingMs <= 0 || pingMs === 999) return 'text-slate-500';
  if (pingMs < 60) return 'text-emerald-400';
  if (pingMs < 150) return 'text-amber-400';
  return 'text-rose-400';
}
