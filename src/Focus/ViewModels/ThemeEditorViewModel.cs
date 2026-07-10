using System.Collections.ObjectModel;
using System.Reflection;
using Focus.Core.Models;
using Focus.Core.Themes;
using Focus.Services;
using Focus.Themes;

namespace Focus.ViewModels;

public sealed class ColorFieldVm : ViewModelBase
{
    private string _hex;

    public ColorFieldVm(string name, string hex)
    {
        Name = name;
        _hex = hex;
    }

    public string Name { get; }

    public string Hex
    {
        get => _hex;
        set
        {
            if (SetProperty(ref _hex, value))
                HexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? HexChanged;
}

public sealed class ThemeEditorViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private ThemeDefinition _theme;
    private string _themeName = "Focus Dark";
    private string _statusMessage = "";

    public ThemeEditorViewModel(AppServices services)
    {
        _services = services;
        var activeId = services.Settings.Current.ActiveThemeId;
        _theme = Clone(services.Themes.GetActive(activeId));
        ThemeName = _theme.Name;
        Colors = new ObservableCollection<ColorFieldVm>(BuildFields(_theme.Colors));
        foreach (var field in Colors)
            field.HexChanged += (_, _) => ApplyLive();
    }

    public ObservableCollection<ColorFieldVm> Colors { get; }

    public string ThemeName
    {
        get => _themeName;
        set => SetProperty(ref _themeName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public void ApplyLive()
    {
        var colors = ReadColors();
        ThemeApplicator.Apply(colors);
    }

    public void Save()
    {
        _theme.Name = string.IsNullOrWhiteSpace(ThemeName) ? "Custom" : ThemeName.Trim();
        _theme.Colors = ReadColors();
        if (string.IsNullOrWhiteSpace(_theme.Id))
            _theme.Id = ThemeCatalog.FocusDarkId;

        _services.Themes.Upsert(_theme);
        _services.Settings.Current.ActiveThemeId = _theme.Id;
        _services.Settings.Save();
        ThemeApplicator.Apply(_theme);
        StatusMessage = "Theme saved.";
    }

    public void ResetToDefaults()
    {
        var dark = ThemeCatalog.CreateFocusDark();
        _theme.Colors = dark.Colors;
        ThemeName = dark.Name;
        Colors.Clear();
        foreach (var f in BuildFields(_theme.Colors))
        {
            f.HexChanged += (_, _) => ApplyLive();
            Colors.Add(f);
        }
        ApplyLive();
        StatusMessage = "Reset to Focus Dark defaults (not saved yet).";
    }

    private ThemeColors ReadColors()
    {
        var colors = new ThemeColors();
        var type = typeof(ThemeColors);
        foreach (var field in Colors)
        {
            var prop = type.GetProperty(field.Name, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || !prop.CanWrite) continue;
            var hex = field.Hex?.Trim() ?? "";
            if (!hex.StartsWith('#') && hex.Length is 6 or 8)
                hex = "#" + hex;
            prop.SetValue(colors, hex);
        }
        return colors;
    }

    private static List<ColorFieldVm> BuildFields(ThemeColors colors)
    {
        var list = new List<ColorFieldVm>();
        foreach (var prop in typeof(ThemeColors).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType != typeof(string) || !prop.CanRead)
                continue;
            var value = prop.GetValue(colors) as string ?? "";
            list.Add(new ColorFieldVm(prop.Name, value));
        }
        return list;
    }

    private static ThemeDefinition Clone(ThemeDefinition src) => new()
    {
        Id = src.Id,
        Name = src.Name,
        Colors = new ThemeColors
        {
            BgApp = src.Colors.BgApp,
            BgSidebar = src.Colors.BgSidebar,
            BgToolbar = src.Colors.BgToolbar,
            BgSurface = src.Colors.BgSurface,
            BgSurfaceAlt = src.Colors.BgSurfaceAlt,
            BorderDefault = src.Colors.BorderDefault,
            BorderFocus = src.Colors.BorderFocus,
            TextPrimary = src.Colors.TextPrimary,
            TextSecondary = src.Colors.TextSecondary,
            TextMuted = src.Colors.TextMuted,
            Accent = src.Colors.Accent,
            Success = src.Colors.Success,
            Warning = src.Colors.Warning,
            Danger = src.Colors.Danger,
            Overdue = src.Colors.Overdue,
            SelectionBg = src.Colors.SelectionBg,
            SelectionFg = src.Colors.SelectionFg,
        }
    };
}
