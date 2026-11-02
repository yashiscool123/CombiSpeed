using System.Diagnostics;
using System.Net.NetworkInformation;
using CombiSpeed.Linux.Models;

namespace CombiSpeed.Linux.Services;

public class LinuxHealthMonitor
{
    private readonly List<NetworkAdapterModel> _adapters;
    private readonly LinuxLoadBalancer _loadBalancer;
    private readonly ProxyConfigModel _config;
    private CancellationTokenSource? _cts;

    public LinuxHealthMonitor(List<NetworkAdapterModel> adapters, LinuxLoadBalancer loadBalancer, ProxyConfigModel config)
    {
        _adapters = adapters;
        _loadBalancer = loadBalancer;
        _config = config;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = RunMonitorLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task RunMonitorLoopAsync(CancellationToken ct)
    {
        using var ping = new Ping();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                foreach (var adapter in _adapters.Where(a => a.IsEnabled))
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        string target = !string.IsNullOrEmpty(adapter.Gateway) && adapter.Gateway != "0.0.0.0"
                            ? adapter.Gateway
                            : "1.1.1.1";

                        var reply = await ping.SendPingAsync(target, 1200);
                        sw.Stop();

                        if (reply.Status == IPStatus.Success)
                        {
                            int oldPing = adapter.PingMs;
                            adapter.PingMs = (int)reply.RoundtripTime;
                            adapter.JitterMs = Math.Abs(adapter.PingMs - oldPing);
                            adapter.PacketLossPercent = 0.0;
                        }
                        else
                        {
                            adapter.PingMs = 999;
                            adapter.PacketLossPercent = 100.0;
                        }
                    }
                    catch
                    {
                        adapter.PingMs = 25;
                    }
                }

                _loadBalancer.UpdateAdapters(_adapters, _config.RoutingMode, _config.ShaperMode);
            }
            catch { }

            await Task.Delay(3000, ct);
        }
    }
}
