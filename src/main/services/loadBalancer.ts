import { NetworkAdapter, RoutingMode } from '../../shared/types';

export class LoadBalancer {
  private currentIndex = 0;
  private currentWeightCounter = 0;
  private routingMode: RoutingMode = 'weighted';
  private primaryAdapterId?: string;

  setMode(mode: RoutingMode): void {
    this.routingMode = mode;
    this.currentIndex = 0;
    this.currentWeightCounter = 0;
  }

  setPrimaryAdapter(id: string): void {
    this.primaryAdapterId = id;
  }

  getNextAdapter(adapters: NetworkAdapter[]): NetworkAdapter | null {
    // Filter active, enabled adapters with valid IPv4
    const available = adapters.filter(a => a.enabled && a.address);
    if (available.length === 0) return null;

    // Filter healthy adapters first, but fallback to all available if all are degraded/down
    const healthy = available.filter(a => a.status !== 'down');
    const pool = healthy.length > 0 ? healthy : available;

    if (pool.length === 1) {
      return pool[0];
    }

    switch (this.routingMode) {
      case 'least-latency': {
        // Pick the adapter with lowest ping
        return [...pool].sort((a, b) => (a.pingMs || 999) - (b.pingMs || 999))[0];
      }

      case 'failover': {
        // Check if designated primary adapter is healthy
        if (this.primaryAdapterId) {
          const primary = pool.find(a => a.id === this.primaryAdapterId);
          if (primary && primary.status === 'up') {
            return primary;
          }
        }
        // Fallback to highest link speed or first healthy adapter
        return pool[0];
      }

      case 'equal': {
        // Pure round robin
        this.currentIndex = (this.currentIndex + 1) % pool.length;
        return pool[this.currentIndex];
      }

      case 'weighted':
      default: {
        // Smooth weighted round-robin
        // Find minimum non-zero weight
        const validWeights = pool.filter(a => a.weight > 0);
        if (validWeights.length === 0) {
          this.currentIndex = (this.currentIndex + 1) % pool.length;
          return pool[this.currentIndex];
        }

        const minWeight = Math.max(1, Math.min(...validWeights.map(a => a.weight)));
        const current = validWeights[this.currentIndex % validWeights.length];
        
        this.currentWeightCounter++;
        const quota = Math.max(1, Math.round(current.weight / minWeight));

        if (this.currentWeightCounter >= quota) {
          this.currentIndex = (this.currentIndex + 1) % validWeights.length;
          this.currentWeightCounter = 0;
        }

        return current;
      }
    }
  }
}
