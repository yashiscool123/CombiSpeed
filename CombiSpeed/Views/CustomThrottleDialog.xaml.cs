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
    private bool _isReady = false;

    public CustomThrottleDialog(ProxyConfigModel config, IEnumerable<NetworkAdapterModel> adapters)
    {
        InitializeComponent();
        _config = config;
        _adapters = adapters;

        // Populate Shaper Mode
        SelectShaperMode(config.ShaperMode);
        ChkDynamicShaping.IsChecked = config.EnableDynamicShaping;

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
        ChkCombineSpeeds.IsChecked = config.CombineSpeedsMode;

        // Burst Window
        SelectBurstWindow(config.BurstToleranceSeconds);

        // Adapters
        ListAdapters.ItemsSource = _adapters.Where(a => a.IsEnabled).ToList();

        _isReady = true;
        UpdateEquivalents();
    }

    private void SelectShaperMode(SpeedShaperMode mode)
    {
        foreach (ComboBoxItem item in CmbShaperMode.Items)
        {
            if (item.Tag?.ToString() == mode.ToString())
            {
                item.IsSelected = true;
                break;
            }
        }
        UpdateShaperModeDescription(mode);
    }

    private void UpdateShaperModeDescription(SpeedShaperMode mode)
    {
        if (TxtShaperModeDescription == null) return;
        TxtShaperModeDescription.Text = mode switch
        {
            SpeedShaperMode.LowestLatency => "Optimizes for online multiplayer gaming, Discord voice & video calls. Anti-bufferbloat tight pacing (20ms) with real-time ping-weighted priority and jitter dampening.",
            SpeedShaperMode.BalancedDynamic => "Dynamically balances aggregate bandwidth across adapters while maintaining smooth latency response and proportional queue pacing.",
            _ => "Optimizes TCP window expansion, large burst buffers (150-200ms), and dynamic cross-adapter capacity sharing for peak download & upload speeds."
        };
    }

    private void ShaperMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isReady) return;
        if (CmbShaperMode?.SelectedItem is ComboBoxItem item && Enum.TryParse<SpeedShaperMode>(item.Tag?.ToString(), out var mode))
        {
            UpdateShaperModeDescription(mode);
            // Automatically align burst window to optimal mode profile
            if (CmbBurstWindow != null)
            {
                switch (mode)
                {
                    case SpeedShaperMode.LowestLatency:
                        SelectBurstWindow(0.02);
                        break;
                    case SpeedShaperMode.MaximumBandwidth:
                        SelectBurstWindow(0.25);
                        break;
                    case SpeedShaperMode.BalancedDynamic:
                        SelectBurstWindow(0.05);
                        break;
                }
            }
        }
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

    private void UpdateEquivalents()
    {
        if (!_isReady) return;
        if (TxtDownloadEquiv == null || TxtUploadEquiv == null || CmbDownloadUnit == null || CmbUploadUnit == null || TxtDownloadLimit == null || TxtUploadLimit == null) return;

        if (double.TryParse(TxtDownloadLimit.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double dl) && dl > 0)
        {
            string unit = (CmbDownloadUnit.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mbps";
            long bps = BandwidthThrottler.ConvertToBytesPerSec(dl, unit);
            TxtDownloadEquiv.Text = $"= {NetworkAdapterModel.FormatSpeedWithBits(bps)}";
        }
        else
        {
            TxtDownloadEquiv.Text = "= Unlimited / Dynamic";
        }

        if (double.TryParse(TxtUploadLimit.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double ul) && ul > 0)
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
        if (!_isReady) return;
        UpdateEquivalents();
        if (ChkAutoUpload?.IsChecked == true && TxtDownloadLimit != null && double.TryParse(TxtDownloadLimit.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double dl))
        {
            if (TxtUploadLimit != null)
                TxtUploadLimit.Text = (dl / 2.0).ToString("F1", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            if (CmbUploadUnit != null && CmbDownloadUnit != null)
                CmbUploadUnit.SelectedIndex = CmbDownloadUnit.SelectedIndex;
        }
    }

    private void DownloadUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isReady) return;
        UpdateEquivalents();
        if (ChkAutoUpload?.IsChecked == true && CmbUploadUnit != null && CmbDownloadUnit != null)
        {
            CmbUploadUnit.SelectedIndex = CmbDownloadUnit.SelectedIndex;
        }
    }

    private void UploadLimit_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isReady) return;
        UpdateEquivalents();
    }

    private void UploadUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isReady) return;
        UpdateEquivalents();
    }

    private void AutoUpload_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        if (TxtDownloadLimit != null && double.TryParse(TxtDownloadLimit.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double dl))
        {
            if (TxtUploadLimit != null)
                TxtUploadLimit.Text = (dl / 2.0).ToString("F1", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            if (CmbUploadUnit != null && CmbDownloadUnit != null)
                CmbUploadUnit.SelectedIndex = CmbDownloadUnit.SelectedIndex;
        }
    }

    private void QuickCap_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null && TxtDownloadLimit != null && CmbDownloadUnit != null)
        {
            TxtDownloadLimit.Text = btn.Tag.ToString();
            SelectUnit(CmbDownloadUnit, "Mbps");
            if (ChkAutoUpload?.IsChecked == true && double.TryParse(btn.Tag.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dl))
            {
                if (TxtUploadLimit != null)
                    TxtUploadLimit.Text = (dl / 2.0).ToString("F1", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
                if (CmbUploadUnit != null)
                    SelectUnit(CmbUploadUnit, "Mbps");
            }
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // 1. Shaper Mode & Dynamic Adaptation
        if (CmbShaperMode?.SelectedItem is ComboBoxItem modeItem && Enum.TryParse<SpeedShaperMode>(modeItem.Tag?.ToString(), out var mode))
        {
            _config.ShaperMode = mode;
        }
        _config.EnableDynamicShaping = ChkDynamicShaping?.IsChecked == true;

        // 2. Download
        if (TxtDownloadLimit != null && double.TryParse(TxtDownloadLimit.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double dl) && dl > 0)
        {
            string unit = (CmbDownloadUnit?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mbps";
            _config.GlobalDownloadLimitBps = BandwidthThrottler.ConvertToBytesPerSec(dl, unit);
            _config.CustomThrottleUnit = unit;
        }
        else
        {
            _config.GlobalDownloadLimitBps = 0;
        }

        // 3. Upload
        if (TxtUploadLimit != null && double.TryParse(TxtUploadLimit.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double ul) && ul > 0)
        {
            string unit = (CmbUploadUnit?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mbps";
            _config.GlobalUploadLimitBps = BandwidthThrottler.ConvertToBytesPerSec(ul, unit);
        }
        else
        {
            _config.GlobalUploadLimitBps = 0;
        }

        // 4. Policies
        _config.EnableBandwidthThrottling = ChkEnableThrottling?.IsChecked == true;
        _config.CombineSpeedsMode = ChkCombineSpeeds?.IsChecked == true;
        _config.EnforceProportionalThrottling = !_config.CombineSpeedsMode;

        // 5. Burst Window
        if (CmbBurstWindow?.SelectedItem is ComboBoxItem burstItem && burstItem.Tag != null &&
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
