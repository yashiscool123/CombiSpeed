using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

public class HealthMonitor : IDisposable
{
    private readonly Func<List<NetworkAdapterModel>> _getAdapters;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private bool _isChecking;

    public event Action<List<NetworkAdapterModel>>? HealthUpdated;

    public HealthMonitor(Func<List<NetworkAdapterModel>> getAdapters)
    {
        _getAdapters = getAdapters;
    }

    public void Start(int intervalMs = 3500)
    {
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMs));

        _ = Task.Run(async () =>
        {
            // Initial immediate check
            await CheckAllAsync();

            while (_timer != null && await _timer.WaitForNextTickAsync(_cts.Token))
            {
                await CheckAllAsync();
            }
        });
    }

    public void Stop()
    {
        _cts?.Cancel();
        _timer?.Dispose();
        _timer = null;
    }

    public void TriggerImmediateCheck()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await CheckAllAsync();
            }
            catch
            {
                // Suppress background probe errors
            }
        });
    }

    private async Task CheckAllAsync()
    {
        if (_isChecking) return;
        _isChecking = true;

        try
        {
            var adapters = _getAdapters().Where(a => a.IsEnabled).ToList();
            var tasks = adapters.Select(CheckAdapterAsync);
            await Task.WhenAll(tasks);

            HealthUpdated?.Invoke(_getAdapters());
        }
        catch
        {
            // Suppress background probe errors
        }
        finally
        {
            _isChecking = false;
        }
    }

    private async Task CheckAdapterAsync(NetworkAdapterModel adapter)
    {
        if (!IPAddress.TryParse(adapter.Address, out var localIp)) return;

        var sw = Stopwatch.StartNew();
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = 2000;
            socket.SendTimeout = 2000;

            // Bind outgoing socket to this adapter's IP address!
            socket.Bind(new IPEndPoint(localIp, 0));

            using var cts = new CancellationTokenSource(2000);
            await socket.ConnectAsync("1.1.1.1", 80, cts.Token);

            sw.Stop();
            int rtt = (int)sw.ElapsedMilliseconds;

            adapter.PingMs = rtt;
            adapter.Status = rtt > 350 ? AdapterStatus.Degraded : AdapterStatus.Up;
            adapter.PacketLossPercent = Math.Max(0, adapter.PacketLossPercent - 10);
        }
        catch
        {
            adapter.PingMs = 999;
            adapter.PacketLossPercent = Math.Min(100, adapter.PacketLossPercent + 25);
            adapter.Status = adapter.PacketLossPercent >= 75 ? AdapterStatus.Down : AdapterStatus.Degraded;
        }

        // Asynchronously resolve WAN IP and GeoIP Location if not yet resolved
        if ((adapter.WanIp == "Resolving..." || string.IsNullOrWhiteSpace(adapter.Country)) && adapter.Status != AdapterStatus.Down)
        {
            _ = GeoIpService.ResolveAdapterLocationAsync(adapter, localIp);
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
