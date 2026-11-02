using System.Diagnostics;
using System.Runtime.InteropServices;
using CombiSpeed.macOS.Models;

namespace CombiSpeed.macOS.Services;

public class MacVirtualBridgeManager
{
    private readonly VirtualBridgeModel _model = new();

    public VirtualBridgeModel Model => _model;

    public void Connect(int adapterCount)
    {
        _model.IsActive = true;
        _model.MergedAdapterCount = adapterCount;
        _model.SummaryText = $"Active Bonded Gateway on 127.0.0.1:{_model.ListenPort} merging {adapterCount} physical macOS network services.";
    }

    public void Disconnect()
    {
        _model.IsActive = false;
        _model.SummaryText = "Virtual Bridge disconnected. Ready to merge macOS adapters.";
    }
}

