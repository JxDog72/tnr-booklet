namespace Focus.Core.Data;

public static class DatabasePaths
{
    public static string GetDefaultDataDirectory()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(root, "TnrBooklet");
        var legacy = Path.Combine(root, "Focus");
        if (!Directory.Exists(dir) && Directory.Exists(legacy))
        {
            try
            {
                Directory.Move(legacy, dir);
            }
            catch
            {
                // Fall through and use a fresh TnrBooklet folder.
            }
        }

        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetDbPath(string? dataDir = null)
    {
        var dir = dataDir ?? GetDefaultDataDirectory();
        var path = Path.Combine(dir, "tnr-booklet.db");
        var legacy = Path.Combine(dir, "focus.db");
        if (!File.Exists(path) && File.Exists(legacy))
        {
            try
            {
                File.Move(legacy, path);
            }
            catch
            {
                return legacy;
            }
        }

        return path;
    }

    public static string GetSettingsPath(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetDefaultDataDirectory(), "settings.json");

    public static string GetThemesPath(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetDefaultDataDirectory(), "themes.json");
}
