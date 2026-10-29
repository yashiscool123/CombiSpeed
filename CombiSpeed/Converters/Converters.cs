using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CombiSpeed.Models;

namespace CombiSpeed.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AdapterStatus status)
        {
            return status switch
            {
                AdapterStatus.Up => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                AdapterStatus.Degraded => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                AdapterStatus.Down => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F43F5E")),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class PingToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int ping)
        {
            if (ping <= 0 || ping >= 900) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            if (ping < 60) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            if (ping < 150) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F43F5E"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is bool b && b) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
