import * as https from 'https';
import * as http from 'http';
import { EventEmitter } from 'events';
import { NetworkAdapter, SpeedTestProgress, SpeedTestResult } from '../../shared/types';

export class SpeedTester extends EventEmitter {
  private isTesting = false;

  async runTest(adapters: NetworkAdapter[]): Promise<SpeedTestResult> {
    if (this.isTesting) {
      throw new Error('Speed test already in progress');
    }

    this.isTesting = true;
    const activeAdapters = adapters.filter(a => a.enabled && a.address && a.status !== 'down');

    if (activeAdapters.length === 0) {
      this.isTesting = false;
      throw new Error('No active adapters available for speed test');
    }

    try {
      this.emitProgress('testing-adapters', 10, 'Testing individual network adapters...');

      const adapterResults: Array<{ id: string; name: string; downloadMbps: number; latencyMs: number }> = [];

      // Test each adapter independently
      for (let i = 0; i < activeAdapters.length; i++) {
        const adapter = activeAdapters[i];
        const stepPercent = 10 + Math.round(((i + 1) / (activeAdapters.length + 1)) * 40);
        this.emitProgress(
          'testing-adapters',
          stepPercent,
          `Benchmarking ${adapter.name} (${adapter.address})...`,
          adapter.name
        );

        const latency = await this.measureLatency(adapter.address);
        const speedMbps = await this.measureThroughput(adapter.address, 4); // 4 concurrent streams

        adapterResults.push({
          id: adapter.id,
          name: adapter.name,
          downloadMbps: Number(speedMbps.toFixed(2)),
          latencyMs: latency
        });
      }

      // Test bonded aggregate
      this.emitProgress('testing-bonded', 70, 'Testing combined multi-adapter bonded throughput...');

      const bondedStart = Date.now();
      const bondedSpeed = await this.measureBondedThroughput(activeAdapters, 8); // 8 concurrent streams across adapters
      const bondedDuration = Date.now() - bondedStart;

      const sumIndividualMbps = adapterResults.reduce((acc, r) => acc + r.downloadMbps, 0);
      const avgIndividualLatency = Math.round(adapterResults.reduce((acc, r) => acc + r.latencyMs, 0) / adapterResults.length);

      const boostPercentage = sumIndividualMbps > 0
        ? Math.round((bondedSpeed / (adapterResults[0]?.downloadMbps || 1)) * 100) - 100
        : 0;

      const result: SpeedTestResult = {
        timestamp: Date.now(),
        adapterResults,
        bondedResult: {
          downloadMbps: Number(bondedSpeed.toFixed(2)),
          latencyMs: avgIndividualLatency
        },
        boostPercentage: Math.max(0, boostPercentage)
      };

      this.emitProgress('completed', 100, 'Speed test completed successfully!', undefined, result);
      return result;
    } catch (err: any) {
      this.emitProgress('error', 0, `Speed test error: ${err?.message || 'Unknown error'}`);
      throw err;
    } finally {
      this.isTesting = false;
    }
  }

  private emitProgress(
    stage: SpeedTestProgress['stage'],
    percent: number,
    message: string,
    adapterName?: string,
    results?: SpeedTestResult
  ): void {
    const progress: SpeedTestProgress = {
      stage,
      percent,
      message,
      currentAdapterName: adapterName,
      results
    };
    this.emit('progress', progress);
  }

  private measureLatency(localAddress: string): Promise<number> {
    return new Promise((resolve) => {
      const start = Date.now();
      const req = https.request('https://1.1.1.1/cdn-cgi/trace', {
        localAddress,
        timeout: 3000
      }, (res) => {
        res.resume();
        res.on('end', () => resolve(Date.now() - start));
      });

      req.on('error', () => resolve(999));
      req.on('timeout', () => {
        req.destroy();
        resolve(999);
      });
      req.end();
    });
  }

  private async measureThroughput(localAddress: string, parallelStreams = 4, durationMs = 3500): Promise<number> {
    let totalBytes = 0;
    const testUrl = 'https://speed.cloudflare.com/__down?bytes=5000000'; // 5MB chunk
    const startTime = Date.now();
    let isDone = false;

    const runStream = (): Promise<void> => {
      return new Promise((resolve) => {
        if (isDone) return resolve();

        const req = https.request(testUrl, {
          localAddress,
          timeout: 4000
        }, (res) => {
          res.on('data', (chunk) => {
            totalBytes += chunk.length;
            if (Date.now() - startTime >= durationMs) {
              isDone = true;
              req.destroy();
              resolve();
            }
          });
          res.on('end', () => {
            if (!isDone && Date.now() - startTime < durationMs) {
              runStream().then(resolve);
            } else {
              resolve();
            }
          });
          res.on('error', () => resolve());
        });

        req.on('error', () => resolve());
        req.on('timeout', () => {
          req.destroy();
          resolve();
        });
        req.end();
      });
    };

    await Promise.all(Array.from({ length: parallelStreams }, () => runStream()));
    const elapsedSeconds = Math.max(0.5, (Date.now() - startTime) / 1000);
    const mbps = (totalBytes * 8) / (elapsedSeconds * 1024 * 1024);
    return mbps;
  }

  private async measureBondedThroughput(adapters: NetworkAdapter[], totalStreams = 8, durationMs = 4000): Promise<number> {
    let totalBytes = 0;
    const testUrl = 'https://speed.cloudflare.com/__down?bytes=5000000';
    const startTime = Date.now();
    let isDone = false;

    const streams = Array.from({ length: totalStreams }, (_, i) => {
      const adapter = adapters[i % adapters.length];
      return new Promise<void>((resolve) => {
        const fetchNext = () => {
          if (isDone || Date.now() - startTime >= durationMs) {
            isDone = true;
            return resolve();
          }

          const req = https.request(testUrl, {
            localAddress: adapter.address,
            timeout: 4000
          }, (res) => {
            res.on('data', (chunk) => {
              totalBytes += chunk.length;
              if (Date.now() - startTime >= durationMs) {
                isDone = true;
                req.destroy();
                resolve();
              }
            });
            res.on('end', () => {
              if (!isDone) fetchNext();
              else resolve();
            });
            res.on('error', () => resolve());
          });

          req.on('error', () => resolve());
          req.on('timeout', () => {
            req.destroy();
            resolve();
          });
          req.end();
        };

        fetchNext();
      });
    });

    await Promise.all(streams);
    const elapsedSeconds = Math.max(0.5, (Date.now() - startTime) / 1000);
    const mbps = (totalBytes * 8) / (elapsedSeconds * 1024 * 1024);
    return mbps;
  }
}
