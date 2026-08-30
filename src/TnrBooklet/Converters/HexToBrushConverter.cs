using System.Globalization;
using System.Windows.Data;
using Focus.Themes;
using Media = System.Windows.Media;

namespace Focus.Converters;

public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hex = value as string;
        if (ThemeApplicator.TryParseColor(hex ?? "", out var color))
            return new Media.SolidColorBrush(color);
        return new Media.SolidColorBrush(Media.Color.FromRgb(0xA7, 0x8B, 0xFA));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
