import * as net from 'net';
import * as http from 'http';
import { EventEmitter } from 'events';
import { NetworkAdapter } from '../../shared/types';

export class HealthMonitor extends EventEmitter {
  private timer: NodeJS.Timeout | null = null;
  private isChecking = false;

  constructor(private getAdapters: () => NetworkAdapter[]) {
    super();
  }

  start(intervalMs = 4000): void {
    if (this.timer) clearInterval(this.timer);
    this.checkAll();
    this.timer = setInterval(() => this.checkAll(), intervalMs);
  }

  stop(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  private async checkAll(): Promise<void> {
    if (this.isChecking) return;
    this.isChecking = true;

    try {
      const adapters = this.getAdapters().filter(a => a.enabled);
      await Promise.all(adapters.map(adapter => this.checkAdapter(adapter)));
      this.emit('healthUpdated', this.getAdapters());
    } finally {
      this.isChecking = false;
    }
  }

  private async checkAdapter(adapter: NetworkAdapter): Promise<void> {
    // 1. Measure Latency via bound TCP ping to 1.1.1.1:80
    const start = Date.now();
    try {
      await new Promise<void>((resolve, reject) => {
        const socket = net.createConnection({
          host: '1.1.1.1',
          port: 80,
          localAddress: adapter.address,
          timeout: 2000
        });

        socket.on('connect', () => {
          socket.destroy();
          resolve();
        });

        socket.on('timeout', () => {
          socket.destroy();
          reject(new Error('Timeout'));
        });

        socket.on('error', (err) => {
          socket.destroy();
          reject(err);
        });
      });

      const rtt = Date.now() - start;
      adapter.pingMs = rtt;
      adapter.status = rtt > 350 ? 'degraded' : 'up';
      adapter.packetLossPercent = Math.max(0, adapter.packetLossPercent - 10);
    } catch (err) {
      adapter.pingMs = 999;
      adapter.packetLossPercent = Math.min(100, adapter.packetLossPercent + 25);
      adapter.status = adapter.packetLossPercent >= 75 ? 'down' : 'degraded';
    }

    // 2. Refresh WAN IP if missing
    if (!adapter.wanIp && adapter.status !== 'down') {
      this.detectWanIp(adapter).catch(() => {});
    }
  }

  private detectWanIp(adapter: NetworkAdapter): Promise<void> {
    return new Promise((resolve) => {
      const req = http.request({
        host: 'api.ipify.org',
        path: '/?format=json',
        method: 'GET',
        localAddress: adapter.address,
        timeout: 4000
      }, (res) => {
        let body = '';
        res.on('data', chunk => body += chunk);
        res.on('end', () => {
          try {
            const data = JSON.parse(body);
            if (data.ip) {
              adapter.wanIp = data.ip;
            }
          } catch {}
          resolve();
        });
      });

      req.on('error', () => resolve());
      req.on('timeout', () => {
        req.destroy();
        resolve();
      });
      req.end();
    });
  }
}
