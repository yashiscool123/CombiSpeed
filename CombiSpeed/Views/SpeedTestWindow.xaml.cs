using System.Windows;
using CombiSpeed.Models;
using CombiSpeed.Services;

namespace CombiSpeed.Views;

public partial class SpeedTestWindow : Window
{
    private readonly SpeedTester _speedTester;
    private readonly List<NetworkAdapterModel> _adapters;

    public SpeedTestWindow(SpeedTester speedTester, List<NetworkAdapterModel> adapters)
    {
        InitializeComponent();
        _speedTester = speedTester;
        _adapters = adapters;

        _speedTester.OnProgress += progress =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                TxtStatusMessage.Text = progress.Message;
                TxtProgressPercent.Text = $"{progress.Percent}%";
                ProgressBarTest.Value = progress.Percent;

                if (progress.Results != null)
                {
                    ShowResults(progress.Results);
                    BtnStartTest.IsEnabled = true;
                    BtnStartTest.Content = "Run Test Again";
                }
            });
        };
    }

    private async void StartTest_Click(object sender, RoutedEventArgs e)
    {
        BtnStartTest.IsEnabled = false;
        BtnStartTest.Content = "Testing...";
        BannerResult.Visibility = Visibility.Collapsed;

        try
        {
            await _speedTester.RunTestAsync(_adapters);
        }
        catch (Exception ex)
        {
            TxtStatusMessage.Text = $"Error: {ex.Message}";
            BtnStartTest.IsEnabled = true;
            BtnStartTest.Content = "Start Speed Test";
        }
    }

    private void ShowResults(SpeedTestResult res)
    {
        BannerResult.Visibility = Visibility.Visible;
        TxtBondedSpeed.Text = res.BondedDownloadMbps.ToString("F2");
        TxtBoostBadge.Text = $"+{res.BoostPercentage}% Boost";
        TxtBondedLatency.Text = $"Avg Latency: {res.BondedLatencyMs} ms";

        ItemsAdapterBars.ItemsSource = res.AdapterResults;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
