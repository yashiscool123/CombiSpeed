import { app, BrowserWindow, ipcMain, shell } from 'electron';
import * as path from 'path';
import { NetworkDetector } from './services/networkDetector';
import { HealthMonitor } from './services/healthMonitor';
import { LoadBalancer } from './services/loadBalancer';
import { ProxyServer } from './services/proxyServer';
import { WindowsSystemProxyManager } from './services/systemProxy';
import { SpeedTester } from './services/speedTest';
import { ProxyConfig, ProxyStatus } from '../shared/types';

let mainWindow: BrowserWindow | null = null;

// Services instances
const networkDetector = new NetworkDetector();
const loadBalancer = new LoadBalancer();
const proxyServer = new ProxyServer(loadBalancer, () => networkDetector.getAllCached());
const healthMonitor = new HealthMonitor(() => networkDetector.getAllCached());
const systemProxyManager = new WindowsSystemProxyManager();
const speedTester = new SpeedTester();

let currentConfig: ProxyConfig = {
  httpPort: 8080,
  socksPort: 1080,
  routingMode: 'weighted',
  enableWindowsProxy: false
};

async function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 840,
    minWidth: 1024,
    minHeight: 700,
    frame: false, // Custom sleek titlebar
    backgroundColor: '#080c14',
    show: false,
    webPreferences: {
      preload: path.join(__dirname, '../preload/index.js'),
      sandbox: false,
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  mainWindow.setMenuBarVisibility(false);

  // If in dev mode (VITE_DEV_SERVER_URL or localhost:5173)
  const devServerUrl = process.env.VITE_DEV_SERVER_URL || process.env.ELECTRON_RENDERER_URL;
  if (devServerUrl) {
    mainWindow.loadURL(devServerUrl);
  } else {
    mainWindow.loadFile(path.join(__dirname, '../../renderer/index.html'));
  }

  mainWindow.once('ready-to-show', () => {
    mainWindow?.show();
  });

  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url);
    return { action: 'deny' };
  });

  // Relay proxy server events to renderer
  proxyServer.on('telemetry', (data) => {
    mainWindow?.webContents.send('telemetry:updated', data);
  });

  proxyServer.on('auditLog', (log) => {
    mainWindow?.webContents.send('audit:log', log);
  });

  healthMonitor.on('healthUpdated', (adapters) => {
    mainWindow?.webContents.send('adapters:updated', adapters);
  });

  speedTester.on('progress', (progress) => {
    mainWindow?.webContents.send('speedtest:progress', progress);
  });
}

function registerIpcHandlers() {
  ipcMain.handle('adapters:get', async () => {
    const list = networkDetector.getAllCached();
    if (list.length === 0) {
      return await networkDetector.detectAdapters();
    }
    return list;
  });

  ipcMain.handle('adapters:refresh', async () => {
    return await networkDetector.detectAdapters();
  });

  ipcMain.handle('adapters:updateWeights', (_event, weights: Record<string, number>) => {
    networkDetector.updateWeights(weights);
    return;
  });

  ipcMain.handle('adapters:toggle', (_event, id: string, enabled: boolean) => {
    networkDetector.toggleAdapter(id, enabled);
    return;
  });

  ipcMain.handle('proxy:start', async (_event, config: ProxyConfig) => {
    currentConfig = { ...config };
    loadBalancer.setMode(config.routingMode);
    if (config.primaryAdapterId) {
      loadBalancer.setPrimaryAdapter(config.primaryAdapterId);
    }

    await proxyServer.start(config.httpPort, config.socksPort);

    if (config.enableWindowsProxy) {
      await systemProxyManager.enable(config.httpPort, config.socksPort);
    }

    const status = proxyServer.getStatus(
      config.httpPort,
      config.socksPort,
      systemProxyManager.isCurrentlyApplied()
    );
    mainWindow?.webContents.send('proxy:statusUpdated', status);
    return status;
  });

  ipcMain.handle('proxy:stop', async () => {
    if (systemProxyManager.isCurrentlyApplied()) {
      await systemProxyManager.disable();
    }

    await proxyServer.stop();
    const status = proxyServer.getStatus(
      currentConfig.httpPort,
      currentConfig.socksPort,
      false
    );
    mainWindow?.webContents.send('proxy:statusUpdated', status);
    return status;
  });

  ipcMain.handle('proxy:status', () => {
    return proxyServer.getStatus(
      currentConfig.httpPort,
      currentConfig.socksPort,
      systemProxyManager.isCurrentlyApplied()
    );
  });

  ipcMain.handle('proxy:setWindowsProxy', async (_event, enabled: boolean) => {
    if (enabled) {
      const ok = await systemProxyManager.enable(currentConfig.httpPort, currentConfig.socksPort);
      currentConfig.enableWindowsProxy = ok;
      return ok;
    } else {
      const ok = await systemProxyManager.disable();
      currentConfig.enableWindowsProxy = false;
      return ok;
    }
  });

  ipcMain.handle('telemetry:get', () => {
    return proxyServer.getTelemetryState();
  });

  ipcMain.handle('audit:getLogs', () => {
    return proxyServer.getAuditLogs();
  });

  ipcMain.handle('speedtest:run', async () => {
    const adapters = networkDetector.getAllCached();
    return await speedTester.runTest(adapters);
  });

  // Window controls
  ipcMain.on('window:minimize', () => mainWindow?.minimize());
  ipcMain.on('window:maximize', () => {
    if (mainWindow?.isMaximized()) {
      mainWindow.unmaximize();
    } else {
      mainWindow?.maximize();
    }
  });
  ipcMain.on('window:close', () => mainWindow?.close());
}

app.whenReady().then(async () => {
  registerIpcHandlers();
  await networkDetector.detectAdapters();
  healthMonitor.start(4000);
  await createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', async () => {
  // Always clean up Windows System Proxy on exit to never leave user internet broken!
  if (systemProxyManager.isCurrentlyApplied()) {
    await systemProxyManager.disable();
  }
  healthMonitor.stop();
  await proxyServer.stop();
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

process.on('SIGINT', async () => {
  if (systemProxyManager.isCurrentlyApplied()) {
    await systemProxyManager.disable();
  }
  process.exit(0);
});

process.on('SIGTERM', async () => {
  if (systemProxyManager.isCurrentlyApplied()) {
    await systemProxyManager.disable();
  }
  process.exit(0);
});
