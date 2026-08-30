using System.Windows;
using Focus.ViewModels;

namespace Focus.Views;

public partial class FolderEditorWindow : Window
{
    private readonly FolderEditorViewModel _vm;

    public FolderEditorWindow(FolderEditorViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
    }

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        var pickerVm = new ColorPickerViewModel(_vm.ColorHex);
        var dlg = new ColorPickerWindow(pickerVm) { Owner = this };
        if (dlg.ShowDialog() == true)
            _vm.ColorHex = dlg.SelectedHex;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_vm.Name))
        {
            System.Windows.MessageBox.Show("Folder name is required.", "TNR-Booklet",
                MessageBoxButton.OK, MessageBoxImage.Warning);
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
