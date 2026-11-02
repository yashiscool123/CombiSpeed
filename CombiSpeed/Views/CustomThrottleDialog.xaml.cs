using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CombiSpeed.Models;
using CombiSpeed.Services;

namespace CombiSpeed.Views;

public partial class CustomThrottleDialog : Window
{
    private readonly ProxyConfigModel _config;
    private readonly IEnumerable<NetworkAdapterModel> _adapters;

    public CustomThrottleDialog(ProxyConfigModel config, IEnumerable<NetworkAdapterModel> adapters)
    {
        InitializeComponent();
        _config = config;
        _adapters = adapters;

        // Populate Download
        if (config.GlobalDownloadLimitBps > 0)
        {
            var (dlVal, dlUnit) = BandwidthThrottler.ConvertFromBytesPerSec(config.GlobalDownloadLimitBps, config.CustomThrottleUnit ?? "Mbps");
            TxtDownloadLimit.Text = dlVal.ToString("F1", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            SelectUnit(CmbDownloadUnit, dlUnit);
        }
        else
        {
            TxtDownloadLimit.Text = "500";
            SelectUnit(CmbDownloadUnit, "Mbps");
        }

        // Populate Upload
        if (config.GlobalUploadLimitBps > 0)
        {
            var (ulVal, ulUnit) = BandwidthThrottler.ConvertFromBytesPerSec(config.GlobalUploadLimitBps, config.CustomThrottleUnit ?? "Mbps");
            TxtUploadLimit.Text = ulVal.ToString("F1", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            SelectUnit(CmbUploadUnit, ulUnit);
        }
        else
        {
            TxtUploadLimit.Text = "250";
            SelectUnit(CmbUploadUnit, "Mbps");
        }

        ChkEnableThrottling.IsChecked = config.EnableBandwidthThrottling;
        ChkProportional.IsChecked = config.EnforceProportionalThrottling;

        // Burst Window
        SelectBurstWindow(config.BurstToleranceSeconds);

        // Adapters
        ListAdapters.ItemsSource = _adapters.Where(a => a.IsEnabled).ToList();

        UpdateEquivalents();
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
        // Default to 50ms
        CmbBurstWindow.SelectedIndex = 1;
    }

    private void UpdateEquivalents()
    {
        if (TxtDownloadEquiv == null || TxtUploadEquiv == null) return;

        if (double.TryParse(TxtDownloadLimit?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double dl) && dl > 0)
        {
            string unit = (CmbDownloadUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mbps";
            long bps = BandwidthThrottler.ConvertToBytesPerSec(dl, unit);
            TxtDownloadEquiv.Text = $"= {NetworkAdapterModel.FormatSpeedWithBits(bps)}";
        }
        else
        {
            TxtDownloadEquiv.Text = "= Unlimited / Dynamic";
        }

        if (double.TryParse(TxtUploadLimit?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double ul) && ul > 0)
        {
            string unit = (CmbUploadUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mbps";
            long bps = BandwidthThrottler.ConvertToBytesPerSec(ul, unit);
            TxtUploadEquiv.Text = $"= {NetworkAdapterModel.FormatSpeedWithBits(bps)}";
        }
        else
        {
            TxtUploadEquiv.Text = "= Unlimited / Dynamic";
        }
    }

    private void DownloadLimit_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateEquivalents();
        if (ChkAutoUpload?.IsChecked == true && double.TryParse(TxtDownloadLimit.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double dl))
        {
            TxtUploadLimit.Text = (dl / 2.0).ToString("F1", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            CmbUploadUnit.SelectedIndex = CmbDownloadUnit.SelectedIndex;
        }
    }

    private void DownloadUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateEquivalents();
        if (ChkAutoUpload?.IsChecked == true)
        {
            CmbUploadUnit.SelectedIndex = CmbDownloadUnit.SelectedIndex;
        }
    }

    private void UploadLimit_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateEquivalents();
    }

    private void UploadUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateEquivalents();
    }

    private void AutoUpload_Checked(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(TxtDownloadLimit.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double dl))
        {
            TxtUploadLimit.Text = (dl / 2.0).ToString("F1", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            CmbUploadUnit.SelectedIndex = CmbDownloadUnit.SelectedIndex;
        }
    }

    private void QuickCap_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
        {
            TxtDownloadLimit.Text = btn.Tag.ToString();
            SelectUnit(CmbDownloadUnit, "Mbps");
            if (ChkAutoUpload?.IsChecked == true && double.TryParse(btn.Tag.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dl))
            {
                TxtUploadLimit.Text = (dl / 2.0).ToString("F1", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
                SelectUnit(CmbUploadUnit, "Mbps");
            }
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // 1. Download
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

        // 2. Upload
        if (double.TryParse(TxtUploadLimit.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double ul) && ul > 0)
        {
            string unit = (CmbUploadUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mbps";
            _config.GlobalUploadLimitBps = BandwidthThrottler.ConvertToBytesPerSec(ul, unit);
        }
        else
        {
            _config.GlobalUploadLimitBps = 0;
        }

        // 3. Policies
        _config.EnableBandwidthThrottling = ChkEnableThrottling.IsChecked == true;
        _config.EnforceProportionalThrottling = ChkProportional.IsChecked == true;

        // 4. Burst Window
        if (CmbBurstWindow.SelectedItem is ComboBoxItem burstItem && burstItem.Tag != null &&
            double.TryParse(burstItem.Tag.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double burst))
        {
            _config.BurstToleranceSeconds = burst;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
