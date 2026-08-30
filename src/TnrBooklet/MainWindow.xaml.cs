using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Focus.Services;
using Focus.ViewModels;

namespace Focus;

public partial class MainWindow : Window
{
    private readonly AppServices _services;
    private readonly TrayService _tray;
    private readonly MainViewModel _vm;
    private bool _forceClose;
    private System.Windows.Point _dragStart;
    private TaskListItemVm? _dragItem;

    public MainWindow(AppServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        InitializeComponent();

        Width = Math.Max(720, services.Settings.Current.WindowWidth);
        Height = Math.Max(480, services.Settings.Current.WindowHeight);

        _tray = new TrayService(
            services,
            showMain: ShowFromTray,
            exitApp: ExitFromTray);
        _tray.Initialize();

        _vm = new MainViewModel(services, onTrayVisibilityChanged: ApplyTrayVisibility);
        _vm.PropertyChanged += VmOnPropertyChanged;
        DataContext = _vm;
        ApplySidebarWidth();
        ApplyTrayVisibility();
    }

    private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.SidebarCollapsed) or nameof(MainViewModel.SidebarColumnWidth))
            ApplySidebarWidth();
    }

    private void ApplySidebarWidth()
    {
        SidebarColumn.Width = new GridLength(_vm.SidebarColumnWidth);
    }

    private void ApplyTrayVisibility()
    {
        _tray.SetVisible(_services.Settings.Current.TrayEnabled);
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitFromTray()
    {
        _forceClose = true;
        Close();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        // Persist size
        _services.Settings.Current.WindowWidth = Width;
        _services.Settings.Current.WindowHeight = Height;
        _services.Settings.Save();

        if (!_forceClose && _services.Settings.Current.CloseToTray && _services.Settings.Current.TrayEnabled)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _tray.Dispose();
    }

    private void QuickAddBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _vm.QuickAddCommand.CanExecute(null))
        {
            _vm.QuickAddCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void TaskList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_vm.SelectedTask is not null && _vm.EditTaskCommand.CanExecute(_vm.SelectedTask))
            _vm.EditTaskCommand.Execute(_vm.SelectedTask);
    }

    private void CompleteCheck_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.CheckBox { DataContext: TaskListItemVm item })
            _vm.CompleteTaskCommand.Execute(item);
    }

    private void TaskList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragItem = null;
        if (FindTaggedAncestor(e.OriginalSource as DependencyObject, "DragGrip") is null)
            return;
        _dragItem = FindDataContext<TaskListItemVm>(e.OriginalSource as DependencyObject);
        _dragStart = e.GetPosition(null);
    }

    private void TaskList_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_dragItem is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        System.Windows.DragDrop.DoDragDrop(TaskList, _dragItem, System.Windows.DragDropEffects.Move);
        _dragItem = null;
    }

    private void TaskList_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(TaskListItemVm))
            ? System.Windows.DragDropEffects.Move
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void TaskList_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetData(typeof(TaskListItemVm)) is not TaskListItemVm source)
            return;
        var target = FindDataContext<TaskListItemVm>(e.OriginalSource as DependencyObject);
        if (target is null || ReferenceEquals(source, target))
            return;
        _vm.ReorderTask(source, target);
    }

    private void TaskList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Alt)
            return;
        if (e.Key is Key.Up)
        {
            _vm.MoveSelected(-1);
            e.Handled = true;
        }
        else if (e.Key is Key.Down)
        {
            _vm.MoveSelected(1);
            e.Handled = true;
        }
    }

    private static T? FindDataContext<T>(DependencyObject? start) where T : class
    {
        var d = start;
        while (d is not null)
        {
            if (d is FrameworkElement { DataContext: T match })
                return match;
            d = VisualTreeHelper.GetParent(d);
        }
        return null;
    }

    private static FrameworkElement? FindTaggedAncestor(DependencyObject? start, string tag)
    {
        var d = start;
        while (d is not null)
        {
            if (d is FrameworkElement fe && Equals(fe.Tag, tag))
                return fe;
            d = VisualTreeHelper.GetParent(d);
        }
        return null;
    }

    protected override void OnClosed(EventArgs e)
    {
        _vm.PropertyChanged -= VmOnPropertyChanged;
        base.OnClosed(e);
    }
}
