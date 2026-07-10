namespace Focus.Core.Themes;

using Focus.Core.Models;

public static class ThemeCatalog
{
    public const string FocusDarkId = "focus-dark";

    public static ThemeDefinition CreateFocusDark() => new()
    {
        Id = FocusDarkId,
        Name = "Focus Dark",
        Colors = new ThemeColors() // defaults already Focus Dark
    };

    public static ThemeDefinition EnsureDefaults(ThemeDefinition? theme)
    {
        if (theme is null) return CreateFocusDark();
        theme.Colors ??= new ThemeColors();
        return theme;
    }
}
