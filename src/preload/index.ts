import { contextBridge, ipcRenderer } from 'electron';
import {
  NetworkAdapter,
  ProxyConfig,
  ProxyStatus,
  TelemetryState,
  AuditLog,
  SpeedTestProgress,
  SpeedTestResult
} from '../shared/types';

export const combispeedApi = {
  getAdapters: (): Promise<NetworkAdapter[]> => ipcRenderer.invoke('adapters:get'),
  refreshAdapters: (): Promise<NetworkAdapter[]> => ipcRenderer.invoke('adapters:refresh'),
  updateWeights: (weights: Record<string, number>): Promise<void> =>
    ipcRenderer.invoke('adapters:updateWeights', weights),
  toggleAdapter: (id: string, enabled: boolean): Promise<void> =>
    ipcRenderer.invoke('adapters:toggle', id, enabled),

  startProxy: (config: ProxyConfig): Promise<ProxyStatus> =>
    ipcRenderer.invoke('proxy:start', config),
  stopProxy: (): Promise<ProxyStatus> =>
    ipcRenderer.invoke('proxy:stop'),
  getProxyStatus: (): Promise<ProxyStatus> =>
    ipcRenderer.invoke('proxy:status'),
  setWindowsProxy: (enabled: boolean): Promise<boolean> =>
    ipcRenderer.invoke('proxy:setWindowsProxy', enabled),

  getTelemetry: (): Promise<TelemetryState> =>
    ipcRenderer.invoke('telemetry:get'),
  getAuditLogs: (): Promise<AuditLog[]> =>
    ipcRenderer.invoke('audit:getLogs'),

  runSpeedTest: (): Promise<SpeedTestResult> =>
    ipcRenderer.invoke('speedtest:run'),

  minimizeWindow: () => ipcRenderer.send('window:minimize'),
  maximizeWindow: () => ipcRenderer.send('window:maximize'),
  closeWindow: () => ipcRenderer.send('window:close'),

  onAdaptersUpdated: (callback: (adapters: NetworkAdapter[]) => void) => {
    const handler = (_event: any, data: NetworkAdapter[]) => callback(data);
    ipcRenderer.on('adapters:updated', handler);
    return () => ipcRenderer.removeListener('adapters:updated', handler);
  },

  onTelemetryUpdated: (callback: (data: TelemetryState) => void) => {
    const handler = (_event: any, data: TelemetryState) => callback(data);
    ipcRenderer.on('telemetry:updated', handler);
    return () => ipcRenderer.removeListener('telemetry:updated', handler);
  },

  onProxyStatusUpdated: (callback: (status: ProxyStatus) => void) => {
    const handler = (_event: any, data: ProxyStatus) => callback(data);
    ipcRenderer.on('proxy:statusUpdated', handler);
    return () => ipcRenderer.removeListener('proxy:statusUpdated', handler);
  },

  onAuditLog: (callback: (log: AuditLog) => void) => {
    const handler = (_event: any, data: AuditLog) => callback(data);
    ipcRenderer.on('audit:log', handler);
    return () => ipcRenderer.removeListener('audit:log', handler);
  },

  onSpeedTestProgress: (callback: (progress: SpeedTestProgress) => void) => {
    const handler = (_event: any, data: SpeedTestProgress) => callback(data);
    ipcRenderer.on('speedtest:progress', handler);
    return () => ipcRenderer.removeListener('speedtest:progress', handler);
  }
};

contextBridge.exposeInMainWorld('combispeed', combispeedApi);

declare global {
  interface Window {
    combispeed: typeof combispeedApi;
  }
}
