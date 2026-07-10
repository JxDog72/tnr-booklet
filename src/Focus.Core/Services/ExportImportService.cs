using Focus.Core.Data;
using Focus.Core.Models;

namespace Focus.Core.Services;

public sealed class ExportImportService
{
    public ExportBundle Export(TaskStore store, AppSettings settings, IReadOnlyList<ThemeDefinition> themes)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(themes);

        return new ExportBundle
        {
            Version = 1,
            Folders = store.GetFolders().ToList(),
            Tags = store.GetTags().ToList(),
            Tasks = store.QueryTasks(SmartView.All, null, null).ToList(),
            Settings = settings,
            Themes = themes.ToList()
        };
    }

    public void ImportReplace(
        TaskStore store,
        SettingsService settingsService,
        ThemeFileService themeService,
        ExportBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(themeService);
        ArgumentNullException.ThrowIfNull(bundle);

        if (bundle.Version < 1)
            throw new InvalidDataException("Export bundle version must be >= 1.");

        if (bundle.Folders is null)
            throw new InvalidDataException("Export bundle folders collection is null.");
        if (bundle.Tags is null)
            throw new InvalidDataException("Export bundle tags collection is null.");
        if (bundle.Tasks is null)
            throw new InvalidDataException("Export bundle tasks collection is null.");
        if (bundle.Themes is null)
            throw new InvalidDataException("Export bundle themes collection is null.");
        if (bundle.Settings is null)
            throw new InvalidDataException("Export bundle settings is null.");

        foreach (var task in bundle.Tasks)
        {
            if (task is null)
                throw new InvalidDataException("Export bundle contains a null task.");
            if (string.IsNullOrWhiteSpace(task.Title))
                throw new InvalidDataException("Export bundle task is missing a title.");
            if (string.IsNullOrWhiteSpace(task.FolderId))
                throw new InvalidDataException("Export bundle task is missing a folder.");
        }

        store.ClearAll();

        foreach (var folder in bundle.Folders)
            store.UpsertFolder(folder);

        foreach (var tag in bundle.Tags)
            store.UpsertTag(tag);

        foreach (var task in bundle.Tasks)
            store.UpsertTask(task);

        settingsService.Save(bundle.Settings);
        themeService.SaveAll(bundle.Themes);
    }
}
