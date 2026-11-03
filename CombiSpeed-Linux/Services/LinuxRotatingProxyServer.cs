using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CombiSpeed.Linux.Models;

namespace CombiSpeed.Linux.Services;

public class LinuxRotatingProxyServer
{
    private readonly RotatingProxyConfigModel _config;
    private readonly LinuxRotatingProxyRouter _router;
    private readonly Func<IEnumerable<NetworkAdapterModel>> _getAdapters;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public int Port => _config.RotatingProxyPort;

    public event Action<string>? OnLog;

    public LinuxRotatingProxyServer(
        RotatingProxyConfigModel config,
        LinuxRotatingProxyRouter router,
        Func<IEnumerable<NetworkAdapterModel>> getAdapters)
    {
        _config = config;
        _router = router;
        _getAdapters = getAdapters;
    }

    public void Start()
    {
        if (_isRunning) return;

        try
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, _config.RotatingProxyPort);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start(256);
            _isRunning = true;
            OnLog?.Invoke($"[RotatingProxy] Linux Rotating Proxy server listening on 127.0.0.1:{_config.RotatingProxyPort}");
            _ = AcceptLoopAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[RotatingProxy] Failed to start: {ex.Message}");
            _isRunning = false;
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
        }
        catch { }
        finally
        {
            _isRunning = false;
        }
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _isRunning)
        {
            try
            {
                var clientSocket = await _listener!.AcceptSocketAsync(ct);
                _ = Task.Run(async () =>
                {
                    try { await HandleClientAsync(clientSocket, ct); }
                    catch { }
                    finally { try { clientSocket.Dispose(); } catch { } }
                }, ct);
            }
            catch (OperationCanceledException) { break; }
            catch { await Task.Delay(100, ct); }
        }
    }

    private async Task HandleClientAsync(Socket clientSocket, CancellationToken ct)
    {
        clientSocket.NoDelay = true;
        using var clientStream = new NetworkStream(clientSocket, ownsSocket: false);

        byte[] headerBuffer = new byte[8192];
        int bytesRead = await clientStream.ReadAsync(headerBuffer, 0, headerBuffer.Length, ct);
        if (bytesRead <= 0) return;

        string requestString = Encoding.ASCII.GetString(headerBuffer, 0, bytesRead);
        string firstLine = requestString.Split("\r\n")[0];
        var parts = firstLine.Split(' ');
        if (parts.Length < 2) return;

        string method = parts[0].ToUpperInvariant();
        string target = parts[1];
        string targetHost = target.Contains(':') ? target.Split(':')[0] : target;
        string clientIp = ((IPEndPoint?)clientSocket.RemoteEndPoint)?.Address.ToString() ?? "127.0.0.1";

        bool isConnect = method == "CONNECT";
        var proxy = _router.SelectUpstreamProxy(targetHost, clientIp);
        if (proxy == null)
        {
            string err = "HTTP/1.1 503 Service Unavailable\r\nConnection: close\r\n\r\nNo proxies available in pool.\r\n";
            await clientStream.WriteAsync(Encoding.ASCII.GetBytes(err), ct);
            return;
        }

        try
        {
            var adapters = _getAdapters();
            using var upstreamSocket = await _router.ConnectToProxyAsync(proxy, adapters, ct);
            using var upstreamStream = new NetworkStream(upstreamSocket, ownsSocket: true);

            if (isConnect)
            {
                if (proxy.Protocol == ProxyProtocol.Socks5)
                {
                    byte[] greeting = string.IsNullOrEmpty(proxy.Username)
                        ? new byte[] { 0x05, 0x01, 0x00 }
                        : new byte[] { 0x05, 0x02, 0x00, 0x02 };
                    await upstreamStream.WriteAsync(greeting, ct);
                    byte[] gResp = new byte[2];
                    int gLen = await upstreamStream.ReadAsync(gResp, ct);
                    if (gLen != 2 || gResp[0] != 0x05) return;

                    if (gResp[1] == 0x02 && !string.IsNullOrEmpty(proxy.Username))
                    {
                        byte[] uB = Encoding.ASCII.GetBytes(proxy.Username);
                        byte[] pB = Encoding.ASCII.GetBytes(proxy.Password ?? "");
                        byte[] aB = new byte[1 + 1 + uB.Length + 1 + pB.Length];
                        aB[0] = 0x01; aB[1] = (byte)uB.Length;
                        Array.Copy(uB, 0, aB, 2, uB.Length);
                        aB[2 + uB.Length] = (byte)pB.Length;
                        Array.Copy(pB, 0, aB, 3 + uB.Length, pB.Length);
                        await upstreamStream.WriteAsync(aB, ct);
                        byte[] aResp = new byte[2];
                        int aLen = await upstreamStream.ReadAsync(aResp, ct);
                        if (aLen != 2 || aResp[1] != 0x00) return;
                    }

                    string host = target;
                    int port = 443;
                    if (target.Contains(':'))
                    {
                        var targetParts = target.Split(':');
                        host = targetParts[0];
                        _ = int.TryParse(targetParts[1], out port);
                    }
                    byte[] hB = Encoding.ASCII.GetBytes(host);
                    byte[] sReq = new byte[4 + 1 + hB.Length + 2];
                    sReq[0] = 0x05; sReq[1] = 0x01; sReq[2] = 0x00; sReq[3] = 0x03; sReq[4] = (byte)hB.Length;
                    Array.Copy(hB, 0, sReq, 5, hB.Length);
                    sReq[5 + hB.Length] = (byte)(port >> 8);
                    sReq[6 + hB.Length] = (byte)(port & 0xFF);
                    await upstreamStream.WriteAsync(sReq, ct);
                    byte[] sResp = new byte[10];
                    int sLen = await upstreamStream.ReadAsync(sResp, ct);
                    if (sLen < 4 || sResp[1] != 0x00) return;
                }
                else
                {
                    string authHeader = "";
                    if (!string.IsNullOrEmpty(proxy.Username))
                    {
                        string creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{proxy.Username}:{proxy.Password ?? ""}"));
                        authHeader = $"Proxy-Authorization: Basic {creds}\r\n";
                    }
                    string connectReq = $"CONNECT {target} HTTP/1.1\r\nHost: {target}\r\n{authHeader}Proxy-Connection: Keep-Alive\r\n\r\n";
                    await upstreamStream.WriteAsync(Encoding.ASCII.GetBytes(connectReq), ct);
                    byte[] respBuf = new byte[1024];
                    int respLen = await upstreamStream.ReadAsync(respBuf, ct);
                    string respStr = Encoding.ASCII.GetString(respBuf, 0, respLen);
                    if (!respStr.Contains("200")) return;
                }

                string ok = "HTTP/1.1 200 Connection Established\r\nProxy-Agent: CombiSpeed-Rotator/2.1.1\r\n\r\n";
                await clientStream.WriteAsync(Encoding.ASCII.GetBytes(ok), ct);
            }
            else
            {
                if (!string.IsNullOrEmpty(proxy.Username) && !requestString.Contains("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase))
                {
                    string creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{proxy.Username}:{proxy.Password ?? ""}"));
                    int firstCrLf = requestString.IndexOf("\r\n");
                    string injectedReq = firstCrLf >= 0
                        ? requestString.Insert(firstCrLf + 2, $"Proxy-Authorization: Basic {creds}\r\n")
                        : requestString;
                    await upstreamStream.WriteAsync(Encoding.ASCII.GetBytes(injectedReq), ct);
                }
                else
                {
                    await upstreamStream.WriteAsync(headerBuffer.AsMemory(0, bytesRead), ct);
                }
            }

            var t1 = clientStream.CopyToAsync(upstreamStream, ct);
            var t2 = upstreamStream.CopyToAsync(clientStream, ct);
            await Task.WhenAny(t1, t2);
        }
        catch
        {
            proxy.Failures++;
            proxy.IsHealthy = false;
        }
    }
}

