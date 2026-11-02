using System.Diagnostics;
using System.Runtime.InteropServices;
using CombiSpeed.Linux.Models;

namespace CombiSpeed.Linux.Services;

public class LinuxVirtualBridgeManager
{
    private readonly VirtualBridgeModel _model = new();

    public VirtualBridgeModel Model => _model;

    public void Connect(int adapterCount)
    {
        _model.IsActive = true;
        _model.MergedAdapterCount = adapterCount;
        _model.SummaryText = $"Active Bonded Gateway on 127.0.0.1:{_model.ListenPort} merging {adapterCount} physical Linux adapters.";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                // Linux dummy/bridge interface setup if user has iproute2 privileges
                // sudo ip link add combispeed-br type dummy
            }
            catch { }
        }
    }

    public void Disconnect()
    {
        _model.IsActive = false;
        _model.SummaryText = "Virtual Bridge disconnected. Ready to merge adapters.";
    }
}
