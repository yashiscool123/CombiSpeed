using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

/// <summary>
/// Implements RFC 1928 SOCKS5 UDP Associate Relay with multi-WAN adapter socket binding.
/// Accelerates UDP traffic (Gaming, DNS, VoIP, Video Streaming, WebRTC).
/// </summary>
public class Socks5UdpRelay : IDisposable
{
    private readonly LoadBalancerEngine _loadBalancer;
    private readonly Func<List<NetworkAdapterModel>> _getAdapters;
    private readonly Action<NetworkAdapterModel, long, long> _recordBytes;
    private readonly Action<AuditLogModel> _logAction;
    private readonly BandwidthThrottler? _throttler;

    private Socket? _clientUdpSocket;
    private IPEndPoint? _clientExpectedEndpoint;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<string, Socket> _outboundSockets = new();

    public int RelayPort { get; private set; }
    public IPAddress RelayAddress { get; private set; } = IPAddress.Loopback;

    public Socks5UdpRelay(
        LoadBalancerEngine loadBalancer,
        Func<List<NetworkAdapterModel>> getAdapters,
        Action<NetworkAdapterModel, long, long> recordBytes,
        Action<AuditLogModel> logAction,
        BandwidthThrottler? throttler = null)
    {
        _loadBalancer = loadBalancer;
        _getAdapters = getAdapters;
        _recordBytes = recordBytes;
        _logAction = logAction;
        _throttler = throttler;
    }

    /// <summary>
    /// Initializes the UDP relay listener on an ephemeral port.
    /// </summary>
    public int StartRelay(IPEndPoint? clientReportedEndpoint)
    {
        _cts = new CancellationTokenSource();
        _clientExpectedEndpoint = clientReportedEndpoint;

        _clientUdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _clientUdpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

        RelayPort = ((IPEndPoint)_clientUdpSocket.LocalEndPoint!).Port;

        // Start listening for UDP packets from SOCKS5 client
        _ = RunClientUdpListenerAsync(_clientUdpSocket, _cts.Token);

        return RelayPort;
    }

    private async Task RunClientUdpListenerAsync(Socket serverSocket, CancellationToken token)
    {
        var buffer = new byte[65535];
        EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

        while (!token.IsCancellationRequested)
        {
            try
            {
                var result = await serverSocket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEp);
                int length = result.ReceivedBytes;
                var senderEp = (IPEndPoint)result.RemoteEndPoint;

                // Update expected client endpoint dynamically on first datagram
                if (_clientExpectedEndpoint == null || _clientExpectedEndpoint.Address.Equals(IPAddress.Any))
                {
                    _clientExpectedEndpoint = senderEp;
                }

                if (length < 10) continue;

                // RFC 1928 SOCKS5 UDP Request format:
                // [0x00, 0x00] RSV (2 bytes)
                // [FRAG] (1 byte)
                // [ATYP] (1 byte): 0x01 (IPv4), 0x03 (Domain), 0x04 (IPv6)
                if (buffer[0] != 0x00 || buffer[1] != 0x00) continue;
                if (buffer[2] != 0x00) continue; // Fragmented not supported

                byte atyp = buffer[3];
                string targetHost = "";
                int targetPort = 0;
                int dataOffset = 0;

                if (atyp == 0x01) // IPv4
                {
                    var ip = new IPAddress(new byte[] { buffer[4], buffer[5], buffer[6], buffer[7] });
                    targetHost = ip.ToString();
                    targetPort = (buffer[8] << 8) | buffer[9];
                    dataOffset = 10;
                }
                else if (atyp == 0x03) // Domain name
                {
                    int domainLen = buffer[4];
                    targetHost = Encoding.ASCII.GetString(buffer, 5, domainLen);
                    targetPort = (buffer[5 + domainLen] << 8) | buffer[5 + domainLen + 1];
                    dataOffset = 5 + domainLen + 2;
                }
                else
                {
                    continue; // IPv6 ignored
                }

                int payloadLen = length - dataOffset;
                if (payloadLen <= 0) continue;

                byte[] payload = new byte[payloadLen];
                Buffer.BlockCopy(buffer, dataOffset, payload, 0, payloadLen);

                // Dispatch UDP payload through chosen physical adapter
                _ = Task.Run(() => ForwardUdpDatagramAsync(targetHost, targetPort, payload, senderEp, token), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Socket closed or error
            }
        }
    }

    private async Task ForwardUdpDatagramAsync(string targetHost, int targetPort, byte[] payload, IPEndPoint clientEp, CancellationToken token)
    {
        var adapters = _getAdapters();
        var chosenAdapter = _loadBalancer.GetNextAdapter(adapters);
        if (chosenAdapter == null || !IPAddress.TryParse(chosenAdapter.Address, out var localIp))
        {
            return;
        }

        try
        {
            // Resolve target endpoint
            IPAddress? targetIp = null;
            if (IPAddress.TryParse(targetHost, out var parsedIp))
            {
                targetIp = parsedIp;
            }
            else
            {
                var resolved = await Dns.GetHostAddressesAsync(targetHost, token);
                targetIp = resolved.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            }

            if (targetIp == null) return;

            var targetEndpoint = new IPEndPoint(targetIp, targetPort);
            string outboundKey = $"{chosenAdapter.Id}_{targetEndpoint}";

            var outboundSocket = _outboundSockets.GetOrAdd(outboundKey, _ =>
            {
                var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.Bind(new IPEndPoint(localIp, 0)); // BIND UDP SOCKET TO PHYSICAL ADAPTER IP!

                // Start receiver loop for responses from target
                Task.Run(() => RunTargetUdpReceiverAsync(sock, chosenAdapter, clientEp, token), token);
                return sock;
            });

            if (_throttler != null && payload.Length > 0)
            {
                await _throttler.ThrottleUploadAsync(chosenAdapter, payload.Length, token);
            }

            await outboundSocket.SendToAsync(payload, SocketFlags.None, targetEndpoint);
            _recordBytes(chosenAdapter, 0, payload.Length);

            _logAction(new AuditLogModel
            {
                Protocol = "SOCKS5 UDP-RELAY",
                TargetHost = targetHost,
                TargetPort = targetPort,
                AdapterId = chosenAdapter.Id,
                AdapterName = $"{chosenAdapter.Name} ({chosenAdapter.EffectivePercentage:F0}%)",
                ClientIp = clientEp.Address.ToString(),
                BytesTransferred = payload.Length,
                DurationMs = 1,
                Status = "Forwarded"
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UDP relay error: {ex.Message}");
        }
    }

    private async Task RunTargetUdpReceiverAsync(Socket outboundSocket, NetworkAdapterModel adapter, IPEndPoint clientEp, CancellationToken token)
    {
        var recvBuffer = new byte[65535];
        EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

        while (!token.IsCancellationRequested)
        {
            try
            {
                var result = await outboundSocket.ReceiveFromAsync(recvBuffer, SocketFlags.None, remoteEp);
                int length = result.ReceivedBytes;
                var sourceEp = (IPEndPoint)result.RemoteEndPoint;

                if (length <= 0 || _clientUdpSocket == null) continue;

                // Prepend RFC 1928 SOCKS5 UDP Header:
                // [0x00, 0x00, 0x00, 0x01, IP (4 bytes), Port (2 bytes), DATA]
                byte[] socksPacket = new byte[10 + length];
                socksPacket[0] = 0x00;
                socksPacket[1] = 0x00;
                socksPacket[2] = 0x00; // FRAG
                socksPacket[3] = 0x01; // IPv4

                byte[] ipBytes = sourceEp.Address.GetAddressBytes();
                Buffer.BlockCopy(ipBytes, 0, socksPacket, 4, 4);

                socksPacket[8] = (byte)((sourceEp.Port >> 8) & 0xFF);
                socksPacket[9] = (byte)(sourceEp.Port & 0xFF);

                Buffer.BlockCopy(recvBuffer, 0, socksPacket, 10, length);

                if (_throttler != null && length > 0)
                {
                    await _throttler.ThrottleDownloadAsync(adapter, length, token);
                }

                // Send encapsulated reply to client
                await _clientUdpSocket.SendToAsync(socksPacket, SocketFlags.None, clientEp);
                _recordBytes(adapter, length, 0);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try { _clientUdpSocket?.Close(); } catch { }
        foreach (var s in _outboundSockets.Values)
        {
            try { s.Close(); } catch { }
        }
        _outboundSockets.Clear();
    }
}
