using System.Windows;
using System.Windows.Controls;
using CombiSpeed.Models;

namespace CombiSpeed.Views;

public partial class SettingsWindow : Window
{
    private readonly ProxyConfigModel _config;

    public SettingsWindow(ProxyConfigModel config)
    {
        InitializeComponent();
        _config = config;

        TxtHttpPort.Text = config.HttpPort.ToString();
        TxtSocksPort.Text = config.SocksPort.ToString();
        TxtBridgePort.Text = config.VirtualBridgePort.ToString();
        ChkAutoVirtualBridge.IsChecked = config.EnableVirtualBridge;
        ChkAutoWindowsProxy.IsChecked = config.EnableWindowsProxy;

        foreach (ComboBoxItem item in CmbRoutingMode.Items)
        {
            if (item.Tag?.ToString() == config.RoutingMode.ToString())
            {
                item.IsSelected = true;
                break;
            }
        }

        UpdateCliText();
        TxtHttpPort.TextChanged += (s, e) => UpdateCliText();
        TxtSocksPort.TextChanged += (s, e) => UpdateCliText();
    }

    private void UpdateCliText()
    {
        string http = int.TryParse(TxtHttpPort.Text, out int h) ? h.ToString() : "8080";
        string socks = int.TryParse(TxtSocksPort.Text, out int sk) ? sk.ToString() : "1080";
        TxtCliProxy.Text = $"HTTP_PROXY=http://127.0.0.1:{http} HTTPS_PROXY=http://127.0.0.1:{http} ALL_PROXY=socks5://127.0.0.1:{socks}";
    }

    private void CopyCli_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtCliProxy.Text);
        MessageBox.Show("Copied proxy environment string to clipboard!", "CombiSpeed", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtHttpPort.Text, out int httpPort)) _config.HttpPort = httpPort;
        if (int.TryParse(TxtSocksPort.Text, out int socksPort)) _config.SocksPort = socksPort;
        if (int.TryParse(TxtBridgePort.Text, out int bridgePort)) _config.VirtualBridgePort = bridgePort;

        if (CmbRoutingMode.SelectedItem is ComboBoxItem item && Enum.TryParse<RoutingMode>(item.Tag?.ToString(), out var mode))
        {
            _config.RoutingMode = mode;
        }

        _config.EnableVirtualBridge = ChkAutoVirtualBridge.IsChecked == true;
        _config.EnableWindowsProxy = ChkAutoWindowsProxy.IsChecked == true;
        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
