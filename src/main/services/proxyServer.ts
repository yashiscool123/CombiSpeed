import * as net from 'net';
import * as http from 'http';
import { EventEmitter } from 'events';
import { NetworkAdapter, AuditLog, TelemetryState, TelemetryPoint, ProxyStatus } from '../../shared/types';
import { LoadBalancer } from './loadBalancer';

export class ProxyServer extends EventEmitter {
  private httpProxyServer: http.Server | null = null;
  private socksServer: net.Server | null = null;
  private isRunning = false;
  private activeConnectionsCount = 0;
  private totalConnectionsCount = 0;
  private startTime = 0;

  // Telemetry tracking
  private telemetryTimer: NodeJS.Timeout | null = null;
  private lastTotalDownloadBytes = 0;
  private lastTotalUploadBytes = 0;
  private totalDownloadBytes = 0;
  private totalUploadBytes = 0;
  private lastAdapterBytes: Map<string, { rx: number; tx: number }> = new Map();
  private telemetryHistory: TelemetryPoint[] = [];

  // Audit Logs (in-memory ring buffer)
  private auditLogs: AuditLog[] = [];
  private maxLogs = 100;

  constructor(
    private loadBalancer: LoadBalancer,
    private getAdapters: () => NetworkAdapter[]
  ) {
    super();
  }

  getStatus(httpPort: number, socksPort: number, windowsProxyActive: boolean): ProxyStatus {
    return {
      running: this.isRunning,
      httpPort,
      socksPort,
      activeConnections: this.activeConnectionsCount,
      totalConnections: this.totalConnectionsCount,
      windowsProxyActive,
      startedAt: this.startTime
    };
  }

  getAuditLogs(): AuditLog[] {
    return [...this.auditLogs];
  }

  getTelemetryState(): TelemetryState {
    const adapters = this.getAdapters();
    const adapterMap: TelemetryState['adapters'] = {};

    for (const a of adapters) {
      adapterMap[a.id] = {
        id: a.id,
        name: a.name,
        downloadSpeedBps: a.downloadSpeedBps,
        uploadSpeedBps: a.uploadSpeedBps,
        totalDownloadBytes: a.downloadBytes,
        totalUploadBytes: a.uploadBytes,
        pingMs: a.pingMs,
        status: a.status
      };
    }

    return {
      totalDownloadSpeedBps: this.lastTotalDownloadBytes,
      totalUploadSpeedBps: this.lastTotalUploadBytes,
      totalDownloadBytes: this.totalDownloadBytes,
      totalUploadBytes: this.totalUploadBytes,
      history: this.telemetryHistory,
      adapters: adapterMap
    };
  }

  async start(httpPort = 8080, socksPort = 1080): Promise<void> {
    if (this.isRunning) return;

    await Promise.all([
      this.startHttpProxy(httpPort),
      this.startSocks5Proxy(socksPort)
    ]);

    this.isRunning = true;
    this.startTime = Date.now();

    // Start 1-second telemetry interval
    this.telemetryTimer = setInterval(() => this.tickTelemetry(), 1000);
    this.emit('started');
  }

  async stop(): Promise<void> {
    if (!this.isRunning) return;

    if (this.telemetryTimer) {
      clearInterval(this.telemetryTimer);
      this.telemetryTimer = null;
    }

    await Promise.all([
      new Promise<void>((resolve) => {
        if (this.httpProxyServer) {
          this.httpProxyServer.close(() => resolve());
          this.httpProxyServer = null;
        } else resolve();
      }),
      new Promise<void>((resolve) => {
        if (this.socksServer) {
          this.socksServer.close(() => resolve());
          this.socksServer = null;
        } else resolve();
      })
    ]);

    this.isRunning = false;
    this.activeConnectionsCount = 0;
    this.emit('stopped');
  }

  private startHttpProxy(port: number): Promise<void> {
    return new Promise((resolve, reject) => {
      this.httpProxyServer = http.createServer((clientReq, clientRes) => {
        // Direct HTTP GET/POST handling
        const adapters = this.getAdapters();
        const adapter = this.loadBalancer.getNextAdapter(adapters);
        if (!adapter) {
          clientRes.writeHead(502, { 'Content-Type': 'text/plain' });
          clientRes.end('CombiSpeed: No network adapters available');
          return;
        }

        const url = new URL(clientReq.url || '', `http://${clientReq.headers.host}`);
        const targetHost = url.hostname;
        const targetPort = Number(url.port) || 80;

        this.trackConnectionStart();
        const startTime = Date.now();
        const logId = `http_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`;
        let bytesTransferred = 0;

        const options: http.RequestOptions = {
          hostname: targetHost,
          port: targetPort,
          path: url.pathname + url.search,
          method: clientReq.method,
          headers: clientReq.headers,
          localAddress: adapter.address // Socket bind to adapter!
        };

        const targetReq = http.request(options, (targetRes) => {
          clientRes.writeHead(targetRes.statusCode || 200, targetRes.headers);

          targetRes.on('data', (chunk) => {
            bytesTransferred += chunk.length;
            this.recordBytes(adapter, chunk.length, 0);
          });

          targetRes.pipe(clientRes);
        });

        clientReq.on('data', (chunk) => {
          bytesTransferred += chunk.length;
          this.recordBytes(adapter, 0, chunk.length);
        });

        clientReq.pipe(targetReq);

        const onFinish = (err?: Error) => {
          this.trackConnectionEnd();
          this.recordAuditLog({
            id: logId,
            timestamp: Date.now(),
            protocol: 'HTTP',
            targetHost,
            targetPort,
            adapterId: adapter.id,
            adapterName: adapter.name,
            clientIp: clientReq.socket.remoteAddress || '127.0.0.1',
            bytesTransferred,
            durationMs: Date.now() - startTime,
            status: err ? 'failed' : 'completed',
            errorMessage: err?.message
          });
        };

        targetReq.on('error', (err) => {
          if (!clientRes.headersSent) {
            clientRes.writeHead(502, { 'Content-Type': 'text/plain' });
            clientRes.end(`CombiSpeed Gateway Error: ${err.message}`);
          }
          onFinish(err);
        });

        clientRes.on('finish', () => onFinish());
      });

      // Handle HTTPS CONNECT tunneling
      this.httpProxyServer.on('connect', (req, clientSocket, head) => {
        const adapters = this.getAdapters();
        const adapter = this.loadBalancer.getNextAdapter(adapters);
        if (!adapter) {
          clientSocket.write('HTTP/1.1 502 Bad Gateway\r\n\r\nNo active adapters');
          clientSocket.destroy();
          return;
        }

        const [targetHost, targetPortStr] = (req.url || '').split(':');
        const targetPort = parseInt(targetPortStr, 10) || 443;

        this.trackConnectionStart();
        const startTime = Date.now();
        const logId = `connect_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`;
        let bytesTransferred = 0;

        // Force connection through chosen adapter IP
        const targetSocket = net.connect({
          host: targetHost,
          port: targetPort,
          localAddress: adapter.address
        }, () => {
          clientSocket.write('HTTP/1.1 200 Connection Established\r\n\r\n');
          if (head && head.length > 0) {
            targetSocket.write(head);
            bytesTransferred += head.length;
            this.recordBytes(adapter, 0, head.length);
          }

          clientSocket.on('data', (chunk) => {
            bytesTransferred += chunk.length;
            this.recordBytes(adapter, 0, chunk.length);
          });

          targetSocket.on('data', (chunk) => {
            bytesTransferred += chunk.length;
            this.recordBytes(adapter, chunk.length, 0);
          });

          clientSocket.pipe(targetSocket);
          targetSocket.pipe(clientSocket);
        });

        const cleanup = (err?: Error) => {
          clientSocket.destroy();
          targetSocket.destroy();
          this.trackConnectionEnd();
          this.recordAuditLog({
            id: logId,
            timestamp: Date.now(),
            protocol: 'HTTPS (CONNECT)',
            targetHost,
            targetPort,
            adapterId: adapter.id,
            adapterName: adapter.name,
            clientIp: (clientSocket as net.Socket).remoteAddress || '127.0.0.1',
            bytesTransferred,
            durationMs: Date.now() - startTime,
            status: err ? 'failed' : 'completed',
            errorMessage: err?.message
          });
        };

        targetSocket.on('error', (err) => cleanup(err));
        clientSocket.on('error', (err) => cleanup(err));
        targetSocket.on('end', () => cleanup());
        clientSocket.on('end', () => cleanup());
      });

      this.httpProxyServer.on('error', (err) => reject(err));
      this.httpProxyServer.listen(port, '0.0.0.0', () => resolve());
    });
  }

  private startSocks5Proxy(port: number): Promise<void> {
    return new Promise((resolve, reject) => {
      this.socksServer = net.createServer((clientSocket) => {
        clientSocket.once('data', (handshakeData) => {
          if (handshakeData[0] !== 0x05) {
            clientSocket.destroy();
            return;
          }

          // Method selection: 0x05 (SOCKS5), 0x00 (No Authentication Required)
          clientSocket.write(Buffer.from([0x05, 0x00]));

          clientSocket.once('data', (requestData) => {
            if (requestData[0] !== 0x05 || requestData[1] !== 0x01) {
              // Only SOCKS5 CONNECT supported
              clientSocket.write(Buffer.from([0x05, 0x07, 0x00, 0x01, 0, 0, 0, 0, 0, 0]));
              clientSocket.destroy();
              return;
            }

            const addressType = requestData[3];
            let targetHost = '';
            let targetPort = 0;

            if (addressType === 0x01) {
              // IPv4
              targetHost = requestData.slice(4, 8).join('.');
              targetPort = requestData.readUInt16BE(8);
            } else if (addressType === 0x03) {
              // Domain Name
              const domainLen = requestData[4];
              targetHost = requestData.slice(5, 5 + domainLen).toString('utf-8');
              targetPort = requestData.readUInt16BE(5 + domainLen);
            } else {
              clientSocket.write(Buffer.from([0x05, 0x08, 0x00, 0x01, 0, 0, 0, 0, 0, 0]));
              clientSocket.destroy();
              return;
            }

            const adapters = this.getAdapters();
            const adapter = this.loadBalancer.getNextAdapter(adapters);
            if (!adapter) {
              clientSocket.write(Buffer.from([0x05, 0x01, 0x00, 0x01, 0, 0, 0, 0, 0, 0]));
              clientSocket.destroy();
              return;
            }

            this.trackConnectionStart();
            const startTime = Date.now();
            const logId = `socks_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`;
            let bytesTransferred = 0;

            // Connect to target explicitly bound to adapter IP!
            const targetSocket = net.connect({
              host: targetHost,
              port: targetPort,
              localAddress: adapter.address
            }, () => {
              // SOCKS5 success response
              const reply = Buffer.alloc(requestData.length);
              requestData.copy(reply);
              reply[1] = 0x00; // Success
              clientSocket.write(reply);

              clientSocket.on('data', (chunk) => {
                bytesTransferred += chunk.length;
                this.recordBytes(adapter, 0, chunk.length);
              });

              targetSocket.on('data', (chunk) => {
                bytesTransferred += chunk.length;
                this.recordBytes(adapter, chunk.length, 0);
              });

              clientSocket.pipe(targetSocket);
              targetSocket.pipe(clientSocket);
            });

            const cleanup = (err?: Error) => {
              clientSocket.destroy();
              targetSocket.destroy();
              this.trackConnectionEnd();
              this.recordAuditLog({
                id: logId,
                timestamp: Date.now(),
                protocol: 'SOCKS5',
                targetHost,
                targetPort,
                adapterId: adapter.id,
                adapterName: adapter.name,
                clientIp: clientSocket.remoteAddress || '127.0.0.1',
                bytesTransferred,
                durationMs: Date.now() - startTime,
                status: err ? 'failed' : 'completed',
                errorMessage: err?.message
              });
            };

            targetSocket.on('error', (err) => cleanup(err));
            clientSocket.on('error', (err) => cleanup(err));
            targetSocket.on('end', () => cleanup());
            clientSocket.on('end', () => cleanup());
          });
        });
      });

      this.socksServer.on('error', (err) => reject(err));
      this.socksServer.listen(port, '0.0.0.0', () => resolve());
    });
  }

  private recordBytes(adapter: NetworkAdapter, rxBytes: number, txBytes: number): void {
    adapter.downloadBytes += rxBytes;
    adapter.uploadBytes += txBytes;
    this.totalDownloadBytes += rxBytes;
    this.totalUploadBytes += txBytes;
  }

  private trackConnectionStart(): void {
    this.activeConnectionsCount++;
    this.totalConnectionsCount++;
  }

  private trackConnectionEnd(): void {
    this.activeConnectionsCount = Math.max(0, this.activeConnectionsCount - 1);
  }

  private recordAuditLog(log: AuditLog): void {
    this.auditLogs.unshift(log);
    if (this.auditLogs.length > this.maxLogs) {
      this.auditLogs.pop();
    }
    this.emit('auditLog', log);
  }

  private tickTelemetry(): void {
    const adapters = this.getAdapters();
    const adapterSpeeds: Record<string, number> = {};

    let currentSecTotalRx = 0;
    let currentSecTotalTx = 0;

    for (const adapter of adapters) {
      const prev = this.lastAdapterBytes.get(adapter.id) || { rx: adapter.downloadBytes, tx: adapter.uploadBytes };
      const deltaRx = Math.max(0, adapter.downloadBytes - prev.rx);
      const deltaTx = Math.max(0, adapter.uploadBytes - prev.tx);

      adapter.downloadSpeedBps = deltaRx;
      adapter.uploadSpeedBps = deltaTx;

      adapterSpeeds[adapter.id] = deltaRx;
      currentSecTotalRx += deltaRx;
      currentSecTotalTx += deltaTx;

      this.lastAdapterBytes.set(adapter.id, { rx: adapter.downloadBytes, tx: adapter.uploadBytes });
    }

    this.lastTotalDownloadBytes = currentSecTotalRx;
    this.lastTotalUploadBytes = currentSecTotalTx;

    // Add to 60-point sliding window history
    const point: TelemetryPoint = {
      timestamp: Date.now(),
      totalDownloadBps: currentSecTotalRx,
      totalUploadBps: currentSecTotalTx,
      adapterSpeeds
    };

    this.telemetryHistory.push(point);
    if (this.telemetryHistory.length > 60) {
      this.telemetryHistory.shift();
    }

    this.emit('telemetry', this.getTelemetryState());
  }
}
