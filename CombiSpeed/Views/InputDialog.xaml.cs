using System.Windows;

namespace CombiSpeed.Views;

public partial class InputDialog : Window
{
    public string ResponseText => TxtInput.Text.Trim();

    public InputDialog(string title, string subtitle, string defaultText = "")
    {
        InitializeComponent();
        TxtTitle.Text = title;
        TxtSubtitle.Text = subtitle;
        TxtInput.Text = defaultText;

        Loaded += (s, e) =>
        {
            TxtInput.Focus();
            TxtInput.SelectAll();
        };
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ResponseText))
        {
            MessageBox.Show("Please enter a valid profile name.", "CombiSpeed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
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

