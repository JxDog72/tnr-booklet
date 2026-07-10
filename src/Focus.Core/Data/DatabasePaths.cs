namespace Focus.Core.Data;

public static class DatabasePaths
{
    public static string GetDefaultDataDirectory()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Focus");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetDbPath(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetDefaultDataDirectory(), "focus.db");

    public static string GetSettingsPath(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetDefaultDataDirectory(), "settings.json");

    public static string GetThemesPath(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetDefaultDataDirectory(), "themes.json");
}
