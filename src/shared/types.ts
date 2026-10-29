export type AdapterType = 'ethernet' | 'wifi' | 'cellular' | 'bridge' | 'virtual' | 'other';
export type AdapterStatus = 'up' | 'down' | 'degraded';
export type RoutingMode = 'weighted' | 'equal' | 'least-latency' | 'failover';

export interface NetworkAdapter {
  id: string;
  name: string;
  description: string;
  address: string;
  netmask: string;
  mac: string;
  family: 'IPv4' | 'IPv6';
  type: AdapterType;
  linkSpeed: string;
  status: AdapterStatus;
  enabled: boolean;
  weight: number; // 0 - 100%
  pingMs: number;
  packetLossPercent: number;
  downloadBytes: number;
  uploadBytes: number;
  downloadSpeedBps: number;
  uploadSpeedBps: number;
  wanIp?: string;
}

export interface ProxyConfig {
  httpPort: number;
  socksPort: number;
  routingMode: RoutingMode;
  enableWindowsProxy: boolean;
  primaryAdapterId?: string;
}

export interface ProxyStatus {
  running: boolean;
  httpPort: number;
  socksPort: number;
  activeConnections: number;
  totalConnections: number;
  windowsProxyActive: boolean;
  startedAt?: number;
}

export interface AdapterTelemetry {
  id: string;
  name: string;
  downloadSpeedBps: number;
  uploadSpeedBps: number;
  totalDownloadBytes: number;
  totalUploadBytes: number;
  pingMs: number;
  status: AdapterStatus;
}

export interface TelemetryPoint {
  timestamp: number;
  totalDownloadBps: number;
  totalUploadBps: number;
  adapterSpeeds: Record<string, number>;
}

export interface TelemetryState {
  totalDownloadSpeedBps: number;
  totalUploadSpeedBps: number;
  totalDownloadBytes: number;
  totalUploadBytes: number;
  history: TelemetryPoint[];
  adapters: Record<string, AdapterTelemetry>;
}

export interface AuditLog {
  id: string;
  timestamp: number;
  protocol: 'HTTP' | 'HTTPS (CONNECT)' | 'SOCKS5';
  targetHost: string;
  targetPort: number;
  adapterId: string;
  adapterName: string;
  clientIp: string;
  bytesTransferred: number;
  durationMs: number;
  status: 'active' | 'completed' | 'failed';
  errorMessage?: string;
}

export interface SpeedTestProgress {
  stage: 'idle' | 'testing-adapters' | 'testing-bonded' | 'completed' | 'error';
  currentAdapterName?: string;
  percent: number;
  message: string;
  results?: SpeedTestResult;
}

export interface SpeedTestResult {
  timestamp: number;
  adapterResults: Array<{
    id: string;
    name: string;
    downloadMbps: number;
    latencyMs: number;
  }>;
  bondedResult: {
    downloadMbps: number;
    latencyMs: number;
  };
  boostPercentage: number;
}
