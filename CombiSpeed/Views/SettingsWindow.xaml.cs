using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CombiSpeed.Models;
using CombiSpeed.Services;

namespace CombiSpeed.Views;

public partial class SettingsWindow : Window
{
    private readonly ProxyConfigModel _config;
    private readonly Action? _onConfigChanged;
    private readonly WindowsProxyManager? _proxyManager;
    private bool _isInitializing = true;

    public SettingsWindow(ProxyConfigModel config, Action? onConfigChanged = null, WindowsProxyManager? proxyManager = null)
    {
        InitializeComponent();
        _config = config;
        _onConfigChanged = onConfigChanged;
        _proxyManager = proxyManager;

        TxtHttpPort.Text = config.HttpPort.ToString();
        TxtSocksPort.Text = config.SocksPort.ToString();
        TxtBridgePort.Text = config.VirtualBridgePort.ToString();
        ChkAutoVirtualBridge.IsChecked = config.EnableVirtualBridge;
        ChkAutoWindowsProxy.IsChecked = config.EnableWindowsProxy;
        ChkEnablePac.IsChecked = config.EnablePac;
        ChkEnableThrottling.IsChecked = config.EnableBandwidthThrottling;
        ChkCombineSpeeds.IsChecked = config.CombineSpeedsMode;

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

        _isInitializing = false;

        // Wire immediate auto-save on every setting change
        ChkAutoVirtualBridge.Checked += (s, e) => OnSettingInputChanged();
        ChkAutoVirtualBridge.Unchecked += (s, e) => OnSettingInputChanged();
        ChkAutoWindowsProxy.Checked += (s, e) => OnSettingInputChanged();
        ChkAutoWindowsProxy.Unchecked += (s, e) => OnSettingInputChanged();
        ChkEnablePac.Checked += (s, e) => OnSettingInputChanged();
        ChkEnablePac.Unchecked += (s, e) => OnSettingInputChanged();
        ChkEnableThrottling.Checked += (s, e) => OnSettingInputChanged();
        ChkEnableThrottling.Unchecked += (s, e) => OnSettingInputChanged();
        ChkCombineSpeeds.Checked += (s, e) => OnSettingInputChanged();
        ChkCombineSpeeds.Unchecked += (s, e) => OnSettingInputChanged();
        ChkDynamicShaping.Checked += (s, e) => OnSettingInputChanged();
        ChkDynamicShaping.Unchecked += (s, e) => OnSettingInputChanged();
        ChkHighContrast.Checked += (s, e) => OnSettingInputChanged();
        ChkHighContrast.Unchecked += (s, e) => OnSettingInputChanged();
        ChkReduceMotion.Checked += (s, e) => OnSettingInputChanged();
        ChkReduceMotion.Unchecked += (s, e) => OnSettingInputChanged();
        ChkScreenReader.Checked += (s, e) => OnSettingInputChanged();
        ChkScreenReader.Unchecked += (s, e) => OnSettingInputChanged();

        TxtHttpPort.TextChanged += (s, e) => OnSettingInputChanged();
        TxtSocksPort.TextChanged += (s, e) => OnSettingInputChanged();
        TxtBridgePort.TextChanged += (s, e) => OnSettingInputChanged();
        TxtDownloadLimit.TextChanged += (s, e) => OnSettingInputChanged();
        TxtUploadLimit.TextChanged += (s, e) => OnSettingInputChanged();

        CmbRoutingMode.SelectionChanged += (s, e) => OnSettingInputChanged();
        CmbShaperMode.SelectionChanged += (s, e) => OnSettingInputChanged();
        CmbDownloadUnit.SelectionChanged += (s, e) => OnSettingInputChanged();
        CmbUploadUnit.SelectionChanged += (s, e) => OnSettingInputChanged();
        CmbBurstWindow.SelectionChanged += (s, e) => OnSettingInputChanged();
        CmbUiScale.SelectionChanged += (s, e) => OnSettingInputChanged();
    }

    private void OnSettingInputChanged()
    {
        if (_isInitializing) return;
        ApplySettingsToModel();
        _onConfigChanged?.Invoke();
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

    private void ExportPac_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int httpPort = int.TryParse(TxtHttpPort.Text, out int h) ? h : 8080;
            int socksPort = int.TryParse(TxtSocksPort.Text, out int s) ? s : 1080;
            string pac = PacConfigModel.Current.GeneratePacScript(httpPort, socksPort);

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Proxy Auto-Config (PAC) File",
                FileName = "proxy.pac",
                DefaultExt = ".pac",
                Filter = "PAC Script (*.pac)|*.pac|JavaScript (*.js)|*.js|All Files (*.*)|*.*"
            };

            if (saveDialog.ShowDialog(this) == true)
            {
                System.IO.File.WriteAllText(saveDialog.FileName, pac, System.Text.Encoding.UTF8);
                MessageBox.Show($"PAC script successfully exported to:\n{saveDialog.FileName}", "Export PAC Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export PAC file: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplySettingsToModel()
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

        bool pacEnabled = ChkEnablePac.IsChecked == true;
        if (_config.EnablePac != pacEnabled)
        {
            _config.EnablePac = pacEnabled;
            if (_proxyManager != null)
            {
                if (pacEnabled) _proxyManager.EnablePac(_config.PacUrl);
                else _proxyManager.DisablePac();
            }
        }
        else
        {
            _config.EnablePac = pacEnabled;
        }

        if (CmbShaperMode.SelectedItem is ComboBoxItem shaperItem && Enum.TryParse<SpeedShaperMode>(shaperItem.Tag?.ToString(), out var shaperMode))
        {
            _config.ShaperMode = shaperMode;
        }
        _config.EnableDynamicShaping = ChkDynamicShaping.IsChecked == true;

        _config.EnableBandwidthThrottling = ChkEnableThrottling.IsChecked == true;
        _config.CombineSpeedsMode = ChkCombineSpeeds.IsChecked == true;
        _config.EnforceProportionalThrottling = !_config.CombineSpeedsMode;

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
            _config.CustomThrottleUnit = unit;
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
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ApplySettingsToModel();
        _onConfigChanged?.Invoke();
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
