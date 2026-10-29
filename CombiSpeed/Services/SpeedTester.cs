using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

public class SpeedTestResult
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<AdapterSpeedItem> AdapterResults { get; set; } = new();
    public double BondedDownloadMbps { get; set; }
    public int BondedLatencyMs { get; set; }
    public int BoostPercentage { get; set; }
}

public class AdapterSpeedItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double DownloadMbps { get; set; }
    public int LatencyMs { get; set; }
}

public class SpeedTestProgress
{
    public string Stage { get; set; } = "idle";
    public int Percent { get; set; }
    public string Message { get; set; } = string.Empty;
    public SpeedTestResult? Results { get; set; }
}

public class SpeedTester
{
    private bool _isTesting;

    public event Action<SpeedTestProgress>? OnProgress;

    public async Task<SpeedTestResult> RunTestAsync(List<NetworkAdapterModel> adapters, CancellationToken token = default)
    {
        if (_isTesting) throw new InvalidOperationException("Speed test already in progress.");
        _isTesting = true;

        var activeAdapters = adapters.Where(a => a.IsEnabled && !string.IsNullOrWhiteSpace(a.Address) && a.Status != AdapterStatus.Down).ToList();
        if (activeAdapters.Count == 0)
        {
            _isTesting = false;
            throw new InvalidOperationException("No active adapters available for speed test.");
        }

        var result = new SpeedTestResult();

        try
        {
            EmitProgress("testing-adapters", 10, "Testing individual network adapters...");

            for (int i = 0; i < activeAdapters.Count; i++)
            {
                var adapter = activeAdapters[i];
                int stepPercent = 10 + (int)Math.Round(((double)(i + 1) / (activeAdapters.Count + 1)) * 40);

                EmitProgress("testing-adapters", stepPercent, $"Benchmarking {adapter.Name} ({adapter.Address})...");

                int latency = await MeasureLatencyAsync(adapter.Address, token);
                double speedMbps = await MeasureThroughputAsync(adapter.Address, parallelStreams: 4, durationMs: 3000, token);

                result.AdapterResults.Add(new AdapterSpeedItem
                {
                    Id = adapter.Id,
                    Name = adapter.Name,
                    DownloadMbps = Math.Round(speedMbps, 2),
                    LatencyMs = latency
                });
            }

            // Benchmark bonded concurrent throughput across all adapters
            EmitProgress("testing-bonded", 70, "Testing combined multi-adapter bonded throughput...");
            double bondedSpeed = await MeasureBondedThroughputAsync(activeAdapters, totalStreams: 8, durationMs: 3500, token);
            result.BondedDownloadMbps = Math.Round(bondedSpeed, 2);

            int avgLatency = result.AdapterResults.Count > 0 ? (int)result.AdapterResults.Average(a => a.LatencyMs) : 25;
            result.BondedLatencyMs = avgLatency;

            double primarySpeed = result.AdapterResults.FirstOrDefault()?.DownloadMbps ?? 1.0;
            result.BoostPercentage = primarySpeed > 0
                ? Math.Max(0, (int)Math.Round((bondedSpeed / primarySpeed) * 100) - 100)
                : 0;

            EmitProgress("completed", 100, "Speed test completed successfully!", result);
            return result;
        }
        finally
        {
            _isTesting = false;
        }
    }

    private void EmitProgress(string stage, int percent, string message, SpeedTestResult? results = null)
    {
        OnProgress?.Invoke(new SpeedTestProgress
        {
            Stage = stage,
            Percent = percent,
            Message = message,
            Results = results
        });
    }

    private static async Task<int> MeasureLatencyAsync(string ipAddress, CancellationToken token)
    {
        if (!IPAddress.TryParse(ipAddress, out var localIp)) return 999;
        var sw = Stopwatch.StartNew();

        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(localIp, 0));
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(2500);

            await socket.ConnectAsync("1.1.1.1", 80, cts.Token);
            sw.Stop();
            return (int)sw.ElapsedMilliseconds;
        }
        catch
        {
            return 999;
        }
    }

    private static async Task<double> MeasureThroughputAsync(string ipAddress, int parallelStreams, int durationMs, CancellationToken token)
    {
        if (!IPAddress.TryParse(ipAddress, out var localIp)) return 0;
        long totalBytes = 0;
        var sw = Stopwatch.StartNew();
        var timeoutCts = new CancellationTokenSource(durationMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);

        var tasks = Enumerable.Range(0, parallelStreams).Select(async _ =>
        {
            byte[] buf = new byte[16384];
            while (!linkedCts.Token.IsCancellationRequested)
            {
                try
                {
                    var handler = new SocketsHttpHandler
                    {
                        ConnectCallback = async (ctx, ct) =>
                        {
                            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            s.Bind(new IPEndPoint(localIp, 0));
                            await s.ConnectAsync(ctx.DnsEndPoint.Host, ctx.DnsEndPoint.Port, ct);
                            return new NetworkStream(s, ownsSocket: true);
                        }
                    };

                    using var client = new HttpClient(handler);
                    using var stream = await client.GetStreamAsync("https://speed.cloudflare.com/__down?bytes=5000000", linkedCts.Token);

                    while (!linkedCts.Token.IsCancellationRequested)
                    {
                        int read = await stream.ReadAsync(buf.AsMemory(0, buf.Length), linkedCts.Token);
                        if (read <= 0) break;
                        Interlocked.Add(ref totalBytes, read);
                    }
                }
                catch { break; }
            }
        });

        await Task.WhenAll(tasks);
        sw.Stop();
        double elapsedSec = Math.Max(0.5, sw.ElapsedMilliseconds / 1000.0);
        return (totalBytes * 8.0) / (elapsedSec * 1024 * 1024);
    }

    private static async Task<double> MeasureBondedThroughputAsync(List<NetworkAdapterModel> adapters, int totalStreams, int durationMs, CancellationToken token)
    {
        long totalBytes = 0;
        var sw = Stopwatch.StartNew();
        var timeoutCts = new CancellationTokenSource(durationMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);

        var tasks = Enumerable.Range(0, totalStreams).Select(async idx =>
        {
            var adapter = adapters[idx % adapters.Count];
            if (!IPAddress.TryParse(adapter.Address, out var localIp)) return;

            byte[] buf = new byte[16384];
            while (!linkedCts.Token.IsCancellationRequested)
            {
                try
                {
                    var handler = new SocketsHttpHandler
                    {
                        ConnectCallback = async (ctx, ct) =>
                        {
                            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            s.Bind(new IPEndPoint(localIp, 0));
                            await s.ConnectAsync(ctx.DnsEndPoint.Host, ctx.DnsEndPoint.Port, ct);
                            return new NetworkStream(s, ownsSocket: true);
                        }
                    };

                    using var client = new HttpClient(handler);
                    using var stream = await client.GetStreamAsync("https://speed.cloudflare.com/__down?bytes=5000000", linkedCts.Token);

                    while (!linkedCts.Token.IsCancellationRequested)
                    {
                        int read = await stream.ReadAsync(buf.AsMemory(0, buf.Length), linkedCts.Token);
                        if (read <= 0) break;
                        Interlocked.Add(ref totalBytes, read);
                    }
                }
                catch { break; }
            }
        });

        await Task.WhenAll(tasks);
        sw.Stop();
        double elapsedSec = Math.Max(0.5, sw.ElapsedMilliseconds / 1000.0);
        return (totalBytes * 8.0) / (elapsedSec * 1024 * 1024);
    }
}
