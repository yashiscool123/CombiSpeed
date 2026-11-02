using System.Globalization;
using System.Windows;
using CombiSpeed.Models;
using CombiSpeed.ViewModels;

namespace CombiSpeed.Views;

public partial class AccessibilityDialog : Window
{
    private readonly MainViewModel _vm;

    public AccessibilityDialog(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Owner = Application.Current.MainWindow;

        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        ChkHighContrast.IsChecked = _vm.IsHighContrast;
        ChkReduceMotion.IsChecked = _vm.IsReduceMotion;
        ChkScreenReader.IsChecked = _vm.IsScreenReaderOptimized;

        double scale = _vm.UiScale;
        if (scale >= 1.25)
        {
            RbScale130.IsChecked = true;
        }
        else if (scale >= 1.10)
        {
            RbScale115.IsChecked = true;
        }
        else
        {
            RbScale100.IsChecked = true;
        }
    }

    private void ScaleRadio_Checked(object sender, RoutedEventArgs e)
    {
        // Handled when Apply is clicked
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        ChkHighContrast.IsChecked = false;
        ChkReduceMotion.IsChecked = false;
        ChkScreenReader.IsChecked = false;
        RbScale100.IsChecked = true;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        double scale = 1.0;
        if (RbScale130.IsChecked == true) scale = 1.30;
        else if (RbScale115.IsChecked == true) scale = 1.15;

        _vm.IsHighContrast = ChkHighContrast.IsChecked == true;
        _vm.IsReduceMotion = ChkReduceMotion.IsChecked == true;
        _vm.IsScreenReaderOptimized = ChkScreenReader.IsChecked == true;
        _vm.UiScale = scale;

        // Persist
        _vm.Config.IsHighContrast = _vm.IsHighContrast;
        _vm.Config.IsReduceMotion = _vm.IsReduceMotion;
        _vm.Config.IsScreenReaderOptimized = _vm.IsScreenReaderOptimized;
        _vm.Config.UiScale = scale;

        _vm.SaveAccessibilitySettings();

        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
