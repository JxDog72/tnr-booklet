using System.Globalization;
using System.Windows.Data;
using Wpf = System.Windows;

namespace Focus.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is true;
        if (Invert) flag = !flag;
        return flag ? Wpf.Visibility.Visible : Wpf.Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var vis = value is Wpf.Visibility.Visible;
        return Invert ? !vis : vis;
    }
}
