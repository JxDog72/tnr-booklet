namespace Focus.Core.Data;

public static class DatabasePaths
{
    public const string LocationPointerFileName = "data-location.txt";

    public static string GetAppRootDirectory()
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

    public static string GetDefaultDataDirectory() => GetAppRootDirectory();

    public static string GetLocationPointerPath() =>
        Path.Combine(GetAppRootDirectory(), LocationPointerFileName);

    public static string GetActiveDataDirectory()
    {
        var root = GetAppRootDirectory();
        try
        {
            var pointer = GetLocationPointerPath();
            if (File.Exists(pointer))
            {
                var custom = File.ReadAllText(pointer).Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(custom))
                {
                    Directory.CreateDirectory(custom);
                    return Path.GetFullPath(custom);
                }
            }
        }
        catch
        {
            // Fall through to the default folder.
        }

        return root;
    }

    public static bool IsDefaultDirectory(string directory)
    {
        try
        {
            return PathsEqual(directory, GetAppRootDirectory());
        }
        catch
        {
            return false;
        }
    }

    public static bool PathsEqual(string a, string b)
    {
        var left = Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var right = Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetActiveDataDirectory(string? directory)
    {
        var pointer = GetLocationPointerPath();
        if (string.IsNullOrWhiteSpace(directory) || IsDefaultDirectory(directory))
        {
            if (File.Exists(pointer))
                File.Delete(pointer);
            return;
        }

        var full = Path.GetFullPath(directory);
        Directory.CreateDirectory(full);
        File.WriteAllText(pointer, full);
    }

    public static void CopyDataFiles(string fromDir, string toDir)
    {
        Directory.CreateDirectory(toDir);
        string[] names =
        {
            "tnr-booklet.db",
            "tnr-booklet.db-wal",
            "tnr-booklet.db-shm",
            "focus.db",
            "settings.json",
            "themes.json"
        };

        foreach (var name in names)
        {
            var src = Path.Combine(fromDir, name);
            if (File.Exists(src))
                File.Copy(src, Path.Combine(toDir, name), overwrite: true);
        }

        foreach (var src in Directory.GetFiles(fromDir, "last-fire-*.txt"))
        {
            var dest = Path.Combine(toDir, Path.GetFileName(src));
            File.Copy(src, dest, overwrite: true);
        }
    }

    public static string GetDbPath(string? dataDir = null)
    {
        var dir = dataDir ?? GetActiveDataDirectory();
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
        Path.Combine(dataDir ?? GetActiveDataDirectory(), "settings.json");

    public static string GetThemesPath(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetActiveDataDirectory(), "themes.json");
}
