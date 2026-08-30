using System.Text.Json;
using Focus.Core.Models;
using Focus.Core.Themes;

namespace Focus.Core.Services;

public sealed class ThemeFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _path;
    private List<ThemeDefinition> _themes = new();

    public ThemeFileService(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    public IReadOnlyList<ThemeDefinition> LoadOrSeed()
    {
        if (!File.Exists(_path))
        {
            _themes = new List<ThemeDefinition> { ThemeCatalog.CreateFocusDark() };
            SaveAll(_themes);
            return _themes;
        }

        var json = File.ReadAllText(_path);
        var loaded = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<List<ThemeDefinition>>(json, JsonOptions);

        if (loaded is null || loaded.Count == 0)
        {
            _themes = new List<ThemeDefinition> { ThemeCatalog.CreateFocusDark() };
            SaveAll(_themes);
            return _themes;
        }

        _themes = loaded
            .Select(t => ThemeCatalog.EnsureDefaults(t))
            .ToList();
        return _themes;
    }

    public IReadOnlyList<ThemeDefinition> GetAll() => _themes;

    public ThemeDefinition GetActive(string id)
    {
        if (_themes.Count == 0)
            LoadOrSeed();

        return _themes.FirstOrDefault(t => t.Id == id)
            ?? _themes.FirstOrDefault(t => t.Id == ThemeCatalog.FocusDarkId)
            ?? ThemeCatalog.CreateFocusDark();
    }

    public void SaveAll(IReadOnlyList<ThemeDefinition> themes)
    {
        ArgumentNullException.ThrowIfNull(themes);
        _themes = themes
            .Select(t => ThemeCatalog.EnsureDefaults(t))
            .ToList();
        SettingsService.AtomicWrite(_path, JsonSerializer.Serialize(_themes, JsonOptions));
    }

    public void Upsert(ThemeDefinition theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        theme = ThemeCatalog.EnsureDefaults(theme);
        var index = _themes.FindIndex(t => t.Id == theme.Id);
        if (index >= 0)
            _themes[index] = theme;
        else
            _themes.Add(theme);
        SaveAll(_themes);
    }
}
