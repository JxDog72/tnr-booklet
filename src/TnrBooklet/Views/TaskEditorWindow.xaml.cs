using System.Windows;
using System.Windows.Controls.Primitives;
using Focus.ViewModels;

namespace Focus.Views;

public partial class TaskEditorWindow : Window
{
    private const double NotesMinHeight = 100;
    private const double EditorChromeHeight = 280;

    private readonly TaskEditorViewModel _vm;

    public TaskEditorWindow(TaskEditorViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
    }

    private void NotesResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var current = NotesRow.ActualHeight;
        if (current <= 0)
            current = NotesBox.ActualHeight;

        var max = Math.Max(NotesMinHeight, ActualHeight - EditorChromeHeight);
        var next = Math.Clamp(current + e.VerticalChange, NotesMinHeight, max);
        NotesRow.Height = new GridLength(next, GridUnitType.Pixel);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!_vm.TryBuild(out _))
        {
            System.Windows.MessageBox.Show(_vm.ValidationError ?? "Invalid task.", "TNR-Booklet", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
