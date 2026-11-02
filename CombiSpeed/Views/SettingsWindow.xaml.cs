using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CombiSpeed.Models;
using CombiSpeed.Services;

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
        ChkEnablePac.IsChecked = config.EnablePac;
        ChkEnableThrottling.IsChecked = config.EnableBandwidthThrottling;
        ChkProportionalThrottling.IsChecked = config.EnforceProportionalThrottling;

        foreach (ComboBoxItem item in CmbShaperMode.Items)
        {
            if (item.Tag?.ToString() == config.ShaperMode.ToString())
            {
                item.IsSelected = true;
                break;
            }
        }
        ChkDynamicShaping.IsChecked = config.EnableDynamicShaping;

        ChkHighContrast.IsChecked = config.IsHighContrast;
        ChkReduceMotion.IsChecked = config.IsReduceMotion;
        ChkScreenReader.IsChecked = config.IsScreenReaderOptimized;
        SelectScale(config.UiScale);

        if (config.GlobalDownloadLimitBps > 0)
        {
            var (dlVal, dlUnit) = BandwidthThrottler.ConvertFromBytesPerSec(config.GlobalDownloadLimitBps, config.CustomThrottleUnit ?? "Mbps");
            TxtDownloadLimit.Text = dlVal.ToString("F1", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            SelectUnit(CmbDownloadUnit, dlUnit);
        }
        else
        {
            TxtDownloadLimit.Text = "0";
            SelectUnit(CmbDownloadUnit, "Mbps");
        }

        if (config.GlobalUploadLimitBps > 0)
        {
            var (ulVal, ulUnit) = BandwidthThrottler.ConvertFromBytesPerSec(config.GlobalUploadLimitBps, config.CustomThrottleUnit ?? "Mbps");
            TxtUploadLimit.Text = ulVal.ToString("F1", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            SelectUnit(CmbUploadUnit, ulUnit);
        }
        else
        {
            TxtUploadLimit.Text = "0";
            SelectUnit(CmbUploadUnit, "Mbps");
        }

        SelectBurstWindow(config.BurstToleranceSeconds);

        GridPacRules.ItemsSource = PacConfigModel.Current.Rules;

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

    private void SelectUnit(ComboBox cmb, string unit)
    {
        foreach (ComboBoxItem item in cmb.Items)
        {
            if (string.Equals(item.Content?.ToString(), unit, StringComparison.OrdinalIgnoreCase))
            {
                item.IsSelected = true;
                break;
            }
        }
    }

    private void SelectBurstWindow(double seconds)
    {
        foreach (ComboBoxItem item in CmbBurstWindow.Items)
        {
            if (item.Tag != null && double.TryParse(item.Tag.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double s))
            {
                if (Math.Abs(s - seconds) < 0.01)
                {
                    item.IsSelected = true;
                    return;
                }
            }
        }
        CmbBurstWindow.SelectedIndex = 1;
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

    private void PreviewPac_Click(object sender, RoutedEventArgs e)
    {
        int httpPort = int.TryParse(TxtHttpPort.Text, out int h) ? h : 8080;
        int socksPort = int.TryParse(TxtSocksPort.Text, out int s) ? s : 1080;
        string pac = PacConfigModel.Current.GeneratePacScript(httpPort, socksPort);
        MessageBox.Show(pac, "Generated PAC Script Preview", MessageBoxButton.OK, MessageBoxImage.Information);
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
        _config.EnablePac = ChkEnablePac.IsChecked == true;

        if (CmbShaperMode.SelectedItem is ComboBoxItem shaperItem && Enum.TryParse<SpeedShaperMode>(shaperItem.Tag?.ToString(), out var shaperMode))
        {
            _config.ShaperMode = shaperMode;
        }
        _config.EnableDynamicShaping = ChkDynamicShaping.IsChecked == true;

        _config.EnableBandwidthThrottling = ChkEnableThrottling.IsChecked == true;
        _config.EnforceProportionalThrottling = ChkProportionalThrottling.IsChecked == true;

        if (double.TryParse(TxtDownloadLimit.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double dl) && dl > 0)
        {
            string unit = (CmbDownloadUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mbps";
            _config.GlobalDownloadLimitBps = BandwidthThrottler.ConvertToBytesPerSec(dl, unit);
            _config.CustomThrottleUnit = unit;
        }
        else
        {
            _config.GlobalDownloadLimitBps = 0;
        }

        if (double.TryParse(TxtUploadLimit.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double ul) && ul > 0)
        {
            string unit = (CmbUploadUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mbps";
            _config.GlobalUploadLimitBps = BandwidthThrottler.ConvertToBytesPerSec(ul, unit);
        }
        else
        {
            _config.GlobalUploadLimitBps = 0;
        }

        if (CmbBurstWindow.SelectedItem is ComboBoxItem burstItem && burstItem.Tag != null &&
            double.TryParse(burstItem.Tag.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double burst))
        {
            _config.BurstToleranceSeconds = burst;
        }

        _config.IsHighContrast = ChkHighContrast.IsChecked == true;
        _config.IsReduceMotion = ChkReduceMotion.IsChecked == true;
        _config.IsScreenReaderOptimized = ChkScreenReader.IsChecked == true;

        if (CmbUiScale.SelectedItem is ComboBoxItem scaleItem && scaleItem.Tag != null &&
            double.TryParse(scaleItem.Tag.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double sc))
        {
            _config.UiScale = sc;
        }

        DialogResult = true;
        Close();
    }

    private void SelectScale(double scale)
    {
        foreach (ComboBoxItem item in CmbUiScale.Items)
        {
            if (item.Tag != null && double.TryParse(item.Tag.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double s))
            {
                if (Math.Abs(s - scale) < 0.05)
                {
                    item.IsSelected = true;
                    return;
                }
            }
        }
        CmbUiScale.SelectedIndex = 0;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
