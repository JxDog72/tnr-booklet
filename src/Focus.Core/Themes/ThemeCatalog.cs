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
        var fallback = new ThemeColors();
        FillIfEmpty(theme.Colors, fallback);
        return theme;
    }

    private static void FillIfEmpty(ThemeColors colors, ThemeColors fallback)
    {
        static string Pick(string? value, string def) =>
            string.IsNullOrWhiteSpace(value) ? def : value;

        colors.ComboTypeBg = Pick(colors.ComboTypeBg, fallback.ComboTypeBg);
        colors.ComboTypeFg = Pick(colors.ComboTypeFg, fallback.ComboTypeFg);
        colors.ComboFolderBg = Pick(colors.ComboFolderBg, fallback.ComboFolderBg);
        colors.ComboFolderFg = Pick(colors.ComboFolderFg, fallback.ComboFolderFg);
        colors.ComboPriorityBg = Pick(colors.ComboPriorityBg, fallback.ComboPriorityBg);
        colors.ComboPriorityFg = Pick(colors.ComboPriorityFg, fallback.ComboPriorityFg);
    }
}
