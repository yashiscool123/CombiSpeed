using System.Windows;
using System.Windows.Input;
using CombiSpeed.ViewModels;

namespace CombiSpeed.Views;

public partial class MainWindow : Window
{
    private MainViewModel? ViewModel => DataContext as MainViewModel;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            App.ViewModel = ViewModel;

            if (App.StartMinimized)
            {
                WindowState = WindowState.Minimized;
            }

            if (App.AutoStart && ViewModel != null)
            {
                if (!ViewModel.IsEngineRunning)
                {
                    await ViewModel.TogglePowerAsync();
                }
                if (!ViewModel.IsWindowsProxyActive)
                {
                    ViewModel.ToggleWindowsProxy();
                }
            }
        };
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OpenSpeedTest_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var win = new SpeedTestWindow(ViewModel.SpeedTesterService, ViewModel.Adapters.ToList())
        {
            Owner = this
        };
        win.ShowDialog();
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var win = new SettingsWindow(ViewModel.Config)
        {
            Owner = this
        };
        if (win.ShowDialog() == true)
        {
            ViewModel.UpdateThrottlingState();
            ViewModel.SaveCurrentCombinationImmediate();
        }
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var dlg = new InputDialog("Save Combination Profile", "Enter a name for this adapter combination and weight ratio:", "Custom Profile")
        {
            Owner = this
        };

        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.ResponseText))
        {
            ViewModel.CreateNewProfile(dlg.ResponseText);
        }
    }

    private void CustomThrottle_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.OpenCustomThrottleDialog();
    }
}
