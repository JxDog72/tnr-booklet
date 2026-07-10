using Focus.Core.Data;
using Focus.Core.Models;
using Focus.Core.Services;
using Focus.Core.Themes;
using FluentAssertions;
using Xunit;

namespace Focus.Tests;

public class ExportImportServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly TaskStore _store;
    private readonly SettingsService _settings;
    private readonly ThemeFileService _themes;
    private readonly ExportImportService _exportImport = new();

    public ExportImportServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "focus-export-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _store = new TaskStore(DatabasePaths.GetDbPath(_dir));
        _store.Initialize();
        _settings = new SettingsService(DatabasePaths.GetSettingsPath(_dir));
        _settings.Load();
        _themes = new ThemeFileService(DatabasePaths.GetThemesPath(_dir));
        _themes.LoadOrSeed();
    }

    public void Dispose()
    {
        _store.Dispose();
        try { Directory.Delete(_dir, true); } catch { /* ignore */ }
    }

    [Fact]
    public void Export_import_replace_roundtrip_preserves_task_title_and_folder_count()
    {
        var folder = _store.GetFolders().First();
        _store.UpsertTask(new TaskItem
        {
            Title = "Roundtrip task",
            FolderId = folder.Id
        });

        var folderCount = _store.GetFolders().Count;
        var bundle = _exportImport.Export(_store, _settings.Current, _themes.GetAll());

        // Mutate store so import must restore
        _store.ClearAll();
        _store.GetFolders().Should().BeEmpty();

        _exportImport.ImportReplace(_store, _settings, _themes, bundle);

        var tasks = _store.QueryTasks(SmartView.All, null, null);
        tasks.Should().Contain(t => t.Title == "Roundtrip task");
        _store.GetFolders().Should().HaveCount(folderCount);
    }

    [Fact]
    public void ImportReplace_null_tasks_throws_InvalidDataException()
    {
        var bundle = new ExportBundle
        {
            Version = 1,
            Folders = new List<Folder>(),
            Tags = new List<Tag>(),
            Tasks = null!,
            Settings = new AppSettings(),
            Themes = new List<ThemeDefinition> { ThemeCatalog.CreateFocusDark() }
        };

        var act = () => _exportImport.ImportReplace(_store, _settings, _themes, bundle);
        act.Should().Throw<InvalidDataException>();
    }
}
