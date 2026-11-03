using System.Globalization;
using System.Windows;
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

public class EqualityToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter != null)
        {
            if (targetType == typeof(int) && int.TryParse(parameter.ToString(), out int intVal))
                return intVal;
            if (targetType == typeof(double) && double.TryParse(parameter.ToString(), CultureInfo.InvariantCulture, out double dVal))
                return dVal;
            return parameter;
        }
        return Binding.DoNothing;
    }
}

public class ScaleToPercentStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double scale)
        {
            return $"{Math.Round(scale * 100)}%";
        }
        return "100%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class EqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;
        bool match = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        return match ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToRotatorStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is bool b && b) ? "🛑 Stop Rotator" : "▶ Start Rotator";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToOnlineStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is bool b && b) ? "ONLINE (ACTIVE)" : "STOPPED";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
