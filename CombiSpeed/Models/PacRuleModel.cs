using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CombiSpeed.Models;

public enum PacAction
{
    Bonded,   // Route through CombiSpeed multi-WAN bonded pipeline
    Direct,   // Bypass proxy directly through default network
    Socks5Only // Route explicitly through SOCKS5
}

public class PacRuleModel : INotifyPropertyChanged
{
    private string _pattern = "*";
    public string Pattern
    {
        get => _pattern;
        set => SetField(ref _pattern, value);
    }

    private PacAction _action = PacAction.Bonded;
    public PacAction Action
    {
        get => _action;
        set => SetField(ref _action, value);
    }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    private string _description = "";
    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
