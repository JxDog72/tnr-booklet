using System.Windows.Media;
using Focus.Core.Models;
using Media = System.Windows.Media;

namespace Focus.Themes;

public static class ThemeApplicator
{
    private static readonly (string Property, string ResourceKey)[] Map =
    [
        (nameof(ThemeColors.BgApp), "BgAppBrush"),
        (nameof(ThemeColors.BgSidebar), "BgSidebarBrush"),
        (nameof(ThemeColors.BgToolbar), "BgToolbarBrush"),
        (nameof(ThemeColors.BgSurface), "BgSurfaceBrush"),
        (nameof(ThemeColors.BgSurfaceAlt), "BgSurfaceAltBrush"),
        (nameof(ThemeColors.BorderDefault), "BorderDefaultBrush"),
        (nameof(ThemeColors.BorderFocus), "BorderFocusBrush"),
        (nameof(ThemeColors.TextPrimary), "TextPrimaryBrush"),
        (nameof(ThemeColors.TextSecondary), "TextSecondaryBrush"),
        (nameof(ThemeColors.TextMuted), "TextMutedBrush"),
        (nameof(ThemeColors.Accent), "AccentBrush"),
        (nameof(ThemeColors.Success), "SuccessBrush"),
        (nameof(ThemeColors.Warning), "WarningBrush"),
        (nameof(ThemeColors.Danger), "DangerBrush"),
        (nameof(ThemeColors.Overdue), "OverdueBrush"),
        (nameof(ThemeColors.SelectionBg), "SelectionBgBrush"),
        (nameof(ThemeColors.SelectionFg), "SelectionFgBrush"),
        (nameof(ThemeColors.ComboTypeBg), "ComboTypeBgBrush"),
        (nameof(ThemeColors.ComboTypeFg), "ComboTypeFgBrush"),
        (nameof(ThemeColors.ComboFolderBg), "ComboFolderBgBrush"),
        (nameof(ThemeColors.ComboFolderFg), "ComboFolderFgBrush"),
        (nameof(ThemeColors.ComboPriorityBg), "ComboPriorityBgBrush"),
        (nameof(ThemeColors.ComboPriorityFg), "ComboPriorityFgBrush"),
    ];

    public static void Apply(ThemeColors colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        var app = Application.Current;
        if (app is null)
            return;

        foreach (var (property, key) in Map)
        {
            var hex = typeof(ThemeColors).GetProperty(property)?.GetValue(colors) as string;
            if (string.IsNullOrWhiteSpace(hex))
                continue;
            if (!TryParseColor(hex, out var color))
                continue;
            app.Resources[key] = new SolidColorBrush(color);
        }
    }

    public static void Apply(ThemeDefinition theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Apply(theme.Colors ?? new ThemeColors());
    }

    public static bool TryParseColor(string hex, out Media.Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        hex = hex.Trim();
        if (hex.StartsWith('#'))
            hex = hex[1..];

        try
        {
            if (hex.Length == 6)
            {
                var r = Convert.ToByte(hex[..2], 16);
                var g = Convert.ToByte(hex[2..4], 16);
                var b = Convert.ToByte(hex[4..6], 16);
                color = Media.Color.FromRgb(r, g, b);
                return true;
            }

            if (hex.Length == 8)
            {
                var a = Convert.ToByte(hex[..2], 16);
                var r = Convert.ToByte(hex[2..4], 16);
                var g = Convert.ToByte(hex[4..6], 16);
                var b = Convert.ToByte(hex[6..8], 16);
                color = Media.Color.FromArgb(a, r, g, b);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
