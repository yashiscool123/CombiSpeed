import * as os from 'os';
import { exec } from 'child_process';
import { promisify } from 'util';
import { NetworkAdapter, AdapterType, AdapterStatus } from '../../shared/types';

const execAsync = promisify(exec);

interface WindowsNetAdapter {
  Name: string;
  InterfaceDescription: string;
  Status: string;
  LinkSpeed: string;
  MacAddress: string;
}

function determineAdapterType(name: string, description: string): AdapterType {
  const combined = `${name} ${description}`.toLowerCase();
  if (combined.includes('wi-fi') || combined.includes('wifi') || combined.includes('wireless') || combined.includes('802.11')) {
    return 'wifi';
  }
  if (combined.includes('bridge') || combined.includes('multiplexor')) {
    return 'bridge';
  }
  if (combined.includes('cellular') || combined.includes('mobile') || combined.includes('lte') || combined.includes('wwan')) {
    return 'cellular';
  }
  if (combined.includes('tap') || combined.includes('vpn') || combined.includes('hyper-v') || combined.includes('vethernet') || combined.includes('virtual')) {
    return 'virtual';
  }
  if (combined.includes('ethernet') || combined.includes('gigabit') || combined.includes('lan') || combined.includes('intel') || combined.includes('realtek')) {
    return 'ethernet';
  }
  return 'other';
}

function normalizeMac(mac: string | undefined): string {
  if (!mac) return '';
  return mac.replace(/[:-]/g, '').toLowerCase();
}

export class NetworkDetector {
  private lastAdapters: Map<string, NetworkAdapter> = new Map();

  async detectAdapters(): Promise<NetworkAdapter[]> {
    let winAdapters: WindowsNetAdapter[] = [];

    // Query Windows NetAdapter metadata via PowerShell
    try {
      const psCommand = `powershell -NoProfile -Command "Get-NetAdapter | Select-Object Name, InterfaceDescription, Status, LinkSpeed, MacAddress | ConvertTo-Json -Compress"`;
      const { stdout } = await execAsync(psCommand, { timeout: 4000 });
      if (stdout.trim()) {
        const parsed = JSON.parse(stdout.trim());
        winAdapters = Array.isArray(parsed) ? parsed : [parsed];
      }
    } catch (err) {
      // Fallback if powershell times out or fails
      // We will still extract details from os.networkInterfaces()
    }

    const osInterfaces = os.networkInterfaces();
    const result: NetworkAdapter[] = [];

    for (const [ifaceName, ifaceDetails] of Object.entries(osInterfaces)) {
      if (!ifaceDetails) continue;

      for (const iface of ifaceDetails) {
        // We only aggregate active non-internal IPv4 connections
        if (iface.internal || iface.family !== 'IPv4') continue;

        // Skip obvious virtual switch bridges if direct physical exists, but allow user configuration
        const normMac = normalizeMac(iface.mac);
        
        // Find matching Windows Adapter metadata
        const matchedWin = winAdapters.find(w => {
          if (normMac && normalizeMac(w.MacAddress) === normMac) return true;
          return w.Name.toLowerCase() === ifaceName.toLowerCase();
        });

        const id = `adapter_${normMac || iface.address.replace(/\./g, '_')}`;
        const existing = this.lastAdapters.get(id);

        const displayName = matchedWin?.Name || ifaceName;
        const description = matchedWin?.InterfaceDescription || ifaceName;
        const linkSpeed = matchedWin?.LinkSpeed || 'Unknown';
        const type = determineAdapterType(displayName, description);

        // Retain existing user-customized weights, or default
        const adapter: NetworkAdapter = {
          id,
          name: displayName,
          description,
          address: iface.address,
          netmask: iface.netmask,
          mac: iface.mac,
          family: 'IPv4',
          type,
          linkSpeed,
          status: (matchedWin?.Status?.toLowerCase() === 'up' || !matchedWin) ? 'up' : 'down',
          enabled: existing ? existing.enabled : true,
          weight: existing ? existing.weight : 50,
          pingMs: existing ? existing.pingMs : 0,
          packetLossPercent: existing ? existing.packetLossPercent : 0,
          downloadBytes: existing ? existing.downloadBytes : 0,
          uploadBytes: existing ? existing.uploadBytes : 0,
          downloadSpeedBps: existing ? existing.downloadSpeedBps : 0,
          uploadSpeedBps: existing ? existing.uploadSpeedBps : 0,
          wanIp: existing?.wanIp
        };

        this.lastAdapters.set(id, adapter);
        result.push(adapter);
      }
    }

    // Auto-normalize weights if all are 0 or unconfigured
    const enabled = result.filter(a => a.enabled);
    if (enabled.length > 0 && enabled.every(a => a.weight === 0)) {
      const equalShare = Math.floor(100 / enabled.length);
      enabled.forEach((a, idx) => {
        a.weight = idx === 0 ? 100 - equalShare * (enabled.length - 1) : equalShare;
      });
    }

    return result;
  }

  updateWeights(weights: Record<string, number>): void {
    for (const [id, weight] of Object.entries(weights)) {
      const adapter = this.lastAdapters.get(id);
      if (adapter) {
        adapter.weight = Math.max(0, Math.min(100, weight));
      }
    }
  }

  toggleAdapter(id: string, enabled: boolean): void {
    const adapter = this.lastAdapters.get(id);
    if (adapter) {
      adapter.enabled = enabled;
    }
  }

  getAdapter(id: string): NetworkAdapter | undefined {
    return this.lastAdapters.get(id);
  }

  getAllCached(): NetworkAdapter[] {
    return Array.from(this.lastAdapters.values());
  }
}
