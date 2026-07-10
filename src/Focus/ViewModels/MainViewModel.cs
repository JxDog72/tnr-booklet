using System.Collections.ObjectModel;
using System.Text.Json;
using Focus.Core.Data;
using Focus.Core.Models;
using Focus.Core.Recurrence;
using Focus.Core.Services.Messaging;
using Focus.Services;
using Focus.Themes;
using Focus.Views;
using Wpf = System.Windows;
using WpfControls = System.Windows.Controls;

namespace Focus.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private readonly Action? _onTrayVisibilityChanged;
    private string _quickAddText = "";
    private string _searchText = "";
    private string _statusText = "";
    private bool _sidebarCollapsed;
    private SmartView _selectedView = SmartView.All;
    private string? _selectedFolderId;
    private string? _selectedTagId;
    private TaskListItemVm? _selectedTask;
    private double _sidebarWidth = 220;

    public MainViewModel(AppServices services, Action? onTrayVisibilityChanged = null)
    {
        _services = services;
        _onTrayVisibilityChanged = onTrayVisibilityChanged;
        _sidebarCollapsed = services.Settings.Current.SidebarCollapsed;

        ToggleSidebarCommand = new RelayCommand(ToggleSidebar);
        SelectSidebarItemCommand = new RelayCommand(SelectSidebarItem);
        QuickAddCommand = new RelayCommand(QuickAdd, () => !string.IsNullOrWhiteSpace(QuickAddText));
        NewTaskCommand = new RelayCommand(() => OpenEditor(ItemKind.Todo));
        NewNoteCommand = new RelayCommand(() => OpenEditor(ItemKind.Note));
        EditTaskCommand = new RelayCommand(p => EditTask(p as TaskListItemVm ?? SelectedTask), _ => SelectedTask is not null || _ is TaskListItemVm);
        CompleteTaskCommand = new RelayCommand(p => CompleteTask(p as TaskListItemVm ?? SelectedTask));
        DeleteTaskCommand = new RelayCommand(p => DeleteTask(p as TaskListItemVm ?? SelectedTask), _ => SelectedTask is not null || _ is TaskListItemVm);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenThemeCommand = new RelayCommand(OpenThemeEditor);
        ExportCommand = new RelayCommand(Export);
        ImportCommand = new RelayCommand(Import);
        SendTodayListCommand = new AsyncRelayCommand(SendTodayListAsync);
        RefreshCommand = new RelayCommand(Refresh);
        AddFolderCommand = new RelayCommand(AddFolder);
        EditFolderCommand = new RelayCommand(EditSelectedFolder);

        Load();
    }

    public ObservableCollection<SidebarItemVm> ViewItems { get; } = new();
    public ObservableCollection<SidebarItemVm> FolderItems { get; } = new();
    public ObservableCollection<SidebarItemVm> TagItems { get; } = new();
    public ObservableCollection<TaskListItemVm> Tasks { get; } = new();

    public RelayCommand ToggleSidebarCommand { get; }
    public RelayCommand SelectSidebarItemCommand { get; }
    public RelayCommand QuickAddCommand { get; }
    public RelayCommand NewTaskCommand { get; }
    public RelayCommand NewNoteCommand { get; }
    public RelayCommand EditTaskCommand { get; }
    public RelayCommand CompleteTaskCommand { get; }
    public RelayCommand DeleteTaskCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand OpenThemeCommand { get; }
    public RelayCommand ExportCommand { get; }
    public RelayCommand ImportCommand { get; }
    public AsyncRelayCommand SendTodayListCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddFolderCommand { get; }
    public RelayCommand EditFolderCommand { get; }

    public string QuickAddText
    {
        get => _quickAddText;
        set
        {
            if (SetProperty(ref _quickAddText, value))
                QuickAddCommand.RaiseCanExecuteChanged();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                RefreshTasksOnly();
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool SidebarCollapsed
    {
        get => _sidebarCollapsed;
        set
        {
            if (!SetProperty(ref _sidebarCollapsed, value)) return;
            RaisePropertyChanged(nameof(SidebarColumnWidth));
            RaisePropertyChanged(nameof(SidebarToggleLabel));
            _services.Settings.Current.SidebarCollapsed = value;
            _services.Settings.Save();
        }
    }

    public double SidebarColumnWidth => SidebarCollapsed ? 48 : _sidebarWidth;
    public string SidebarToggleLabel => SidebarCollapsed ? "»" : "«";

    public TaskListItemVm? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (SetProperty(ref _selectedTask, value))
            {
                EditTaskCommand.RaiseCanExecuteChanged();
                DeleteTaskCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasTasks => Tasks.Count > 0;
    public bool IsEmpty => Tasks.Count == 0;
    public string EmptyMessage => "No tasks in this view";

    public string CurrentFilterLabel
    {
        get
        {
            if (_selectedTagId is not null)
            {
                var tag = TagItems.FirstOrDefault(t => t.Id == _selectedTagId);
                return tag is null ? "Tag" : $"#{tag.Title}";
            }
            if (_selectedFolderId is not null)
            {
                var folder = FolderItems.FirstOrDefault(f => f.Id == _selectedFolderId);
                return folder?.Title ?? "Folder";
            }
            return _selectedView.ToString();
        }
    }

    public void Load()
    {
        RebuildSidebar();
        RefreshTasksOnly();
        StatusText = $"Loaded · {_services.Store.GetFolders().Count} folders";
    }

    public void Refresh() => Load();

    private void RebuildSidebar()
    {
        ViewItems.Clear();
        foreach (var view in Enum.GetValues<SmartView>())
        {
            ViewItems.Add(new SidebarItemVm
            {
                Kind = SidebarItemKind.SmartView,
                Id = view.ToString(),
                Title = view.ToString(),
                View = view,
                IsSelected = _selectedFolderId is null && _selectedTagId is null && _selectedView == view
            });
        }

        FolderItems.Clear();
        foreach (var folder in _services.Store.GetFolders())
        {
            FolderItems.Add(new SidebarItemVm
            {
                Kind = SidebarItemKind.Folder,
                Id = folder.Id,
                Title = folder.Name,
                Color = folder.Color,
                IsSelected = _selectedFolderId == folder.Id && _selectedTagId is null
            });
        }

        TagItems.Clear();
        foreach (var tag in _services.Store.GetTags())
        {
            TagItems.Add(new SidebarItemVm
            {
                Kind = SidebarItemKind.Tag,
                Id = tag.Id,
                Title = tag.Name,
                Color = tag.Color,
                IsSelected = _selectedTagId == tag.Id
            });
        }

        RaisePropertyChanged(nameof(CurrentFilterLabel));
    }

    private void RefreshTasksOnly()
    {
        var folders = _services.Store.GetFolders();
        var tags = _services.Store.GetTags();
        var folderMap = folders.ToDictionary(f => f.Id);

        var view = _selectedTagId is not null || _selectedFolderId is not null
            ? SmartView.All
            : _selectedView;

        // When a folder is selected from sidebar we still honor smart view if we want;
        // plan says folder filter + smart view selection. Use selected view always, with optional folder/tag.
        if (_selectedFolderId is null && _selectedTagId is null)
            view = _selectedView;
        else
            view = SmartView.All;

        var tasks = _services.Store.QueryTasks(view, _selectedFolderId, _selectedTagId);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            tasks = tasks
                .Where(t =>
                    t.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || t.Notes.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        Tasks.Clear();
        foreach (var task in tasks)
        {
            folderMap.TryGetValue(task.FolderId, out var folder);
            Tasks.Add(new TaskListItemVm(task, folder, tags));
        }

        RaisePropertyChanged(nameof(HasTasks));
        RaisePropertyChanged(nameof(IsEmpty));
        RaisePropertyChanged(nameof(CurrentFilterLabel));
    }

    private void ToggleSidebar() => SidebarCollapsed = !SidebarCollapsed;

    private void SelectSidebarItem(object? parameter)
    {
        if (parameter is not SidebarItemVm item)
            return;

        foreach (var i in ViewItems) i.IsSelected = false;
        foreach (var i in FolderItems) i.IsSelected = false;
        foreach (var i in TagItems) i.IsSelected = false;
        item.IsSelected = true;

        switch (item.Kind)
        {
            case SidebarItemKind.SmartView:
                _selectedView = item.View ?? SmartView.All;
                _selectedFolderId = null;
                _selectedTagId = null;
                break;
            case SidebarItemKind.Folder:
                _selectedFolderId = item.Id;
                _selectedTagId = null;
                _selectedView = SmartView.All;
                break;
            case SidebarItemKind.Tag:
                _selectedTagId = item.Id;
                _selectedFolderId = null;
                _selectedView = SmartView.All;
                break;
        }

        RefreshTasksOnly();
    }

    private void QuickAdd()
    {
        var title = QuickAddText.Trim();
        if (string.IsNullOrEmpty(title))
            return;

        var folders = _services.Store.GetFolders();
        var folderId = _selectedFolderId
            ?? _services.Settings.Current.DefaultFolderId
            ?? folders.FirstOrDefault()?.Id;
        if (folderId is null)
        {
            StatusText = "No folder available.";
            return;
        }

        var task = new TaskItem
        {
            Title = title,
            FolderId = folderId,
            Kind = ItemKind.Todo,
            Status = FocusTaskStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        PersistAndSync(task);
        QuickAddText = "";
        RefreshTasksOnly();
        StatusText = $"Added todo “{title}”";
    }

    private void OpenEditor(ItemKind kind)
    {
        var folders = _services.Store.GetFolders();
        var tags = _services.Store.GetTags();
        var vm = new TaskEditorViewModel(null, folders, tags, kind);
        if (_selectedFolderId is not null)
            vm.SelectedFolder = folders.FirstOrDefault(f => f.Id == _selectedFolderId) ?? vm.SelectedFolder;

        var dlg = new TaskEditorWindow(vm) { Owner = Wpf.Application.Current?.MainWindow };
        if (dlg.ShowDialog() == true && vm.TryBuild(out var task))
        {
            EnsureNewTags(task);
            PersistAndSync(task);
            RebuildSidebar();
            RefreshTasksOnly();
            StatusText = task.Kind == ItemKind.Note
                ? $"Created note “{task.Title}”"
                : $"Created todo “{task.Title}”";
        }
    }

    private void EditTask(TaskListItemVm? item)
    {
        if (item is null) return;
        var existing = _services.Store.GetTask(item.Id);
        if (existing is null) return;

        var folders = _services.Store.GetFolders();
        var tags = _services.Store.GetTags();
        var vm = new TaskEditorViewModel(existing, folders, tags, existing.Kind);
        var dlg = new TaskEditorWindow(vm) { Owner = Wpf.Application.Current?.MainWindow };
        if (dlg.ShowDialog() == true && vm.TryBuild(out var task))
        {
            task.Status = existing.Status;
            task.CompletedAtUtc = existing.CompletedAtUtc;
            EnsureNewTags(task);
            PersistAndSync(task);
            RebuildSidebar();
            RefreshTasksOnly();
            StatusText = $"Updated “{task.Title}”";
        }
    }

    private void CompleteTask(TaskListItemVm? item)
    {
        if (item is null) return;
        var task = _services.Store.GetTask(item.Id);
        if (task is null) return;

        if (task.Status == FocusTaskStatus.Done)
        {
            task.Status = FocusTaskStatus.Open;
            task.CompletedAtUtc = null;
            task.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            ReminderAdvance.OnCompleted(task, DateTime.Now);
        }

        PersistAndSync(task);
        RefreshTasksOnly();
        StatusText = task.Status == FocusTaskStatus.Done
            ? $"Completed “{task.Title}”"
            : task.Recurrence.IsRecurring
                ? $"Advanced recurring “{task.Title}”"
                : $"Reopened “{task.Title}”";
    }

    private void DeleteTask(TaskListItemVm? item)
    {
        if (item is null) return;
        var result = Wpf.MessageBox.Show(
            $"Delete “{item.Title}”?",
            "FOCUS",
            Wpf.MessageBoxButton.YesNo,
            Wpf.MessageBoxImage.Question);
        if (result != Wpf.MessageBoxResult.Yes)
            return;

        try
        {
            _services.ReminderScheduler.RemoveReminder(item.Id);
        }
        catch { /* ignore */ }

        _services.Store.DeleteTask(item.Id);
        RefreshTasksOnly();
        StatusText = $"Deleted “{item.Title}”";
    }

    private void EnsureNewTags(TaskItem task)
    {
        var existing = _services.Store.GetTags().ToList();
        var resolved = new List<string>();
        foreach (var id in task.TagIds)
        {
            if (id.StartsWith("__new__:", StringComparison.Ordinal))
            {
                var name = id["__new__:".Length..];
                var tag = existing.FirstOrDefault(t =>
                    string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
                if (tag is null)
                {
                    tag = new Tag { Name = name };
                    _services.Store.UpsertTag(tag);
                    existing.Add(tag);
                }
                resolved.Add(tag.Id);
            }
            else
            {
                resolved.Add(id);
            }
        }
        task.TagIds = resolved;
    }

    private void PersistAndSync(TaskItem task)
    {
        task.UpdatedAtUtc = DateTime.UtcNow;
        _services.Store.UpsertTask(task);
        var exe = _services.GetExePath() ?? "";
        try
        {
            _services.CreateSchedulerSync().SyncTask(task, exe);
        }
        catch (Exception ex)
        {
            StatusText = $"Saved, but scheduler sync failed: {ex.Message}";
        }
    }

    private void OpenSettings()
    {
        var vm = new SettingsViewModel(_services);
        var dlg = new SettingsWindow(vm) { Owner = Wpf.Application.Current?.MainWindow };
        dlg.ShowDialog();
        _onTrayVisibilityChanged?.Invoke();
        StatusText = "Settings closed.";
    }

    private void OpenThemeEditor()
    {
        var vm = new ThemeEditorViewModel(_services);
        var dlg = new ThemeEditorWindow(vm) { Owner = Wpf.Application.Current?.MainWindow };
        dlg.ShowDialog();
        var theme = _services.Themes.GetActive(_services.Settings.Current.ActiveThemeId);
        ThemeApplicator.Apply(theme);
        StatusText = "Theme editor closed.";
    }

    private void Export()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export FOCUS data",
            Filter = "FOCUS export (*.json)|*.json|All files (*.*)|*.*",
            FileName = $"focus-export-{DateTime.Now:yyyyMMdd-HHmm}.json"
        };
        if (dlg.ShowDialog() != true)
            return;

        var bundle = _services.ExportImport.Export(
            _services.Store,
            _services.Settings.Current,
            _services.Themes.GetAll());

        var json = JsonSerializer.Serialize(bundle, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(dlg.FileName, json);
        StatusText = $"Exported to {dlg.FileName}";
    }

    private void Import()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import FOCUS data",
            Filter = "FOCUS export (*.json)|*.json|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true)
            return;

        var confirm = Wpf.MessageBox.Show(
            "Import will REPLACE all local tasks, folders, tags, settings, and themes. Continue?",
            "FOCUS Import",
            Wpf.MessageBoxButton.YesNo,
            Wpf.MessageBoxImage.Warning);
        if (confirm != Wpf.MessageBoxResult.Yes)
            return;

        try
        {
            var json = File.ReadAllText(dlg.FileName);
            var bundle = JsonSerializer.Deserialize<ExportBundle>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            if (bundle is null)
                throw new InvalidDataException("Could not parse export file.");

            // Remove existing scheduler entries before wipe.
            foreach (var t in _services.Store.QueryTasks(SmartView.All, null, null))
            {
                try { _services.ReminderScheduler.RemoveReminder(t.Id); } catch { /* ignore */ }
            }

            _services.ExportImport.ImportReplace(_services.Store, _services.Settings, _services.Themes, bundle);

            var theme = _services.Themes.GetActive(_services.Settings.Current.ActiveThemeId);
            ThemeApplicator.Apply(theme);

            var sync = _services.CreateSchedulerSync();
            var exe = _services.GetExePath() ?? "";
            foreach (var t in _services.Store.QueryTasks(SmartView.All, null, null))
            {
                if (t.Status == FocusTaskStatus.Open)
                    sync.SyncTask(t, exe);
            }

            SidebarCollapsed = _services.Settings.Current.SidebarCollapsed;
            _onTrayVisibilityChanged?.Invoke();
            Load();
            StatusText = "Import complete.";
        }
        catch (Exception ex)
        {
            Wpf.MessageBox.Show($"Import failed: {ex.Message}", "FOCUS", Wpf.MessageBoxButton.OK, Wpf.MessageBoxImage.Error);
            StatusText = "Import failed.";
        }
    }

    private async Task SendTodayListAsync()
    {
        var folders = _services.Store.GetFolders();
        var folderNames = folders.ToDictionary(f => f.Id, f => f.Name);
        var today = _services.Store.QueryTasks(SmartView.Today, null, null);
        var text = TodoListFormatter.FormatSummary("Today", today, folderNames);

        var messaging = _services.CreateMessaging();
        var results = await messaging.SendAsync(text);
        if (results.Count == 0 || results.All(r => r.Error == "no channels"))
        {
            StatusText = "No messaging channels configured. Open Settings.";
            Wpf.MessageBox.Show(
                "Enable Telegram and/or Discord in Settings and add credentials, then try again.",
                "FOCUS",
                Wpf.MessageBoxButton.OK,
                Wpf.MessageBoxImage.Information);
            return;
        }

        if (results.All(r => r.Success))
            StatusText = "Sent today's list.";
        else
            StatusText = "Send issues: " + string.Join("; ", results.Where(r => !r.Success).Select(r => r.Error ?? "error"));
    }

    private void AddFolder()
    {
        var folders = _services.Store.GetFolders();
        var vm = new FolderEditorViewModel(null)
        {
            SortOrder = folders.Count == 0 ? 0 : folders.Max(f => f.SortOrder) + 1
        };
        var dlg = new FolderEditorWindow(vm) { Owner = Wpf.Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true)
            return;

        var folder = vm.ToFolder();
        _services.Store.UpsertFolder(folder);
        RebuildSidebar();
        StatusText = $"Folder “{folder.Name}” added.";
    }

    private void EditSelectedFolder()
    {
        if (_selectedFolderId is null)
        {
            StatusText = "Select a folder first.";
            return;
        }

        var folder = _services.Store.GetFolders().FirstOrDefault(f => f.Id == _selectedFolderId);
        if (folder is null) return;

        var vm = new FolderEditorViewModel(folder);
        var dlg = new FolderEditorWindow(vm) { Owner = Wpf.Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true)
            return;

        var updated = vm.ToFolder();
        _services.Store.UpsertFolder(updated);
        RebuildSidebar();
        RefreshTasksOnly();
        StatusText = $"Folder “{updated.Name}” updated.";
    }

    private static string? PromptText(string message, string title, string defaultValue)
    {
        // Lightweight prompt without a dedicated dialog type.
        var app = Wpf.Application.Current;
        var window = new Wpf.Window
        {
            Title = title,
            Width = 380,
            Height = 160,
            WindowStartupLocation = Wpf.WindowStartupLocation.CenterOwner,
            Owner = app?.MainWindow,
            ResizeMode = Wpf.ResizeMode.NoResize,
            Background = (System.Windows.Media.Brush)app!.Resources["BgSurfaceBrush"],
            Foreground = (System.Windows.Media.Brush)app.Resources["TextPrimaryBrush"]
        };

        var panel = new WpfControls.StackPanel { Margin = new Wpf.Thickness(16) };
        panel.Children.Add(new WpfControls.TextBlock
        {
            Text = message,
            Margin = new Wpf.Thickness(0, 0, 0, 8),
            Foreground = (System.Windows.Media.Brush)app.Resources["TextSecondaryBrush"]
        });
        var box = new WpfControls.TextBox
        {
            Text = defaultValue,
            Style = (Wpf.Style)app.Resources["FocusTextBox"]
        };
        panel.Children.Add(box);

        var buttons = new WpfControls.StackPanel
        {
            Orientation = WpfControls.Orientation.Horizontal,
            HorizontalAlignment = Wpf.HorizontalAlignment.Right,
            Margin = new Wpf.Thickness(0, 12, 0, 0)
        };
        var ok = new WpfControls.Button
        {
            Content = "OK",
            Width = 80,
            Margin = new Wpf.Thickness(0, 0, 8, 0),
            Style = (Wpf.Style)app.Resources["AccentButton"],
            IsDefault = true
        };
        var cancel = new WpfControls.Button
        {
            Content = "Cancel",
            Width = 80,
            Style = (Wpf.Style)app.Resources["FocusButton"],
            IsCancel = true
        };
        string? result = null;
        ok.Click += (_, _) => { result = box.Text; window.DialogResult = true; };
        cancel.Click += (_, _) => { window.DialogResult = false; };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        window.Content = panel;
        box.Focus();
        box.SelectAll();
        return window.ShowDialog() == true ? result : null;
    }
}
