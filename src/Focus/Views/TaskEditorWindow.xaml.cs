using System.Windows;
using Focus.ViewModels;

namespace Focus.Views;

public partial class TaskEditorWindow : Window
{
    private readonly TaskEditorViewModel _vm;

    public TaskEditorWindow(TaskEditorViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!_vm.TryBuild(out _))
        {
            System.Windows.MessageBox.Show(_vm.ValidationError ?? "Invalid task.", "FOCUS", MessageBoxButton.OK, MessageBoxImage.Warning);
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
