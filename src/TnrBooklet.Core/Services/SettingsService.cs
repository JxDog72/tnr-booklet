using System.Text.Json;
using Focus.Core.Models;

namespace Focus.Core.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _path;

    public SettingsService(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    public AppSettings Current { get; private set; } = new();

    public AppSettings Load()
    {
        if (!File.Exists(_path))
        {
            Current = new AppSettings();
            Save(Current);
            return Current;
        }

        var json = File.ReadAllText(_path);
        if (string.IsNullOrWhiteSpace(json))
        {
            Current = new AppSettings();
            Save(Current);
            return Current;
        }

        Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        return Current;
    }

    public void Save() => Save(Current);

    public void Save(AppSettings settings)
    {
        Current = settings ?? throw new ArgumentNullException(nameof(settings));
        AtomicWrite(_path, JsonSerializer.Serialize(Current, JsonOptions));
    }

    internal static void AtomicWrite(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, contents);
        File.Move(tempPath, path, overwrite: true);
    }
}
