using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using CombiSpeed.Models;

namespace CombiSpeed.Views;

public partial class ProxySourcesDialog : Window
{
    private readonly RotatingProxyConfigModel _config;

    public ProxySourcesDialog(RotatingProxyConfigModel config)
    {
        InitializeComponent();
        _config = config;

        // Populate existing sources
        TxtSources.Text = _config.GetSourcesAsText();
        UpdateStats();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void TxtSources_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateStats();
    }

    private void UpdateStats()
    {
        if (TxtUrlCount == null || TxtSources == null) return;

        int count = CountValidUrls(TxtSources.Text);
        TxtUrlCount.Text = $"Total URLs: {count}";
    }

    private int CountValidUrls(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int validCount = 0;

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
            {
                continue;
            }

            if (Uri.TryCreate(line, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                validCount++;
            }
        }

        return validCount;
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        var defaults = RotatingProxyConfigModel.GetDefaultSources();
        TxtSources.Text = string.Join(Environment.NewLine, defaults);
        TxtFeedback.Text = $"Restored {defaults.Count} default repository URLs.";
    }

    private void PasteClipboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                string clip = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(clip))
                {
                    if (string.IsNullOrWhiteSpace(TxtSources.Text))
                    {
                        TxtSources.Text = clip.Trim();
                    }
                    else
                    {
                        TxtSources.Text = TxtSources.Text.TrimEnd() + Environment.NewLine + clip.Trim();
                    }
                    TxtFeedback.Text = "Appended clipboard text successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            TxtFeedback.Text = $"Clipboard error: {ex.Message}";
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        TxtSources.Clear();
        TxtFeedback.Text = "Cleared all proxy sources.";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        int count = _config.SetSourcesFromText(TxtSources.Text);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
